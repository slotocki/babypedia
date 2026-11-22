using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Infrastructure.Assistant;

public static class AssistantModule
{
    private const int MaxQuestionLength = 500;
    private const int SearchResults = 6;
    private const int ModelSources = 15;
    private const int SupportingSources = 4;

    public static IServiceCollection AddAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        var openCodeFlag = configuration["OPENCODE_ENABLED"];
        if (string.IsNullOrWhiteSpace(openCodeFlag)) openCodeFlag = configuration["PENCODE_ENABLED"];
        var useOpenCodeChat = bool.TryParse(openCodeFlag, out var openCodeEnabled) && openCodeEnabled;
        var provider = useOpenCodeChat ? "opencode-chat" : configuration["ASSISTANT_PROVIDER"]?.Trim().ToLowerInvariant() ?? "openai";
        var keyVariable = provider is "opencode" or "opencode-go" or "opencode-chat" ? "OPENCODE_API_KEY" : "ANTHROPIC_API_KEY";
        var baseUrl = useOpenCodeChat ? configuration["OPENCODE_BASE_URL"] : configuration["ASSISTANT_BASE_URL"];
        var model = useOpenCodeChat ? configuration["OPENCODE_MODEL"] : configuration["ASSISTANT_MODEL"];
        var options = new AssistantOptions(
            useOpenCodeChat || bool.TryParse(configuration["ASSISTANT_ENABLED"], out var enabled) && enabled,
            provider,
            baseUrl,
            model,
            configuration[keyVariable],
            int.TryParse(configuration["ASSISTANT_DAILY_LIMIT"], out var dailyLimit) ? Math.Max(0, dailyLimit) : 250);

        services.AddSingleton(options);
        services.AddSingleton<LegalSearch>();
        services.AddSingleton<AssistantBudget>();
        services.AddHttpClient<TextAssistant>(client =>
        {
            if (Uri.TryCreate(options.BaseUrl?.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var uri) &&
                uri.Scheme is "http" or "https" && (!options.UseOpenCodeChat || IsOpenCodeChatUrl(uri)))
                client.BaseAddress = uri;
            client.Timeout = TimeSpan.FromSeconds(30);
            client.MaxResponseContentBufferSize = 64 * 1024;
        });
        return services;
    }

    private static bool IsOpenCodeChatUrl(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && uri.Host == "opencode.ai" && uri.IsDefaultPort &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
        uri.AbsolutePath is "/inference/go/openai/v1/" or "/inference/openai/v1/";

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        _ = app.ServiceProvider.GetRequiredService<LegalSearch>();
        app.MapGet("/api/law/search", (string? q, HttpContext context, LegalSearch search) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Validate(q) ?? Results.Ok(OutOfScope(q!) ?? AsSearchAnswer(search.Find(q!)));
        }).WithTags("pomocnik");

        app.MapPost("/api/assistant/ask", async (
            AssistantQuestion body, HttpContext context, LegalSearch search, TextAssistant model,
            AssistantBudget budget, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var invalid = Validate(body.Question);
            if (invalid is not null) return invalid;

            var question = body.Question!.Trim();
            if (OutOfScope(question) is { } outOfScope) return Results.Ok(outOfScope);
            if (AssistantPolicy.IsPersonalCase(question))
                return Results.Ok(new AssistantAnswer("Swoją sytuację sprawdzisz w kalkulatorze świadczeń.", [], "", "calculator"));

            var previous = body.PreviousQuestion?.Trim();
            if (string.IsNullOrEmpty(previous) || previous.Length > MaxQuestionLength ||
                AssistantPolicy.HasSensitiveData(previous) || AssistantPolicy.IsPersonalCase(previous) ||
                !AssistantPolicy.IsFollowUp(question))
                previous = null;

            // Wyszukiwarka pokazuje 6 najlepszych fragmentów, model dostaje szerszy wybór (w tym poradnik ZUS),
            // żeby właściwy przepis nie odpadł przez gorsze dopasowanie słów.
            var hits = search.Find(previous is null ? question : $"{previous} {question}", ModelSources, SupportingSources);
            var fallback = AsSearchAnswer(hits.Take(SearchResults).ToArray());
            var sources = hits.Select(hit => hit.Excerpt).ToArray();
            if (!model.Enabled || !sources.Any(excerpt => excerpt.IsLegalAct) ||
                !budget.TryTake(context.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
                return Results.Ok(fallback);

            try
            {
                var reply = await model.AskAsync(question, previous, sources, ct);
                return Results.Ok(AssistantPolicy.Verify(reply, sources) ?? fallback);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException ||
                                              exception is OperationCanceledException && !ct.IsCancellationRequested)
            {
                return Results.Ok(fallback);
            }
        }).WithTags("pomocnik");
        return app;
    }

    private static IResult? Validate(string? question)
    {
        var message = string.IsNullOrWhiteSpace(question) || question.Length > MaxQuestionLength
            ? $"Wpisz pytanie (najwyżej {MaxQuestionLength} znaków)."
            : AssistantPolicy.HasSensitiveData(question)
                ? "Usuń dane osobowe i klucze dostępu, a potem wyślij pytanie ponownie."
                : null;
        return message is null ? null : Results.ValidationProblem(
            new Dictionary<string, string[]> { ["question"] = [message] });
    }

    private const string NoResults = "Nie znaleźliśmy odpowiedniego przepisu w materiałach aplikacji.";

    private static AssistantAnswer AsSearchAnswer(IReadOnlyList<LegalHit> hits) => hits.Count == 0
        ? new AssistantAnswer(NoResults, [], "", "no_results")
        : new AssistantAnswer("", hits.Select(hit => hit.Excerpt.Citation(hit.Preview)).ToArray(), "", "search");

    private static AssistantAnswer? OutOfScope(string question) =>
        AssistantPolicy.OutOfScope(question) is { } message ? new AssistantAnswer(message, [], "", "no_results") : null;

    private sealed class AssistantBudget(AssistantOptions options) : IDisposable
    {
        private readonly PartitionedRateLimiter<string> limiter = PartitionedRateLimiter.Create<string, string>(address =>
            RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

        private readonly object gate = new();
        private DateOnly day;
        private int used;

        public bool TryTake(string address)
        {
            lock (gate)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (day != today)
                {
                    day = today;
                    used = 0;
                }

                if (used >= options.DailyLimit) return false;
                using var lease = limiter.AttemptAcquire(address);
                if (!lease.IsAcquired) return false;
                used++;
                return true;
            }
        }

        public void Dispose() => limiter.Dispose();
    }
}
