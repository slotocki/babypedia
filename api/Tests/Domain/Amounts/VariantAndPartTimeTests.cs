using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Amounts.AmountsKit;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Amounts;

public sealed class VariantAndPartTimeTests
{
    private const string Nbsp = " ";

    private static FactSet Birth(PersonFacts? me = null, PersonFacts? other = null, string date = "2026-03-02") =>
        Facts(BirthCase(date), me ?? Mother(), other);

    private static PersonFacts WithBasis(PersonFacts person, decimal amount) => person with { PodstawaUstalonaPrzezPlatnika = F(amount) };

    [Theory]
    [InlineData("2026-03-23", 0.815, 0.815)]
    [InlineData("2026-03-24", 1, 0.7)]
    [InlineData("2026-03-02", 0.815, 0.815)]
    public void Termin_21_dni_po_porodzie_wlacznie_z_21_dniem(string filedOn, double macierzynski, double rodzicielski)
    {
        var result = Run(Birth(WithVariant(Mother(), filedOn)));

        Assert.Equal((decimal)macierzynski, Get(result, BenefitId.Macierzynski).Rate);
        Assert.Equal((decimal)rodzicielski, Get(result, BenefitId.Rodzicielski).Rate);
    }

    [Fact]
    public void Wniosek_po_terminie_zostaje_w_sciezce_decyzji_i_notatce()
    {
        var card = Get(Run(Birth(WithVariant(Mother(), "2026-03-24"))), BenefitId.Macierzynski);

        var step = Assert.Single(card.Trace, t => t.Rule == "wniosek_usredniony");
        Assert.False(step.Met);
        Assert.Contains("ustawa art. 30a", step.Basis);
        Assert.Contains(card.Notes, n => n.Contains("po terminie") && n.Contains("23.03.2026"));
    }

    [Fact]
    public void Brak_daty_wniosku_przed_terminem_zaklada_zlozenie_w_terminie_z_zastrzezeniem()
    {
        var card = Get(Run(Birth(WithVariant(Mother(), null)), null, D("2026-03-10")), BenefitId.Macierzynski);

        Assert.Equal(0.815m, card.Rate);
        Assert.Contains(card.Notes, n => n.Contains("pod warunkiem złożenia wniosku najpóźniej 23.03.2026"));
        Assert.Null(Assert.Single(card.Trace, t => t.Rule == "wniosek_usredniony").Met);
    }

    [Fact]
    public void Brak_daty_wniosku_po_uplywie_terminu_to_stawki_standardowe()
    {
        var result = Run(Birth(WithVariant(Mother(), null)), null, D("2026-03-24"));

        Assert.Equal(1m, Get(result, BenefitId.Macierzynski).Rate);
        Assert.Equal(0.7m, Get(result, BenefitId.Rodzicielski).Rate);
    }

    [Fact]
    public void Wniosek_o_wariant_usredniony_przy_urodzeniu_sklada_matka_wiec_ojciec_ma_stawki_standardowe()
    {
        var father = WithVariant(Father(), "2026-03-10");
        var result = Run(Birth(father));

        var card = Get(result, BenefitId.Rodzicielski);
        Assert.Equal(0.7m, card.Rate);
        Assert.Contains(card.Notes, n => n.Contains("składa matka"));
    }

    [Fact]
    public void Bez_planu_kwota_obejmuje_tylko_czesc_rodzica_bez_9_tygodni_drugiego_rodzica()
    {
        var card = Get(Run(Birth(WithBasis(WithVariant(Mother(), "2026-03-10"), 3000m))), BenefitId.Rodzicielski);

        Assert.Equal(287, card.Period!.Days);
        Assert.Equal(0.815m, card.Rate);
        Assert.Empty(card.Parts);
        Assert.Equal(81.5m * 224, card.Amount!.Total);
        Assert.Contains(card.Notes, n => n.Contains("wyłącznie drugiemu rodzicowi") && n.Contains("224 dni"));
    }

    [Fact]
    public void Bez_planu_stawka_standardowa_tez_bez_9_tygodni_drugiego_rodzica()
    {
        var card = Get(Run(Birth(WithBasis(Mother(), 3000m))), BenefitId.Rodzicielski);

        Assert.Equal(287, card.Period!.Days);
        Assert.Equal(70m * 224, card.Amount!.Total);
    }

