using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class NextStepTests
{
    private static Deadline Dl(string id, string date, DeadlineType type = DeadlineType.NajpozniejDo) =>
        new(id, type, D(date), id);

    private static BenefitResult Benefit(BenefitId id, PersonRef person, params Deadline[] deadlines) => new()
    {
        Id = id,
        Person = person,
        Status = BenefitStatus.Przysluguje,
        Deadlines = deadlines,
    };

    private static readonly CompletedDeadlines None = new(null);

    [Fact]
    public void Picks_the_earliest_unfinished_latest_by_deadline()
    {
        var benefits = new[]
        {
            Benefit(BenefitId.Rodzicielski, PersonRef.Wnioskodawca, Dl("wniosek_rodzicielski", "2027-07-12", DeadlineType.NajpozniejPrzed)),
            Benefit(BenefitId.Macierzynski, PersonRef.Wnioskodawca, Dl("wniosek_usredniona", "2027-04-05")),
        };

        var next = NextStepSelector.Select(benefits, D("2026-11-21"), None);

        Assert.Equal(new NextStep("wniosek_usredniona", BenefitId.Macierzynski, PersonRef.Wnioskodawca), next);
    }

    [Fact]
    public void Windows_and_limitation_periods_are_never_the_next_step()
    {
        var benefits = new[]
        {
            Benefit(BenefitId.Rodzicielski, PersonRef.Wnioskodawca,
                Dl("wykorzystanie_rodzicielskiego", "2027-01-01", DeadlineType.Okno),
                Dl("przedawnienie_rodzicielski", "2027-01-02", DeadlineType.Przedawnienie)),
        };

        Assert.Null(NextStepSelector.Select(benefits, D("2026-11-21"), None));
    }

    [Fact]
    public void Completed_deadlines_are_skipped_by_id_or_by_person_prefixed_id()
    {
        var benefits = new[]
        {
            Benefit(BenefitId.Macierzynski, PersonRef.Wnioskodawca, Dl("wniosek_usredniona", "2027-04-05")),
            Benefit(BenefitId.Rodzicielski, PersonRef.DrugiRodzic, Dl("wniosek_rodzicielski", "2027-07-12", DeadlineType.NajpozniejPrzed)),
            Benefit(BenefitId.Ojcowski, PersonRef.Wnioskodawca, Dl("wniosek_ojcowski", "2027-09-01", DeadlineType.NajpozniejPrzed)),
        };

        var byId = NextStepSelector.Select(benefits, D("2026-11-21"), new CompletedDeadlines(["wniosek_usredniona"]));
        var byPerson = NextStepSelector.Select(benefits, D("2026-11-21"), new CompletedDeadlines(["wnioskodawca/wniosek_usredniona", "wniosek_rodzicielski"]));
        var otherPerson = NextStepSelector.Select(benefits, D("2026-11-21"), new CompletedDeadlines(["drugiRodzic/wniosek_usredniona"]));

        Assert.Equal("wniosek_rodzicielski", byId!.DeadlineId);
        Assert.Equal(PersonRef.DrugiRodzic, byId.Person);
        Assert.Equal("wniosek_ojcowski", byPerson!.DeadlineId);
        Assert.Equal("wniosek_usredniona", otherPerson!.DeadlineId);
    }

    [Fact]
    public void Past_deadlines_are_not_next_but_a_deadline_due_today_is()
    {
        var benefits = new[]
        {
            Benefit(BenefitId.Macierzynski, PersonRef.Wnioskodawca, Dl("wniosek_usredniona", "2027-04-05")),
            Benefit(BenefitId.Rodzicielski, PersonRef.Wnioskodawca, Dl("wniosek_rodzicielski", "2027-07-12", DeadlineType.NajpozniejPrzed)),
        };

        Assert.Equal("wniosek_usredniona", NextStepSelector.Select(benefits, D("2027-04-05"), None)!.DeadlineId);
        Assert.Equal("wniosek_rodzicielski", NextStepSelector.Select(benefits, D("2027-04-06"), None)!.DeadlineId);
        Assert.Null(NextStepSelector.Select(benefits, D("2027-07-13"), None));
    }

    [Fact]
    public void Hidden_benefits_are_ignored_and_ties_prefer_catalog_order()
    {
        var hidden = Benefit(BenefitId.Ojcowski, PersonRef.Wnioskodawca, Dl("wniosek_ojcowski", "2027-01-01")) with { Status = BenefitStatus.NieDotyczy };
        var first = Benefit(BenefitId.Macierzynski, PersonRef.Wnioskodawca, Dl("a_termin", "2027-02-01"));
        var second = Benefit(BenefitId.Rodzicielski, PersonRef.Wnioskodawca, Dl("b_termin", "2027-02-01"));

        var next = NextStepSelector.Select([hidden, first, second], D("2026-11-21"), None);

        Assert.Equal("a_termin", next!.DeadlineId);
    }

    [Fact]
    public void Engine_returns_the_earliest_deadline_then_moves_on_when_it_is_completed_or_passed()
    {
        var facts = Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), 150));

        var first = Assess(facts);
        var afterCompleting = Assess(facts, completed: ["wniosek_usredniona"]);
        var afterPassing = Assess(facts, asOf: D("2027-04-06"));
        var afterAll = Assess(facts, asOf: D("2027-07-13"));

        Assert.Equal(new NextStep("wniosek_usredniona", BenefitId.Macierzynski, PersonRef.Wnioskodawca), first.NextStep);
        Assert.Equal("wniosek_rodzicielski", afterCompleting.NextStep!.DeadlineId);
        Assert.Equal(BenefitId.Rodzicielski, afterCompleting.NextStep.BenefitId);
        Assert.Equal("wniosek_rodzicielski", afterPassing.NextStep!.DeadlineId);
        Assert.Null(afterAll.NextStep);
    }

    [Fact]
    public void Passed_deadline_stays_visible_with_its_consequence_but_is_not_the_next_step()
    {
        var facts = Facts(Case(EventKind.Ciaza, "2027-03-15"), Person(ParentRole.Matka, Title(), 150));

        var result = Assess(facts, asOf: D("2027-04-06"));

        var passed = Get(result, BenefitId.Macierzynski).Deadlines.Single(d => d.Id == "wniosek_usredniona");
        Assert.Contains("minął", passed.Text);
        Assert.Contains("Skutek", passed.Text);
        Assert.NotEqual("wniosek_usredniona", result.NextStep?.DeadlineId);
    }

    [Fact]
    public void Debt_repayment_is_the_next_step_for_a_denied_benefit()
    {
        var mother = Person(
            ParentRole.Matka,
            Title(TitleKind.Dzialalnosc, "2020-01-01", null, SicknessInsurance.Dobrowolne),
            null,
            b => b.Debt = new ContributionDebt(D("2026-08-10"), 120m));

        var result = Assess(Facts(Case(EventKind.Urodzenie, "2026-08-10"), mother));

        Assert.Equal("splata_zadluzenia", result.NextStep!.DeadlineId);
        Assert.Equal(BenefitId.Macierzynski, result.NextStep.BenefitId);
    }
}
