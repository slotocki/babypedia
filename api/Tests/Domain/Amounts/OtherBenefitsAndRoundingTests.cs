using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Domain.Periods;
using BabyRadnik.Tests.Reference;
using static BabyRadnik.Tests.Domain.Amounts.AmountsKit;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Amounts;

public sealed class OtherBenefitsAndRoundingTests
{
    private const string Nbsp = " ";

    private static FactSet Birth(PersonFacts? me = null, string date = "2026-03-02", EventKind kind = EventKind.Urodzenie) =>
        Facts(BirthCase(date, kind: kind), me ?? Mother());

    private static PersonFacts WithBasis(PersonFacts person, decimal amount) => person with { PodstawaUstalonaPrzezPlatnika = F(amount) };

    [Fact]
    public void Brak_podstawy_zwraca_okresy_i_stawki_bez_kwoty()
    {
        var result = Run(Birth());

        foreach (var id in new[] { BenefitId.Macierzynski, BenefitId.Rodzicielski })
        {
            var card = Get(result, id);
            Assert.NotNull(card.Rate);
            Assert.NotNull(card.Period);
            Assert.Null(card.Amount);
            Assert.Contains(card.Notes, n => n.Contains("podaj podstawę, żeby zobaczyć kwotę"));
            Assert.Contains(card.Trace, t => t.Rule == "brak_podstawy" && t.Met is null);
        }

        Assert.Equal(1m, Get(result, BenefitId.Macierzynski).Rate);
        Assert.Equal(0.7m, Get(result, BenefitId.Rodzicielski).Rate);
    }

    [Fact]
    public void Podstawa_ustalona_przez_platnika_ma_pierwszenstwo_przed_przekazana_przez_wywolujacego()
    {
        var result = Run(Birth(WithBasis(Mother(), 3000m)), BasisOf(9999m));

        var amount = Get(result, BenefitId.Macierzynski).Amount!;
        Assert.Equal(3000m, amount.Basis);
        Assert.Equal(BasisSource.UstalonaPrzezPlatnika, amount.BasisSource);
    }

    [Fact]
    public void Podstawa_przekazana_przez_wywolujacego_zachowuje_zrodlo()
    {
        var amount = Get(Run(Birth(), BasisOf(3000m, BasisSource.Minimalna)), BenefitId.Macierzynski).Amount!;

        Assert.Equal(BasisSource.Minimalna, amount.BasisSource);
        Assert.Equal(3000m, amount.Basis);
    }

    [Fact]
    public void Zaokraglenie_stawka_dzienna_do_grosza_potem_stawka_procentowa_potem_grosz_i_mnozenie_przez_dni()
    {
        var me = WithVariant(WithBasis(Mother(), 5000m), "2026-03-10");

        var amount = Get(Run(Birth(me)), BenefitId.Macierzynski).Amount!;

        Assert.Equal(135.84m, amount.Daily);
        Assert.Equal(135.84m * 140, amount.Total);
        Assert.Equal(19017.60m, amount.Total);
    }

    [Fact]
    public void Tryb_iloczyn_na_koncu_zaokragla_dopiero_kwote_za_okres()
    {
        var me = WithBasis(Mother(), 5000m);
        var amount = Get(Run(Birth(me), null, null, TextRow(AmountsParameterKeys.ZaokraglanieKwotaZaOkres, "iloczyn_na_koncu", "2001-01-01")),
            BenefitId.Macierzynski).Amount!;

        Assert.Equal(166.67m, amount.Daily);
        Assert.Equal(23333.33m, amount.Total);
    }

    [Fact]
    public void Nieznany_tryb_zaokraglania_to_nie_rozstrzygam_kwoty_ale_stawka_zostaje()
    {
        var me = WithBasis(Mother(), 5000m);
        var card = Get(Run(Birth(me), null, null, TextRow(AmountsParameterKeys.ZaokraglanieKwotaZaOkres, "inny", "2001-01-01")), BenefitId.Macierzynski);

        Assert.Null(card.Amount);
        Assert.Equal(1m, card.Rate);
        Assert.Contains(card.Notes, n => n.Contains("Nieznane ustawienie zaokrąglania") && n.Contains("nie rozstrzygam"));
    }

    [Fact]
    public void Liczba_miejsc_po_przecinku_pochodzi_z_parametru()
    {
        var me = WithBasis(Mother(), 5000m);
        var amount = Get(Run(Birth(me), null, null, Row(AmountsParameterKeys.ZaokraglanieMiejsca, 1, "2001-01-01")), BenefitId.Macierzynski).Amount!;

        Assert.Equal(166.7m, amount.Daily);
        Assert.Equal(166.7m * 140, amount.Total);
    }

