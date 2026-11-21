using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Tests.Domain.Scenarios;

internal sealed class FakeEngine(params AssessmentResult[] responses) : IAssessmentEngine
{
    private int next;

    public List<AssessmentRequest> Requests { get; } = [];

    public AssessmentResult Assess(AssessmentRequest request)
    {
        Requests.Add(request);
        return responses[Math.Min(next++, responses.Length - 1)];
    }

    public static AssessmentResult Result(params BenefitResult[] benefits) => new()
    {
        RulesetVersion = "fake",
        Benefits = benefits,
    };

    public static BenefitResult Benefit(
        BenefitId id,
        BenefitStatus status,
        decimal? weeks = null,
        decimal? total = null,
        PersonRef person = PersonRef.Wnioskodawca) => new()
    {
        Id = id,
        Person = person,
        Status = status,
        Period = weeks is { } w ? new Period(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1).AddDays((int)(w * 7) - 1), (int)(w * 7), w) : null,
        Amount = total is { } t ? new Amount(0, t, 0, BasisSource.UstalonaPrzezPlatnika) : null,
    };
}
