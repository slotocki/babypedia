using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Scenarios;
using BabyRadnik.Tests.Reference;
using static BabyRadnik.Tests.Domain.Scenarios.ScenarioKit;

namespace BabyRadnik.Tests.Domain.Scenarios;

public sealed class ScenarioOverlayTests
{
    [Fact]
    public void Odchodze_z_pracy_closes_employee_title_and_sets_reason_without_mutating_input()
    {
        var input = PregnantEmployee();
        var result = Apply(new OdchodzeZPracyScenario(), input, """{"data":"2026-11-15"}""");

        var title = Assert.Single(result.Wnioskodawca.UbezpieczenieTytuly!.Value);
        Assert.Equal(D("2026-11-15"), title.Do);
        Assert.Equal(TitleKind.Pracownik, title.RodzajTytulu);
        Assert.Equal(FactSource.Assumption, result.Wnioskodawca.UbezpieczenieTytuly!.Source);
        Assert.Equal(InsuranceEndReason.WypowiedzeniePrzezPracownika, result.Wnioskodawca.UbezpieczenieUstaniePrzyczyna!.Value);
        Assert.Equal(FactSource.Assumption, result.Wnioskodawca.UbezpieczenieUstaniePrzyczyna!.Source);
        Assert.Null(input.Wnioskodawca.UbezpieczenieTytuly!.Value.Single().Do);
        Assert.Null(input.Wnioskodawca.UbezpieczenieUstaniePrzyczyna);
        Assert.Equal(input.Sprawa, result.Sprawa);
    }

    [Fact]
    public void Odchodze_z_pracy_changes_only_title_list_and_end_reason()
    {
        var input = PregnantEmployee(contractEnd: "2026-09-30");
        var result = Apply(new OdchodzeZPracyScenario(), input, """{"data":"2026-11-15"}""");

        Assert.Equal(
            input.Wnioskodawca with
            {
                UbezpieczenieTytuly = result.Wnioskodawca.UbezpieczenieTytuly,
                UbezpieczenieUstaniePrzyczyna = result.Wnioskodawca.UbezpieczenieUstaniePrzyczyna,
            },
            result.Wnioskodawca);
    }

    [Theory]
    [InlineData(null, "scenariusz_brak_parametru")]
    [InlineData("""{}""", "scenariusz_brak_parametru")]
    [InlineData("""{"data":"jutro"}""", "scenariusz_zly_parametr")]
    [InlineData("""{"data":"2026-11-15","dodatkowy":1}""", "scenariusz_nieznany_parametr")]
    public void Odchodze_z_pracy_rejects_bad_params(string? json, string code)
    {
        var outcome = new OdchodzeZPracyScenario().Apply(Context(PregnantEmployee(), json));

        Assert.False(outcome.Succeeded);
        Assert.Contains(outcome.Errors, e => e.Code == code);
    }

    [Fact]
    public void Odchodze_z_pracy_is_not_applicable_without_employee_title()
    {
        var facts = PregnantEmployee() with
        {
            Wnioskodawca = new PersonFacts
            {
                OsobaRola = F(ParentRole.Matka),
                UbezpieczenieTytuly = F<IReadOnlyList<InsuranceTitle>>(
                    [new InsuranceTitle(TitleKind.Dzialalnosc, D("2020-01-01"), null, SicknessInsurance.Dobrowolne)]),
            },
        };

        var outcome = new OdchodzeZPracyScenario().Apply(Context(facts, """{"data":"2026-11-15"}"""));

        Assert.Equal("scenariusz_nie_dotyczy", Assert.Single(outcome.Errors).Code);
    }

    [Fact]
    public void Odchodze_z_pracy_requires_known_titles()
    {
        var facts = PregnantEmployee() with { Wnioskodawca = new PersonFacts { OsobaRola = F(ParentRole.Matka) } };

        var outcome = new OdchodzeZPracyScenario().Apply(Context(facts, """{"data":"2026-11-15"}"""));

        var error = Assert.Single(outcome.Errors);
        Assert.Equal("scenariusz_brak_faktu", error.Code);
        Assert.Equal(FactKeys.UbezpieczenieTytuly, error.Fact);
    }

