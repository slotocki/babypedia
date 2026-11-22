using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Basis.BasisKit;

namespace BabyRadnik.Tests.Domain.Basis;

public sealed class NonEmployeeBasisTests
{
    [Fact]
    public void Twelve_months_of_declared_base_give_average_income_without_minimum()
    {
        var result = Assess(Voluntary(1000));

        Assert.Equal(862.90m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
        Assert.True(result.IsEstimate);
        Assert.Contains(BasisTexts.NotCountedPartialActivity, result.NotCounted);
        Assert.DoesNotContain(result.Trace, t => t.Rule == "minimalna_podstawa_pracownika");
    }

    [Fact]
    public void Declared_5000_gives_4314_50()
    {
        var result = Assess(Voluntary(5000));

        Assert.Equal(4314.50m, result.Basis);
        Assert.Equal(
            ["podstawa_ustalona_przez_platnika", "art43_brak_przeliczenia", "tytul_ubezpieczenia_podstawy", "pelne_miesiace_ubezpieczenia",
             "miesiace_z_nieobecnoscia", "srednia_przychodu_niepracownika", "limit_podstawy_dobrowolnej"],
            result.Trace.Select(t => t.Rule));
    }

    [Fact]
    public void Voluntary_insurance_above_the_limit_is_capped_at_20321_30()
    {
        var result = Assess(Voluntary(25000));

        Assert.Equal(20321.30m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
        Assert.Contains(result.Notes, n => n.Contains("23 550,00 zł") && n.Contains("20 321,30 zł"));
        Assert.Contains("limit", result.Trace[^1].Text);
    }

    [Fact]
    public void Voluntary_insurance_at_the_limit_is_not_changed()
    {
        var result = Assess(Voluntary(23550));

        Assert.Equal(20321.30m, result.Basis);
        Assert.DoesNotContain(result.Notes, n => n.Contains("ograniczona do limitu"));
    }

    [Fact]
    public void Limit_does_not_apply_to_compulsory_sickness_insurance()
    {
        var person = Person([Contractor("2020-01-01", SicknessInsurance.Obowiazkowe)], Contributions("2025-03", 12, 25000), [], []);

        var result = Assess(person);

        Assert.Equal(21572.50m, result.Basis);
    }

    [Fact]
    public void Limit_parameter_missing_for_the_date_is_not_decided()
    {
        var result = Assess(Person([Business("2020-01-01")], Contributions("2024-06", 12, 25000), [], []), "2025-06-10");

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("max_podstawa_dobrowolnej_skladki", result.Message);
    }

    [Fact]
    public void Art48a_adds_average_lowest_base_and_the_excess_times_months_over_twelve()
    {
        var person = Person([Business("2025-09-01")], Contributions("2025-09", 6, 6000), [], []);

        var result = Assess(person, service: ServiceWithLowestBase());

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        var lowest = (4 * 4000m + 2 * 4200m) / 6;
        var excess = (4 * 2000m + 2 * 1800m) / 6;
        Assert.Equal(Math.Round((lowest + excess * 6 / 12) * (1 - 0.1371m), 2), result.Basis);
        Assert.Equal(4343.26m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
        var step = result.Trace.Single(t => t.Rule == "art48a_ubezpieczenie_krotsze_niz_12_miesiecy");
        Assert.Contains("6 pełnych miesięcy", step.Text);
        Assert.Contains("art. 48a ust. 1 i 5", step.Basis);
    }

    [Fact]
    public void Art48a_with_declared_equal_to_lowest_base_gives_the_lowest_base_after_deduction()
    {
        var person = Person([Business("2025-09-01")], Contributions("2025-09", 6, 4200), [], []);
        var rows = new[]
        {
            Row(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, 4200, "2025-01-01"),
        };

        var result = Assess(person, service: Service(rows));

        Assert.Equal(Math.Round(4200m * (1 - 0.1371m), 2), result.Basis);
    }

    [Fact]
    public void Art48a_without_the_lowest_base_parameter_is_not_decided_and_names_the_key()
    {
        var person = Person([Business("2025-09-01")], Contributions("2025-09", 6, 6000), [], []);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Null(result.Basis);
        Assert.Contains(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, result.Message);
        Assert.Contains(BasisTexts.Referral, result.Message);
        Assert.Equal(FactKeys.PodstawaUstalonaPrzezPlatnika, Assert.Single(result.MissingFacts).Fact);
    }

    [Fact]
    public void Art48a_lowest_base_missing_for_one_of_the_years_is_not_decided()
    {
        var person = Person([Business("2025-09-01")], Contributions("2025-09", 6, 6000), [], []);
        var onlyFrom2026 = new[] { Row(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, 4200, "2026-01-01") };

        var result = Assess(person, service: Service(onlyFrom2026));

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("01.09.2025", result.Message);
    }

    [Fact]
    public void Art48a_declared_base_below_the_lowest_base_is_not_decided()
    {
        var person = Person([Business("2025-09-01")], Contributions("2025-09", 6, 3000), [], []);

        var result = Assess(person, service: ServiceWithLowestBase());

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 48a ust. 5", result.Message);
    }

    [Fact]
    public void Art48a_does_not_apply_to_contractors()
    {
        var person = Person([Contractor("2025-09-01")], Contributions("2025-09", 6, 6000), [], []);

        var result = Assess(person, service: ServiceWithLowestBase());

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 49 ust. 1 pkt 2", result.Message);
    }

    [Fact]
    public void Art48a_applies_to_a_cooperating_person_too()
    {
        var title = new InsuranceTitle(TitleKind.OsobaWspolpracujaca, D("2025-09-01"), null, SicknessInsurance.Dobrowolne);

        var result = Assess(Person([title], Contributions("2025-09", 6, 4200), [], []), service: ServiceWithLowestBase());

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
    }

    [Fact]
    public void Art48a_excludes_a_month_fully_covered_by_a_benefit_from_the_month_count()
    {
        var person = Person(
            [Business("2025-09-01")],
            Contributions("2025-09", 6, 6000),
            [],
            [Leave("2025-10-01", "2025-10-31")]);

        var result = Assess(person, service: ServiceWithLowestBase());

        var lowest = (3 * 4000m + 2 * 4200m) / 5;
        var excess = (3 * 2000m + 2 * 1800m) / 5;
        Assert.Equal(Math.Round((lowest + excess * 5 / 12) * (1 - 0.1371m), 2), result.Basis);
        Assert.False(result.Months.Single(m => m.Month == "2025-10").Included);
    }

    [Fact]
    public void Art49_first_month_of_insurance_takes_the_lowest_base_for_the_month_of_the_right()
    {
        var person = Person([Business("2026-02-10")], null, [], []);

        var result = Assess(person, service: ServiceWithLowestBase());

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(3624.18m, result.Basis);
        Assert.Equal(BasisSource.SzacunekZus, result.Source);
        Assert.Contains(result.Trace, t => t.Rule == "art49_ubezpieczenie_krotsze_niz_miesiac");
    }

    [Fact]
    public void Art49_without_the_lowest_base_parameter_is_not_decided()
    {
        var result = Assess(Person([Business("2026-02-10")], null, [], []));

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains(BasisParameterKeys.NajnizszaPodstawaDzialalnosc, result.Message);
    }

    [Fact]
    public void Art49_for_a_contractor_is_not_decided()
    {
        var result = Assess(Person([Contractor("2026-02-10")], null, [], []), service: ServiceWithLowestBase());

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 49 ust. 1 pkt 2", result.Message);
    }

    [Fact]
    public void Art50_month_fully_covered_by_a_leave_is_left_out_of_the_average()
    {
        var contributions = Override(Contributions("2025-03", 12, 5000), "2025-06", 100);

        var result = Assess(Person([Business("2020-01-01")], contributions, [], [Leave("2025-06-01", "2025-06-30")]));

        Assert.Equal(4314.50m, result.Basis);
        Assert.Equal(11, result.Months.Count(m => m.Included));
    }

    [Fact]
    public void Art50_when_every_month_is_reduced_all_months_are_taken()
    {
        var person = Person([Business("2020-01-01")], Contributions("2025-03", 12, 5000), [], [Leave("2025-03-01", "2026-02-28")]);

        var result = Assess(person);

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(4314.50m, result.Basis);
        Assert.Equal(12, result.Months.Count(m => m.Included));
        Assert.Contains("art. 50 ust. 2", result.Trace.Single(t => t.Rule == "miesiace_z_nieobecnoscia").Condition);
    }

    [Fact]
    public void Non_employee_with_a_close_predecessor_title_is_not_decided()
    {
        var person = Person(
            [Contractor("2025-01-01", SicknessInsurance.Dobrowolne) with { Do = D("2025-10-15") }, Business("2025-11-01")],
            Contributions("2025-11", 4, 6000), [], []);

        var result = Assess(person, service: ServiceWithLowestBase());

        Assert.Equal(BasisOutcome.Undecided, result.Outcome);
        Assert.Contains("art. 48a ust. 2–4", result.Message);
    }
}
