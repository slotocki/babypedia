namespace BabyRadnik.Domain.Amounts;

public static class AmountsParameterKeys
{
    public const string StawkaMacierzynski = "stawka_macierzynski";
    public const string StawkaUzupelniajacy = "stawka_uzupelniajacy";
    public const string StawkaOjcowski = "stawka_ojcowski";
    public const string StawkaRodzicielski = "stawka_rodzicielski";
    public const string StawkaUsredniona = "stawka_usredniona";
    public const string Stawka9TygDrugiegoRodzica = "stawka_9_tyg_drugiego_rodzica";
    public const string TerminWnioskuUsredniona = "termin_wniosku_usredniona_dni";
    public const string RodzicielskiNieprzenoszalneTyg = "rodzicielski_nieprzenoszalne_tyg";
    public const string PracaNaRodzicielskimMaxEtat = "praca_na_rodzicielskim_max_etat";

    public const string ChorobowyCiazaStawka = "chorobowy_ciaza_stawka";
    public const string RehabPierwszeMiesiace = "rehabilitacyjne_stawki.pierwsze_miesiace";
    public const string RehabPierwszeMiesiaceLiczba = "rehabilitacyjne_stawki.pierwsze_miesiace_liczba";
    public const string RehabKolejneMiesiace = "rehabilitacyjne_stawki.kolejne_miesiace";
    public const string RehabWCiazy = "rehabilitacyjne_stawki.w_ciazy";

    public const string OdliczenieSkladek = "odliczenie_skladek";
    public const string PodstawaMinimalnaPelnyEtat = "podstawa_minimalna_pelny_etat";
    public const string MaxPodstawaDobrowolnej = "max_podstawa_dobrowolnej_skladki";
    public const string LimitPodstawyZasilku = "max_podstawa_dobrowolnej_skladki.limit_podstawy_zasilku";

    public const string ZasilekPogrzebowy = "zasilek_pogrzebowy";
    public const string KwotaSwiadczeniaRodzicielskiego = "kwota_swiadczenia_rodzicielskiego";
    public const string ZaokraglanieMiejsca = "zaokraglanie.stawka_dzienna_miejsca";
    public const string ZaokraglanieKwotaZaOkres = "zaokraglanie.kwota_za_okres";
    public const string Kwoty = "kwoty";

    /// <summary>art. 36 ust. 3 w zw. z art. 47: podstawa za jeden dzień = 1/30 podstawy miesięcznej.</summary>
    public const string PodstawaDzielnikDzienny = "podstawa_dzielnik_dzienny";

    /// <summary>art. 11 ust. 5 w zw. z art. 22: miesiąc = 30 dni (okres trzech miesięcy świadczenia rehabilitacyjnego).</summary>
    public const string MiesiacDni = "miesiac_dni";

    /// <summary>art. 31 ust. 3g i 4: „pierwszy rok życia dziecka” = 12 miesięcy od urodzenia.</summary>
    public const string PierwszyRokZyciaMiesiecy = "pierwszy_rok_zycia_miesiecy";

    public const string KwotaZaOkresStawkaDziennaXDni = "stawka_dzienna_x_dni";
    public const string KwotaZaOkresIloczynNaKoncu = "iloczyn_na_koncu";

    public static readonly IReadOnlyList<string> NotInSeed =
    [
        PodstawaDzielnikDzienny,
        MiesiacDni,
        PierwszyRokZyciaMiesiecy,
    ];
}