    [Fact]
    public void Odchodze_z_pracy_for_second_parent_without_facts_is_not_applicable()
    {
        var outcome = new OdchodzeZPracyScenario().Apply(Context(PregnantEmployee(), """{"data":"2026-11-15"}""", PersonRef.DrugiRodzic));

        var error = Assert.Single(outcome.Errors);
        Assert.Equal("scenariusz_nie_dotyczy", error.Code);
        Assert.Equal(PersonRef.DrugiRodzic, error.Person);
    }

    [Fact]
    public void Umowa_nie_zostanie_przedluzona_ends_title_on_contract_end_date()
    {
        var input = PregnantEmployee(contractEnd: "2026-09-30");
        var result = Apply(new UmowaNieZostaniePrzedluzonaScenario(), input, null);

        Assert.Equal(D("2026-09-30"), result.Wnioskodawca.UbezpieczenieTytuly!.Value.Single().Do);
        Assert.Equal(InsuranceEndReason.KoniecUmowyNaCzasOkreslony, result.Wnioskodawca.UbezpieczenieUstaniePrzyczyna!.Value);
        Assert.Equal(FactSource.Assumption, result.Wnioskodawca.UbezpieczenieUstaniePrzyczyna!.Source);
        Assert.Equal(input.Wnioskodawca.UmowaDataKonca, result.Wnioskodawca.UmowaDataKonca);
    }

    [Fact]
    public void Umowa_nie_zostanie_przedluzona_requires_contract_end_date()
    {
        var outcome = new UmowaNieZostaniePrzedluzonaScenario().Apply(Context(PregnantEmployee(), null));

        var error = Assert.Single(outcome.Errors);
        Assert.Equal(FactKeys.UmowaDataKonca, error.Fact);
    }

    [Fact]
    public void Umowa_nie_zostanie_przedluzona_is_not_applicable_to_indefinite_contract()
    {
        var facts = PregnantEmployee(contractEnd: "2026-09-30");
        facts = facts with { Wnioskodawca = facts.Wnioskodawca with { UmowaNaCzasOkreslony = F(false) } };

        var outcome = new UmowaNieZostaniePrzedluzonaScenario().Apply(Context(facts, null));

        Assert.Equal("scenariusz_nie_dotyczy", Assert.Single(outcome.Errors).Code);
    }

    [Fact]
    public void Umowa_nie_zostanie_przedluzona_rejects_params()
    {
        var outcome = new UmowaNieZostaniePrzedluzonaScenario().Apply(Context(PregnantEmployee(contractEnd: "2026-09-30"), """{"data":"2026-09-30"}"""));

        Assert.Contains(outcome.Errors, e => e.Code == "scenariusz_nieznany_parametr");
    }

    [Theory]
    [InlineData(true, SicknessInsurance.Dobrowolne)]
    [InlineData(false, SicknessInsurance.Brak)]
    public void Przechodze_na_dzialalnosc_ends_employee_title_and_adds_business_title(bool voluntary, SicknessInsurance sickness)
    {
        var json = $$"""{"data":"2026-11-01","dobrowolneChorobowe":{{(voluntary ? "true" : "false")}}}""";
        var result = Apply(new PrzechodzeNaDzialalnoscScenario(), PregnantEmployee(), json);

        var titles = result.Wnioskodawca.UbezpieczenieTytuly!.Value;
        Assert.Equal(2, titles.Count);
        Assert.Equal(D("2026-10-31"), titles[0].Do);
        Assert.Equal(TitleKind.Dzialalnosc, titles[1].RodzajTytulu);
        Assert.Equal(D("2026-11-01"), titles[1].Od);
        Assert.Null(titles[1].Do);
        Assert.Equal(sickness, titles[1].Chorobowe);
        Assert.Equal(FactSource.Assumption, result.Wnioskodawca.UbezpieczenieTytuly!.Source);
        Assert.Equal(InsuranceEndReason.Inna, result.Wnioskodawca.UbezpieczenieUstaniePrzyczyna!.Value);
    }

