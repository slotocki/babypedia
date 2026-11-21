namespace BabyRadnik.Domain.Eligibility;

public static class EligibilityParameterKeys
{
    public const string ProgZadluzenia = "prog_zadluzenia";
    public const string TerminSplatyZadluzeniaMies = "termin_splaty_zadluzenia_mies";
    public const string ProgPlatnikaUbezpieczonych = "prog_platnika_ubezpieczonych";
    public const string WyczekiwanieObowiazkoweDni = "wyczekiwanie_obowiazkowe_dni";
    public const string WyczekiwanieDobrowolneDni = "wyczekiwanie_dobrowolne_dni";
    public const string WyczekiwanieMaxPrzerwaDni = "wyczekiwanie_max_przerwa_dni";
    public const string WynagrodzenieChorobowoDoPieciudziesieciu = "wynagrodzenie_chorobowe_dni.do_50_lat";
    public const string WynagrodzenieChorobowoOdPieciudziesieciu = "wynagrodzenie_chorobowe_dni.od_50_lat";
    public const string ChorobowyCiazaMaxDni = "chorobowy_ciaza_max_dni";
    public const string RehabilitacyjneMaxMiesiecy = "rehabilitacyjne_max_miesiecy";
    public const string MatkaMinPoPorodzieTyg = "matka_min_po_porodzie_tyg";
    public const string MatkaMinPoPorodzieSzczegolneTyg = "matka_min_po_porodzie_szczegolne_tyg";

    public const string WyczekiwanieWyjatekLatObowiazkowego = "wyczekiwanie_wyjatek_lat_obowiazkowego";
    public const string WyczekiwanieAbsolwentMaxDni = "wyczekiwanie_absolwent_max_dni";
    public const string WynagrodzenieChorobowoWiekLat = "wynagrodzenie_chorobowe_wiek_lat";
    public const string CiazaUmowaPrzedluzenieMiesiace = "ciaza_umowa_przedluzenie_miesiace";
    public const string CiazaTrwanieTyg = "ciaza_trwanie_tyg";
    public const string ChorobowyPoUstaniuStartDni = "chorobowy_po_ustaniu_start_dni";
    public const string ChorobowyPoUstaniuMinDni = "chorobowy_po_ustaniu_min_dni";
    public const string ChorobowyPoUstaniuZakaznaMiesiace = "chorobowy_po_ustaniu_zakazna_miesiace";
    public const string TerminWyplatyDni = "termin_wyplaty_dni";
    public const string TerminWyplatyPoSplacieDni = "termin_wyplaty_po_splacie_dni";

    public sealed record SupplementRow(string Key, decimal Value, string LegalBasis);

    /// <summary>
    /// Liczby z przepisów, których nie ma w fixtures/parameters.seed.json. Silnik NIE używa ich jako wartości domyślnych:
    /// bez wiersza w dostawcy parametrów odpowiednia reguła zwraca „nie rozstrzygam”. Lista do dodania do seed przez be/params
    /// (używa jej tylko harness testowy).
    /// </summary>
    public static IReadOnlyList<SupplementRow> Supplement { get; } =
    [
        new(WyczekiwanieWyjatekLatObowiazkowego, 10, "ustawa art. 4 ust. 3 pkt 3"),
        new(WyczekiwanieAbsolwentMaxDni, 90, "ustawa art. 4 ust. 3 pkt 1"),
        new(WynagrodzenieChorobowoWiekLat, 50, "KP art. 92 § 1 pkt 1; Załącznik ZUS"),
        new(CiazaUmowaPrzedluzenieMiesiace, 3, "KP art. 177 § 3"),
        new(CiazaTrwanieTyg, 40, "konwencja położnicza (nie przepis); używane tylko, gdy brak faktu dziecko.tydzienCiazyPrzyPorodzie"),
        new(ChorobowyPoUstaniuStartDni, 14, "ustawa art. 7 pkt 1"),
        new(ChorobowyPoUstaniuMinDni, 30, "ustawa art. 7"),
        new(ChorobowyPoUstaniuZakaznaMiesiace, 3, "ustawa art. 7 pkt 2"),
        new(TerminWyplatyDni, 30, "ustawa art. 64 ust. 1"),
        new(TerminWyplatyPoSplacieDni, 60, "ustawa art. 64 ust. 3"),
    ];
}
