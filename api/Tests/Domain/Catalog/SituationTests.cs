using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class DifficultSituationTests
{
    private static PersonFacts Mother() => Person(ParentRole.Matka, Title(), 150);

    private static FactSet After(Func<CaseFacts, CaseFacts> configure) =>
        Facts(configure(Case(EventKind.Urodzenie, "2026-10-08")), Mother());

    private static DateRange Stay() => new(D("2026-10-08"), D("2026-10-30"));

    [Fact]
    public void Ordinary_pregnancy_and_birth_are_not_difficult()
    {
        Assert.False(Assess(Facts(Case(EventKind.Ciaza, "2027-03-15"), Mother())).DifficultSituation);
        Assert.False(Assess(After(c => c)).DifficultSituation);
    }

    [Fact]
    public void Stillbirth_is_difficult()
    {
        Assert.True(Assess(After(c => c with { DzieckoMartweUrodzenie = F(true) })).DifficultSituation);
    }

    [Fact]
    public void Child_death_is_difficult_by_date_or_by_count()
    {
        Assert.True(Assess(After(c => c with { DzieckoDataZgonu = F(D("2026-10-20")) })).DifficultSituation);
        Assert.True(Assess(After(c => c with { DzieckoLiczba = F(2), DzieckoLiczbaZmarlych = F(1) })).DifficultSituation);
    }

    [Fact]
    public void Serious_illness_with_the_za_zyciem_certificate_is_difficult()
    {
        Assert.True(Assess(After(c => c with { DzieckoZaZyciem = F(true) })).DifficultSituation);
        Assert.False(Assess(After(c => c with { DzieckoZaZyciem = F(false) })).DifficultSituation);
    }

    [Fact]
    public void Special_situation_of_the_mother_is_difficult()
    {
        var situation = new MotherSituation(MotherSituationKind.PobytWSzpitalu, D("2026-12-01"));

        Assert.True(Assess(After(c => c with { MatkaSytuacjaSzczegolna = F(situation) })).DifficultSituation);
    }

    [Theory]
    [InlineData(30, null, true)]
    [InlineData(36, null, true)]
    [InlineData(37, null, false)]
    [InlineData(39, null, false)]
    [InlineData(39, 1000, true)]
    [InlineData(39, 1001, false)]
    [InlineData(null, 900, true)]
    public void Prematurity_with_a_hospital_stay_is_difficult(int? week, int? weight, bool expected)
    {
        var facts = After(c => c with
        {
            DzieckoTydzienCiazyPrzyPorodzie = week is { } w ? F(w) : null,
            DzieckoMasaUrodzeniowaG = weight is { } g ? F(g) : null,
            DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([Stay()]),
        });

        Assert.Equal(expected, Assess(facts).DifficultSituation);
    }

    [Fact]
    public void Prematurity_without_a_hospital_stay_or_with_unknown_weeks_is_not_difficult()
    {
        var noStay = After(c => c with { DzieckoTydzienCiazyPrzyPorodzie = F(30) });
        var emptyStays = After(c => c with { DzieckoTydzienCiazyPrzyPorodzie = F(30), DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([]) });
        var stayOnly = After(c => c with { DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([Stay()]) });

        Assert.False(Assess(noStay).DifficultSituation);
        Assert.False(Assess(emptyStays).DifficultSituation);
        Assert.False(Assess(stayOnly).DifficultSituation);
    }

    [Fact]
    public void Prematurity_threshold_comes_from_a_parameter()
    {
        var facts = After(c => c with
        {
            DzieckoTydzienCiazyPrzyPorodzie = F(30),
            DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([Stay()]),
        });
        var provider = new WithoutKeys(Params(), "uzupelniajacy_prog_tydzien_ciazy.donoszony");

        Assert.False(Assess(facts, engine: Engine(provider)).DifficultSituation);
    }
}

public sealed class FuneralBenefitTests
{
    private static FactSet Stillbirth(Func<CaseFacts, CaseFacts>? configure = null) =>
        Facts((configure ?? (c => c))(Case(EventKind.Urodzenie, "2026-10-08") with { DzieckoMartweUrodzenie = F(true) }), Person(ParentRole.Matka, Title(), 150));