    [Fact]
    public void Przechodze_na_dzialalnosc_requires_both_params()
    {
        var outcome = new PrzechodzeNaDzialalnoscScenario().Apply(Context(PregnantEmployee(), """{"data":"2026-11-01"}"""));

        Assert.Contains(outcome.Errors, e => e.Code == "scenariusz_brak_parametru" && e.Message.Contains("dobrowolneChorobowe"));
    }

    [Fact]
    public void Przechodze_na_dzialalnosc_rejects_switch_on_first_day_of_employment()
    {
        var outcome = new PrzechodzeNaDzialalnoscScenario().Apply(Context(PregnantEmployee(), """{"data":"2025-01-01","dobrowolneChorobowe":true}"""));

        Assert.Equal("scenariusz_nie_dotyczy", Assert.Single(outcome.Errors).Code);
    }

    [Fact]
    public void Drugi_rodzic_bierze_splits_parental_leave_between_parents()
    {
        var input = BirthWithBothParents();
        var result = Apply(new DrugiRodzicBierzeScenario(), input, """{"tygodnie":20}""");

        Assert.Equal(20, result.DrugiRodzic!.PlanRodzicielskiCzesci!.Value.Single().Tygodnie);
        Assert.Equal(21, result.Wnioskodawca.PlanRodzicielskiCzesci!.Value.Single().Tygodnie);
        Assert.Equal(FactSource.Assumption, result.DrugiRodzic.PlanRodzicielskiCzesci!.Source);
        Assert.Null(input.DrugiRodzic!.PlanRodzicielskiCzesci);
    }

    [Fact]
    public void Drugi_rodzic_bierze_from_second_parent_perspective_gives_weeks_to_applicant()
    {
        var result = Apply(new DrugiRodzicBierzeScenario(), BirthWithBothParents(), """{"tygodnie":12}""", PersonRef.DrugiRodzic);

        Assert.Equal(12, result.Wnioskodawca.PlanRodzicielskiCzesci!.Value.Single().Tygodnie);
        Assert.Equal(29, result.DrugiRodzic!.PlanRodzicielskiCzesci!.Value.Single().Tygodnie);
    }

    [Fact]
    public void Drugi_rodzic_bierze_needs_second_parent_facts()
    {
        var outcome = new DrugiRodzicBierzeScenario().Apply(Context(BirthWithBothParents() with { DrugiRodzic = null }, """{"tygodnie":20}"""));

        Assert.Equal("scenariusz_nie_dotyczy", Assert.Single(outcome.Errors).Code);
    }

    [Fact]
    public void Drugi_rodzic_bierze_asks_for_facts_needed_for_total_leave()
    {
        var facts = BirthWithBothParents();
        facts = facts with { Sprawa = facts.Sprawa with { DzieckoLiczba = null } };

        var outcome = new DrugiRodzicBierzeScenario().Apply(Context(facts, """{"tygodnie":20}"""));

        Assert.Contains(outcome.Errors, e => e.Code == "scenariusz_brak_faktu" && e.Fact == FactKeys.DzieckoLiczba);
    }

    [Fact]
    public void Drugi_rodzic_bierze_rejects_negative_weeks()
    {
        var outcome = new DrugiRodzicBierzeScenario().Apply(Context(BirthWithBothParents(), """{"tygodnie":-1}"""));

        Assert.Contains(outcome.Errors, e => e.Code == "scenariusz_zly_parametr");
    }

    [Fact]
    public void Praca_na_czesc_etatu_sets_work_plan_only()
    {
        var input = BirthWithBothParents();
        var result = Apply(new PracaNaCzescEtatuScenario(), input, """{"wymiar":0.5,"tygodnie":20}""");

        var work = Assert.Single(result.Wnioskodawca.PlanPracaNaCzescEtatu!.Value);
        Assert.Equal(0.5m, work.Wymiar);
        Assert.Equal(20, work.Tygodnie);
        Assert.Equal(FactSource.Assumption, result.Wnioskodawca.PlanPracaNaCzescEtatu!.Source);
        Assert.Equal(input.DrugiRodzic, result.DrugiRodzic);
        Assert.Null(input.Wnioskodawca.PlanPracaNaCzescEtatu);
    }

