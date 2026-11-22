using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class ConfirmedCasesTests
{
    private static FactSet Case(
        string birth, int week, int mass, string stayFrom, string stayTo, int parentalWeeks) => Facts(
        BirthCase(birth) with
        {
            DzieckoTydzienCiazyPrzyPorodzie = F(week),
            DzieckoMasaUrodzeniowaG = F(mass),
            DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([new DateRange(D(stayFrom), D(stayTo))]),
        },
        Mother() with { PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(parentalWeeks)]) });

    [Fact]
    public void Test2_skrajny_wczesniak_porod_12_06_2025()
    {
        var assessment = Assess(Case("2025-06-12", 26, 900, "2025-06-12", "2025-10-01", 32));

        AssertPeriod(Get(assessment, BenefitId.Macierzynski), "2025-06-12", "2025-10-29", 140);
        var supplementary = Get(assessment, BenefitId.MacierzynskiUzupelniajacy);
        AssertPeriod(supplementary, "2025-10-30", "2026-02-11", 105);
        Assert.Equal(15, supplementary.Period!.Weeks);
        var parental = Get(assessment, BenefitId.Rodzicielski);
        AssertPeriod(parental, "2026-02-12", "2026-09-23", 224);
        Assert.Equal(32, parental.Period!.Weeks);
        Assert.Empty(assessment.ValidationErrors);
    }

    [Fact]
    public void Test2_dzialalnosc_pelny_wymiar_porod_1_12_2025()
    {
        var assessment = Assess(Case("2025-12-01", 34, 2100, "2025-12-01", "2026-01-30", 32));

        AssertPeriod(Get(assessment, BenefitId.Macierzynski), "2025-12-01", "2026-04-19", 140);
        var supplementary = Get(assessment, BenefitId.MacierzynskiUzupelniajacy);
        AssertPeriod(supplementary, "2026-04-20", "2026-06-14", 56);
        Assert.Equal(8, supplementary.Period!.Weeks);
        var parental = Get(assessment, BenefitId.Rodzicielski);
        AssertPeriod(parental, "2026-06-15", "2027-01-24", 224);
        Assert.Equal(32, parental.Period!.Weeks);
    }

    [Fact]
    public void Kazda_decyzja_ma_krok_sciezki_z_regula_podstawa_prawna_i_wyjasnieniem()
    {
        var assessment = Assess(Case("2025-06-12", 26, 900, "2025-06-12", "2025-10-01", 32));

        foreach (var benefit in assessment.Benefits)
        {
            Assert.NotEmpty(benefit.Trace);
            Assert.All(benefit.Trace, step =>
            {
                Assert.False(string.IsNullOrWhiteSpace(step.Rule));
                Assert.False(string.IsNullOrWhiteSpace(step.Basis));
                Assert.False(string.IsNullOrWhiteSpace(step.Text));
            });
        }

        var rules = Get(assessment, BenefitId.MacierzynskiUzupelniajacy).Trace.Select(t => t.Rule).ToList();
        Assert.Equal(["uzupelniajacy_przypadek", "uzupelniajacy_wymiar", "macierzynski_okres", "uzupelniajacy_okres"], rules);
    }
}
