using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class PrzyjecieDzieckaTests
{
    [Fact]
    public void Test8_przysposobienie_dziecka_8_letniego_to_20_plus_41_tygodni()
    {
        var assessment = Assess(Facts(AdoptionCase("2026-11-01", "2018-05-10", AdoptionPath.Przysposobienie)));

        var leave = Get(assessment, BenefitId.PrzyjecieDziecka);
        AssertPeriod(leave, "2026-11-01", "2027-03-20", 140);
        Assert.Equal(20, leave.Period!.Weeks);
        var parental = Get(assessment, BenefitId.Rodzicielski);
        AssertPeriod(parental, "2027-03-21", "2028-01-01", 287);
        Assert.Equal(41, parental.Period!.Weeks);
    }

    [Fact]
    public void Test8_przysposobienie_dziecka_13_lat_11_miesiecy_to_9_plus_38_tygodni()
    {
        var assessment = Assess(Facts(AdoptionCase("2026-11-20", "2012-12-15", AdoptionPath.Przysposobienie)));

        var leave = Get(assessment, BenefitId.PrzyjecieDziecka);
        AssertPeriod(leave, "2026-11-20", "2027-01-21", 63);
        Assert.Equal(9, leave.Period!.Weeks);
        var parental = Get(assessment, BenefitId.Rodzicielski);
        AssertPeriod(parental, "2027-01-22", "2027-10-14", 266);
        Assert.Equal(38, parental.Period!.Weeks);
        Assert.Contains(leave.Trace, t => t.Rule == "przyjecie_okres" && t.Met == true);
    }

    [Fact]
    public void Test8_minimum_z_za_zyciem_daje_62_tygodnie_rodzicielskiego()
    {
        var sprawa = AdoptionCase("2026-11-20", "2012-12-15", AdoptionPath.Przysposobienie) with { DzieckoZaZyciem = F(true) };

        var parental = Get(Assess(Facts(sprawa)), BenefitId.Rodzicielski);

        Assert.Equal(62 * 7, parental.Period!.Days);
    }

    [Fact]
    public void Przysposobienie_dziecka_14_letniego_to_odmowa_a_dzien_przed_14_urodzinami_jeszcze_nie()
    {
        var late = Get(Assess(Facts(AdoptionCase("2026-12-15", "2012-12-15", AdoptionPath.Przysposobienie))), BenefitId.PrzyjecieDziecka);
        var onTime = Get(Assess(Facts(AdoptionCase("2026-12-14", "2012-12-15", AdoptionPath.Przysposobienie))), BenefitId.PrzyjecieDziecka);

        Assert.Equal(BenefitStatus.NiePrzysluguje, late.Status);
        Assert.Contains("14", late.Reason);
        Assert.Equal(BenefitStatus.Przysluguje, onTime.Status);
        Assert.Equal(63, onTime.Period!.Days);
    }

    [Fact]
    public void Przysposobienie_bez_wniosku_do_sadu_to_odmowa_a_bez_informacji_brak_danych()
    {
        var refused = AdoptionCase("2026-11-01", "2018-05-10", AdoptionPath.Przysposobienie) with { PrzyjecieWniosekDoSadu = F(false) };
        var unknown = AdoptionCase("2026-11-01", "2018-05-10", AdoptionPath.Przysposobienie) with { PrzyjecieWniosekDoSadu = null };

        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Assess(Facts(refused)), BenefitId.PrzyjecieDziecka).Status);
        var assessment = Assess(Facts(unknown));
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.PrzyjecieDziecka).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.PrzyjecieWniosekDoSadu);
    }

    [Fact]
    public void Test9_rodzina_zastepcza_niezawodowa_dziecko_5_lat_to_20_plus_41_tygodni()
    {
        var assessment = Assess(Facts(AdoptionCase("2026-11-01", "2021-06-01", AdoptionPath.RodzinaZastepczaNiezawodowa)));

        Assert.Equal(20, Get(assessment, BenefitId.PrzyjecieDziecka).Period!.Weeks);
        Assert.Equal(41, Get(assessment, BenefitId.Rodzicielski).Period!.Weeks);
    }

    [Fact]
    public void Test9_rodzina_zastepcza_dziecko_8_lat_bez_odroczenia_to_odmowa()
    {
        var sprawa = AdoptionCase("2026-11-01", "2018-05-10", AdoptionPath.RodzinaZastepczaNiezawodowa) with
        {
            PrzyjecieOdroczenieObowiazkuSzkolnego = F(false),
        };

        var assessment = Assess(Facts(sprawa));

        var leave = Get(assessment, BenefitId.PrzyjecieDziecka);
        Assert.Equal(BenefitStatus.NiePrzysluguje, leave.Status);
        Assert.Contains("7", leave.Reason);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.Rodzicielski).Status);
    }

    [Fact]
    public void Test9_rodzina_zastepcza_dziecko_8_lat_z_odroczeniem_to_przyznanie()
    {
        var sprawa = AdoptionCase("2026-11-01", "2018-05-10", AdoptionPath.RodzinaZastepczaNiezawodowa) with
        {
            PrzyjecieOdroczenieObowiazkuSzkolnego = F(true),
        };

        var assessment = Assess(Facts(sprawa));

        AssertPeriod(Get(assessment, BenefitId.PrzyjecieDziecka), "2026-11-01", "2027-03-20", 140);
        Assert.Equal(41, Get(assessment, BenefitId.Rodzicielski).Period!.Weeks);
    }

    [Fact]
    public void Test9_granica_10_lat_przy_odroczeniu()
    {
        var older = AdoptionCase("2026-11-01", "2016-11-01", AdoptionPath.RodzinaZastepczaNiezawodowa) with
        {
            PrzyjecieOdroczenieObowiazkuSzkolnego = F(true),
        };
        var justUnder = older with { ZdarzenieData = F(D("2026-10-31")) };

        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(Assess(Facts(older)), BenefitId.PrzyjecieDziecka).Status);
        Assert.Equal(BenefitStatus.Przysluguje, Get(Assess(Facts(justUnder)), BenefitId.PrzyjecieDziecka).Status);
    }

    [Fact]
    public void Test9_rodzina_zastepcza_zawodowa_to_odmowa()
    {
        var assessment = Assess(Facts(AdoptionCase("2026-11-01", "2021-06-01", AdoptionPath.RodzinaZastepczaZawodowa)));

        var leave = Get(assessment, BenefitId.PrzyjecieDziecka);
        Assert.Equal(BenefitStatus.NiePrzysluguje, leave.Status);
        Assert.Contains("zawodowej", leave.Reason);
        Assert.StartsWith("KP art. 183", leave.LegalBasis);
        Assert.Equal(BenefitStatus.NiePrzysluguje, Get(assessment, BenefitId.Rodzicielski).Status);
    }

    [Fact]
    public void Dziecko_7_8_lat_bez_informacji_o_odroczeniu_pyta_o_fakt()
    {
        var sprawa = AdoptionCase("2026-11-01", "2018-05-10", AdoptionPath.RodzinaZastepczaNiezawodowa);

        var assessment = Assess(Facts(sprawa));

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.PrzyjecieDziecka).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.PrzyjecieOdroczenieObowiazkuSzkolnego);
    }

    [Fact]
    public void Dziecko_ponizej_7_lat_nie_wymaga_odroczenia_dopoki_urlop_miesci_sie_w_granicy()
    {
        var far = AdoptionCase("2026-11-01", "2021-06-01", AdoptionPath.RodzinaZastepczaNiezawodowa);
        var nearBorder = AdoptionCase("2026-11-01", "2020-02-01", AdoptionPath.RodzinaZastepczaNiezawodowa);

        Assert.Equal(BenefitStatus.Przysluguje, Get(Assess(Facts(far)), BenefitId.PrzyjecieDziecka).Status);
        Assert.Equal(BenefitStatus.BrakujeDanych, Get(Assess(Facts(nearBorder)), BenefitId.PrzyjecieDziecka).Status);
    }

    [Fact]
    public void Urlop_skrocony_do_granicy_wieku_ale_dluzszy_niz_minimum_to_nie_rozstrzygam_rodzicielskiego()
    {
        var sprawa = AdoptionCase("2026-11-01", "2020-02-01", AdoptionPath.RodzinaZastepczaNiezawodowa) with
        {
            PrzyjecieOdroczenieObowiazkuSzkolnego = F(false),
        };

        var assessment = Assess(Facts(sprawa));

        AssertPeriod(Get(assessment, BenefitId.PrzyjecieDziecka), "2026-11-01", "2027-01-31", 92);
        Assert.Equal(BenefitStatus.NieRozstrzygam, Get(assessment, BenefitId.Rodzicielski).Status);
    }

    [Theory]
    [InlineData(1, 20, 41)]
    [InlineData(2, 31, 43)]
    [InlineData(3, 33, 43)]
    [InlineData(4, 35, 43)]
    [InlineData(5, 37, 43)]
    public void Test1_wymiar_przy_przyjeciu_zalezy_od_liczby_dzieci(int children, int leaveWeeks, int parentalWeeks)
    {
        var assessment = Assess(Facts(AdoptionCase("2026-11-01", "2024-01-01", AdoptionPath.Przysposobienie, children)));

        Assert.Equal(leaveWeeks, Get(assessment, BenefitId.PrzyjecieDziecka).Period!.Weeks);
        Assert.Equal(parentalWeeks, Get(assessment, BenefitId.Rodzicielski).Period!.Weeks);
    }

    [Fact]
    public void Rodzicielski_przy_przysposobieniu_nie_wychodzi_poza_14_rok_zycia_dziecka()
    {
        var assessment = Assess(Facts(AdoptionCase("2026-06-01", "2012-12-15", AdoptionPath.Przysposobienie)));

        AssertPeriod(Get(assessment, BenefitId.PrzyjecieDziecka), "2026-06-01", "2026-10-18", 140);
        var parental = Get(assessment, BenefitId.Rodzicielski);
        AssertPeriod(parental, "2026-10-19", "2026-12-14", 57);
        Assert.Contains(parental.Notes, n => n.Contains("2026-12-14"));
    }

    [Fact]
    public void Przy_przyjeciu_ojciec_dostaje_ojcowski_i_rodzicielski_a_nie_przyjecie_dziecka()
    {
        var assessment = Assess(Facts(AdoptionCase("2026-11-01", "2024-01-01", AdoptionPath.Przysposobienie), Father()));

        Assert.DoesNotContain(assessment.Benefits, b => b.Id == BenefitId.PrzyjecieDziecka);
        Assert.Contains(assessment.Benefits, b => b.Id == BenefitId.Ojcowski);
        Assert.Contains(assessment.Benefits, b => b.Id == BenefitId.Rodzicielski);
    }

    [Fact]
    public void Brak_daty_urodzenia_przyjmowanego_dziecka_to_brak_danych()
    {
        var sprawa = AdoptionCase("2026-11-01", "2018-05-10", AdoptionPath.Przysposobienie) with { DzieckoDataUrodzenia = null };

        var assessment = Assess(Facts(sprawa));

        Assert.Equal(BenefitStatus.BrakujeDanych, Get(assessment, BenefitId.PrzyjecieDziecka).Status);
        Assert.Contains(assessment.MissingFacts, m => m.Fact == FactKeys.DzieckoDataUrodzenia);
    }
}
