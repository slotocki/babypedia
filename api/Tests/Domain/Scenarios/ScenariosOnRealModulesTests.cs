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

    [Fact]
    public void Drugi_rodzic_bierze_33_of_41_weeks_returns_engine_validation_error()
    {
        var facts = BirthWithBothParents();

        var variant = Apply(new DrugiRodzicBierzeScenario(), facts, """{"tygodnie":33}""");
        var assessment = PeriodsEngine().Assess(variant, AsOf);

        var issue = Assert.Single(assessment.ValidationErrors, v => v.Code == "rodzicielski_ponad_limit_jednego_rodzica");
        Assert.Equal(FactKeys.PlanRodzicielskiCzesci, issue.Fact);
        Assert.Equal(PersonRef.DrugiRodzic, issue.Person);
        Assert.Empty(PeriodsEngine().Assess(facts, AsOf).ValidationErrors);
    }

    [Fact]
    public void Drugi_rodzic_bierze_valid_split_has_no_validation_errors()
    {
        var variant = Apply(new DrugiRodzicBierzeScenario(), BirthWithBothParents(), """{"tygodnie":20}""");

        Assert.Empty(PeriodsEngine().Assess(variant, AsOf).ValidationErrors);
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

    [Fact]
    public void Splacam_zadluzenie_after_deadline_does_not_entitle()
    {
        var variant = Apply(new SplacamZadluzenieScenario(), MotherWithDebt(), """{"data":"2027-02-11"}""");

        Assert.Equal(BenefitStatus.NiePrzysluguje, Maternity(variant));
    }
}
