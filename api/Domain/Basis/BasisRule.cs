using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public abstract record BasisStep(IRule Rule, RuleResult Result)
{
    public TraceStep Trace => Rule.ToTrace(Result);

    public bool Ok => Result.Verdict == RuleVerdict.Met;
}

public sealed record BasisStep<T>(IRule Rule, RuleResult Result, T? Value) : BasisStep(Rule, Result);

public static class BasisSteps
{
    public static BasisStep<T> Met<T>(IRule rule, T value, string text, string? condition = null) =>
        new(rule, RuleResult.Met(text, condition), value);

    public static BasisStep<T> NotMet<T>(IRule rule, string text, string? condition = null) =>
        new(rule, RuleResult.NotMet(text, condition), default);

    public static BasisStep<T> Undecided<T>(IRule rule, string text) =>
        new(rule, RuleResult.Undecided(text), default);

    public static BasisStep<T> Missing<T>(IRule rule, string text, params MissingFact[] facts) =>
        new(rule, new RuleResult(RuleVerdict.MissingData, text, Missing: facts), default);

    public static BasisStep<T> Blocked<T>(IRule rule, BasisStep dependency) =>
        new(rule, dependency.Result, default);
}

public abstract class BasisRule<T> : IRule
{
    public abstract string Id { get; }

    public abstract string PodstawaPrawna { get; }

    public BasisStep<T> Calculate(BasisInputs inputs)
    {
        try
        {
            return Compute(inputs);
        }
        catch (ParameterNotFoundException e)
        {
            return BasisSteps.Undecided<T>(this,
                $"Brakuje parametru prawnego „{e.Key}” na dzień {Fmt.Date(e.Date)}. Nie rozstrzygam — skontaktuj się z ZUS.");
        }
    }

    protected abstract BasisStep<T> Compute(BasisInputs inputs);

    public RuleResult Evaluate(EvaluationContext context) =>
        Calculate(new BasisInputs(context.Facts, context.Person, context.AsOf, context.Parameters)).Result;
}
