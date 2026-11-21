using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Scenarios;

public static class ScenarioDiff
{
    public static IReadOnlyList<BenefitDiff> Build(AssessmentResult baseResult, AssessmentResult variant)
    {
        var baseByKey = baseResult.Benefits.GroupBy(b => (b.Id, b.Person)).ToDictionary(g => g.Key, g => g.First());
        var variantByKey = variant.Benefits.GroupBy(b => (b.Id, b.Person)).ToDictionary(g => g.Key, g => g.First());
        var keys = baseResult.Benefits.Concat(variant.Benefits).Select(b => (b.Id, b.Person)).Distinct();

        return keys.Select(key =>
        {
            baseByKey.TryGetValue(key, out var before);
            variantByKey.TryGetValue(key, out var after);
            return new BenefitDiff(
                key.Id,
                key.Person,
                before?.Status ?? BenefitStatus.NieDotyczy,
                after?.Status ?? BenefitStatus.NieDotyczy,
                Delta(Weeks(before), Weeks(after)),
                Delta(Money(before), Money(after)));
        }).ToList();
    }

    private static bool KnownZero(BenefitResult? benefit) =>
        benefit is null || benefit.Status is BenefitStatus.NiePrzysluguje or BenefitStatus.NieDotyczy;

    private static decimal? Weeks(BenefitResult? benefit) =>
        benefit?.Period?.Weeks ?? (KnownZero(benefit) ? 0m : null);

    private static decimal? Money(BenefitResult? benefit) =>
        benefit?.Amount?.Total ?? (KnownZero(benefit) ? 0m : null);

    private static decimal? Delta(decimal? before, decimal? after) =>
        before is { } b && after is { } a ? Math.Round(a - b, 2, MidpointRounding.AwayFromZero) : null;
}
