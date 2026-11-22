using System.Globalization;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

internal static class Text
{
    public static string Money(decimal value) =>
        value.ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',') + " zł";

    public static string Date(DateOnly date) =>
        date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    public static string EventWord(EventKind? kind) => kind switch
    {
        EventKind.Przyjecie => "przyjęcia dziecka na wychowanie",
        EventKind.Urodzenie => "porodu",
        EventKind.Ciaza => "przewidywanego porodu",
        _ => "zdarzenia",
    };
}

internal static class Ask
{
    public static MissingFact Event() => new(
        FactKeys.ZdarzenieRodzaj,
        "Co się dzieje: planujesz dziecko, jesteś w ciąży, dziecko się urodziło czy przyjmujesz dziecko na wychowanie?",
        MissingFactType.Enum,
        null,
        ["planuje", "ciaza", "urodzenie", "przyjecie"]);

    public static MissingFact EventDate() => new(
        FactKeys.ZdarzenieData,
        "Jaka jest przewidywana data porodu, data urodzenia dziecka albo data przyjęcia dziecka?",
        MissingFactType.Date);

    public static MissingFact Role(PersonRef person) => new(
        FactKeys.OsobaRola,
        "Jaka jest rola tej osoby wobec dziecka (matka czy ojciec)?",
        MissingFactType.Enum,
        person,
        ["matka", "ojciec"]);

    public static MissingFact Titles(PersonRef person) => new(
        FactKeys.UbezpieczenieTytuly,
        "Jakie tytuły ubezpieczenia ma ta osoba (zatrudnienie, zlecenie, działalność) i czy obejmują ubezpieczenie chorobowe?",
        MissingFactType.List,
        person);

    public static MissingFact Debt(PersonRef person) => new(
        FactKeys.SkladkiZadluzenie,
        "Czy na dzień powstania prawa do zasiłku jest zadłużenie z tytułu składek na ubezpieczenia społeczne i w jakiej kwocie?",
        MissingFactType.Money,
        person);

    public static MissingFact Headcount(PersonRef person) => new(
        FactKeys.PlatnikLiczbaUbezpieczonych,
        "Ilu ubezpieczonych zgłaszał płatnik składek do ubezpieczenia chorobowego według stanu na 30 listopada poprzedniego roku?",
        MissingFactType.Integer,
        person);

    public static MissingFact PregnancyWeek() => new(
        FactKeys.DzieckoTydzienCiazyPrzyPorodzie,
        "W którym tygodniu ciąży przypada (lub przypadł) poród?",
        MissingFactType.Integer);

    public static MissingFact EndReason(PersonRef person) => new(
        FactKeys.UbezpieczenieUstaniePrzyczyna,
        "Z jakiej przyczyny ustało ubezpieczenie chorobowe?",
        MissingFactType.Enum,
        person,
        [
            "wypowiedzenie_przez_pracownika", "wypowiedzenie_przez_pracodawce", "porozumienie_stron",
            "koniec_umowy_na_czas_okreslony", "likwidacja_pracodawcy", "upadlosc_pracodawcy", "smierc_pracodawcy",
            "naruszenie_prawa_orzeczone_przez_sad", "zamkniecie_dzialalnosci", "inna",
        ]);

    public static MissingFact FixedTerm(PersonRef person) => new(
        FactKeys.UmowaNaCzasOkreslony,
        "Czy umowa była zawarta na czas określony?",
        MissingFactType.Boolean,
        person);

    public static MissingFact ContractEnd(PersonRef person) => new(
        FactKeys.UmowaDataKonca,
        "Kiedy kończy się (kończyła się) umowa na czas określony?",
        MissingFactType.Date,
        person);

    public static MissingFact BirthDate(PersonRef person) => new(
        FactKeys.OsobaDataUrodzenia,
        "Jaka jest data urodzenia tej osoby (wiek od 50 lat zmienia limit wynagrodzenia chorobowego)?",
        MissingFactType.Date,
        person);

    public static MissingFact Resignation(PersonRef person) => new(
        FactKeys.PlanRezygnacjaZMacierzynskiegoPoTyg,
        "Po ilu tygodniach po porodzie matka rezygnuje z zasiłku macierzyńskiego?",
        MissingFactType.Integer,
        person);
}
