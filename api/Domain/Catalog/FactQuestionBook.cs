using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Catalog;

public sealed record FactQuestion(
    string Fact,
    MissingFactType Type,
    string Question,
    string? QuestionAboutOther = null,
    IReadOnlyList<string>? Options = null);

/// <summary>Słownik pytań dla wszystkich kluczy faktów z kontraktu (facts.schema.json). Wartości wyliczeń to wartości z kontraktu; etykiety polskie dodaje interfejs.</summary>
public static class FactQuestionBook
{
    private static readonly string[] EventKinds = ["planuje", "ciaza", "urodzenie", "przyjecie"];
    private static readonly string[] Scopes = ["ja", "ja_i_drugi_rodzic"];
    private static readonly string[] Paths = ["przysposobienie", "rodzina_zastepcza_niezawodowa", "rodzina_zastepcza_zawodowa"];
    private static readonly string[] MotherSituations =
        ["niezdolnosc_do_samodzielnej_egzystencji", "pobyt_w_szpitalu", "zgon", "porzucenie_dziecka"];
    private static readonly string[] Roles = ["matka", "ojciec"];
    private static readonly string[] EndReasons =
    [
        "wypowiedzenie_przez_pracownika", "wypowiedzenie_przez_pracodawce", "porozumienie_stron", "koniec_umowy_na_czas_okreslony",
        "likwidacja_pracodawcy", "upadlosc_pracodawcy", "smierc_pracodawcy", "naruszenie_prawa_orzeczone_przez_sad",
        "zamkniecie_dzialalnosci", "inna",
    ];
    private static readonly string[] Variants = ["standardowy", "usredniony"];

    private static FactQuestion Case(string key, MissingFactType type, string question, string[]? options = null) =>
        new(key, type, question, null, options);

    private static FactQuestion Person(string key, MissingFactType type, string self, string other, string[]? options = null) =>
        new(key, type, self, other, options);

