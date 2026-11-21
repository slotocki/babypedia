using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Periods;
using BabyRadnik.Tests.Domain.Periods;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Amounts;

/// <summary>
/// Przypadki „kwoty” i „parametry”: okresy z modułu Periods → stawki i kwoty z modułu Amounts. Podstawa wymiaru:
/// fakt podstawa.ustalonaPrzezPlatnika albo przeciętna z 12 miesięcy podstawySkladek z odliczeniem, minimum i limitem
/// (uproszczenie testowe — pełne ustalanie podstawy to moduł Basis).
/// </summary>
public abstract class AmountsEvaluatorBase : IReferenceEvaluator
{
    public abstract string Modul { get; }

    public bool CanEvaluate(ReferenceCase referenceCase) => referenceCase.Fakty is not null;

    public void Verify(ReferenceCase referenceCase)
    {
        var facts = referenceCase.Fakty!;
        var asOf = referenceCase.AsOf ?? PeriodsTestSupport.AsOf;
        var parameters = AmountsKit.Params(referenceCase.Parametry);
        var periods = new PeriodsService(parameters).Assess(facts, asOf);
        var basis = AmountsKit.BasisFromFacts(parameters, facts);
        var amounts = AmountsKit.Calculate(parameters, facts, asOf, periods.Benefits, basis);
        var result = new AssessmentResult
        {
            RulesetVersion = "kwoty-test",
            Benefits = amounts.Benefits,
            MissingFacts = periods.MissingFacts,
            ValidationErrors = periods.ValidationErrors,
        };
        ReferenceAssert.Matches(referenceCase, result);
    }
}

public sealed class AmountsReferenceEvaluator : AmountsEvaluatorBase
{
    public override string Modul => "kwoty";
}

public sealed class ParametersReferenceEvaluator : AmountsEvaluatorBase
{
    public override string Modul => "parametry";
}
