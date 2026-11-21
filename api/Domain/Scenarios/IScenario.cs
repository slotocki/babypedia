using System.Text.Json;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Scenarios;

public sealed record ScenarioContext(
    FactSet Facts,
    PersonRef Person,
    JsonElement? Params,
    DateOnly Today,
    IParameterProvider Parameters);

public sealed record ScenarioOutcome(
    FactSet? Facts,
    IReadOnlyList<ValidationIssue> Errors,
    IReadOnlyList<string> Changes)
{
    public bool Succeeded => Errors.Count == 0 && Facts is not null;

    public static ScenarioOutcome Ok(FactSet facts, params string[] changes) => new(facts, [], changes);

    public static ScenarioOutcome Fail(IReadOnlyList<ValidationIssue> errors) => new(null, errors, []);

    public static ScenarioOutcome Fail(params ValidationIssue[] errors) => new(null, errors, []);
}

public interface IScenario
{
    ScenarioId Id { get; }

    string Description { get; }

    ScenarioOutcome Apply(ScenarioContext context);
}
