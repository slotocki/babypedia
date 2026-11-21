using System.Text.RegularExpressions;
using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using BabyRadnik.Infrastructure.Parameters;

namespace BabyRadnik.Api.Endpoints;

/// <summary>Właściciel: gałąź be/catalog. W logach nie ma faktów ani danych osobowych.</summary>
public static partial class AssessmentEndpoints
{
    private const int MaxCompletedDeadlines = 200;
    private const int MaxListItems = 500;

    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/assessment", async (
            AssessmentRequest request,
            ICatalogAssessmentEngine engine,
            IServiceProvider services,
            ILoggerFactory loggers,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Nieprawidłowe dane wejściowe");
            }

            var settings = await ResolveSettingsAsync(services, loggers.CreateLogger("Assessment"), ct);
            return Results.Ok(settings is null ? engine.Assess(request) : engine.Assess(request, settings));
        }).WithTags("ocena");
        return app;
    }

    public static Dictionary<string, string[]> Validate(AssessmentRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request?.Facts is null)
        {
            errors["facts"] = ["Pole facts jest wymagane."];
            return errors;
        }

        if (request.AsOf is { } asOf && (asOf.Year < 2000 || asOf.Year > 2100))
        {
            errors["asOf"] = ["Data asOf musi być z lat 2000–2100."];
        }

        if (request.CompletedDeadlines is { } completed)
        {
            if (completed.Count > MaxCompletedDeadlines)
            {
                errors["completedDeadlines"] = [$"Najwyżej {MaxCompletedDeadlines} wpisów."];
            }
            else if (completed.Any(id => id is null || !CompletedId().IsMatch(id)))
            {
                errors["completedDeadlines"] = ["Wpis to identyfikator terminu (snake_case), opcjonalnie z prefiksem osoby: wnioskodawca/ albo drugiRodzic/."];
            }
        }

        foreach (var (name, person) in new[] { ("facts.wnioskodawca", request.Facts.Wnioskodawca), ("facts.drugiRodzic", request.Facts.DrugiRodzic) })
        {
            if (person is not null && ListTooLong(person))
            {
                errors[name] = [$"Listy faktów mogą mieć najwyżej {MaxListItems} pozycji."];
            }
        }

        if (request.Facts.Sprawa.DzieckoPobytyWSzpitalu?.Value.Count > MaxListItems)
        {
            errors["facts.sprawa"] = [$"Listy faktów mogą mieć najwyżej {MaxListItems} pozycji."];
        }

        return errors;
    }

    private static bool ListTooLong(PersonFacts p) =>
        p.UbezpieczenieTytuly?.Value.Count > MaxListItems
        || p.UbezpieczeniePodstawySkladek?.Value.Count > MaxListItems
        || p.UbezpieczeniePrzerwy?.Value.Count > MaxListItems
        || p.UbezpieczeniePobraneSwiadczenia?.Value.Count > MaxListItems
        || p.EzlaZwolnienia?.Value.Count > MaxListItems
        || p.PlanRodzicielskiCzesci?.Value.Count > MaxListItems
        || p.PlanPracaNaCzescEtatu?.Value.Count > MaxListItems
        || p.PlanOjcowskiCzesci?.Value.Count > MaxListItems;

    /// <summary>Przełączniki „włączone” z panelu administratora; brak ustawień albo błąd odczytu → wszystkie pozycje włączone.</summary>
    private static async Task<ICatalogSettings?> ResolveSettingsAsync(IServiceProvider services, ILogger logger, CancellationToken ct)
    {
        if (services.GetService<AdminSettingsService>() is not { } admin)
        {
            return null;
        }

        try
        {
            var catalog = await admin.GetCatalogAsync(ct);
            var disabled = BenefitCatalog.Entries
                .Where(e => catalog.Any(c => c.Id == BenefitNames.Of(e.Id) && c.Wlaczone == false))
                .Select(e => e.Id);
            return new FixedCatalogSettings(disabled);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Nie udało się odczytać ustawień katalogu; ocena obejmuje wszystkie pozycje.");
            return null;
        }
    }

    [GeneratedRegex("^((wnioskodawca|drugiRodzic)/)?[a-z0-9_]{1,80}$")]
    private static partial Regex CompletedId();
}
