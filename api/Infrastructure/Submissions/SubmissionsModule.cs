using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Simulation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Infrastructure.Submissions;

public static class SubmissionsModule
{
    private const int MaxBodyBytes = 64 * 1024;
    private const string DebtCertificateRoute = "/api/sim/me/debt-certificate";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    public static IServiceCollection AddSubmissions(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISubmissionGateway, SimulatedSubmissionGateway>();
        services.TryAddSingleton<ISubmissionStore, EfSubmissionStore>();
        services.TryAddSingleton<SubmissionDocumentCache>();
        services.TryAddSingleton<SubmissionService>();
        return services;
    }

    public static IEndpointRouteBuilder MapSubmissionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/submissions").WithTags("zgłoszenia");
        group.AddEndpointFilter(MarkSimulation);
        group.MapGet("/templates", () => Results.Json(TemplateCatalog.All.Select(Describe).ToList(), Json));
        group.MapPost("", CreateAsync);
        group.MapGet("", ListAsync);
        group.MapGet("/{id}/pdf", GetPdfAsync);

        // Atrapa z Simulation jest mapowana na tej samej ścieżce; niższy Order wybiera ten endpoint.
        app.MapGet(DebtCertificateRoute, GetDebtCertificateAsync)
            .WithOrder(-1)
            .WithTags("symulacja")
            .AddEndpointFilter(MarkSimulation);
        return app;
    }

    private static async ValueTask<object?> MarkSimulation(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers[SimulationModule.SimulationHeader] = "true";
        return await next(context);
    }

    private static async Task<IResult> CreateAsync(HttpContext http, SessionStore sessions, SubmissionService service, CancellationToken ct)
    {
        var (sessionId, personaId) = Session(http, sessions);
        if (sessionId is null || personaId is null)
        {
            return Unauthorized();
        }

        if (http.Request.ContentLength > MaxBodyBytes)
        {
            return Problem(StatusCodes.Status413PayloadTooLarge, "Za duże żądanie", $"Treść żądania może mieć najwyżej {MaxBodyBytes / 1024} KB.");
        }

        SubmissionRequestBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<SubmissionRequestBody>(LimitedBody(http), Json, ct);
        }
        catch (Exception ex) when (ex is JsonException or BadHttpRequestException or InvalidOperationException)
        {
            return Problem(StatusCodes.Status400BadRequest, "Nieprawidłowe dane wejściowe",
                "Treść żądania musi być obiektem JSON: type, opcjonalnie template, fields (same teksty).");
        }

        if (body is null)
        {
            return Problem(StatusCodes.Status400BadRequest, "Nieprawidłowe dane wejściowe", "Brak treści żądania.");
        }

        var outcome = await service.CreateAsync(sessionId, personaId, body, ct);
        if (outcome.LimitReached)
        {
            return Problem(StatusCodes.Status429TooManyRequests, "Limit zgłoszeń",
                $"Sesja może mieć najwyżej {SubmissionService.MaxPerSession} zgłoszeń. Zaloguj się ponownie.");
        }

        return outcome.Succeeded
            ? Results.Json(outcome.Receipt, Json, statusCode: StatusCodes.Status201Created)
            : Results.ValidationProblem(outcome.Errors.ToDictionary(e => e.Key, e => e.Value), title: "Nieprawidłowe dane wejściowe");
    }

    private static async Task<IResult> ListAsync(HttpContext http, SessionStore sessions, SubmissionService service, CancellationToken ct)
    {
        var (sessionId, _) = Session(http, sessions);
        return sessionId is null ? Unauthorized() : Results.Json(await service.ListAsync(sessionId, ct), Json);
    }

    private static async Task<IResult> GetPdfAsync(string id, HttpContext http, SessionStore sessions, SubmissionService service, CancellationToken ct)
    {
        var (sessionId, _) = Session(http, sessions);
        if (sessionId is null)
        {
            return Unauthorized();
        }

        if (!Guid.TryParse(id, out var guid))
        {
            return Problem(StatusCodes.Status400BadRequest, "Nieprawidłowe dane wejściowe", "Identyfikator zgłoszenia ma postać GUID.");
        }

        var found = await service.FindPdfAsync(sessionId, guid, ct);
        if (!found.Found)
        {
            return Problem(StatusCodes.Status404NotFound, "Nie ma takiego zgłoszenia", "Zgłoszenie nie istnieje albo należy do innej sesji.");
        }

        return found.Pdf is null
            ? Problem(StatusCodes.Status410Gone, "Dokument wygasł",
                "PDF jest przechowywany krótko i bez danych w bazie. Wygeneruj wniosek ponownie.")
            : Results.File(found.Pdf, "application/pdf", $"{found.Number}.pdf");
    }

    private static async Task<IResult> GetDebtCertificateAsync(
        HttpContext http,
        string? date,
        SessionStore sessions,
        IIdentityProvider identity,
        IContributionAccount contributions,
        TimeProvider time,
        CancellationToken ct)
    {
        var (_, personaId) = Session(http, sessions);
        if (personaId is null)
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var onDate = today;
        if (date is not null && (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out onDate)
            || onDate.Year is < 2000 or > 2100))
        {
            return Problem(StatusCodes.Status400BadRequest, "Nieprawidłowe dane wejściowe", "date: data w formacie RRRR-MM-DD z lat 2000–2100.");
        }

        var who = await identity.LoginAsync(personaId, ct);
        if (who is null)
        {
            return Unauthorized();
        }

        var balance = await contributions.GetBalanceAsync(personaId, onDate, ct);
        var number = ReceiptNumber.Create("ZAS", time.GetUtcNow(), $"{personaId}|{onDate:yyyy-MM-dd}|{balance?.Kwota.ToString(CultureInfo.InvariantCulture)}");
        var model = DebtCertificate.Build($"{who.Imie} {who.Nazwisko}", balance, onDate, today, number);
        return Results.File(PdfRenderer.Render(model), "application/pdf", $"{number}.pdf");
    }

    private static object Describe(TemplateSpec spec) => new
    {
        id = spec.Id,
        type = SubmissionNames.ToWire(spec.Type),
        template = spec.Type == SubmissionType.WniosekUrlop ? spec.Id : null,
        recipient = SubmissionNames.ToWire(spec.Recipient),
        title = spec.Title,
        fields = spec.Fields.Select(f => new
        {
            key = f.Key,
            label = f.Label,
            kind = f.Kind.ToString().ToLowerInvariant(),
            required = f.Required,
            maxLength = f.MaxLength,
            basis = f.Basis,
            when = f.When is null ? null : new { key = f.When.Key, value = f.When.Value },
            options = f.Options?.Select(o => new { value = o.Value, label = o.Label }),
        }).ToList(),
    };

    private static (string? SessionId, string? PersonaId) Session(HttpContext http, SessionStore sessions)
    {
        var sessionId = http.Request.Headers[SimulationModule.SessionHeader].FirstOrDefault();
        var personaId = sessions.FindPersonaId(sessionId);
        return personaId is null ? (null, null) : (sessionId, personaId);
    }

    private static Stream LimitedBody(HttpContext http)
    {
        var feature = http.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = MaxBodyBytes;
        }

        return http.Request.Body;
    }

    private static IResult Unauthorized() => Problem(
        StatusCodes.Status401Unauthorized,
        "Brak sesji symulacji",
        $"Zaloguj się (POST /api/sim/login) i przekaż nagłówek {SimulationModule.SessionHeader}.");

    private static IResult Problem(int status, string title, string detail) =>
        Results.Problem(statusCode: status, title: title, detail: detail);
}
