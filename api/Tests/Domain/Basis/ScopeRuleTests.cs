using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Basis.BasisKit;

namespace BabyRadnik.Tests.Domain.Basis;

public sealed class ScopeRuleTests
{
    [Fact]
    public void Payer_basis_is_taken_as_is_and_beats_the_estimate()
    {
        var person = FullTimeEmployee(8000) with { PodstawaUstalonaPrzezPlatnika = new Fact<decimal>(3000m, FactSource.User) };

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(3000m, result.Basis);
        Assert.Equal(BasisSource.UstalonaPrzezPlatnika, result.Source);
        Assert.False(result.IsEstimate);
        Assert.Empty(result.NotCounted);
        Assert.Equal("podstawa_ustalona_przez_platnika", result.Trace[0].Rule);
        Assert.Contains("nie pomniejszam jej ponownie", result.Trace[0].Text);
    }

    [Fact]
    public void Payer_basis_works_without_any_zus_data()
    {
        var result = Assess(new PersonFacts { PodstawaUstalonaPrzezPlatnika = new Fact<decimal>(3000m, FactSource.User) });

        Assert.Equal(3000m, result.Basis);
        Assert.Equal(BasisSource.UstalonaPrzezPlatnika, result.Source);
    }

    [Fact]
    public void Non_positive_payer_basis_is_ignored()
    {
        var result = Assess(FullTimeEmployee(8000) with { PodstawaUstalonaPrzezPlatnika = new Fact<decimal>(0m, FactSource.User) });

        Assert.Equal(6903.20m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
        Assert.Contains("nie jest dodatnia", result.Trace[0].Text);
    }

    [Fact]
    public void Art43_gap_shorter_than_calendar_month_keeps_the_basis_of_the_first_benefit()
    {
        var person = Person(
            [Employee("2020-01-01")],
            Contributions("2025-06", 12, 6000),
            [Benefit(ReceivedBenefitKind.Chorobowy, "2026-06-01", "2026-09-01")]);

        var result = Assess(person, "2026-09-02");

        Assert.Equal(D("2026-06-01"), result.ReferenceDate);
        Assert.Equal(5177.40m, result.Basis);
        var step = Assert.Single(result.Trace, t => t.Rule == "art43_brak_przeliczenia");
        Assert.Contains("nie ustala się na nowo", step.Text);
        Assert.Contains(BasisTexts.NotCountedWorkingTimeChange, result.NotCounted);
    }

    [Fact]
    public void Art43_gap_of_exactly_one_calendar_month_recalculates()
    {
        var person = Person(
            [Employee("2020-01-01")],
            Contributions("2025-08", 12, 6000),
            [Benefit(ReceivedBenefitKind.Chorobowy, "2026-06-01", "2026-07-15")]);

        var chained = Assess(person, "2026-08-15");
        var recalculated = Assess(person, "2026-08-16");

        Assert.Equal(D("2026-06-01"), chained.ReferenceDate);
        Assert.Equal(BasisOutcome.MissingData, chained.Outcome);
        Assert.Equal(D("2026-08-16"), recalculated.ReferenceDate);
        Assert.Equal(BasisOutcome.Determined, recalculated.Outcome);
    }

    [Fact]
    public void Art43_chain_follows_several_benefits_back()
    {
        var benefits = new[]
        {
            Benefit(ReceivedBenefitKind.Chorobowy, "2026-06-01", "2026-07-31"),
            Benefit(ReceivedBenefitKind.Macierzynski, "2026-08-01", "2026-10-31"),
        };

        var result = Assess(Person([Employee("2020-01-01")], Contributions("2025-06", 12, 6000), benefits), "2026-11-10");

        Assert.Equal(D("2026-06-01"), result.ReferenceDate);
        Assert.Equal(5177.40m, result.Basis);
    }

    [Fact]
    public void Art43_wynagrodzenie_chorobowe_and_other_benefits_do_not_chain()
    {
        var benefits = new[]
        {
            Benefit(ReceivedBenefitKind.WynagrodzenieChorobowe, "2026-02-01", "2026-03-01"),
            Benefit(ReceivedBenefitKind.Inne, "2026-02-01", "2026-03-01"),
        };

        var result = Assess(Person([Employee("2020-01-01")], Contributions("2025-03", 12, 8000), benefits));

        Assert.Equal(D(Birth), result.ReferenceDate);
        Assert.Equal(6903.20m, result.Basis);
    }

    [Fact]
    public void Art43_chain_without_data_for_the_earlier_window_asks_for_the_months()
    {
        var person = Person(
            [Employee("2020-01-01")],
            Contributions("2025-03", 12, 8000),
            [Benefit(ReceivedBenefitKind.Chorobowy, "2026-01-10", "2026-03-01")]);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.MissingData, result.Outcome);
        Assert.Null(result.Basis);
        Assert.Contains(result.MissingFacts, m => m.Fact == FactKeys.UbezpieczeniePodstawySkladek);
        Assert.Contains("01.2025", result.Message);
    }

