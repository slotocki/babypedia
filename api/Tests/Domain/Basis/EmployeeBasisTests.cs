using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Basis.BasisKit;

namespace BabyRadnik.Tests.Domain.Basis;

public sealed class EmployeeBasisTests
{
    [Fact]
    public void Twelve_months_of_8000_give_6903_20_as_zus_estimate()
    {
        var result = Assess(FullTimeEmployee(8000));

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(6903.20m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
        Assert.True(result.IsEstimate);
        Assert.Equal(12, result.Months.Count(m => m.Included));
        Assert.Equal(["2025-03", "2026-02"], [result.Months[0].Month, result.Months[^1].Month]);
    }

    [Fact]
    public void Average_is_taken_over_months_not_over_the_last_value()
    {
        var contributions = Override(Contributions("2025-03", 12, 8000), "2026-02", 5000);

        var result = Assess(Person([Employee("2020-01-01")], contributions, [], []));

        Assert.Equal(Math.Round((11 * 8000m + 5000m) / 12 * (1 - 0.1371m), 2), result.Basis);
    }

    [Fact]
    public void Trace_lists_rules_in_order_with_legal_basis()
    {
        var result = Assess(FullTimeEmployee(8000));

        Assert.Equal(
            [
                "podstawa_ustalona_przez_platnika", "art43_brak_przeliczenia", "tytul_ubezpieczenia_podstawy", "pelne_miesiace_ubezpieczenia",
                "miesiace_z_nieobecnoscia", "srednia_wynagrodzen_pracownika", "minimalna_podstawa_pracownika",
            ],
            result.Trace.Select(t => t.Rule));
        Assert.All(result.Trace, t => Assert.False(string.IsNullOrWhiteSpace(t.Basis)));
        Assert.Contains("6903,20 zł", result.Trace.Single(t => t.Rule == "srednia_wynagrodzen_pracownika").Text);
    }

    [Fact]
    public void Shorter_employment_uses_only_full_calendar_months_of_insurance()
    {
        var person = Person([Employee("2025-09-10")], Contributions("2025-09", 6, 5000), [], []);

        var result = Assess(person);

        Assert.Equal(4314.50m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
        Assert.Equal(["2025-10", "2025-11", "2025-12", "2026-01", "2026-02"], result.Months.Select(m => m.Month));
        Assert.Contains("5 pełnych miesięcy", result.Trace.Single(t => t.Rule == "srednia_wynagrodzen_pracownika").Text);
    }

    [Fact]
    public void Employment_shorter_than_one_full_month_is_not_decided()
    {
        var person = Person([Employee("2026-02-10")], Contributions("2026-02", 1, 5000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 37", result.Message);
        Assert.Null(result.Basis);
    }

    [Fact]
    public void Missing_contribution_month_asks_for_it_instead_of_averaging_the_rest()
    {
        var contributions = Contributions("2025-03", 12, 8000).Where(c => c.Miesiac != "2025-08").ToList();

        var result = Assess(Person([Employee("2020-01-01")], contributions, [], []));

        Assert.Equal(BasisOutcome.MissingData, result.Outcome);
        Assert.Null(result.Basis);
        Assert.Contains("2025-08", result.Message);
        Assert.Contains(result.MissingFacts, m => m.Fact == FactKeys.UbezpieczeniePodstawySkladek && m.Question.Contains("2025-08"));
    }

    [Fact]
    public void Contribution_facts_missing_entirely_is_missing_data()
    {
        var result = Assess(Person([Employee("2020-01-01")], null, [], []));

        Assert.Equal(BasisOutcome.MissingData, result.Outcome);
        Assert.Contains(result.MissingFacts, m => m.Fact == FactKeys.UbezpieczeniePodstawySkladek);
    }

    [Fact]
    public void Two_entries_for_one_month_are_not_decided()
    {
        var contributions = Contributions("2025-03", 12, 8000).Append(new ContributionBase("2025-05", 100)).ToList();

        var result = Assess(Person([Employee("2020-01-01")], contributions, [], []));

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("2025-05", result.Message);
    }

    [Fact]
    public void Month_fully_covered_by_a_benefit_is_left_out_of_the_average()
    {
        var contributions = Override(Contributions("2025-03", 12, 8000), "2025-06", 100);
        var benefits = new[] { Benefit(ReceivedBenefitKind.Macierzynski, "2025-06-01", "2025-06-30") };

        var result = Assess(Person([Employee("2020-01-01")], contributions, benefits, []));

        Assert.Equal(6903.20m, result.Basis);
        var june = result.Months.Single(m => m.Month == "2025-06");
        Assert.False(june.Included);
        Assert.Contains("pominięty", june.Remark);
        Assert.Equal(11, result.Months.Count(m => m.Included));
    }

    [Fact]
    public void Consecutive_leaves_are_merged_before_checking_a_month()
    {
        var contributions = Override(Contributions("2025-03", 12, 8000), "2025-07", 200);
        var leaves = new[] { Leave("2025-07-01", "2025-07-15"), Leave("2025-07-16", "2025-07-31") };

        var result = Assess(Person([Employee("2020-01-01")], contributions, [], leaves));

        Assert.Equal(6903.20m, result.Basis);
        Assert.False(result.Months.Single(m => m.Month == "2025-07").Included);
    }

    [Fact]
    public void Partial_absence_keeps_the_month_and_says_it_is_not_recognised()
    {
        var contributions = Override(Contributions("2025-03", 12, 8000), "2025-07", 6000);
        var leaves = new[] { Leave("2025-07-10", "2025-07-20") };

        var result = Assess(Person([Employee("2020-01-01")], contributions, [], leaves));

        Assert.Equal(Math.Round((11 * 8000m + 6000m) / 12 * (1 - 0.1371m), 2), result.Basis);
        var july = result.Months.Single(m => m.Month == "2025-07");
        Assert.True(july.Included);
        Assert.Contains("częściowa nieobecność", july.Remark);
        Assert.Contains(BasisTexts.NotCountedPartialAbsence, result.NotCounted);
    }

    [Fact]
    public void Every_month_absent_for_an_employee_needs_supplementing_and_is_not_decided()
    {
        var leaves = new[] { Leave("2025-03-01", "2026-02-28") };

        var result = Assess(Person([Employee("2020-01-01")], Contributions("2025-03", 12, 800), [], leaves));

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 38 ust. 3", result.Message);
    }

    [Fact]
    public void Minimum_for_full_time_is_4147_10()
    {
        var result = Assess(FullTimeEmployee(1000));

        Assert.Equal(4147.10m, result.Basis);
        Assert.Equal(BasisSource.Minimalna, result.Source);
        Assert.True(result.IsEstimate);
        Assert.Contains(result.Notes, n => n.Contains("do potwierdzenia z ZUS"));
    }

    [Fact]
    public void Minimum_for_half_time_is_2073_55_and_the_proportion_is_flagged_as_assumption()
    {
        var person = Person([Employee("2020-01-01", workingTime: 0.5m)], Contributions("2025-03", 12, 1000), [], []);

        var result = Assess(person);

        Assert.Equal(2073.55m, result.Basis);
        Assert.Equal(BasisSource.Minimalna, result.Source);
        Assert.Contains(result.Notes, n => n.Contains("Art. 45 ust. 1") && n.Contains("założenie do potwierdzenia"));
    }

    [Fact]
    public void Half_time_average_above_the_proportional_minimum_stays_an_estimate()
    {
        var person = Person([Employee("2020-01-01", workingTime: 0.5m)], Contributions("2025-03", 12, 3000), [], []);

        var result = Assess(person);

        Assert.Equal(2588.70m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
    }

    [Fact]
    public void Unknown_working_time_matters_only_when_the_average_is_below_the_full_time_minimum()
    {
        var below = Assess(Person([Employee("2020-01-01", workingTime: null)], Contributions("2025-03", 12, 1000), [], []));
        var above = Assess(Person([Employee("2020-01-01", workingTime: null)], Contributions("2025-03", 12, 8000), [], []));

        Assert.Equal(BasisOutcome.MissingData, below.Outcome);
        Assert.Contains(below.MissingFacts, m => m.Question.Contains("wymiar etatu"));
        Assert.Equal(6903.20m, above.Basis);
    }

    [Fact]
    public void Working_time_outside_the_range_is_not_decided()
    {
        var result = Assess(Person([Employee("2020-01-01", workingTime: 1.5m)], Contributions("2025-03", 12, 1000), [], []));

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
    }

    [Fact]
    public void Estimate_lists_exactly_what_is_not_counted()
    {
        var result = Assess(FullTimeEmployee(8000));

        Assert.Equal(
            [
                BasisTexts.NotCountedPartialAbsence,
                BasisTexts.NotCountedQuarterlyAnnual,
                BasisTexts.NotCountedExcludedComponents,
                BasisTexts.NotCountedEstimate,
            ],
            result.NotCounted);
        Assert.Contains("Szacunek podstawy z danych ZUS", result.Message);
        Assert.Contains("podstawa ustalona przez płatnika", result.Message);
        Assert.Contains(result.Notes, n => n.Contains("13,71%"));
    }

    [Fact]
    public void Parameter_valid_from_a_later_date_does_not_change_an_earlier_result()
    {
        var later = Row(BasisParameterKeys.PodstawaMinimalnaPelnyEtat, 9999, "2027-01-01");
        var person = FullTimeEmployee(1000);

        var in2026 = Assess(person, service: Service(later));
        var in2027 = Assess(Person([Employee("2020-01-01")], Contributions("2026-01", 12, 1000), [], []), "2027-01-15", Service(later));

        Assert.Equal(4147.10m, in2026.Basis);
        Assert.Equal(9999m, in2027.Basis);
    }
}
