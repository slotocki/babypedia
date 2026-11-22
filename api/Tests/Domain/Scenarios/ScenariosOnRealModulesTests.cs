using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Scenarios;
using static BabyRadnik.Tests.Domain.Scenarios.ScenarioKit;

namespace BabyRadnik.Tests.Domain.Scenarios;

public sealed class ScenariosOnRealModulesTests
{
    private static BenefitStatus Maternity(FactSet facts) =>
        Benefit(EligibilityEngine().Assess(facts, AsOf), BenefitId.Macierzynski).Status;

    [Fact]
    public void Odchodze_z_pracy_changes_maternity_from_entitled_to_not_entitled()
    {
        var facts = PregnantEmployee();

        var variant = Apply(new OdchodzeZPracyScenario(), facts, """{"data":"2026-11-15"}""");

        Assert.Equal(BenefitStatus.Przysluguje, Maternity(facts));
        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(variant));
    }

    [Fact]
    public void Umowa_nie_zostanie_przedluzona_after_third_month_of_pregnancy_still_entitles()
    {
        var facts = PregnantEmployee(contractEnd: "2026-09-30");

        var variant = Apply(new UmowaNieZostaniePrzedluzonaScenario(), facts, null);

        Assert.Equal(BenefitStatus.Przysluguje, Maternity(facts));
        Assert.Equal(BenefitStatus.Przysluguje, Maternity(variant));
        var rule = Benefit(EligibilityEngine().Assess(variant, AsOf), BenefitId.Macierzynski).Trace
            .Single(t => t.Rule == "wyjatek_ustanie_ubezpieczenia_w_ciazy");
        Assert.True(rule.Met);
        Assert.Contains("177", rule.Text);
    }

    [Fact]
    public void Umowa_nie_zostanie_przedluzona_before_end_of_third_month_of_pregnancy_does_not_entitle()
    {
        var facts = PregnantEmployee(contractEnd: "2026-07-31");

        var variant = Apply(new UmowaNieZostaniePrzedluzonaScenario(), facts, null);

        Assert.Equal(BenefitStatus.Przysluguje, Maternity(facts));
        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(variant));
    }

    [Fact]
    public void Przechodze_na_dzialalnosc_without_voluntary_sickness_insurance_does_not_entitle()
    {
        var facts = PregnantEmployee();

        var variant = Apply(new PrzechodzeNaDzialalnoscScenario(), facts, """{"data":"2026-11-01","dobrowolneChorobowe":false}""");

        Assert.Equal(BenefitStatus.Przysluguje, Maternity(facts));
        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(variant));
    }

    [Fact]
    public void Przechodze_na_dzialalnosc_with_voluntary_sickness_insurance_is_no_longer_denied_for_missing_insurance()
    {
        var variant = Apply(new PrzechodzeNaDzialalnoscScenario(), PregnantEmployee(), """{"data":"2026-11-01","dobrowolneChorobowe":true}""");

        var result = Benefit(EligibilityEngine().Assess(variant, AsOf), BenefitId.Macierzynski);

        Assert.NotEqual(BenefitStatus.NiePrzysluguje, result.Status);
    }

    [Theory]
    [InlineData(PersonRef.DrugiRodzic)]
    [InlineData(PersonRef.Wnioskodawca)]
    public void Drugi_rodzic_bierze_33_of_41_weeks_flags_the_parent_who_takes_them(PersonRef taker)
    {
        var facts = BirthWithBothParents();

        var variant = Apply(new DrugiRodzicBierzeScenario(), facts, """{"tygodnie":33}""", taker);
        var assessment = PeriodsEngine().Assess(variant, AsOf);

        var issue = Assert.Single(assessment.ValidationErrors, v => v.Code == "rodzicielski_ponad_limit_jednego_rodzica");
        Assert.Equal(FactKeys.PlanRodzicielskiCzesci, issue.Fact);
        Assert.Equal(taker, issue.Person);
        Assert.Empty(PeriodsEngine().Assess(facts, AsOf).ValidationErrors);
    }

    [Theory]
    [InlineData(PersonRef.DrugiRodzic, 9)]
    [InlineData(PersonRef.DrugiRodzic, 32)]
    [InlineData(PersonRef.Wnioskodawca, 9)]
    [InlineData(PersonRef.Wnioskodawca, 32)]
    public void Drugi_rodzic_bierze_up_to_total_minus_9_weeks_for_either_parent_is_valid(PersonRef taker, int weeks)
    {
        var variant = Apply(new DrugiRodzicBierzeScenario(), BirthWithBothParents(), $$"""{"tygodnie":{{weeks}}}""", taker);

        Assert.Empty(PeriodsEngine().Assess(variant, AsOf).ValidationErrors);
    }

    [Fact]
    public void Drugi_rodzic_bierze_valid_split_has_no_validation_errors()
    {
        var variant = Apply(new DrugiRodzicBierzeScenario(), BirthWithBothParents(), """{"tygodnie":20}""", PersonRef.DrugiRodzic);

        Assert.Empty(PeriodsEngine().Assess(variant, AsOf).ValidationErrors);
    }

    // Zgłoszenie: część drugiego rodzica miała „nie ustalam okresu”. Teraz obie części mają okres (część ojca — założenie scenariusza).
    [Fact]
    public void Drugi_rodzic_bierze_9_weeks_gives_both_parents_a_parental_leave_period()
    {
        var variant = Apply(new DrugiRodzicBierzeScenario(), BirthWithBothParents(), """{"tygodnie":9}""", PersonRef.DrugiRodzic);

        var periods = PeriodsEngine().Assess(variant, AsOf).Benefits.Where(b => b.Id == BenefitId.Rodzicielski).ToList();
        var mother = periods.Single(b => b.Person == PersonRef.Wnioskodawca).Period!;
        var father = periods.Single(b => b.Person == PersonRef.DrugiRodzic).Period!;
        Assert.Equal(32m, mother.Weeks);
        Assert.Equal(D("2026-07-20"), mother.From);
        Assert.Equal(9m, father.Weeks);
        Assert.Equal(mother.To.AddDays(1), father.From);
    }

    [Fact]
    public void Praca_na_czesc_etatu_half_time_for_20_weeks_extends_parental_leave_by_10_weeks()
    {
        var facts = BirthWithBothParents() with { DrugiRodzic = null };

        var variant = Apply(new PracaNaCzescEtatuScenario(), facts, """{"wymiar":0.5,"tygodnie":20}""");

        var before = PeriodsEngine().Assess(facts, AsOf).Benefits.Single(b => b.Id == BenefitId.Rodzicielski).Period!.Weeks;
        var after = PeriodsEngine().Assess(variant, AsOf).Benefits.Single(b => b.Id == BenefitId.Rodzicielski).Period!.Weeks;
        Assert.Equal(41m, before);
        Assert.Equal(51m, after);
    }

    [Fact]
    public void Splacam_zadluzenie_in_time_changes_denial_to_entitlement()
    {
        var facts = MotherWithDebt();

        var variant = Apply(new SplacamZadluzenieScenario(), facts, """{"data":"2026-11-30"}""");

        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(facts));
        Assert.Equal(BenefitStatus.Przysluguje, Maternity(variant));
    }

    // Zgłoszenie (edyta-demo): saldo z 20.11.2026, poród 05.05.2027 — spłata 01.06.2027 mieści się w 6 mies. od dnia powstania prawa.
    [Fact]
    public void Splacam_zadluzenie_deadline_counts_from_the_day_the_right_arises_not_from_the_balance_date()
    {
        var facts = MotherWithDebt() with
        {
            Sprawa = new CaseFacts { ZdarzenieRodzaj = F(EventKind.Ciaza), ZdarzenieData = F(D("2027-05-05")), DzieckoLiczba = F(1) },
        };
        facts = facts with { Wnioskodawca = facts.Wnioskodawca with { SkladkiZadluzenie = F(new ContributionDebt(D("2026-11-20"), 120m)) } };

        var inTime = Apply(new SplacamZadluzenieScenario(), facts, """{"data":"2027-06-01"}""");
        var late = Apply(new SplacamZadluzenieScenario(), facts, """{"data":"2027-11-06"}""");

        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(facts));
        Assert.Equal(BenefitStatus.Przysluguje, Maternity(inTime));
        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(late));
    }

    [Fact]
    public void Splacam_zadluzenie_after_deadline_does_not_entitle()
    {
        var variant = Apply(new SplacamZadluzenieScenario(), MotherWithDebt(), """{"data":"2027-02-11"}""");

        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(variant));
    }
}
