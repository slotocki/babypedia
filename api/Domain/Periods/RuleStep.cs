using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public record RuleStep(IRule Rule, RuleResult Result, IReadOnlyList<ValidationIssue> Issues, bool NotApplicable = false, bool Soft = false)
{
    public TraceStep Trace => Rule.ToTrace(Result);

    public bool CanProceed => !NotApplicable && (Result.Verdict == RuleVerdict.Met || Soft);
}

public sealed record RuleStep<T>(IRule Rule, RuleResult Result, T? Value, IReadOnlyList<ValidationIssue> Issues, bool NotApplicable = false, bool Soft = false)
    : RuleStep(Rule, Result, Issues, NotApplicable, Soft);

public static class Step
{
    private static readonly IReadOnlyList<ValidationIssue> NoIssues = [];

    public static RuleStep<T> Met<T>(IRule rule, T value, string text, string? condition = null) =>
        new(rule, RuleResult.Met(text, condition), value, NoIssues);

    public static RuleStep<T> NotMet<T>(IRule rule, string text, string? condition = null, params ValidationIssue[] issues) =>
        new(rule, RuleResult.NotMet(text, condition), default, issues);

    public static RuleStep<T> SoftIssue<T>(IRule rule, T fallback, string text, ValidationIssue issue, string? condition = null) =>
        new(rule, RuleResult.NotMet(text, condition), fallback, [issue], Soft: true);

    public static RuleStep<T> Undecided<T>(IRule rule, string text) =>
        new(rule, RuleResult.Undecided(text), default, NoIssues);

    public static RuleStep<T> Missing<T>(IRule rule, params MissingFact[] facts) =>
        new(rule, RuleResult.MissingData(facts), default, NoIssues);

    public static RuleStep<T>? MissingIf<T>(IRule rule, params MissingFact?[] facts)
    {
        var missing = facts.OfType<MissingFact>().ToArray();
        return missing.Length == 0 ? null : Missing<T>(rule, missing);
    }

    public static RuleStep<T> Inapplicable<T>(IRule rule, string text) =>
        new(rule, RuleResult.NotMet(text), default, NoIssues, NotApplicable: true);

    public static RuleStep<T> Blocked<T>(IRule rule, RuleStep dependency) =>
        new(rule, dependency.Result, default, NoIssues, dependency.NotApplicable);
}

public abstract class PeriodRule<T> : IRule
{
    public abstract string Id { get; }

    public abstract string PodstawaPrawna { get; }

    public RuleStep<T> Calculate(PeriodsInputs inputs)
    {
        try
        {
            return Compute(inputs);
        }
        catch (ParameterNotFoundException e)
        {
            return Step.Undecided<T>(this,
                $"Brakuje parametru prawnego „{e.Key}” na dzień {PeriodMath.Format(e.Date)}. Nie rozstrzygam — skontaktuj się z ZUS.");
        }
    }

    protected abstract RuleStep<T> Compute(PeriodsInputs inputs);

    public RuleResult Evaluate(EvaluationContext context) => Calculate(new PeriodsInputs(context)).Result;
}