    public static IReadOnlyDictionary<string, FactQuestion> All { get; } = new[]
    {
        Case(FactKeys.ZdarzenieRodzaj, MissingFactType.Enum, "Co się dzieje: planujesz dziecko, jesteś w ciąży, dziecko się urodziło czy przyjmujesz dziecko na wychowanie?", EventKinds),
        Case(FactKeys.ZdarzenieData, MissingFactType.Date, "Jaka jest przewidywana data porodu, data urodzenia dziecka albo data przyjęcia dziecka?"),
        Case(FactKeys.ZdarzenieDlaKogo, MissingFactType.Enum, "Dla kogo liczymy: tylko dla Ciebie czy dla Ciebie i drugiego rodzica?", Scopes),
        Case(FactKeys.DzieckoLiczba, MissingFactType.Integer, "Ile dzieci rodzi się (albo zostaje przyjętych) jednocześnie?"),
        Case(FactKeys.DzieckoDataUrodzenia, MissingFactType.Date, "Jaka jest data urodzenia dziecka?"),
        Case(FactKeys.DzieckoTydzienCiazyPrzyPorodzie, MissingFactType.Integer, "W którym tygodniu ciąży odbył się poród (albo jest planowany)?"),
        Case(FactKeys.DzieckoMasaUrodzeniowaG, MissingFactType.Integer, "Ile gramów ważyło dziecko po urodzeniu (przy kilkorgu dzieci — to z najniższą masą)?"),
        Case(FactKeys.DzieckoPobytyWSzpitalu, MissingFactType.List, "W jakich terminach dziecko przebywało w szpitalu po porodzie (przy kilkorgu dzieci — to z najdłuższym pobytem)?"),
        Case(FactKeys.DzieckoZaZyciem, MissingFactType.Boolean, "Czy dziecko ma zaświadczenie „Za życiem”?"),
        Case(FactKeys.DzieckoMartweUrodzenie, MissingFactType.Boolean, "Czy dziecko urodziło się martwe?"),
        Case(FactKeys.DzieckoLiczbaZmarlych, MissingFactType.Integer, "Ile z dzieci urodzonych przy jednym porodzie zmarło lub urodziło się martwych?"),
        Case(FactKeys.DzieckoDataZgonu, MissingFactType.Date, "Kiedy zmarło dziecko?"),
        Case(FactKeys.DzieckoDataPochowku, MissingFactType.Date, "Kiedy odbył się pochówek dziecka?"),
        Case(FactKeys.PrzyjecieSciezka, MissingFactType.Enum, "Na jakich zasadach przyjmujesz dziecko: przysposobienie, rodzina zastępcza niezawodowa czy zawodowa?", Paths),
        Case(FactKeys.PrzyjecieWniosekDoSadu, MissingFactType.Boolean, "Czy złożono do sądu opiekuńczego wniosek o przysposobienie dziecka?"),
        Case(FactKeys.PrzyjecieDataUprawomocnieniaPostanowienia, MissingFactType.Date, "Kiedy uprawomocniło się postanowienie o przysposobieniu dziecka?"),
        Case(FactKeys.PrzyjecieOdroczenieObowiazkuSzkolnego, MissingFactType.Boolean, "Czy dla dziecka podjęto decyzję o odroczeniu obowiązku szkolnego?"),
        Case(FactKeys.MatkaSytuacjaSzczegolna, MissingFactType.Enum, "Czy matka dziecka jest w jednej z sytuacji szczególnych: niezdolność do samodzielnej egzystencji, pobyt w szpitalu, zgon albo porzucenie dziecka?", MotherSituations),

        Person(FactKeys.OsobaRola, MissingFactType.Enum, "Czy jesteś matką, czy ojcem dziecka?", "Czy drugi rodzic jest matką, czy ojcem dziecka?", Roles),
        Person(FactKeys.OsobaDataUrodzenia, MissingFactType.Date, "Jaka jest Twoja data urodzenia?", "Jaka jest data urodzenia drugiego rodzica?"),
        Person(FactKeys.OsobaDataUkonczeniaSzkoly, MissingFactType.Date, "Kiedy ukończyłaś lub ukończyłeś szkołę (dotyczy absolwentów)?", "Kiedy drugi rodzic ukończył szkołę (dotyczy absolwentów)?"),
        Person(FactKeys.UbezpieczenieTytuly, MissingFactType.List, "Jakie masz tytuły ubezpieczenia (zatrudnienie, zlecenie, działalność) i czy obejmują ubezpieczenie chorobowe?", "Jakie tytuły ubezpieczenia ma drugi rodzic (zatrudnienie, zlecenie, działalność) i czy obejmują ubezpieczenie chorobowe?"),
        Person(FactKeys.UbezpieczeniePodstawySkladek, MissingFactType.List, "Jakie były podstawy Twoich składek w ostatnich 12 miesiącach?", "Jakie były podstawy składek drugiego rodzica w ostatnich 12 miesiącach?"),
        Person(FactKeys.UbezpieczeniePrzerwy, MissingFactType.List, "Czy w Twoim ubezpieczeniu były przerwy (jeśli tak — w jakich terminach)?", "Czy w ubezpieczeniu drugiego rodzica były przerwy (jeśli tak — w jakich terminach)?"),
        Person(FactKeys.UbezpieczeniePobraneSwiadczenia, MissingFactType.List, "Jakie zasiłki z ubezpieczenia chorobowego już pobrałaś lub pobrałeś i w jakich okresach?", "Jakie zasiłki z ubezpieczenia chorobowego pobrał już drugi rodzic i w jakich okresach?"),
        Person(FactKeys.UbezpieczenieLatWczesniejszegoObowiazkowego, MissingFactType.Number, "Ile lat trwało Twoje wcześniejsze obowiązkowe ubezpieczenie chorobowe?", "Ile lat trwało wcześniejsze obowiązkowe ubezpieczenie chorobowe drugiego rodzica?"),
        Person(FactKeys.UbezpieczeniePodlegaChorobowemu, MissingFactType.Boolean, "Czy podlegasz ubezpieczeniu chorobowemu?", "Czy drugi rodzic podlega ubezpieczeniu chorobowemu?"),
        Person(FactKeys.UbezpieczenieUstaniePrzyczyna, MissingFactType.Enum, "Z jakiej przyczyny ustało Twoje ubezpieczenie chorobowe?", "Z jakiej przyczyny ustało ubezpieczenie chorobowe drugiego rodzica?", EndReasons),
        Person(FactKeys.UmowaNaCzasOkreslony, MissingFactType.Boolean, "Czy Twoja umowa jest zawarta na czas określony?", "Czy umowa drugiego rodzica jest zawarta na czas określony?"),
        Person(FactKeys.UmowaDataKonca, MissingFactType.Date, "Do kiedy trwa Twoja umowa?", "Do kiedy trwa umowa drugiego rodzica?"),
        Person(FactKeys.UmowaPlanowaneOdejscieData, MissingFactType.Date, "Kiedy planujesz odejść z pracy?", "Kiedy drugi rodzic planuje odejść z pracy?"),
        Person(FactKeys.PlatnikLiczbaUbezpieczonych, MissingFactType.Integer, "Ilu ubezpieczonych zgłosił Twój płatnik składek według stanu na 30 listopada poprzedniego roku?", "Ilu ubezpieczonych zgłosił płatnik składek drugiego rodzica według stanu na 30 listopada poprzedniego roku?"),
        Person(FactKeys.SkladkiZadluzenie, MissingFactType.Money, "Jakie jest Twoje zadłużenie z tytułu składek w dniu powstania prawa do zasiłku (kwota w złotych)?", "Jakie jest zadłużenie drugiego rodzica z tytułu składek w dniu powstania prawa do zasiłku (kwota w złotych)?"),
        Person(FactKeys.SkladkiDataSplaty, MissingFactType.Date, "Kiedy spłacisz (albo spłaciłaś lub spłaciłeś) całość zadłużenia?", "Kiedy drugi rodzic spłaci całość zadłużenia?"),
        Person(FactKeys.EzlaZwolnienia, MissingFactType.List, "Jakie zwolnienia lekarskie (e-ZLA) masz w czasie ciąży?", "Jakie zwolnienia lekarskie (e-ZLA) ma drugi rodzic?"),
        Person(FactKeys.PodstawaUstalonaPrzezPlatnika, MissingFactType.Money, "Jaka jest podstawa wymiaru zasiłku ustalona przez Twojego płatnika (kwota brutto w złotych)?", "Jaka jest podstawa wymiaru zasiłku drugiego rodzica ustalona przez płatnika (kwota brutto w złotych)?"),
        Person(FactKeys.PlanWariantWyplaty, MissingFactType.Enum, "Jaki wariant wypłaty wybierasz: standardowy (100% i 70%) czy uśredniony (81,5%)?", "Jaki wariant wypłaty wybiera drugi rodzic: standardowy czy uśredniony?", Variants),
        Person(FactKeys.PlanDataWnioskuUsrednionego, MissingFactType.Date, "Kiedy złożysz (albo złożyłaś lub złożyłeś) wniosek o wariant uśredniony?", "Kiedy drugi rodzic złoży wniosek o wariant uśredniony?"),
        Person(FactKeys.PlanMacierzynskiPrzedPorodemTyg, MissingFactType.Integer, "Ile tygodni urlopu macierzyńskiego chcesz wykorzystać przed porodem?", "Ile tygodni urlopu macierzyńskiego drugi rodzic chce wykorzystać przed porodem?"),
        Person(FactKeys.PlanRezygnacjaZMacierzynskiegoPoTyg, MissingFactType.Integer, "Po ilu tygodniach po porodzie matka rezygnuje z zasiłku macierzyńskiego?", "Po ilu tygodniach po porodzie matka rezygnuje z zasiłku macierzyńskiego?"),
        Person(FactKeys.PlanRodzicielskiCzesci, MissingFactType.List, "Na ile części i w jakich terminach chcesz podzielić swój urlop rodzicielski?", "Na ile części i w jakich terminach drugi rodzic chce podzielić swój urlop rodzicielski?"),
        Person(FactKeys.PlanPracaNaCzescEtatu, MissingFactType.List, "Ile pracy (wymiar etatu) i przez ile tygodni chcesz łączyć z urlopem rodzicielskim?", "Ile pracy (wymiar etatu) i przez ile tygodni drugi rodzic chce łączyć z urlopem rodzicielskim?"),
        Person(FactKeys.PlanOjcowskiCzesci, MissingFactType.List, "Na ile części i w jakich terminach chcesz wykorzystać urlop ojcowski?", "Na ile części i w jakich terminach drugi rodzic chce wykorzystać urlop ojcowski?"),
    }.ToDictionary(q => q.Fact);

    public static MissingFact Ask(string fact, PersonRef? person = null)
    {
        var question = All[fact];
        if (question.QuestionAboutOther is null)
        {
            return new MissingFact(fact, question.Question, question.Type, null, question.Options);
        }

        var text = person == PersonRef.DrugiRodzic ? question.QuestionAboutOther : question.Question;
        return new MissingFact(fact, text, question.Type, person ?? PersonRef.Wnioskodawca, question.Options);
    }

    public static MissingFact Normalize(MissingFact missing) =>
        All.ContainsKey(missing.Fact) ? Ask(missing.Fact, missing.Person) : missing;
}
