using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Catalog;

/// <summary>Ewaluator przypadków modułu „katalog”: pełny silnik (<see cref="AssessmentEngine"/> z modułami podstawy i kwot) na parametrach seed + wiersze z przypadku.</summary>
public class CatalogReferenceEvaluator : IReferenceEvaluator
{
    public virtual string Modul => "katalog";

    public bool CanEvaluate(ReferenceCase referenceCase) => referenceCase.Fakty is not null;

    public void Verify(ReferenceCase referenceCase)
    {
        var provider = CatalogKit.Params([.. referenceCase.Parametry]);
        var request = referenceCase.Request() with { AsOf = referenceCase.AsOf ?? CatalogKit.AsOf };
        ReferenceAssert.Matches(referenceCase, CatalogKit.FullEngine(provider).Assess(request));
    }
}

public sealed class DeadlinesReferenceEvaluator : CatalogReferenceEvaluator
{
    public override string Modul => "terminy";
}
