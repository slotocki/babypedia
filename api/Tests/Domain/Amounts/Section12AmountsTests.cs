using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Amounts.AmountsKit;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Amounts;

/// <summary>Testy 12–15 i 24 z instrukcji (sekcja 11) z dokładnymi liczbami.</summary>
public sealed class Section12AmountsTests
{
    private const string Nbsp = " ";

    private static FactSet BirthFacts(PersonFacts? me = null, PersonFacts? other = null) =>
        Facts(BirthCase("2026-03-02"), me ?? Mother(), other);

    private static BasisInput BasisOf(decimal revenue, TitleKind title = TitleKind.Pracownik, SicknessInsurance sickness = SicknessInsurance.Obowiazkowe, decimal etat = 1m)
    {
        var adjusted = new BasisAdjuster(Params()).FromRevenue(revenue, D("2026-03-02"), new BasisProfile(title, sickness, etat));
        return new BasisInput(adjusted.Amount, adjusted.Source);
    }

    [Fact]
    public void Test12_podstawa_8000_to_6903_20_a_stawka_dzienna_230_11()
    {
        var basis = BasisOf(8000m);
        Assert.Equal(6903.20m, basis.Amount);
        Assert.Equal(BasisSource.SzacunekZus, basis.Source);

        var card = Get(Run(BirthFacts(), basis), BenefitId.Macierzynski);

        Assert.Equal(1m, card.Rate);
        Assert.Equal(230.11m, card.Amount!.Daily);
        Assert.Equal(230.11m * 140, card.Amount.Total);
        Assert.Equal(6903.20m, card.Amount.Basis);
        Assert.Contains(card.Notes, n => n.Contains($"6{Nbsp}903,20{Nbsp}zł") && n.Contains("100%"));
    }

    [Fact]
    public void Test12_miesiecznie_81_5_proc_to_5626_11()
    {
        var facts = BirthFacts(WithVariant(Mother(), "2026-03-10"));
        var card = Get(Run(facts, BasisOf(8000m)), BenefitId.Macierzynski);

        Assert.Equal(0.815m, card.Rate);
        Assert.Contains(card.Notes, n => n.Contains($"81,5%: 5{Nbsp}626,11{Nbsp}zł"));
    }

    [Fact]
    public void Test12_miesiecznie_70_proc_to_4832_24()
    {
        var card = Get(Run(BirthFacts(), BasisOf(8000m)), BenefitId.Rodzicielski);

        Assert.Equal(0.7m, card.Rate);
        Assert.Contains(card.Notes, n => n.Contains($"70%: 4{Nbsp}832,24{Nbsp}zł"));
    }

    [Fact]
    public void Test13_minimalna_podstawa_pelny_etat_i_pol_etatu()
    {
        var full = BasisOf(1000m);
        Assert.Equal(4147.10m, full.Amount);
        Assert.Equal(BasisSource.Minimalna, full.Source);

        var half = BasisOf(1000m, etat: 0.5m);
        Assert.Equal(2073.55m, half.Amount);
        Assert.Equal(BasisSource.Minimalna, half.Source);
    }

    [Fact]
    public void Test13_podstawa_powyzej_minimum_zostaje_bez_zmian()
    {
        var basis = BasisOf(8000m, etat: 0.5m);
        Assert.Equal(6903.20m, basis.Amount);
        Assert.Equal(BasisSource.SzacunekZus, basis.Source);
    }

    [Fact]
    public void Test14_limit_podstawy_dla_dobrowolnie_ubezpieczonych_to_20321_30()
    {
        var limited = BasisOf(25000m, TitleKind.Dzialalnosc, SicknessInsurance.Dobrowolne);
        Assert.Equal(20321.30m, limited.Amount);
        Assert.Equal(BasisSource.SzacunekZus, limited.Source);

        var atMax = BasisOf(23550m, TitleKind.Dzialalnosc, SicknessInsurance.Dobrowolne);
        Assert.Equal(20321.30m, atMax.Amount);

        var below = BasisOf(10000m, TitleKind.Dzialalnosc, SicknessInsurance.Dobrowolne);
        Assert.Equal(8629.00m, below.Amount);
    }

    [Fact]
    public void Test14_limit_nie_dotyczy_pracownika()
    {
        var basis = BasisOf(25000m);
        Assert.Equal(21572.50m, basis.Amount);
    }

