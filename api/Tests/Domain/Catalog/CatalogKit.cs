using System.Globalization;
using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Domain.Periods;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public static class CatalogKit
{
    public static readonly DateOnly AsOf = new(2026, 11, 21);

    public static IReadOnlyList<SeedRow> DeadlineRows() =>
        DeadlineParameterKeys.Supplement
            .Select(r => new SeedRow(r.Key, JsonSerializer.SerializeToElement(r.Value), new DateOnly(2000, 1, 1), r.LegalBasis))
            .ToList();

    public static IParameterProvider Params(params SeedRow[] extra) =>
        SeedParameterProvider.Load([.. DeadlineRows(), .. extra]);

    public static IParameterProvider SeedWithoutDeadlineKeys() => SeedParameterProvider.Load();

    public static AssessmentEngine Engine(
        IParameterProvider? parameters = null,
        IBasisStep? basis = null,
        IAmountsStep? amounts = null,
        ICatalogSettings? settings = null,
        DateOnly? today = null)
    {
        var p = parameters ?? Params();
        var now = (today ?? AsOf).ToDateTime(new TimeOnly(12, 0));
        return new AssessmentEngine(
            new PeriodsService(p),
            new EligibilityService(p),
            p,
            new DeadlineService(),
            new BenefitPipeline(basis ?? new NoBasisStep(), amounts ?? new NoAmountsStep()),
            settings ?? new AllEnabledCatalogSettings(),
            new FixedTimeProvider(new DateTimeOffset(now, TimeSpan.Zero)));
    }

    public static AssessmentResult Assess(FactSet facts, DateOnly? asOf = null, IEnumerable<string>? completed = null, AssessmentEngine? engine = null) =>
        (engine ?? Engine()).Assess(new AssessmentRequest(facts, asOf ?? AsOf, completed?.ToList()));

    public static DateOnly D(string date) => DateOnly.Parse(date, CultureInfo.InvariantCulture);

    public static Fact<T> F<T>(T value) => new(value, FactSource.User);

    public static InsuranceTitle Title(
        TitleKind kind = TitleKind.Pracownik,
        string from = "2020-01-01",
        string? to = null,
        SicknessInsurance sickness = SicknessInsurance.Obowiazkowe) =>
        new(kind, D(from), to is null ? null : D(to), sickness);

    public static PersonFacts Person(
        ParentRole role,
        InsuranceTitle? title = null,
        int? headcount = 150,
        Action<PersonFactsBuilder>? configure = null)
    {
        var builder = new PersonFactsBuilder
        {
            Role = role,
            Titles = title is null ? null : [title],
            Headcount = headcount,
        };
        configure?.Invoke(builder);
        return builder.Build();
    }

    public static CaseFacts Case(EventKind kind, string date, int children = 1) =>
        new()
        {
            ZdarzenieRodzaj = F(kind),
            ZdarzenieData = F(D(date)),
            ZdarzenieDlaKogo = F(CalculationScope.Ja),
            DzieckoLiczba = F(children),
        };

    public static FactSet Facts(CaseFacts sprawa, PersonFacts me, PersonFacts? other = null) =>
        new() { Sprawa = sprawa, Wnioskodawca = me, DrugiRodzic = other };

    public static BenefitResult Get(AssessmentResult result, BenefitId id, PersonRef person = PersonRef.Wnioskodawca) =>
        result.Benefits.Single(b => b.Id == id && b.Person == person);

    public static FactSet LoadPersona(string id, string? overlayJson = null)
    {
        var path = Path.Combine(TestPaths.Fixtures, "personas", id + ".json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var facts = doc.RootElement.GetProperty("fakty").Deserialize<FactSet>(ApiJson.Options)!;
        if (overlayJson is null)
        {
            return facts;
        }

        var overlay = JsonDocument.Parse(overlayJson).RootElement.Deserialize<FactSet>(ApiJson.Options)!;
        return facts with { Sprawa = overlay.Sprawa };
    }
}

public sealed class PersonFactsBuilder
{
    public ParentRole? Role { get; set; }
    public IReadOnlyList<InsuranceTitle>? Titles { get; set; }
    public int? Headcount { get; set; }
    public bool? FixedTerm { get; set; }
    public DateOnly? ContractEnd { get; set; }
    public DateOnly? PlannedLeaving { get; set; }
    public IReadOnlyList<ContributionBase>? Bases { get; set; }
    public ContributionDebt? Debt { get; set; }
    public IReadOnlyList<SickLeave>? SickLeaves { get; set; }
    public int? PlanBeforeBirthWeeks { get; set; }
    public int? ResignationAfterWeeks { get; set; }
    public IReadOnlyList<LeavePart>? ParentalParts { get; set; }
    public IReadOnlyList<PartTimeWork>? PartTimeWork { get; set; }
    public IReadOnlyList<LeavePart>? PaternityParts { get; set; }
    public DateOnly? AveragedRequestDate { get; set; }

    public PersonFacts Build() => new()
    {
        OsobaRola = Role is { } r ? CatalogKit.F(r) : null,
        UbezpieczenieTytuly = Titles is null ? null : CatalogKit.F(Titles),
        PlatnikLiczbaUbezpieczonych = Headcount is { } h ? CatalogKit.F(h) : null,
        UmowaNaCzasOkreslony = FixedTerm is { } t ? CatalogKit.F(t) : null,
        UmowaDataKonca = ContractEnd is { } e ? CatalogKit.F(e) : null,
        UmowaPlanowaneOdejscieData = PlannedLeaving is { } l ? CatalogKit.F(l) : null,
        UbezpieczeniePodstawySkladek = Bases is null ? null : CatalogKit.F(Bases),
        SkladkiZadluzenie = Debt is null ? null : CatalogKit.F(Debt),
        EzlaZwolnienia = SickLeaves is null ? null : CatalogKit.F(SickLeaves),
        PlanMacierzynskiPrzedPorodemTyg = PlanBeforeBirthWeeks is { } w ? CatalogKit.F(w) : null,
        PlanRezygnacjaZMacierzynskiegoPoTyg = ResignationAfterWeeks is { } rw ? CatalogKit.F(rw) : null,
        PlanRodzicielskiCzesci = ParentalParts is null ? null : CatalogKit.F(ParentalParts),
        PlanPracaNaCzescEtatu = PartTimeWork is null ? null : CatalogKit.F(PartTimeWork),
        PlanOjcowskiCzesci = PaternityParts is null ? null : CatalogKit.F(PaternityParts),
        PlanDataWnioskuUsrednionego = AveragedRequestDate is { } a ? CatalogKit.F(a) : null,
    };
}