    [Fact]
    public void Stillbirth_gives_information_with_the_amount_from_the_parameter_and_the_documents()
    {
        var result = Assess(Stillbirth(c => c with { DzieckoDataPochowku = F(D("2026-10-12")) }));

        var benefit = Get(result, BenefitId.Pogrzebowy);
        Assert.Equal(BenefitStatus.Przysluguje, benefit.Status);
        Assert.Contains(benefit.Notes, n => n.Contains("7 000,00 zł"));
        Assert.Equal(Payer.Zus, benefit.Payer);
        Assert.Equal(ApplicationPlace.Zus, benefit.WhereToApply);
        Assert.Contains(benefit.Documents, d => d.Contains("Z-12"));
        Assert.Contains(benefit.Documents, d => d.Contains("Rachunki"));
        Assert.Equal("pogrzebowy_martwe_urodzenie_lub_zgon", benefit.Trace.Single().Rule);
        Assert.Equal("Poradnik ZUS 2026 s. 35–36", benefit.Trace.Single().Basis);
        Assert.Null(benefit.Period);
        Assert.Null(benefit.Amount);
        Assert.True(result.DifficultSituation);
    }

    [Fact]
    public void Amount_follows_the_parameter_value()
    {
        var changed = new BabyRadnik.Tests.Reference.SeedRow(
            "zasilek_pogrzebowy",
            System.Text.Json.JsonSerializer.SerializeToElement(8000),
            D("2026-01-01"),
            "test");

        var result = Assess(Stillbirth(), engine: Engine(Params(changed)));

        Assert.Contains(Get(result, BenefitId.Pogrzebowy).Notes, n => n.Contains("8 000,00 zł"));
    }

    [Fact]
    public void Burial_date_is_asked_for_until_given_and_unlocks_the_z12_deadline()
    {
        var without = Assess(Stillbirth());
        var with = Assess(Stillbirth(c => c with { DzieckoDataPochowku = F(D("2026-10-12")) }));

        Assert.Contains(without.MissingFacts, m => m.Fact == FactKeys.DzieckoDataPochowku && m.Type == MissingFactType.Date && m.Person is null);
        Assert.Empty(Get(without, BenefitId.Pogrzebowy).Deadlines);
        Assert.DoesNotContain(with.MissingFacts, m => m.Fact == FactKeys.DzieckoDataPochowku);
        Assert.Equal(D("2027-10-12"), Get(with, BenefitId.Pogrzebowy).Deadlines.Single().Date);
    }

    [Fact]
    public void Child_death_after_birth_gives_the_same_information_with_a_caveat()
    {
        var facts = Facts(
            Case(EventKind.Urodzenie, "2026-10-08") with { DzieckoDataZgonu = F(D("2026-10-20")), DzieckoDataPochowku = F(D("2026-10-25")) },
            Person(ParentRole.Matka, Title(), 150));

        var benefit = Get(Assess(facts), BenefitId.Pogrzebowy);

        Assert.Equal(BenefitStatus.Przysluguje, benefit.Status);
        Assert.Contains(benefit.Notes, n => n.Contains("potwierdź warunki w ZUS"));
        Assert.Equal(D("2027-10-25"), benefit.Deadlines.Single().Date);
    }

    [Fact]
    public void Without_a_stillbirth_or_death_the_funeral_benefit_is_not_applicable()
    {
        var result = Assess(Facts(Case(EventKind.Urodzenie, "2026-10-08"), Person(ParentRole.Matka, Title(), 150)));

        var benefit = Get(result, BenefitId.Pogrzebowy);
        Assert.Equal(BenefitStatus.NieDotyczy, benefit.Status);
        Assert.Empty(benefit.Notes);
    }

    [Fact]
    public void Funeral_information_appears_once_for_the_applicant_not_for_the_second_parent()
    {
        var facts = Stillbirth() with { DrugiRodzic = Person(ParentRole.Ojciec, Title(), 150) };

        var result = Assess(facts);

        Assert.Equal(BenefitStatus.Przysluguje, Get(result, BenefitId.Pogrzebowy).Status);
        Assert.Equal(BenefitStatus.NieDotyczy, Get(result, BenefitId.Pogrzebowy, PersonRef.DrugiRodzic).Status);
    }

    [Fact]
    public void Missing_amount_parameter_is_undecided_with_contact_channels()
    {
        var result = Assess(Stillbirth(), engine: Engine(new WithoutKeys(Params(), "zasilek_pogrzebowy")));

        var benefit = Get(result, BenefitId.Pogrzebowy);
        Assert.Equal(BenefitStatus.NieRozstrzygam, benefit.Status);
        Assert.Contains("zasilek_pogrzebowy", benefit.Reason!);
        Assert.Contains(StatusComposer.ContactNote, benefit.Notes);
    }

    [Fact]
    public void Disabled_funeral_entry_is_omitted()
    {
        var result = Assess(Stillbirth(), engine: Engine(settings: new FixedCatalogSettings([BenefitId.Pogrzebowy])));

        Assert.DoesNotContain(result.Benefits, b => b.Id == BenefitId.Pogrzebowy);
    }
}