    [Fact]
    public void Brak_parametru_spoza_seed_to_nie_rozstrzygam_kwoty_ale_stawka_zostaje()
    {
        var parameters = new WithoutKey(SeedParameterProvider.Load(PeriodsTestSupport.Supplemental), AmountsParameterKeys.PodstawaDzielnikDzienny);
        var facts = Birth(WithBasis(Mother(), 3000m));
        var cards = new[] { Card(BenefitId.Macierzynski, "2026-03-02", 140) };

        var card = Calculate(parameters, facts, AsOf, cards).Benefits[0];

        Assert.Equal(1m, card.Rate);
        Assert.Null(card.Amount);
        Assert.Contains(card.Notes, n => n.Contains(AmountsParameterKeys.PodstawaDzielnikDzienny) && n.Contains("Nie rozstrzygam"));
    }

    [Fact]
    public void Karty_bez_statusu_przysluguje_nie_sa_zmieniane()
    {
        var card = new BenefitResult { Id = BenefitId.Macierzynski, Status = BenefitStatus.NiePrzysluguje, Reason = "powód" };

        var result = Calculate(Params(), Birth(WithBasis(Mother(), 3000m)), AsOf, [card]);

        Assert.Same(card, result.Benefits[0]);
    }

    [Fact]
    public void Karta_bez_ustalonego_okresu_dostaje_stawke_bez_kwoty()
    {
        var mother = Mother();
        var father = WithBasis(Father(), 3000m);

        var result = Run(Facts(BirthCase("2026-03-02"), father, mother));

        var card = Get(result, BenefitId.Rodzicielski);
        Assert.Null(card.Period);
        Assert.Equal(0.7m, card.Rate);
        Assert.Null(card.Amount);
        Assert.Contains(card.Notes, n => n.Contains("nie jest jeszcze ustalony"));
    }

    [Fact]
    public void Chorobowy_w_ciazy_to_100_proc_podstawy()
    {
        var facts = Birth(WithBasis(Mother(), 3000m), "2026-09-01", EventKind.Ciaza);

        var card = Calculate(Params(), facts, AsOf, [Card(BenefitId.ChorobowyWCiazy, "2026-06-01", 10)]).Benefits[0];

        Assert.Equal(1m, card.Rate);
        Assert.Equal(1000m, card.Amount!.Total);
        Assert.Equal("ustawa art. 11 ust. 2 pkt 1", card.Trace.Single(t => t.Rule == "stawka_chorobowy_w_ciazy").Basis);
    }

    [Fact]
    public void Rehabilitacyjne_90_proc_przez_trzy_miesiace_potem_75_proc()
    {
        var facts = Birth(WithBasis(Mother(), 3000m));

        var card = Calculate(Params(), facts, AsOf, [Card(BenefitId.Rehabilitacyjne, "2026-05-01", 100)]).Benefits[0];

        Assert.Equal(2, card.Parts.Count);
        Assert.Equal((90, 0.9m, 90m * 90), (card.Parts[0].Period.Days, card.Parts[0].Rate, card.Parts[0].Amount!.Total));
        Assert.Equal((10, 0.75m, 75m * 10), (card.Parts[1].Period.Days, card.Parts[1].Rate, card.Parts[1].Amount!.Total));
        Assert.Equal(0.9m, card.Rate);
        Assert.Equal(8850m, card.Amount!.Total);
    }

    [Fact]
    public void Rehabilitacyjne_w_okresie_ciazy_100_proc_potem_stawki_podstawowe()
    {
        var facts = Birth(WithBasis(Mother(), 3000m), "2026-09-01", EventKind.Ciaza);

        var card = Calculate(Params(), facts, AsOf, [Card(BenefitId.Rehabilitacyjne, "2026-08-20", 20)]).Benefits[0];

        Assert.Equal([(12, 1m), (8, 0.9m)], card.Parts.Select(p => (p.Period.Days, p.Rate)).ToArray());
        Assert.Equal(100m * 12 + 90m * 8, card.Amount!.Total);
    }

    [Fact]
    public void Zasilek_do_dnia_porodu_ma_wysokosc_zasilku_macierzynskiego()
    {
        var facts = Birth(WithBasis(Mother(), 3000m), "2026-09-01", EventKind.Ciaza);

        var card = Calculate(Params(), facts, AsOf, [Card(BenefitId.ZasilekDoDniaPorodu, "2026-08-01", 31)]).Benefits[0];

        Assert.Equal(1m, card.Rate);
        Assert.Equal(3100m, card.Amount!.Total);
    }