    [Fact]
    public void Dziewiec_tygodni_drugiego_rodzica_przy_planie_ojca_po_wniosku_matki()
    {
        var mother = WithBasis(WithVariant(Mother(), "2026-03-10"), 3000m) with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(20)]),
        };
        var father = WithBasis(Father(), 3000m) with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(12, D("2027-01-04"))]),
        };

        var result = Run(Birth(mother, father));
        var fatherCard = Get(result, BenefitId.Rodzicielski, PersonRef.DrugiRodzic);
        var motherCard = Get(result, BenefitId.Rodzicielski);

        Assert.Equal(0.815m, motherCard.Rate);
        Assert.Single(motherCard.Parts);
        Assert.Equal(2, fatherCard.Parts.Count);
        Assert.Equal((63, 0.7m), (fatherCard.Parts[0].Period.Days, fatherCard.Parts[0].Rate));
        Assert.Equal((21, 0.815m), (fatherCard.Parts[1].Period.Days, fatherCard.Parts[1].Rate));
        Assert.Equal(70m * 63 + 81.5m * 21, fatherCard.Amount!.Total);
    }

    [Fact]
    public void Ojciec_bez_wniosku_matki_ma_caly_rodzicielski_po_70_proc()
    {
        var mother = Mother() with { PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(20)]) };
        var father = WithBasis(Father(), 3000m) with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(12, D("2027-01-04"))]),
        };

        var fatherCard = Get(Run(Birth(mother, father)), BenefitId.Rodzicielski, PersonRef.DrugiRodzic);

        Assert.Equal(0.7m, fatherCard.Rate);
        Assert.Equal(70m * 84, fatherCard.Amount!.Total);
    }

    [Fact]
    public void Rodzicielski_po_roku_zycia_dziecka_to_70_proc_a_macierzynski_ma_informacje_o_wyrownaniu()
    {
        var me = WithBasis(WithVariant(Mother(), "2026-03-10"), 3000m) with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(8, D("2027-04-05"))]),
        };

        var result = Run(Birth(me));
        var rod = Get(result, BenefitId.Rodzicielski);
        var mat = Get(result, BenefitId.Macierzynski);

        Assert.Equal(0.7m, rod.Rate);
        Assert.Contains(rod.Notes, n => n.Contains("31 ust. 3g"));
        Assert.True(Assert.Single(rod.Trace, t => t.Rule == "rodzicielski_niewykorzystany_w_pierwszym_roku").Met);
        Assert.Equal(0.815m, mat.Rate);
        Assert.Equal(11410m, mat.Amount!.Total);
        Assert.Contains(mat.Notes, n => n.Contains($"Wyrównanie do 100%") && n.Contains($"2{Nbsp}590,00{Nbsp}zł"));
        Assert.Contains(mat.Notes, n => n.Contains("31 ust. 3g i 4"));
    }

    [Fact]
    public void Rodzicielski_zaczynajacy_sie_w_ostatnim_dniu_pierwszego_roku_zostaje_po_81_5_proc()
    {
        var me = WithVariant(Mother(), "2026-03-10") with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(8, D("2027-03-01"))]),
        };

        Assert.Equal(0.815m, Get(Run(Birth(me)), BenefitId.Rodzicielski).Rate);
    }

    [Fact]
    public void Uzupelniajacy_i_ojcowski_zawsze_100_proc_takze_w_wariancie_usrednionym()
    {
        var facts = Birth(WithBasis(WithVariant(Mother(), "2026-03-10"), 3000m));
        var cards = new[]
        {
            Card(BenefitId.MacierzynskiUzupelniajacy, "2026-07-20", 21),
            Card(BenefitId.Ojcowski, "2026-04-01", 14),
        };

        var result = Calculate(Params(), facts, AsOf, cards);

        Assert.All(result.Benefits, b => Assert.Equal(1m, b.Rate));
        Assert.Equal(100m * 21, result.Benefits[0].Amount!.Total);
        Assert.Equal(100m * 14, result.Benefits[1].Amount!.Total);
    }

    [Fact]
    public void Przyjecie_dziecka_w_wariancie_usrednionym_to_81_5_proc_przy_wniosku_po_przyjeciu()
    {
        var facts = Facts(AdoptionCase("2026-03-02", "2024-01-10", AdoptionPath.Przysposobienie), WithBasis(WithVariant(Mother(), "2026-03-20"), 3000m));

        var standard = Calculate(Params(), Facts(AdoptionCase("2026-03-02", "2024-01-10", AdoptionPath.Przysposobienie), WithBasis(Mother(), 3000m)), AsOf,
            [Card(BenefitId.PrzyjecieDziecka, "2026-03-02", 140)]);
        var averaged = Calculate(Params(), facts, AsOf, [Card(BenefitId.PrzyjecieDziecka, "2026-03-02", 140)]);

        Assert.Equal(1m, standard.Benefits[0].Rate);
        Assert.Equal(0.815m, averaged.Benefits[0].Rate);
        Assert.Equal(81.5m * 140, averaged.Benefits[0].Amount!.Total);
    }

    [Fact]
    public void Przyjecie_starszego_dziecka_art_31_ust_3g_i_4_to_nie_rozstrzygam_a_stawka_zostaje_81_5_proc()
    {
        var facts = Facts(AdoptionCase("2026-03-02", "2024-01-10", AdoptionPath.Przysposobienie), WithVariant(Mother(), "2026-03-20"));

        var card = Calculate(Params(), facts, AsOf, [Card(BenefitId.Rodzicielski, "2026-07-20", 287)]).Benefits[0];

        Assert.Equal(0.815m, card.Rate);
        Assert.Null(Assert.Single(card.Trace, t => t.Rule == "rodzicielski_niewykorzystany_w_pierwszym_roku").Met);
    }

    [Fact]
    public void Przejecie_przez_ojca_po_wniosku_matki_to_nie_rozstrzygam_kwoty()
    {
        var facts = Birth(WithBasis(WithVariant(Mother(), "2026-03-10"), 3000m), WithBasis(Father(), 3000m));
        var card = Card(BenefitId.PrzejeciePrzezOjca, "2026-07-20", 28, PersonRef.DrugiRodzic);

        var result = Calculate(Params(), facts, AsOf, [card]).Benefits[0];

        Assert.Null(result.Rate);
        Assert.Null(result.Amount);
        Assert.Contains(result.Notes, n => n.Contains("nie rozstrzygam"));
    }

    [Fact]
    public void Przejecie_przez_ojca_bez_wniosku_matki_to_100_proc()
    {
        var facts = Birth(Mother(), WithBasis(Father(), 3000m));

        var result = Calculate(Params(), facts, AsOf, [Card(BenefitId.PrzejeciePrzezOjca, "2026-07-20", 28, PersonRef.DrugiRodzic)]).Benefits[0];

        Assert.Equal(1m, result.Rate);
        Assert.Equal(2800m, result.Amount!.Total);
    }

    [Fact]
    public void Praca_na_czesc_etatu_pomniejsza_zasilek_za_te_dni_art_31_ust_4a()
    {
        var me = WithBasis(Mother(), 3000m) with
        {
            PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.5m, 20)]),
        };

        var card = Get(Run(Birth(me)), BenefitId.Rodzicielski);

        Assert.Equal(357, card.Period!.Days);
        Assert.Equal(2, card.Parts.Count);
        // Część własna: (41 − 9) tyg. + 10 tyg. wydłużenia z pracy = 294 dni; 9 tyg. drugiego rodzica nie wchodzi do kwoty.
        Assert.Equal((154, 0.7m, 70m * 154), (card.Parts[0].Period.Days, card.Parts[0].Rate, card.Parts[0].Amount!.Total));
        Assert.Equal((140, 0.7m, 35m * 140), (card.Parts[1].Period.Days, card.Parts[1].Rate, card.Parts[1].Amount!.Total));
        Assert.Contains("praca na 0,5 etatu", card.Parts[1].Label);
        Assert.Equal(70m * 154 + 35m * 140, card.Amount!.Total);
        Assert.Equal(70m, card.Amount.Daily);
        Assert.Contains(card.Trace, t => t.Rule == "praca_na_czesc_etatu" && t.Met == true && t.Basis.Contains("art. 31 ust. 4a"));
        Assert.Equal(card.Period.From.AddDays(293), card.Parts[1].Period.To);
    }

    [Fact]
    public void Praca_na_czesc_etatu_z_data_pracy_w_srodku_czesci()
    {
        var me = WithBasis(Mother(), 3000m) with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(10, D("2026-08-03"))]),
            PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.5m, 2, D("2026-08-10"))]),
        };

        var card = Get(Run(Birth(me)), BenefitId.Rodzicielski);

        Assert.Equal([7, 14, 56], card.Parts.Select(p => p.Period.Days).ToArray());
        Assert.Equal(70m * 7 + 35m * 14 + 70m * 56, card.Amount!.Total);
    }

    [Fact]
    public void Praca_ponad_pol_etatu_nie_pomniejsza_zasilku()
    {
        var me = WithBasis(Mother(), 3000m) with
        {
            PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.6m, 10)]),
        };

        var card = Get(Run(Birth(me)), BenefitId.Rodzicielski);

        Assert.Equal(70m * 224, card.Amount!.Total);
        Assert.Contains(card.Notes, n => n.Contains("Pominięto wpis o pracy na część etatu"));
    }

    [Fact]
    public void Zasilek_zmienia_sie_tylko_w_dniach_pracy_gdy_praca_przecina_date_zmiany_parametru()
    {
        var me = WithBasis(Mother(), 3000m) with
        {
            PlanRodzicielskiCzesci = F<IReadOnlyList<LeavePart>>([new LeavePart(6, D("2026-12-14"))]),
            PlanPracaNaCzescEtatu = F<IReadOnlyList<PartTimeWork>>([new PartTimeWork(0.5m, 2, D("2026-12-28"))]),
        };

        var card = Get(Run(Birth(me), null, null, Row(AmountsParameterKeys.StawkaRodzicielski, 0.75m, "2027-01-01")), BenefitId.Rodzicielski);

        Assert.Equal(
            [(D("2026-12-14"), D("2026-12-27"), 0.7m), (D("2026-12-28"), D("2026-12-31"), 0.7m), (D("2027-01-01"), D("2027-01-10"), 0.75m), (D("2027-01-11"), D("2027-01-31"), 0.75m)],
            card.Parts.Select(p => (p.Period.From, p.Period.To, p.Rate)).ToArray());
        Assert.Equal(70m * 14 + 35m * 4 + 37.5m * 10 + 75m * 21, card.Amount!.Total);
    }
}
