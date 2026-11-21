using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class ZgonDzieckaTests
{
    private const string Birth = "2026-03-01";

    private static FactSet Stillbirth(int children = 1, int? dead = null) => Facts(BirthCase(Birth, children) with
    {
        DzieckoMartweUrodzenie = F(true),
        DzieckoLiczbaZmarlych = dead is null ? null : F(dead.Value),
    });

    private static FactSet Death(string deathDate, int children = 1, int? dead = null) => Facts(BirthCase(Birth, children) with
    {
        DzieckoDataZgonu = F(D(deathDate)),
        DzieckoLiczbaZmarlych = dead is null ? null : F(dead.Value),
    });

    [Fact]
    public void Test6_martwe_urodzenie_to_8_tygodni_po_porodzie()
    {
        var result = Get(Assess(Stillbirth()), BenefitId.Macierzynski);

        AssertPeriod(result, Birth, "2026-04-25", 56);
        Assert.Equal(8, result.Period!.Weeks);
        Assert.Contains(result.Trace, t => t.Rule == "martwe_urodzenie_zgon_dziecka" && t.Basis == "KP art. 180¹ § 1–2");
    }

    [Fact]
    public void Test6_zgon_w_10_tygodniu_zycia_to_7_dni_od_zgonu()
    {
        var result = Get(Assess(Death("2026-05-05")), BenefitId.Macierzynski);

        AssertPeriod(result, "2026-05-05", "2026-05-11", 7);
        Assert.Equal(1, result.Period!.Weeks);
    }

    [Fact]
    public void Zgon_w_dniu_56_zycia_jest_juz_po_8_tygodniach()
    {
        AssertPeriod(Get(Assess(Death("2026-04-26")), BenefitId.Macierzynski), "2026-04-26", "2026-05-02", 7);
        AssertPeriod(Get(Assess(Death("2026-04-25")), BenefitId.Macierzynski), Birth, "2026-05-01", 62);
    }

    [Theory]
    [InlineData("2026-04-05", "2026-04-25", 56)]
    [InlineData("2026-04-23", "2026-04-29", 60)]
    public void Zgon_przed_8_tygodniem_to_8_tygodni_ale_nie_krocej_niz_7_dni_od_zgonu(string death, string end, int days)
    {
        AssertPeriod(Get(Assess(Death(death)), BenefitId.Macierzynski), Birth, end, days);
    }

    [Fact]
    public void Zgon_pod_koniec_urlopu_nie_wydluza_wymiaru_a_po_koncu_nic_nie_zmienia()
    {
        AssertPeriod(Get(Assess(Death("2026-07-17")), BenefitId.Macierzynski), "2026-07-17", "2026-07-18", 2);
        Assert.Equal(BenefitStatus.Przysluguje, Get(Assess(Death("2026-07-19")), BenefitId.Macierzynski).Status);
        AssertPeriod(Get(Assess(Death("2026-07-19")), BenefitId.Macierzynski), Birth, "2026-07-18", 140);
    }

    [Fact]
    public void Wieloraczki_jedno_martwe_urodzone_to_wymiar_dla_dzieci_pozostalych_przy_zyciu()
    {
        AssertPeriod(Get(Assess(Stillbirth(2, 1)), BenefitId.Macierzynski), Birth, "2026-07-18", 140);
        AssertPeriod(Get(Assess(Stillbirth(3, 1)), BenefitId.Macierzynski), Birth, "2026-10-03", 217);
        AssertPeriod(Get(Assess(Stillbirth(2, 2)), BenefitId.Macierzynski), Birth, "2026-04-25", 56);
    }

    [Fact]
    public void Wieloraczki_zgon_jednego_po_8_tygodniach_nie_skraca_ponizej_wymiaru_dla_pozostalych()
    {
        AssertPeriod(Get(Assess(Death("2026-05-05", 2, 1)), BenefitId.Macierzynski), "2026-05-05", "2026-07-18", 75);
    }

    [Fact]
    public void Wieloraczki_bez_liczby_zmarlych_pytaja_o_fakt()
    {
        var assessment = Assess(Stillbirth(2));

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Macierzynski).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.DzieckoLiczbaZmarlych);
    }

    [Fact]
    public void Po_martwym_urodzeniu_rodzicielski_uzupelniajacy_i_ojcowski_nie_przysluguja()
    {
        var assessment = Assess(Stillbirth());

        var parental = Get(assessment, BenefitId.Rodzicielski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, parental.Status);
        Assert.Contains("opieki nad dzieckiem", parental.Reason);
        Assert.Contains("8 tygodni po porodzie", parental.Reason);
        Assert.Contains("182¹a", parental.LegalBasis);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.MacierzynskiUzupelniajacy).Status);
        var father = Assess(Facts(BirthCase(Birth) with { DzieckoMartweUrodzenie = F(true) }, Father()));
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(father, BenefitId.Ojcowski).Status);
    }

    [Fact]
    public void Wieloraczki_wszystkie_martwe_urodzone_to_tez_nie_przysluguje()
    {
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Assess(Stillbirth(2, 2)), BenefitId.Rodzicielski).Status);
    }

    [Fact]
    public void Zgon_przed_rozpoczeciem_rodzicielskiego_to_nie_przysluguje()
    {
        var result = Get(Assess(Death("2026-05-05")), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("przed rozpoczęciem urlopu rodzicielskiego", result.Reason);
        Assert.Equal("KP art. 180¹ § 2 w zw. z art. 182¹g", result.LegalBasis);
    }

    [Fact]
    public void Zgon_w_trakcie_rodzicielskiego_to_7_dni_od_zgonu()
    {
        // Macierzyński 1.03–18.07, rodzicielski od 19.07; zgon 10.08 → urlop do 16.08 (7 dni od zgonu).
        var result = Get(Assess(Death("2026-08-10")), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        AssertPeriod(result, "2026-07-19", "2026-08-16", 29);
        Assert.Contains(result.Trace, t => t.Rule == "zgon_dziecka_w_trakcie_rodzicielskiego" && t.Met == true);
        Assert.Empty(result.Deadlines);
    }

    [Fact]
    public void Zgon_w_trakcie_uzupelniajacego_to_7_dni_od_zgonu()
    {
        var facts = Facts(BirthCase(Birth) with
        {
            DzieckoTydzienCiazyPrzyPorodzie = F(27),
            DzieckoMasaUrodzeniowaG = F(900),
            DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([new DateRange(D("2026-03-01"), D("2026-05-09"))]),
            DzieckoDataZgonu = F(D("2026-08-01")),
        });
        var assessment = Assess(facts);

        AssertPeriod(Get(assessment, BenefitId.MacierzynskiUzupelniajacy), "2026-07-19", "2026-08-07", 20);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.Rodzicielski).Status);
    }

    [Fact]
    public void Zgon_przed_uzupelniajacym_to_nie_przysluguje()
    {
        var facts = Facts(BirthCase(Birth) with
        {
            DzieckoTydzienCiazyPrzyPorodzie = F(27),
            DzieckoMasaUrodzeniowaG = F(900),
            DzieckoPobytyWSzpitalu = F<IReadOnlyList<DateRange>>([new DateRange(D("2026-03-01"), D("2026-05-09"))]),
            DzieckoDataZgonu = F(D("2026-05-05")),
        });

        var result = Get(Assess(facts), BenefitId.MacierzynskiUzupelniajacy);
        Assert.Equal(BenefitStatus.NiePrzysluguje, result.Status);
        Assert.Contains("przed rozpoczęciem uzupełniającego urlopu macierzyńskiego", result.Reason);
    }

    [Fact]
    public void Zgon_po_porodzie_ojcowski_zostaje_nie_rozstrzygam()
    {
        var father = Assess(Facts(BirthCase(Birth) with { DzieckoDataZgonu = F(D("2026-04-10")) }, Father()));
        var result = Get(father, BenefitId.Ojcowski);

        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
        Assert.Contains("182³", result.Reason);
    }

    [Fact]
    public void Blizneta_jedno_martwe_drugie_zyje_rodzicielski_przysluguje_w_wymiarze_dla_jednego_dziecka_z_nota()
    {
        var result = Get(Assess(Stillbirth(2, 1)), BenefitId.Rodzicielski);

        Assert.Equal(BenefitStatus.Przysluguje, result.Status);
        Assert.Equal(41, result.Period!.Weeks);
        Assert.Equal(D("2026-07-19"), result.Period.From);
        Assert.Contains(result.Notes, n => n.Contains("ZUS może przyznać wymiar jak dla dwojga dzieci", StringComparison.Ordinal));

        var father = Assess(Facts(BirthCase(Birth, 2) with { DzieckoMartweUrodzenie = F(true), DzieckoLiczbaZmarlych = F(1) }, Father()));
        Assert.Equal(BenefitStatus.Przysluguje, Get(father, BenefitId.Ojcowski).Status);
    }

    [Fact]
    public void Trojaczki_dwoje_zyje_wymiar_43_tygodnie_bez_noty()
    {
        var result = Get(Assess(Stillbirth(3, 1)), BenefitId.Rodzicielski);

        Assert.Equal(43, result.Period!.Weeks);
        Assert.DoesNotContain(result.Notes, n => n.Contains("dwojga dzieci", StringComparison.Ordinal));
    }

    [Fact]
    public void Blizneta_bez_liczby_zmarlych_rodzicielski_pyta_o_fakt()
    {
        var assessment = Assess(Stillbirth(2));

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.Rodzicielski).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.DzieckoLiczbaZmarlych);
    }

    [Fact]
    public void Zgon_przed_porodem_dat_to_nie_rozstrzygam()
    {
        var result = Get(Assess(Death("2026-02-20")), BenefitId.Macierzynski);

        Assert.Equal(BenefitStatus.NieRozstrzygam, result.Status);
    }
}