    [Fact]
    public void Test15_rownowaznosc_standardowy_20_tyg_100_i_32_tyg_70_to_29680()
    {
        var me = Mother() with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(32)]),
            PodstawaUstalonaPrzezPlatnika = F(3000m),
        };
        var result = Run(BirthFacts(me));
        var mat = Get(result, BenefitId.Macierzynski);
        var rod = Get(result, BenefitId.Rodzicielski);

        Assert.Equal(100m, mat.Amount!.Daily);
        Assert.Equal(14000m, mat.Amount.Total);
        Assert.Equal(BasisSource.UstalonaPrzezPlatnika, mat.Amount.BasisSource);
        Assert.Equal(70m, rod.Amount!.Daily);
        Assert.Equal(15680m, rod.Amount.Total);
        Assert.Equal(29680m, mat.Amount.Total + rod.Amount.Total);
        Assert.Equal(42.4m * 700m, mat.Amount.Total + rod.Amount.Total);
    }

    [Fact]
    public void Test15_rownowaznosc_usredniony_52_tyg_po_81_5_to_29666()
    {
        var me = WithVariant(Mother(), "2026-03-10") with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(32)]),
            PodstawaUstalonaPrzezPlatnika = F(3000m),
        };
        var result = Run(BirthFacts(me));
        var mat = Get(result, BenefitId.Macierzynski);
        var rod = Get(result, BenefitId.Rodzicielski);

        Assert.Equal(0.815m, mat.Rate);
        Assert.Equal(0.815m, rod.Rate);
        Assert.Equal(11410m, mat.Amount!.Total);
        Assert.Equal(18256m, rod.Amount!.Total);
        Assert.Equal(29666m, mat.Amount.Total + rod.Amount.Total);
    }

    private static PersonFacts MotherWithPart(string from, int weeks) =>
        Mother() with { PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(weeks, D(from))]) };

    private static Reference.SeedRow Rate75From2027 =>
        Row(AmountsParameterKeys.StawkaRodzicielski, 0.75m, "2027-01-01", "Parametr testowy (test 24)");

    [Fact]
    public void Test24_stawka_75_od_2027_nie_zmienia_dnia_zasilku_w_2026()
    {
        var result = Run(BirthFacts(MotherWithPart("2026-08-03", 8)), null, null, Rate75From2027);
        var card = Get(result, BenefitId.Rodzicielski);

        Assert.Equal(0.7m, card.Rate);
        Assert.Single(card.Parts);
        Assert.Equal(0.7m, card.Parts[0].Rate);
    }

    [Fact]
    public void Test24_stawka_75_od_2027_zmienia_dzien_zasilku_w_2027()
    {
        var result = Run(BirthFacts(MotherWithPart("2027-02-01", 8)), null, null, Rate75From2027);
        var card = Get(result, BenefitId.Rodzicielski);

        Assert.Equal(0.75m, card.Rate);
        Assert.Equal(0.75m, card.Parts.Single().Rate);
    }

    [Fact]
    public void Test24_okres_przechodzacy_przez_date_zmiany_parametru_jest_dzielony_na_czesci()
    {
        var me = MotherWithPart("2026-12-14", 4) with { PodstawaUstalonaPrzezPlatnika = F(3000m) };
        var card = Get(Run(BirthFacts(me), null, null, Rate75From2027), BenefitId.Rodzicielski);

        Assert.Equal(2, card.Parts.Count);
        Assert.Equal((D("2026-12-14"), D("2026-12-31"), 18, 0.7m), (card.Parts[0].Period.From, card.Parts[0].Period.To, card.Parts[0].Period.Days, card.Parts[0].Rate));
        Assert.Equal((D("2027-01-01"), D("2027-01-10"), 10, 0.75m), (card.Parts[1].Period.From, card.Parts[1].Period.To, card.Parts[1].Period.Days, card.Parts[1].Rate));
        Assert.Equal(70m * 18, card.Parts[0].Amount!.Total);
        Assert.Equal(75m * 10, card.Parts[1].Amount!.Total);
        Assert.Equal(70m * 18 + 75m * 10, card.Amount!.Total);
        Assert.Equal(0.7m, card.Rate);
        Assert.Contains(card.Notes, n => n.Contains("Stawka zmienia się"));
    }
}
