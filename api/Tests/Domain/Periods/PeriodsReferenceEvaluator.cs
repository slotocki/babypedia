using BabyRadnik.Domain.Periods;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class PeriodsReferenceEvaluator : IReferenceEvaluator
{
    public string Modul => "okresy";

    public bool CanEvaluate(ReferenceCase referenceCase) => referenceCase.Fakty is not null;

    public void Verify(ReferenceCase referenceCase)
    {
        var parameters = SeedParameterProvider.Load([.. PeriodsTestSupport.Supplemental, .. referenceCase.Parametry]);
        var service = new PeriodsService(parameters);
        var assessment = service.Assess(referenceCase.Fakty!, referenceCase.AsOf ?? PeriodsTestSupport.AsOf);
        ReferenceAssert.Matches(referenceCase, assessment.ToAssessmentResult("okresy-test"));
    }
}
