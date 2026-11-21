using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

public sealed record RuleOutcome(
    RuleResult Result,
    string? Reason = null,
    string? WhatWouldChange = null,
    string? LegalBasis = null,
    IReadOnlyList<string>? Notes = null)
{
    public RuleVerdict Verdict => Result.Verdict;
}

public abstract class EligibilityRule : IRule
{
    public abstract string Id { get; }
    public abstract string PodstawaPrawna { get; }
    public abstract RuleOutcome Assess(EvaluationContext context);

    public RuleResult Evaluate(EvaluationContext context) => Assess(context).Result;

    protected static RuleOutcome Met(string text, string? condition = null, params string[] notes) =>
        new(RuleResult.Met(text, condition), Notes: notes.Length == 0 ? null : notes);

    protected static RuleOutcome NotMet(string text, string? condition, string reason, string? whatWouldChange = null, string? legalBasis = null) =>
        new(RuleResult.NotMet(text, condition), reason, whatWouldChange, legalBasis);

    protected static RuleOutcome Undecided(string text, string? condition = null, string? legalBasis = null) =>
        new(new RuleResult(RuleVerdict.Undecided, text, condition), text, LegalBasis: legalBasis);

    protected static RuleOutcome Missing(string text, string? condition, params MissingFact[] missing) =>
        new(new RuleResult(RuleVerdict.MissingData, text, condition, missing));
}

public sealed record BenefitEligibility(BenefitResult Result, IReadOnlyList<MissingFact> MissingFacts);

public sealed record EligibilityAssessment(
    IReadOnlyList<BenefitResult> Benefits,
    IReadOnlyList<MissingFact> MissingFacts,
    IReadOnlyList<Risk> Risks);

public sealed record PaymentRoute(
    Payer? Payer,
    ApplicationPlace? WhereToApply,
    IReadOnlyList<string> Documents,
    RuleOutcome Outcome);

public enum BenefitKindForDocuments
{
    Maternity,
    Sickness,
    Rehabilitation,
}

public interface IEligibilityService
{
    /// <summary>Ocena uprawnień dla wnioskodawcy i (jeśli podany w faktach) drugiego rodzica, parametry z wstrzykniętego dostawcy.</summary>
    EligibilityAssessment Assess(FactSet facts, DateOnly asOf);

    EligibilityAssessment Assess(FactSet facts, DateOnly asOf, IParameterProvider parameters);

    /// <summary>Ocena jednego świadczenia dla jednej osoby. Wynik bez okresu i kwoty — uzupełnia je katalog z modułów okresów i kwot.</summary>
    BenefitEligibility Evaluate(BenefitId benefit, FactSet facts, PersonRef person, DateOnly asOf);

    BenefitEligibility Evaluate(BenefitId benefit, FactSet facts, PersonRef person, DateOnly asOf, IParameterProvider parameters);
}