    [Fact]
    public void Zasilek_pogrzebowy_to_kwota_z_parametru()
    {
        var facts = Facts(BirthCase("2026-03-02") with { DzieckoMartweUrodzenie = F(true) });
        var card = new BenefitResult { Id = BenefitId.Pogrzebowy, Status = BenefitStatus.Przysluguje };

        var result = Calculate(Params(), facts, AsOf, [card]).Benefits[0];

        Assert.Contains(result.Notes, n => n.Contains($"7{Nbsp}000,00{Nbsp}zł"));
        Assert.Contains(result.Trace, t => t.Rule == "zasilek_pogrzebowy" && t.Met == true);
        Assert.Null(result.Amount);
    }

    [Fact]
    public void Kwota_swiadczenia_rodzicielskiego_pusta_to_informacja_bez_podwyzszenia()
    {
        var card = Get(Run(Birth(WithBasis(Mother(), 3000m))), BenefitId.Macierzynski);

        Assert.Equal(14000m, card.Amount!.Total);
        Assert.Contains(card.Notes, n => n.Contains("Nie stosuję podwyższenia z art. 31 ust. 3a"));
        Assert.Contains(card.Trace, t => t.Rule == "podwyzszenie_do_swiadczenia_rodzicielskiego" && t.Met is null);
    }

    [Fact]
    public void Kwoty_brutto_z_dopiskiem_wg_parametru()
    {
        var card = Get(Run(Birth(WithBasis(Mother(), 3000m))), BenefitId.Macierzynski);

        Assert.Contains(card.Notes, n => n.Contains("Kwoty brutto"));
        Assert.Contains(card.Trace, t => t.Rule == "kwoty_brutto" && t.Met == true);
    }

    [Fact]
    public void Sciezka_decyzji_zawiera_stawke_dzienna_i_kwote_za_okres_z_podstawa_prawna()
    {
        var card = Get(Run(Birth(WithBasis(Mother(), 6903.20m))), BenefitId.Macierzynski);

        var daily = card.Trace.Single(t => t.Rule == "stawka_dzienna");
        Assert.Contains("230,11", daily.Text);
        Assert.Contains("art. 36 ust. 3", daily.Basis);
        var total = card.Trace.Single(t => t.Rule == "kwota_za_okres");
        Assert.Contains("art. 31", total.Basis);
        Assert.Contains(card.Trace, t => t.Rule == "stawka_macierzynski" && t.Basis.Contains("art. 31 ust. 1"));
    }

    [Fact]
    public void Kazda_regula_ma_id_i_podstawe_prawna_a_idy_sa_unikalne()
    {
        var rules = AmountRules.All;

        Assert.All(rules, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Id));
            Assert.False(string.IsNullOrWhiteSpace(r.PodstawaPrawna));
        });
        Assert.Equal(rules.Count, rules.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void Klucze_spoza_seed_sa_wypisane_w_NotInSeed()
    {
        Assert.Equal(AmountsKit.Supplemental.Select(r => r.Key).Order(), AmountsParameterKeys.NotInSeed.Order());
    }

    [Fact]
    public void Odliczenie_skladek_dla_zleceniobiorcy_bez_minimum_i_limitu()
    {
        var basis = new BasisAdjuster(Params()).FromRevenue(1000m, D("2026-03-02"), new BasisProfile(TitleKind.Zleceniobiorca, SicknessInsurance.Obowiazkowe));

        Assert.Equal(862.90m, basis.Amount);
        Assert.Equal(BasisSource.SzacunekZus, basis.Source);
        Assert.Contains(basis.Trace, t => t.Rule == "odliczenie_skladek");
    }

    [Fact]
    public void Minimalna_podstawa_ma_sciezke_decyzji_z_art_45()
    {
        var basis = new BasisAdjuster(Params()).FromRevenue(1000m, D("2026-03-02"), new BasisProfile(TitleKind.Pracownik, SicknessInsurance.Obowiazkowe, 0.5m));

        var step = basis.Trace.Single(t => t.Rule == "minimalna_podstawa");
        Assert.True(step.Met);
        Assert.Contains("art. 45", step.Basis);
        Assert.Contains(basis.Notes, n => n.Contains("niepełnego etatu"));
    }

    private sealed class WithoutKey(IParameterProvider inner, string hidden) : IParameterProvider
    {
        public Parameter Get(string key, DateOnly date) =>
            key == hidden ? throw new ParameterNotFoundException(key, date) : inner.Get(key, date);
    }
}
