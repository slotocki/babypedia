using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Scenarios;

public sealed class ScenarioComparer(IAssessmentEngine engine, IParameterProvider parameters, IEnumerable<IScenario> scenarios) : IScenarioComparer
{
    private readonly IReadOnlyDictionary<ScenarioId, IScenario> catalog = scenarios.ToDictionary(s => s.Id);

    public CompareResult Compare(CompareRequest request)
    {
        var baseResult = engine.Assess(new AssessmentRequest(request.Facts, request.AsOf));
        var today = request.AsOf ?? DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);

        var errors = new List<ValidationIssue>();
        var facts = request.Facts;
        var seen = new HashSet<(ScenarioId, PersonRef)>();
        var changes = new List<string>();

        if (request.Scenarios.Count == 0)
        {
            errors.Add(new ValidationIssue("scenariusz_brak", "Wskaż co najmniej jeden scenariusz do porównania."));
        }

        foreach (var change in request.Scenarios)
        {
            if (!seen.Add((change.Id, change.Person)))
            {
                errors.Add(new ValidationIssue("scenariusz_powtorzony", $"Scenariusz „{change.Id.ToWire()}” podano więcej niż raz dla tej samej osoby.", null, change.Person));
                continue;
            }

            if (!catalog.TryGetValue(change.Id, out var scenario))
            {
                errors.Add(new ValidationIssue("scenariusz_nieznany", $"Nieznany scenariusz „{change.Id.ToWire()}”.", null, change.Person));
                continue;
            }

            var outcome = scenario.Apply(new ScenarioContext(facts, change.Person, change.Params, today, parameters));
            if (outcome.Succeeded)
            {
                facts = outcome.Facts!;
                changes.AddRange(outcome.Changes);
            }
            else
            {
                errors.AddRange(outcome.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return new CompareResult(baseResult, baseResult, [], errors, null, []);
        }

        var variant = engine.Assess(new AssessmentRequest(facts, request.AsOf));
        var introduced = variant.ValidationErrors
            .Where(v => !baseResult.ValidationErrors.Any(b => b.Code == v.Code && b.Fact == v.Fact && b.Person == v.Person))
            .ToList();
        return new CompareResult(baseResult, variant, ScenarioDiff.Build(baseResult, variant), introduced, facts, changes);
    }
}

internal static class ScenarioIdExtensions
{
    public static string ToWire(this ScenarioId id) => System.Text.Json.JsonSerializer.Serialize(id).Trim('"');
}
