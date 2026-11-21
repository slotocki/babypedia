namespace BabyRadnik.Domain.Contracts;

/// <summary>Zamknięta lista kluczy faktów. Źródło prawdy: contracts/facts.schema.json. Zmienia tylko koordynator.</summary>
public static class FactKeys
{
    public const string ZdarzenieRodzaj = "zdarzenie.rodzaj";
    public const string ZdarzenieData = "zdarzenie.data";
    public const string ZdarzenieDlaKogo = "zdarzenie.dlaKogo";

    public const string DzieckoLiczba = "dziecko.liczba";
    public const string DzieckoDataUrodzenia = "dziecko.dataUrodzenia";
    public const string DzieckoTydzienCiazyPrzyPorodzie = "dziecko.tydzienCiazyPrzyPorodzie";
    public const string DzieckoMasaUrodzeniowaG = "dziecko.masaUrodzeniowaG";
    public const string DzieckoPobytyWSzpitalu = "dziecko.pobytyWSzpitalu";
    public const string DzieckoZaZyciem = "dziecko.zaZyciem";
    public const string DzieckoMartweUrodzenie = "dziecko.martweUrodzenie";
    public const string DzieckoLiczbaZmarlych = "dziecko.liczbaZmarlych";
    public const string DzieckoDataZgonu = "dziecko.dataZgonu";
    public const string DzieckoDataPochowku = "dziecko.dataPochowku";

    public const string PrzyjecieSciezka = "przyjecie.sciezka";
    public const string PrzyjecieWniosekDoSadu = "przyjecie.wniosekDoSadu";
    public const string PrzyjecieDataUprawomocnieniaPostanowienia = "przyjecie.dataUprawomocnieniaPostanowienia";
    public const string PrzyjecieOdroczenieObowiazkuSzkolnego = "przyjecie.odroczenieObowiazkuSzkolnego";

    public const string MatkaSytuacjaSzczegolna = "matka.sytuacjaSzczegolna";

    public const string OsobaRola = "osoba.rola";
    public const string OsobaDataUrodzenia = "osoba.dataUrodzenia";
    public const string OsobaDataUkonczeniaSzkoly = "osoba.dataUkonczeniaSzkoly";

    public const string UbezpieczenieTytuly = "ubezpieczenie.tytuly";
    public const string UbezpieczeniePodstawySkladek = "ubezpieczenie.podstawySkladek";
    public const string UbezpieczeniePrzerwy = "ubezpieczenie.przerwy";
    public const string UbezpieczeniePobraneSwiadczenia = "ubezpieczenie.pobraneSwiadczenia";
    public const string UbezpieczenieLatWczesniejszegoObowiazkowego = "ubezpieczenie.latWczesniejszegoObowiazkowego";
    public const string UbezpieczeniePodlegaChorobowemu = "ubezpieczenie.podlegaChorobowemu";
    public const string UbezpieczenieUstaniePrzyczyna = "ubezpieczenie.ustaniePrzyczyna";

    public const string UmowaNaCzasOkreslony = "umowa.naCzasOkreslony";
    public const string UmowaDataKonca = "umowa.dataKonca";
    public const string UmowaPlanowaneOdejscieData = "umowa.planowaneOdejscieData";

    public const string PlatnikLiczbaUbezpieczonych = "platnik.liczbaUbezpieczonych";

    public const string SkladkiZadluzenie = "skladki.zadluzenie";
    public const string SkladkiDataSplaty = "skladki.dataSplaty";

    public const string EzlaZwolnienia = "ezla.zwolnienia";

    public const string PodstawaUstalonaPrzezPlatnika = "podstawa.ustalonaPrzezPlatnika";

    public const string PlanWariantWyplaty = "plan.wariantWyplaty";
    public const string PlanDataWnioskuUsrednionego = "plan.dataWnioskuUsrednionego";
    public const string PlanMacierzynskiPrzedPorodemTyg = "plan.macierzynskiPrzedPorodemTyg";
    public const string PlanRezygnacjaZMacierzynskiegoPoTyg = "plan.rezygnacjaZMacierzynskiegoPoTyg";
    public const string PlanRodzicielskiCzesci = "plan.rodzicielskiCzesci";
    public const string PlanPracaNaCzescEtatu = "plan.pracaNaCzescEtatu";
    public const string PlanOjcowskiCzesci = "plan.ojcowskiCzesci";
}
