using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Eligibility;

/// <summary>
/// Ewaluator przypadków referencyjnych modułu „uprawnienia”. Dostawca parametrów = seed + wiersze z przypadku
/// + liczby z przepisów, których nie ma w seed (EligibilityParameterKeys.Supplement) — do dodania do seed przez be/params.
/// </summary>
public sealed class EligibilityReferenceEvaluator : IReferenceEvaluator
{
    public string Modul => "uprawnienia";

    public void Verify(ReferenceCase referenceCase)
    {
        var request = referenceCase.Request();
        var asOf = request.AsOf ?? Kit.AsOf;
        var provider = SeedParameterProvider.Load([.. Kit.SupplementRows(), .. referenceCase.Parametry]);
        var assessment = new EligibilityService(provider).Assess(request.Facts, asOf);

        var result = new AssessmentResult
        {
            RulesetVersion = "uprawnienia-test",
            Benefits = assessment.Benefits,
            MissingFacts = assessment.MissingFacts,
            Risks = assessment.Risks,
        };
        ReferenceAssert.Matches(referenceCase, result);
    }
}
