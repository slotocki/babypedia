using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class RiskTests
{
    private static FactSet Pregnancy(PersonFacts me, PersonFacts? other = null, string date = "2027-03-15", EventKind kind = EventKind.Ciaza) =>
        Facts(Case(kind, date), me, other);

    private static PersonFacts Mother(InsuranceTitle? title = null, Action<PersonFactsBuilder>? configure = null) =>
        Person(ParentRole.Matka, title ?? Title(), 150, configure);

    private static InsuranceTitle Contract() => Title(TitleKind.Zleceniobiorca, sickness: SicknessInsurance.Dobrowolne);

    private static IEnumerable<string> Ids(AssessmentResult result) => result.Risks.Select(r => r.Id);

    [Fact]
    public void A_healthy_employee_far_from_birth_sees_only_the_averaged_variant_reminder()
    {
        var result = Assess(Pregnancy(Mother()));

        Assert.Equal(["termin_21_dni"], Ids(result));
    }

    [Fact]
    public void Insurance_ending_before_birth_is_reported_for_an_ended_title()
    {
        var result = Assess(Pregnancy(Mother(Title(to: "2027-01-31"))));

        var risk = Assert.Single(result.Risks, r => r.Id == "koniec_ubezpieczenia_przed_porodem");
        Assert.Equal("ustawa art. 29 ust. 1, art. 30", risk.Basis);
        Assert.Contains("art. 30", risk.Text);
    }

    [Fact]
    public void Insurance_ending_before_birth_is_reported_for_a_fixed_term_contract_and_a_planned_departure()
    {
        var fixedTerm = Assess(Pregnancy(Mother(configure: b => { b.FixedTerm = true; b.ContractEnd = D("2027-01-31"); })));
        var leaving = Assess(Pregnancy(Mother(configure: b => b.PlannedLeaving = D("2027-02-01"))));

        Assert.Contains("koniec_ubezpieczenia_przed_porodem", Ids(fixedTerm));
        Assert.Contains("koniec_ubezpieczenia_przed_porodem", Ids(leaving));
    }

    [Fact]
    public void Insurance_ending_before_birth_is_not_reported_when_another_title_covers_the_birth_or_the_contract_runs_on()
    {
        var titles = new PersonFactsBuilder
        {
            Role = ParentRole.Matka,
            Headcount = 150,
            Titles = [Title(from: "2015-01-01", to: "2018-12-31"), Title(from: "2019-01-01")],
        }.Build();

        var oldTitle = Assess(Pregnancy(titles));
        var runsOn = Assess(Pregnancy(Mother(configure: b => { b.FixedTerm = true; b.ContractEnd = D("2027-06-30"); })));
        var afterBirth = Assess(Pregnancy(Mother(Title(to: "2026-08-31")), date: "2026-10-08", kind: EventKind.Urodzenie));

        Assert.DoesNotContain("koniec_ubezpieczenia_przed_porodem", Ids(oldTitle));
        Assert.DoesNotContain("koniec_ubezpieczenia_przed_porodem", Ids(runsOn));
        Assert.DoesNotContain("koniec_ubezpieczenia_przed_porodem", Ids(afterBirth));
    }

    [Fact]
    public void Missing_voluntary_sickness_insurance_and_ulga_na_start_come_from_rights()
    {
        var noSickness = Assess(Pregnancy(Mother(Title(TitleKind.Zleceniobiorca, sickness: SicknessInsurance.Brak))));
        var ulga = Assess(Pregnancy(Mother(Title(TitleKind.Dzialalnosc, sickness: SicknessInsurance.Brak))));

        Assert.Contains("brak_dobrowolnego_chorobowego", Ids(noSickness));
        Assert.Contains("brak_dobrowolnego_chorobowego", Ids(ulga));
    }

    [Fact]
    public void Contribution_debt_and_work_during_sick_leave_come_from_rights()
    {
        var business = Mother(
            Title(TitleKind.Dzialalnosc, sickness: SicknessInsurance.Dobrowolne),
            b =>
            {
                b.Debt = new ContributionDebt(D("2026-11-01"), 120m);
                b.SickLeaves = [new SickLeave(D("2026-11-02"), D("2026-11-20"), "B", false)];
            });

        var result = Assess(Pregnancy(business));

        Assert.Contains("zadluzenie_skladkowe", Ids(result));
        Assert.Contains("praca_w_czasie_zwolnienia", Ids(result));
    }

    [Fact]
    public void Voluntary_insurance_ends_after_maternity_benefit_so_renewal_is_reported()
    {
        var voluntary = Assess(Pregnancy(Mother(Contract())));
        var employee = Assess(Pregnancy(Mother()));

        var risk = Assert.Single(voluntary.Risks, r => r.Id == "ponowne_zgloszenie_do_chorobowego");
        Assert.Contains("ponownie", risk.Text);
        Assert.DoesNotContain("ponowne_zgloszenie_do_chorobowego", Ids(employee));
    }

    [Fact]
    public void Renewal_risk_is_not_shown_when_no_maternity_benefit_is_granted()
    {
        var business = Person(
            ParentRole.Matka,
            Title(TitleKind.Dzialalnosc, sickness: SicknessInsurance.Dobrowolne),
            null,
            b => b.Debt = new ContributionDebt(D("2026-08-10"), 120m));

        var result = Assess(Facts(Case(EventKind.Urodzenie, "2026-08-10"), business));

        Assert.DoesNotContain("ponowne_zgloszenie_do_chorobowego", Ids(result));
    }

    [Fact]
    public void Averaged_variant_reminder_names_the_date_and_disappears_when_done_or_late()
    {
        var facts = Pregnancy(Mother());

        var open = Assess(facts);
        var completed = Assess(facts, completed: ["wniosek_usredniona"]);
        var filed = Assess(Pregnancy(Mother(configure: b => b.AveragedRequestDate = D("2027-03-20"))));
        var late = Assess(facts, D("2027-04-06"));
        var father = Assess(Pregnancy(Person(ParentRole.Ojciec, Title(), 150)));

        var risk = Assert.Single(open.Risks, r => r.Id == "termin_21_dni");
        Assert.Contains("5 kwietnia 2027", risk.Text);
        Assert.Equal("ustawa art. 30a", risk.Basis);
        Assert.DoesNotContain("termin_21_dni", Ids(completed));
        Assert.DoesNotContain("termin_21_dni", Ids(filed));
        Assert.DoesNotContain("termin_21_dni", Ids(late));
        Assert.DoesNotContain("termin_21_dni", Ids(father));
    }

    [Fact]
    public void Unused_paternity_leave_is_reported_until_it_is_planned_or_done()
    {
        var unplanned = Pregnancy(Person(ParentRole.Ojciec, Title(), 150));
        var planned = Pregnancy(Person(ParentRole.Ojciec, Title(), 150, b => b.PaternityParts = [new LeavePart(2, D("2027-05-03"))]));

        var open = Assess(unplanned);
        var done = Assess(unplanned, completed: ["wykorzystanie_ojcowskiego"]);
        var withPlan = Assess(planned);

        var risk = Assert.Single(open.Risks, r => r.Id == "niewykorzystany_ojcowski");
        Assert.Contains("14 marca 2028", risk.Text);
        Assert.DoesNotContain("niewykorzystany_ojcowski", Ids(done));
        Assert.DoesNotContain("niewykorzystany_ojcowski", Ids(withPlan));
    }

    [Fact]
    public void Claim_limitation_is_reported_only_once_a_period_has_ended()
    {
        var running = Assess(Pregnancy(Mother()));
        var ended = Assess(Pregnancy(Mother(), date: "2026-01-10", kind: EventKind.Urodzenie), D("2026-11-21"));
        var lapsed = Assess(Pregnancy(Mother(), date: "2026-01-10", kind: EventKind.Urodzenie), D("2027-01-15"));

        Assert.DoesNotContain("przedawnienie_zasilku", Ids(running));
        var upcoming = Assert.Single(ended.Risks, r => r.Id == "przedawnienie_zasilku");
        Assert.Contains("przedawnia się 29 listopada 2026", upcoming.Text);
        Assert.Equal("ustawa art. 67", upcoming.Basis);
        var past = Assert.Single(lapsed.Risks, r => r.Id == "przedawnienie_zasilku");
        Assert.Contains("mogło się przedawnić 29 listopada 2026", past.Text);
    }

    [Fact]
    public void Insurance_started_in_pregnancy_is_reported_as_neutral_information()
    {
        var started = Assess(Pregnancy(Mother(Title(from: "2026-09-01"))));
        var old = Assess(Pregnancy(Mother(Title(from: "2020-01-01"))));

        var risk = Assert.Single(started.Risks, r => r.Id == "zmiana_podstawy_lub_umowa_w_ciazy");
        Assert.Contains("informacja ogólna", risk.Text);
        Assert.DoesNotContain("zmiana_podstawy_lub_umowa_w_ciazy", Ids(old));
    }

    [Fact]
    public void A_higher_contribution_base_in_pregnancy_is_reported_but_a_flat_base_is_not()
    {
        static IReadOnlyList<ContributionBase> Bases(decimal before, decimal during) =>
        [
            new("2026-04", before), new("2026-05", before), new("2026-06", before),
            new("2026-09", during), new("2026-10", during),
        ];

        var raised = Assess(Pregnancy(Mother(configure: b => b.Bases = Bases(4000m, 9000m))));
        var flat = Assess(Pregnancy(Mother(configure: b => b.Bases = Bases(8000m, 8000m))));
        var lower = Assess(Pregnancy(Mother(configure: b => b.Bases = Bases(8000m, 7000m))));

        Assert.Contains("zmiana_podstawy_lub_umowa_w_ciazy", Ids(raised));
        Assert.DoesNotContain("zmiana_podstawy_lub_umowa_w_ciazy", Ids(flat));
        Assert.DoesNotContain("zmiana_podstawy_lub_umowa_w_ciazy", Ids(lower));
    }

    [Fact]
    public void Pregnancy_length_parameter_is_required_for_the_neutral_information()
    {
        var provider = new WithoutKeys(Params(), "ciaza_trwanie_tyg");

        var result = Assess(Pregnancy(Mother(Title(from: "2026-09-01"))), engine: Engine(provider));

        Assert.DoesNotContain("zmiana_podstawy_lub_umowa_w_ciazy", Ids(result));
    }

    [Fact]
    public void Risks_are_unique_by_id_and_every_risk_has_text()
    {
        var facts = Pregnancy(Mother(Title(TitleKind.Dzialalnosc, from: "2026-09-01", to: "2027-01-31", sickness: SicknessInsurance.Dobrowolne),
            b => b.Debt = new ContributionDebt(D("2026-11-01"), 120m)));

        var result = Assess(facts);

        Assert.Equal(result.Risks.Count, Ids(result).Distinct().Count());
        Assert.All(result.Risks, r => Assert.False(string.IsNullOrWhiteSpace(r.Text)));
    }

    [Fact]
    public void Neutral_information_does_not_judge_the_user()
    {
        var text = new ContributionOrContractChangeInPregnancyRisk()
            .Evaluate(new RiskContext(Pregnancy(Mother(Title(from: "2026-09-01"))), AsOf, Params(), [], new CompletedDeadlines(null)))
            .Single().Text;

        Assert.DoesNotContain("wyłudz", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pozorn", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nie ocena", text);
    }
}

public sealed class WithoutKeys(IParameterProvider inner, params string[] keys) : IParameterProvider
{
    public Parameter Get(string key, DateOnly date) =>
        keys.Contains(key) ? throw new ParameterNotFoundException(key, date) : inner.Get(key, date);
}
