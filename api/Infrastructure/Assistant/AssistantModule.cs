using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Infrastructure.Assistant;

public static class AssistantModule
{
    private const int MaxQuestionLength = 1000;

    public static IServiceCollection AddAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new AssistantOptions
        {
            Enabled = bool.TryParse(configuration["ASSISTANT_ENABLED"], out var enabled) && enabled,
            BaseUrl = configuration["ASSISTANT_BASE_URL"],
            Model = configuration["ASSISTANT_MODEL"],
        };

        if (options.Enabled && Uri.TryCreate(options.BaseUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            services.AddSingleton(options);
            services.AddHttpClient<ITextAssistant, OpenAiCompatibleTextAssistant>(c =>
            {
                c.BaseAddress = baseUri;
                c.Timeout = TimeSpan.FromSeconds(60);
            });
        }
        else
        {
            services.AddSingleton<ITextAssistant, DisabledTextAssistant>();
        }

        return services;
    }

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/assistant/ask", async (AssistantQuestion body, ITextAssistant assistant, CancellationToken ct) =>
        {
            if (!assistant.Enabled)
            {
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Pomocnik tekstowy jest wyłączony");
            }

            if (string.IsNullOrWhiteSpace(body.Question) || body.Question.Length > MaxQuestionLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["question"] = [$"Wpisz pytanie (najwyżej {MaxQuestionLength} znaków)."],
                });
            }

            return Results.Ok(await assistant.AskAsync(body.Question, ct));
        }).WithTags("pomocnik");
        return app;
    }
}