    [Fact]
    public void Art43_is_not_checked_when_benefits_are_unknown_and_that_is_said()
    {
        var person = Person([Employee("2020-01-01")], Contributions("2025-03", 12, 8000));

        var result = Assess(person);

        Assert.Equal(6903.20m, result.Basis);
        Assert.Contains(BasisTexts.NotCountedBenefitsUnknown, result.NotCounted);
        Assert.Contains(BasisTexts.NotCountedAbsencesUnknown, result.NotCounted);
    }

    [Fact]
    public void Title_missing_facts_ask_for_titles_and_point_to_payer_basis()
    {
        var result = Assess(new PersonFacts());

        Assert.Equal(BasisOutcome.MissingData, result.Outcome);
        Assert.Contains(result.MissingFacts, m => m.Fact == FactKeys.UbezpieczenieTytuly);
        Assert.Equal(FactKeys.PodstawaUstalonaPrzezPlatnika, result.MissingFacts[^1].Fact);
        Assert.Contains(BasisTexts.Referral, result.Message);
    }

    [Fact]
    public void Title_no_sickness_insurance_on_the_day_means_no_basis()
    {
        var person = Person([Employee("2020-01-01", "2026-01-31")], Contributions("2025-03", 12, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.NoTitle, result.Outcome);
        Assert.Null(result.Basis);
        Assert.Contains("art. 30, art. 46", result.Message);
        Assert.Contains(BasisTexts.Referral, result.Message);
    }

    [Fact]
    public void Title_two_concurrent_titles_are_not_decided()
    {
        var person = Person([Employee("2020-01-01"), Contractor("2025-01-01")], Contributions("2025-03", 12, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.StartsWith("Nie rozstrzygam. ", result.Message);
        Assert.Contains("zbieg tytułów", result.Message);
        Assert.Contains(BasisTexts.OutOfScope, result.Message);
        Assert.EndsWith(BasisTexts.Referral, result.Message);
        Assert.Equal(BasisTexts.OutOfScopeItems, result.NotCounted);
        Assert.Equal(FactKeys.PodstawaUstalonaPrzezPlatnika, Assert.Single(result.MissingFacts).Fact);
    }

    [Fact]
    public void Title_business_without_sickness_insurance_next_to_employment_does_not_block_the_employee_basis()
    {
        var business = new InsuranceTitle(TitleKind.Dzialalnosc, D("2024-01-01"), null, SicknessInsurance.Brak);
        var result = Assess(Person([Employee("2020-01-01"), business], Contributions("2025-03", 12, 8000), [], []));

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(Assess(FullTimeEmployee(8000)).Basis, result.Basis);
    }

    [Fact]
    public void Title_code_change_of_the_same_business_without_a_break_counts_all_twelve_months()
    {
        var contributions = Override(Override(Contributions("2025-03", 12, 1000), "2026-01", 5000), "2026-02", 5000);
        var changed = Person(
            [Business("2024-01-01", to: "2025-12-31") with { KodTytulu = "0590" }, Business("2026-01-01") with { KodTytulu = "0510" }],
            contributions, [], []);
        var continuous = Person([Business("2024-01-01")], contributions, [], []);

        var result = Assess(changed);

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(12, result.Months.Count(m => m.Included));
        Assert.Equal(Assess(continuous).Basis, result.Basis);
    }

    [Fact]
    public void Title_business_restarted_after_a_break_is_still_not_decided()
    {
        var person = Person(
            [Business("2024-01-01", to: "2025-12-15"), Business("2026-01-01")],
            Contributions("2025-03", 12, 1000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 48a ust. 2–4", result.Message);
    }

    [Fact]
    public void Title_employer_change_without_a_break_is_not_decided_because_only_the_current_payer_counts()
    {
        var person = Person(
            [Employee("2020-01-01", "2025-10-31") with { PlatnikId = "1111111111" }, Employee("2025-11-01") with { PlatnikId = "2222222222" }],
            Contributions("2025-03", 12, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("Zmiana pracodawcy", result.Message);
        Assert.Contains("art. 36 ust. 4", result.Message);
    }

    [Fact]
    public void Title_new_contract_with_the_same_employer_counts_all_twelve_months()
    {
        var person = Person(
            [Employee("2020-01-01", "2025-10-31") with { PlatnikId = "1111111111" }, Employee("2025-11-01") with { PlatnikId = "1111111111" }],
            Contributions("2025-03", 12, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(6903.20m, result.Basis);
        Assert.Equal(12, result.Months.Count(m => m.Included));
        Assert.DoesNotContain(result.Trace, t => t.Text.Contains("brak identyfikatora płatnika", StringComparison.Ordinal));
    }

    [Fact]
    public void Title_of_a_benefit_recipient_is_not_decided()
    {
        var title = new InsuranceTitle(TitleKind.PobierajacyZasilek, D("2025-01-01"), null, SicknessInsurance.Obowiazkowe);

        var result = Assess(Person([title], Contributions("2025-03", 12, 8000), [], []));

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
    }

    [Fact]
    public void Title_consecutive_employments_with_unknown_payer_are_chained_and_the_assumption_is_said()
    {
        var person = Person(
            [Employee("2020-01-01", "2025-10-31"), Employee("2025-11-01")],
            Contributions("2025-03", 12, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(6903.20m, result.Basis);
        Assert.Equal(12, result.Months.Count(m => m.Included));
        Assert.Contains(result.Trace, t => t.Rule == "tytul_ubezpieczenia_podstawy" && t.Text.Contains("brak identyfikatora płatnika", StringComparison.Ordinal));
    }

    [Fact]
    public void Title_break_of_up_to_30_days_before_the_current_title_is_not_decided()
    {
        var person = Person(
            [Employee("2020-01-01", "2025-09-20"), Employee("2025-10-05")],
            Contributions("2025-10", 5, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 36 ust. 4", result.Message);
    }

    [Fact]
    public void Title_break_longer_than_30_days_starts_a_new_period()
    {
        var person = Person(
            [Employee("2020-01-01", "2025-08-31"), Employee("2025-10-15")],
            Contributions("2025-11", 4, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(4, result.Months.Count);
    }

    [Fact]
    public void Title_working_time_change_in_the_window_is_not_decided()
    {
        var person = Person(
            [Employee("2020-01-01", "2025-10-31"), Employee("2025-11-01", workingTime: 0.5m)],
            Contributions("2025-03", 12, 8000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 40", result.Message);
    }

    [Fact]
    public void Title_insurance_break_shortens_the_continuous_period()
    {
        var breaks = new[] { new InsuranceBreak(D("2025-12-10"), D("2026-01-20")) };
        var person = Person([Employee("2020-01-01")], Contributions("2025-03", 12, 8000), [], [], breaks: breaks);

        var result = Assess(person);

        Assert.Single(result.Months);
        Assert.Equal("2026-02", result.Months[0].Month);
    }

    [Fact]
    public void Every_rule_has_id_and_legal_basis()
    {
        var rules = typeof(BasisRules).GetFields().Select(f => f.GetValue(null)).OfType<IRule>().ToList();

        Assert.Equal(11, rules.Count);
        Assert.All(rules, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Id));
            Assert.False(string.IsNullOrWhiteSpace(r.PodstawaPrawna));
        });
        Assert.Equal(rules.Count, rules.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void Rule_can_be_evaluated_on_its_own_through_the_common_contract()
    {
        var context = new EvaluationContext(
            Facts(FullTimeEmployee(8000)), PersonRef.Wnioskodawca, D(Birth), BabyRadnik.Tests.Reference.SeedParameterProvider.Load());

        var result = BasisRules.EmployeeMinimum.Evaluate(context);

        Assert.Equal(RuleVerdict.Met, result.Verdict);
    }

    [Fact]
    public void Missing_parameter_for_the_date_is_not_decided_with_the_key_named()
    {
        var person = Person([Employee("2020-01-01")], Contributions("2024-06", 12, 8000), [], []);

        var result = Assess(person, "2025-06-10");

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("podstawa_minimalna_pelny_etat", result.Message);
        Assert.Contains("10.06.2025", result.Message);
        Assert.Null(result.Basis);
    }
}
