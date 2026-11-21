namespace BabyRadnik.Domain.Periods;

public static class PeriodsParameterKeys
{
    public const string MacierzynskiPrzedPorodemMax = "macierzynski_przed_porodem_max_tyg";
    public const string MatkaMinPoPorodzieTyg = "matka_min_po_porodzie_tyg";
    public const string MatkaMinPoPorodzieSzczegolneTyg = "matka_min_po_porodzie_szczegolne_tyg";

    public const string RodzicielskiNieprzenoszalneTyg = "rodzicielski_nieprzenoszalne_tyg";
    public const string RodzicielskiMaxCzesci = "rodzicielski_max_czesci";
    public const string RodzicielskiDoKoncaRokuWieku = "rodzicielski_do_konca_roku_wieku";
    public const string PracaNaRodzicielskimMaxEtat = "praca_na_rodzicielskim_max_etat";

    public const string OjcowskiTyg = "ojcowski_tyg";
    public const string OjcowskiMaxCzesci = "ojcowski_tyg.max_czesci";
    public const string OjcowskiMinCzescTyg = "ojcowski_tyg.min_czesc_tyg";
    public const string OjcowskiDoMiesiacaZycia = "ojcowski_do_miesiaca_zycia";

    public const string PrzyjecieMinTyg = "przyjecie_min_tyg";
    public const string PrzyjecieRodzicielskiPrzyMinimum = "przyjecie_rodzicielski_przy_minimum_tyg";
    public const string PrzyjecieRodzicielskiPrzyMinimumZaZyciem = "przyjecie_rodzicielski_przy_minimum_tyg.za_zyciem";
    public const string WiekPrzysposobienieMax = "wiek_przysposobienie_max";
    public const string WiekRodzinaZastepczaMax = "wiek_rodzina_zastepcza_max";
    public const string WiekRodzinaZastepczaMaxOdroczenie = "wiek_rodzina_zastepcza_max.przy_odroczeniu_obowiazku_szkolnego";

    public const string MartweUrodzenieTygPoPorodzie = "martwe_urodzenie_tyg_po_porodzie";
    public const string MartweUrodzenieMinDniOdZgonu = "martwe_urodzenie_tyg_po_porodzie.min_dni_od_zgonu";
    public const string ZgonPo8TygDni = "zgon_po_8_tyg_dni";

    public const string StawkaMacierzynski = "stawka_macierzynski";
    public const string StawkaUzupelniajacy = "stawka_uzupelniajacy";
    public const string StawkaOjcowski = "stawka_ojcowski";
    public const string StawkaRodzicielski = "stawka_rodzicielski";

    public const string UzupelniajacyMaxTygWczesniak = "uzupelniajacy_max_tyg.wczesniak";
    public const string UzupelniajacyMaxTygPrzedwczesny = "uzupelniajacy_max_tyg.przedwczesny";
    public const string UzupelniajacyMaxTygDonoszony = "uzupelniajacy_max_tyg.donoszony";
    public const string UzupelniajacyProgTydzienWczesniak = "uzupelniajacy_prog_tydzien_ciazy.wczesniak";
    public const string UzupelniajacyProgTydzienDonoszony = "uzupelniajacy_prog_tydzien_ciazy.donoszony";
    public const string UzupelniajacyProgMasaG = "uzupelniajacy_prog_masa_g";
    public const string UzupelniajacyDonoszonyMinKolejneDni = "uzupelniajacy_donoszony_min_kolejne_dni";
    public const string UzupelniajacyDonoszonyPierwszyDzienOd = "uzupelniajacy_donoszony_pierwszy_dzien_od";
    public const string UzupelniajacyDonoszonyPierwszyDzienDo = "uzupelniajacy_donoszony_pierwszy_dzien_do";
    public const string UzupelniajacyDonoszonyLiczenieOdDnia = "uzupelniajacy_donoszony_liczenie_od_dnia";

    public static string MacierzynskiTyg(int children) =>
        children >= 5 ? "macierzynski_tyg.dzieci_5_i_wiecej" : $"macierzynski_tyg.dzieci_{children}";

    public static string RodzicielskiTyg(int children, bool zaZyciem) =>
        $"{(zaZyciem ? "rodzicielski_za_zyciem_tyg" : "rodzicielski_tyg")}.{(children == 1 ? "jedno_dziecko" : "wiecej_dzieci")}";

    public static string RodzicielskiZPracaMaxTyg(int children, bool zaZyciem) =>
        $"rodzicielski_z_praca_max_tyg.{(zaZyciem ? "za_zyciem_" : "")}{(children == 1 ? "jedno_dziecko" : "wiecej_dzieci")}";

    public static readonly IReadOnlyList<string> NotInSeed =
    [
        UzupelniajacyMaxTygWczesniak,
        UzupelniajacyMaxTygPrzedwczesny,
        UzupelniajacyMaxTygDonoszony,
        UzupelniajacyProgTydzienWczesniak,
        UzupelniajacyProgTydzienDonoszony,
        UzupelniajacyProgMasaG,
        UzupelniajacyDonoszonyMinKolejneDni,
        UzupelniajacyDonoszonyPierwszyDzienOd,
        UzupelniajacyDonoszonyPierwszyDzienDo,
        UzupelniajacyDonoszonyLiczenieOdDnia,
    ];
}
