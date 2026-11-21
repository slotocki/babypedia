using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class UzupelniajacyTests
{
    private const string Birth = "2026-03-01";

    private static FactSet Preterm(int week, int mass, params (string From, string To)[] stays) => Facts(
        BirthCase(Birth) with
        {
            DzieckoTydzienCiazyPrzyPorodzie = F(week),
            DzieckoMasaUrodzeniowaG = F(mass),
            DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>(stays.Select(s => new DateRange(D(s.From), D(s.To))).ToList()),
        });

    [Fact]
    public void Test4_wczesniak_z_27_tygodnia_70_dni_w_szpitalu_to_10_tygodni()
    {
        var result = Get(Assess(Preterm(27, 900, ("2026-03-01", "2026-05-09"))), BenefitId.MacierzynskiUzupelniajacy);

        AssertPeriod(result, "2026-07-19", "2026-09-26", 70);
        Assert.Equal(10, result.Period!.Weeks);
        Assert.Contains(result.Trace, t => t.Rule == "uzupelniajacy_wymiar" && t.Basis.StartsWith("KP art. 180²"));
    }

    [Fact]
    public void Test4_wczesniak_120_dni_w_szpitalu_to_limit_15_tygodni()
    {
        var result = Get(Assess(Preterm(27, 900, ("2026-03-01", "2026-06-28"))), BenefitId.MacierzynskiUzupelniajacy);

        AssertPeriod(result, "2026-07-19", "2026-10-31", 105);
        Assert.Equal(15, result.Period!.Weeks);
    }

    [Fact]
    public void Test5_porod_w_39_tygodniu_pobyt_od_6_do_20_dnia_to_3_tygodnie()
    {
        var result = Get(Assess(Preterm(39, 3300, ("2026-03-07", "2026-03-21"))), BenefitId.MacierzynskiUzupelniajacy);

        AssertPeriod(result, "2026-07-19", "2026-08-08", 21);
        Assert.Equal(3, result.Period!.Weeks);
    }

    [Fact]
    public void Porod_po_37_tygodniu_pobyt_zaczety_przed_5_dniem_liczy_sie_od_5_dnia()
    {
        var result = Get(Assess(Preterm(39, 3300, ("2026-03-04", "2026-03-11"))), BenefitId.MacierzynskiUzupelniajacy);

        AssertPeriod(result, "2026-07-19", "2026-07-25", 7);
    }

    [Theory]
    [InlineData("2026-03-02", "2026-03-03")]
    [InlineData("2026-03-11", "2026-03-11")]
    [InlineData("2026-04-05", "2026-04-20")]
    public void Porod_po_37_tygodniu_bez_dwoch_kolejnych_dni_zaczetych_miedzy_5_a_28_dniem_nie_przysluguje(string from, string to)
    {
        var result = Get(Assess(Preterm(39, 3300, (from, to))), BenefitId.MacierzynskiUzupelniajacy);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("kolejne dni", result.Reason);
        Assert.StartsWith("KP art. 180²", result.LegalBasis);
    }

    [Fact]
    public void Porod_miedzy_28_a_37_tygodniem_ma_limit_8_tygodni_i_sumuje_przerwy_w_pobycie()
    {
        var oneStay = Get(Assess(Preterm(33, 2000, ("2026-03-01", "2026-04-29"))), BenefitId.MacierzynskiUzupelniajacy);
        AssertPeriod(oneStay, "2026-07-19", "2026-09-12", 56);

        var twoStays = Get(Assess(Preterm(33, 2000, ("2026-03-01", "2026-03-10"), ("2026-03-20", "2026-03-30"))), BenefitId.MacierzynskiUzupelniajacy);
        AssertPeriod(twoStays, "2026-07-19", "2026-08-08", 21);
    }

    [Fact]
    public void Masa_1000_g_to_przypadek_wczesniaka_a_1001_g_przy_33_tygodniu_to_przypadek_posredni()
    {
        var extreme = Get(Assess(Preterm(33, 1000, ("2026-03-01", "2026-04-09"))), BenefitId.MacierzynskiUzupelniajacy);
        Assert.Equal(6, extreme.Period!.Weeks);

        var longStay = Get(Assess(Preterm(33, 1000, ("2026-03-01", "2026-06-20"))), BenefitId.MacierzynskiUzupelniajacy);
        Assert.Equal(15, longStay.Period!.Weeks);

        var middle = Get(Assess(Preterm(33, 1001, ("2026-03-01", "2026-06-20"))), BenefitId.MacierzynskiUzupelniajacy);
        Assert.Equal(8, middle.Period!.Weeks);
    }

    [Fact]
    public void Porod_w_28_tygodniu_to_nie_wczesniak_ale_27_to_wczesniak()
    {
        var week27 = Get(Assess(Preterm(27, 1500, ("2026-03-01", "2026-06-20"))), BenefitId.MacierzynskiUzupelniajacy);
        var week28 = Get(Assess(Preterm(28, 1500, ("2026-03-01", "2026-06-20"))), BenefitId.MacierzynskiUzupelniajacy);

        Assert.Equal(15, week27.Period!.Weeks);
        Assert.Equal(8, week28.Period!.Weeks);
    }

    [Fact]
    public void Uzupelniajacy_bez_tygodnia_albo_masy_pyta_o_brakujacy_fakt()
    {
        var facts = Preterm(33, 2000, ("2026-03-01", "2026-03-20"));
        var noMass = facts with { Sprawa = facts.Sprawa with { DzieckoMasaUrodzeniowaG = null } };

        var assessment = Assess(noMass);

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.MacierzynskiUzupelniajacy).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.DzieckoMasaUrodzeniowaG);
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Rodzicielski).Status);
    }

    [Fact]
    public void Bez_pobytu_w_szpitalu_uzupelniajacy_nie_dotyczy_i_rodzicielski_idzie_po_macierzynskim()
    {
        var assessment = Assess(Facts(BirthCase(Birth)));

        Assert.Equal(BenefitStatus.NieDotyczy, Get(assessment, BenefitId.MacierzynskiUzupelniajacy).Status);
        Assert.Equal(D("2026-07-19"), Get(assessment, BenefitId.Rodzicielski).Period!.From);
    }

    [Fact]
    public void Uzupelniajacy_jest_miedzy_macierzynskim_a_rodzicielskim_i_przesuwa_rodzicielski()
    {
        var assessment = Assess(Preterm(27, 900, ("2026-03-01", "2026-05-09")));

        var supplementary = Get(assessment, BenefitId.MacierzynskiUzupelniajacy);
        var parental = Get(assessment, BenefitId.Rodzicielski);
        Assert.Equal(Get(assessment, BenefitId.Macierzynski).Period!.To.AddDays(1), supplementary.Period!.From);
        Assert.Equal(supplementary.Period.To.AddDays(1), parental.Period!.From);
    }

    [Fact]
    public void Uzupelniajacy_dla_ojca_i_przed_porodem_nie_dotyczy()
    {
        var before = Assess(Facts(BirthCase(Birth, kind: EventKind.Ciaza)));

        Assert.Equal(BenefitStatus.NieDotyczy, Get(before, BenefitId.MacierzynskiUzupelniajacy).Status);
        Assert.DoesNotContain(Assess(Facts(BirthCase(Birth), Father())).Benefits, b => b.Id == BenefitId.MacierzynskiUzupelniajacy);
    }
}