    [Theory]
    [InlineData("""{"wymiar":0.6,"tygodnie":20}""", "praca_na_rodzicielskim_wymiar")]
    [InlineData("""{"wymiar":1,"tygodnie":20}""", "praca_na_rodzicielskim_wymiar")]
    [InlineData("""{"wymiar":0,"tygodnie":20}""", "scenariusz_zly_parametr")]
    [InlineData("""{"wymiar":0.5,"tygodnie":0}""", "scenariusz_zly_parametr")]
    [InlineData("""{"wymiar":0.5}""", "scenariusz_brak_parametru")]
    [InlineData("""{"tygodnie":10}""", "scenariusz_brak_parametru")]
    public void Praca_na_czesc_etatu_rejects_invalid_params(string json, string code)
    {
        var outcome = new PracaNaCzescEtatuScenario().Apply(Context(BirthWithBothParents(), json));

        Assert.Contains(outcome.Errors, e => e.Code == code);
    }

    [Fact]
    public void Praca_na_czesc_etatu_limit_comes_from_parameter()
    {
        var parameters = SeedParameterProvider.Load(
        [
            new SeedRow("praca_na_rodzicielskim_max_etat", JsonSerializer.SerializeToElement(0.75m), new DateOnly(2026, 1, 1), "test"),
        ]);
        var context = new ScenarioContext(BirthWithBothParents(), PersonRef.Wnioskodawca, P("""{"wymiar":0.7,"tygodnie":10}"""), AsOf, parameters);

        var outcome = new PracaNaCzescEtatuScenario().Apply(context);

        Assert.True(outcome.Succeeded);
    }

    [Theory]
    [InlineData("standardowy", PaymentVariant.Standardowy)]
    [InlineData("usredniony", PaymentVariant.Usredniony)]
    public void Wariant_wyplaty_sets_payment_variant(string name, PaymentVariant expected)
    {
        var result = Apply(new WariantWyplatyScenario(), BirthWithBothParents(), $$"""{"wariant":"{{name}}"}""");

        Assert.Equal(expected, result.Wnioskodawca.PlanWariantWyplaty!.Value);
        Assert.Equal(FactSource.Assumption, result.Wnioskodawca.PlanWariantWyplaty!.Source);
    }

    [Theory]
    [InlineData("""{"wariant":"inny"}""")]
    [InlineData("""{}""")]
    public void Wariant_wyplaty_rejects_invalid_value(string json)
    {
        var outcome = new WariantWyplatyScenario().Apply(Context(BirthWithBothParents(), json));

        Assert.False(outcome.Succeeded);
    }

    [Fact]
    public void Splacam_zadluzenie_sets_repayment_date()
    {
        var input = MotherWithDebt();
        var result = Apply(new SplacamZadluzenieScenario(), input, """{"data":"2026-11-30"}""");

        Assert.Equal(D("2026-11-30"), result.Wnioskodawca.SkladkiDataSplaty!.Value);
        Assert.Equal(FactSource.Assumption, result.Wnioskodawca.SkladkiDataSplaty!.Source);
        Assert.Null(input.Wnioskodawca.SkladkiDataSplaty);
        Assert.Equal(input.Wnioskodawca.SkladkiZadluzenie, result.Wnioskodawca.SkladkiZadluzenie);
    }

    [Fact]
    public void Splacam_zadluzenie_is_not_applicable_without_known_debt()
    {
        var outcome = new SplacamZadluzenieScenario().Apply(Context(PregnantEmployee(), """{"data":"2026-11-30"}"""));

        var error = Assert.Single(outcome.Errors);
        Assert.Equal(FactKeys.SkladkiZadluzenie, error.Fact);
    }

    [Fact]
    public void Splacam_zadluzenie_requires_date()
    {
        var outcome = new SplacamZadluzenieScenario().Apply(Context(MotherWithDebt(), null));

        Assert.Equal("scenariusz_brak_parametru", Assert.Single(outcome.Errors).Code);
    }
}
