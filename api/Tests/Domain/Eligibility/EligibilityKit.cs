using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Eligibility;

internal static class Kit
{
    public static readonly DateOnly AsOf = new(2026, 11, 21);

    public static IReadOnlyList<SeedRow> SupplementRows() =>
        EligibilityParameterKeys.Supplement
            .Select(r => new SeedRow(r.Key, JsonSerializer.SerializeToElement(r.Value), new DateOnly(2000, 1, 1), r.LegalBasis))
            .ToList();

    public static IParameterProvider Params(params SeedRow[] extra) =>
        SeedParameterProvider.Load([.. SupplementRows(), .. extra]);

    public static IParameterProvider SeedOnly() => SeedParameterProvider.Load();

    public static EligibilityService Service(params SeedRow[] extra) => new(Params(extra));

    public static SeedRow Row(string key, decimal value, DateOnly validFrom) =>
        new(key, JsonSerializer.SerializeToElement(value), validFrom, "test");

    public static Fact<T> F<T>(T value) => new(value, FactSource.User);

    public static DateOnly D(int y, int m, int d) => new(y, m, d);

    public static InsuranceTitle Title(
        TitleKind kind,
        DateOnly from,
        DateOnly? to = null,
        SicknessInsurance sickness = SicknessInsurance.Obowiazkowe) =>
        new(kind, from, to, sickness);

    public static CaseFacts Case(EventKind kind, DateOnly date, int? weekAtBirth = null) => new()
    {
        ZdarzenieRodzaj = F(kind),
        ZdarzenieData = F(date),
        DzieckoTydzienCiazyPrzyPorodzie = weekAtBirth is { } w ? F(w) : null,
    };

    public static PersonFacts Person(ParentRole role, params InsuranceTitle[] titles) => new()
    {
        OsobaRola = F(role),
        UbezpieczenieTytuly = F<IReadOnlyList<InsuranceTitle>>(titles),
    };

    public static FactSet Facts(CaseFacts sprawa, PersonFacts applicant, PersonFacts? other = null) => new()
    {
        Sprawa = sprawa,
        Wnioskodawca = applicant,
        DrugiRodzic = other,
    };

    public static BenefitResult Get(EligibilityAssessment assessment, BenefitId id, PersonRef person = PersonRef.Wnioskodawca) =>
        assessment.Benefits.Single(b => b.Id == id && b.Person == person);

    public static SickLeave Leave(DateOnly from, DateOnly to, string? code = "B") => new(from, to, code, false);
}
