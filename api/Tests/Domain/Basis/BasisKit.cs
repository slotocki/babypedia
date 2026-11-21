using System.Globalization;
using System.Text.Json;
using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Basis;

internal static class BasisKit
{
    public const string Birth = "2026-03-02";

    public static DateOnly D(string date) => DateOnly.Parse(date, CultureInfo.InvariantCulture);

    public static Fact<T> F<T>(T value) => new(value, FactSource.Zus);

    public static SeedRow Row(string key, decimal value, string validFrom) =>
        new(key, JsonDocument.Parse(value.ToString(CultureInfo.InvariantCulture)).RootElement.Clone(), DateOnly.Parse(validFrom, CultureInfo.InvariantCulture), "test: wartość syntetyczna, nie przepis");

    public static IReadOnlyList<SeedRow> LowestBase { get; } =
    [
        Row(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, 4000, "2025-01-01"),
        Row(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, 4200, "2026-01-01"),
    ];

    public static BasisService Service(params SeedRow[] extra) => new(SeedParameterProvider.Load(extra));

    public static BasisService ServiceWithLowestBase() => Service([.. LowestBase]);

    public static InsuranceTitle Employee(string from, string? to = null, decimal? workingTime = 1) =>
        new(TitleKind.Pracownik, D(from), to is null ? null : D(to), SicknessInsurance.Obowiazkowe, WymiarEtatu: workingTime);

    public static InsuranceTitle Business(string from, SicknessInsurance sickness = SicknessInsurance.Dobrowolne, string? to = null) =>
        new(TitleKind.Dzialalnosc, D(from), to is null ? null : D(to), sickness);

    public static InsuranceTitle Contractor(string from, SicknessInsurance sickness = SicknessInsurance.Dobrowolne) =>
        new(TitleKind.Zleceniobiorca, D(from), null, sickness);

    public static IReadOnlyList<ContributionBase> Contributions(string firstMonth, int count, decimal amount)
    {
        var start = D(firstMonth + "-01");
        return Enumerable.Range(0, count)
            .Select(k => new ContributionBase(start.AddMonths(k).ToString("yyyy-MM", CultureInfo.InvariantCulture), amount))
            .ToList();
    }

    public static IReadOnlyList<ContributionBase> Override(IReadOnlyList<ContributionBase> source, string month, decimal amount) =>
        source.Select(c => c.Miesiac == month ? c with { Kwota = amount } : c).ToList();

    public static ReceivedBenefit Benefit(ReceivedBenefitKind kind, string from, string to) => new(kind, D(from), D(to));

    public static SickLeave Leave(string from, string to) => new(D(from), D(to), "A", false);

    public static PersonFacts Person(
        IReadOnlyList<InsuranceTitle>? titles,
        IReadOnlyList<ContributionBase>? contributions = null,
        IReadOnlyList<ReceivedBenefit>? benefits = null,
        IReadOnlyList<SickLeave>? leaves = null,
        decimal? payerBasis = null,
        IReadOnlyList<InsuranceBreak>? breaks = null) => new()
    {
        UbezpieczenieTytuly = titles is null ? null : F(titles),
        UbezpieczeniePodstawySkladek = contributions is null ? null : F(contributions),
        UbezpieczeniePobraneSwiadczenia = benefits is null ? null : F(benefits),
        EzlaZwolnienia = leaves is null ? null : new Fact<IReadOnlyList<SickLeave>>(leaves, FactSource.Ezla),
        UbezpieczeniePrzerwy = breaks is null ? null : F(breaks),
        PodstawaUstalonaPrzezPlatnika = payerBasis is null ? null : new Fact<decimal>(payerBasis.Value, FactSource.User),
    };

    public static FactSet Facts(PersonFacts person) => new() { Wnioskodawca = person };

    public static PersonFacts FullTimeEmployee(decimal amount, IReadOnlyList<ReceivedBenefit>? benefits = null, IReadOnlyList<SickLeave>? leaves = null) =>
        Person([Employee("2020-01-01")], Contributions("2025-03", 12, amount), benefits ?? [], leaves ?? []);

    public static PersonFacts Voluntary(decimal amount, string from = "2020-01-01", IReadOnlyList<SickLeave>? leaves = null) =>
        Person([Business(from)], Contributions("2025-03", 12, amount), [], leaves ?? []);

    public static BasisAssessment Assess(PersonFacts person, string date = Birth, BasisService? service = null) =>
        (service ?? Service()).Assess(Facts(person), PersonRef.Wnioskodawca, D(date));
}
