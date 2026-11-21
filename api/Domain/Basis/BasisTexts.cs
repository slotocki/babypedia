using System.Globalization;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public static class BasisTexts
{
    public const string Referral = "Podaj podstawę, żeby zobaczyć kwotę — wpisz „podstawę ustaloną przez płatnika”.";

    public const string NotCountedPartialAbsence =
        "Miesiące z częściową usprawiedliwioną nieobecnością: nie sprawdzamy, czy przepracowano co najmniej połowę czasu pracy (art. 38 ust. 2), więc ich nie wyłączamy ani nie uzupełniamy wynagrodzenia składnikami stałymi do pełnego miesiąca (art. 37 ust. 2). Wyłączamy tylko miesiące w całości objęte pobranymi świadczeniami lub zwolnieniami e-ZLA.";

    public const string NotCountedQuarterlyAnnual =
        "Składniki kwartalne i roczne (1/12 kwot z czterech kwartałów albo z poprzedniego roku, art. 42 ust. 2–4): dane z ZUS zawierają tylko kwotę za miesiąc, więc ich nie wydzielamy — bierzemy kwoty tak, jak wykazano w poszczególnych miesiącach.";

    public const string NotCountedExcludedComponents =
        "Składniki wyłączone z podstawy (art. 41) i zasiłek wyrównawczy (art. 39): nie rozpoznajemy ich w danych z ZUS.";

    public const string NotCountedPartialActivity =
        "Miesiące, w których działalność wykonywano tylko przez część miesiąca: wyłączamy wyłącznie miesiące w całości objęte pobranymi świadczeniami lub zwolnieniami e-ZLA (art. 50 ust. 1); nie sprawdzamy, czy działalność wykonywano przez co najmniej połowę miesiąca.";

    public const string NotCountedBenefitsUnknown =
        "Nie sprawdzono, czy zasiłek następuje po innym zasiłku bez przerwy albo z przerwą krótszą niż miesiąc kalendarzowy (art. 43): brak danych o pobranych świadczeniach.";

    public const string NotCountedAbsencesUnknown =
        "Brak danych o zwolnieniach e-ZLA lub pobranych świadczeniach — nie wyłączamy miesięcy z nieobecnością (art. 38 ust. 2, art. 50).";

    public const string NotCountedWorkingTimeChange =
        "Zmiana wymiaru czasu pracy między kolejnymi zasiłkami: Poradnik ZUS (s. 25) podaje wyjątek od art. 43, którego nie ma w ustawie — stosujemy ustawę i podstawy nie przeliczamy.";

    public const string NotCountedEstimate =
        "Wynik jest szacunkiem z danych ZUS (miesiąc i kwota podstawy składki chorobowej). Dokładną podstawę ustala płatnik składek.";

    public static readonly IReadOnlyList<string> OutOfScopeItems =
    [
        "zbieg tytułów ubezpieczenia chorobowego (w tym działalność obok zatrudnienia)",
        "zatrudnienie krótsze niż jeden pełny miesiąc kalendarzowy — wynagrodzenie z umowy (art. 37)",
        "zmiana wymiaru czasu pracy w okresie obliczeniowym (art. 40)",
        "zmiana tytułu ubezpieczenia lub przerwa do 30 dni między tytułami w okresie obliczeniowym (art. 36 ust. 4, art. 48a ust. 2–4, art. 49 ust. 2–3)",
        "miesiące z częściową nieobecnością, w których trzeba uzupełnić wynagrodzenie (art. 37 ust. 2, art. 38 ust. 2 pkt 2 i ust. 3)",
        "składniki kwartalne i roczne po 1/12 (art. 42 ust. 2–4)",
        "umowa zlecenia przy ubezpieczeniu krótszym niż 12 miesięcy (art. 48a, art. 49 ust. 1 pkt 2) oraz tytuły inne niż pracownik, zleceniobiorca, działalność i osoba współpracująca",
        "zasiłek po ustaniu tytułu ubezpieczenia (art. 30, art. 46)",
    ];

    public static string OutOfScope => "Poziom 1 nie liczy podstawy wymiaru m.in. w tych przypadkach: " + string.Join("; ", OutOfScopeItems) + ".";

    public static string Fail(string reason) => $"Nie rozstrzygam. {reason} {OutOfScope} {Referral}";
}

internal static class Fmt
{
    public static string Money(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',') + " zł";

    public static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Percent(decimal fraction) =>
        (fraction * 100).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') + "%";

    public static string Fraction(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture).Replace('.', ',');

    public static string Months(IEnumerable<DateOnly> months) =>
        string.Join(", ", months.Select(m => m.ToString("yyyy-MM", CultureInfo.InvariantCulture)));

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

internal static class BasisAsk
{
    public static MissingFact Titles(PersonRef person) => new(
        FactKeys.UbezpieczenieTytuly,
        "Jakie tytuły ubezpieczenia ma ta osoba (zatrudnienie, zlecenie, działalność) i czy obejmują ubezpieczenie chorobowe?",
        MissingFactType.List,
        person);

    public static MissingFact Contributions(PersonRef person, string question) => new(
        FactKeys.UbezpieczeniePodstawySkladek, question, MissingFactType.List, person);

    public static MissingFact WorkingTime(PersonRef person) => new(
        FactKeys.UbezpieczenieTytuly,
        "Jaki jest wymiar etatu (np. 1 albo 0,5) w tytule ubezpieczenia tej osoby?",
        MissingFactType.List,
        person);

    public static MissingFact PayerBasis(PersonRef person) => new(
        FactKeys.PodstawaUstalonaPrzezPlatnika,
        "Jaka jest podstawa wymiaru zasiłku ustalona przez płatnika składek (kwota miesięczna po odliczeniu składek)?",
        MissingFactType.Money,
        person);
}

internal static class MonthMath
{
    public static DateOnly First(DateOnly date) => new(date.Year, date.Month, 1);

    public static DateOnly Last(DateOnly firstOfMonth) => firstOfMonth.AddMonths(1).AddDays(-1);

    public static IReadOnlyList<DateOnly> Window(DateOnly reference)
    {
        var month = First(reference);
        return Enumerable.Range(1, 12).Select(k => month.AddMonths(-k)).Reverse().ToList();
    }

    public static bool TryParse(string? text, out DateOnly month) =>
        DateOnly.TryParseExact(text + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out month);

    public static bool GapShorterThanMonth(DateOnly benefitEnd, DateOnly next) => next < benefitEnd.AddDays(1).AddMonths(1);

    public static int OverlapDays(DateOnly from, DateOnly to, DateOnly month)
    {
        var start = from > month ? from : month;
        var last = Last(month);
        var end = to < last ? to : last;
        return end >= start ? end.DayNumber - start.DayNumber + 1 : 0;
    }
}
