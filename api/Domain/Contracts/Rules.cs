namespace BabyRadnik.Domain.Contracts;

public enum RuleVerdict
{
    Met,
    NotMet,
    MissingData,
    Undecided,
}

public sealed record RuleResult(
    RuleVerdict Verdict,
    string Text,
    string? Condition = null,
    IReadOnlyList<MissingFact>? Missing = null)
{
    public static RuleResult Met(string text, string? condition = null) => new(RuleVerdict.Met, text, condition);
    public static RuleResult NotMet(string text, string? condition = null) => new(RuleVerdict.NotMet, text, condition);
    public static RuleResult Undecided(string text) => new(RuleVerdict.Undecided, text);
    public static RuleResult MissingData(params MissingFact[] missing) =>
        new(RuleVerdict.MissingData, "Brakuje danych.", Missing: missing);
}

/// <summary>Kontekst oceny reguł. Wspólny dla wszystkich modułów silnika.</summary>
public sealed record EvaluationContext(FactSet Facts, PersonRef Person, DateOnly AsOf, IParameterProvider Parameters)
{
    public PersonFacts? PersonFacts => Facts.For(Person);
}

/// <summary>Każda reguła to mała klasa z identyfikatorem i podstawą prawną (sekcja 12).</summary>
public interface IRule
{
    string Id { get; }
    string PodstawaPrawna { get; }
    RuleResult Evaluate(EvaluationContext context);
}

public static class RuleExtensions
{
    public static TraceStep ToTrace(this IRule rule, RuleResult result) => new(
        rule.Id,
        rule.PodstawaPrawna,
        result.Verdict switch
        {
            RuleVerdict.Met => true,
            RuleVerdict.NotMet => false,
            _ => null,
        },
        result.Text,
        result.Condition);
}
