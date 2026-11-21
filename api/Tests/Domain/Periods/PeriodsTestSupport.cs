using System.Globalization;
using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Periods;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Periods;

public static class PeriodsTestSupport
{
    public static readonly DateOnly AsOf = new(2026, 11, 21);

    public static SeedRow Row(string key, decimal value, string validFrom = "2000-01-01", string basis = "test") =>
        new(key, JsonDocument.Parse(value.ToString(CultureInfo.InvariantCulture)).RootElement.Clone(), DateOnly.Parse(validFrom), basis);

    public static IReadOnlyList<SeedRow> Supplemental { get; } =
    [
        Row(PeriodsParameterKeys.UzupelniajacyMaxTygWczesniak, 15, basis: "KP art. 180² § 1 pkt 1"),
        Row(PeriodsParameterKeys.UzupelniajacyMaxTygPrzedwczesny, 8, basis: "KP art. 180² § 1 pkt 2"),
        Row(PeriodsParameterKeys.UzupelniajacyMaxTygDonoszony, 8, basis: "KP art. 180² § 1 pkt 3"),
        Row(PeriodsParameterKeys.UzupelniajacyProgTydzienWczesniak, 28, basis: "KP art. 180² § 1 pkt 1–2"),
        Row(PeriodsParameterKeys.UzupelniajacyProgTydzienDonoszony, 37, basis: "KP art. 180² § 1 pkt 2–3"),
        Row(PeriodsParameterKeys.UzupelniajacyProgMasaG, 1000, basis: "KP art. 180² § 1 pkt 1–2"),
        Row(PeriodsParameterKeys.UzupelniajacyDonoszonyMinKolejneDni, 2, basis: "KP art. 180² § 1 pkt 3"),
        Row(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienOd, 5, basis: "KP art. 180² § 1 pkt 3"),
        Row(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienDo, 28, basis: "KP art. 180² § 1 pkt 3"),
        Row(PeriodsParameterKeys.UzupelniajacyDonoszonyLiczenieOdDnia, 5, basis: "KP art. 180² § 1 pkt 3"),
    ];

    public static IParameterProvider Parameters(params SeedRow[] extra) =>
        SeedParameterProvider.Load([.. Supplemental, .. extra]);

    public static PeriodsService Service(params SeedRow[] extra) => new(Parameters(extra));

    public static DateOnly D(string date) => DateOnly.Parse(date, CultureInfo.InvariantCulture);

    public static Fact<T> F<T>(T value) => new(value, FactSource.User);

    public static CaseFacts BirthCase(string date, int children = 1, EventKind kind = EventKind.Urodzenie) => new()
    {
        ZdarzenieRodzaj = F(kind),
        ZdarzenieData = F(D(date)),
        DzieckoLiczba = F(children),
    };

    public static CaseFacts AdoptionCase(string takenOn, string childBirth, AdoptionPath path, int children = 1) => new()
    {
        ZdarzenieRodzaj = F(EventKind.Przyjecie),
        ZdarzenieData = F(D(takenOn)),
        DzieckoLiczba = F(children),
        DzieckoDataUrodzenia = F(D(childBirth)),
        PrzyjecieSciezka = F(path),
        PrzyjecieWniosekDoSadu = path == AdoptionPath.Przysposobienie ? F(true) : null,
    };

    public static PersonFacts Mother() => new() { OsobaRola = F(ParentRole.Matka) };

    public static PersonFacts Father() => new() { OsobaRola = F(ParentRole.Ojciec) };

    public static FactSet Facts(CaseFacts sprawa, PersonFacts? me = null, PersonFacts? other = null) => new()
    {
        Sprawa = sprawa,
        Wnioskodawca = me ?? Mother(),
        DrugiRodzic = other,
    };

    public static PeriodsAssessment Assess(FactSet facts, DateOnly? asOf = null, params SeedRow[] extra) =>
        Service(extra).Assess(facts, asOf ?? AsOf);

    public static BenefitResult Get(PeriodsAssessment assessment, BenefitId id, PersonRef person = PersonRef.Wnioskodawca) =>
        assessment.Benefits.Single(b => b.Id == id && b.Person == person);

    public static void AssertPeriod(BenefitResult benefit, string from, string to, int days)
    {
        Assert.Equal(BenefitStatus.Przysluguje, benefit.Status);
        Assert.NotNull(benefit.Period);
        Assert.Equal(D(from), benefit.Period.From);
        Assert.Equal(D(to), benefit.Period.To);
        Assert.Equal(days, benefit.Period.Days);
        Assert.Equal(days / 7m, benefit.Period.Weeks);
    }
}
