using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Eligibility;

public sealed record TitleFeatures(
    TitleKind Kind,
    SicknessInsurance Sickness,
    bool HasSicknessInsurance,
    bool WagesFromEmployerBeforeBenefit,
    bool LeaveApplicationToEmployer,
    bool DebtControl,
    bool EmploymentProtection,
    string? WaitingPeriodKey)
{
    public static TitleFeatures Of(InsuranceTitle title)
    {
        var insured = title.Chorobowe != SicknessInsurance.Brak;
        var employee = title.RodzajTytulu == TitleKind.Pracownik;
        return new TitleFeatures(
            title.RodzajTytulu,
            title.Chorobowe,
            insured,
            employee && insured,
            employee,
            title.RodzajTytulu is TitleKind.Dzialalnosc or TitleKind.OsobaWspolpracujaca,
            employee,
            !insured ? null : title.Chorobowe == SicknessInsurance.Obowiazkowe
                ? EligibilityParameterKeys.WyczekiwanieObowiazkoweDni
                : EligibilityParameterKeys.WyczekiwanieDobrowolneDni);
    }
}

internal enum InsuranceState
{
    Insured,
    NotInsured,
    Unknown,
    Concurrent,
    DeclaredInsured,
    DeclaredNotInsured,
}

/// <summary>
/// Stan ubezpieczenia w danym dniu. Przy zbiegu tytułów (<see cref="InsuranceState.Concurrent"/>) <c>Title</c> jest pusty,
/// a <c>SicknessTitles</c> zawiera wszystkie tytuły z ubezpieczeniem chorobowym — prawo do zasiłku istnieje z każdego z nich.
/// </summary>
internal sealed record InsuranceView(InsuranceState State, InsuranceTitle? Title, IReadOnlyList<InsuranceTitle> ActiveWithoutSickness)
{
    public IReadOnlyList<InsuranceTitle> SicknessTitles { get; init; } = Title is null ? [] : [Title];
}

internal static class Insurance
{
    public static bool IsSickness(InsuranceTitle title) => title.Chorobowe != SicknessInsurance.Brak;

    public static bool ActiveOn(InsuranceTitle title, DateOnly date) => title.Od <= date && (title.Do is null || title.Do >= date);

    public static bool IsBusiness(InsuranceTitle title) => title.RodzajTytulu is TitleKind.Dzialalnosc or TitleKind.OsobaWspolpracujaca;

    public static IReadOnlyList<InsuranceTitle>? Titles(PersonFacts? person) => person?.UbezpieczenieTytuly?.Value;

    public static InsuranceView View(PersonFacts? person, DateOnly date)
    {
        var titles = Titles(person);
        if (titles is null)
        {
            return person?.UbezpieczeniePodlegaChorobowemu?.Value switch
            {
                true => new InsuranceView(InsuranceState.DeclaredInsured, null, []),
                false => new InsuranceView(InsuranceState.DeclaredNotInsured, null, []),
                _ => new InsuranceView(InsuranceState.Unknown, null, []),
            };
        }

        var active = titles.Where(t => ActiveOn(t, date)).ToList();
        var sickness = active.Where(IsSickness).ToList();
        var other = active.Where(t => !IsSickness(t)).ToList();

        if (sickness.Count > 1)
        {
            return new InsuranceView(InsuranceState.Concurrent, null, other) { SicknessTitles = sickness };
        }

        if (sickness.Count == 1)
        {
            // Tytuł bez ubezpieczenia chorobowego (np. działalność opłacająca tylko składkę zdrowotną obok etatu)
            // nie daje prawa do zasiłku i nie tworzy zbiegu — zasiłek przysługuje z jedynego tytułu z chorobowym.
            return new InsuranceView(InsuranceState.Insured, sickness[0], other);
        }

        return new InsuranceView(InsuranceState.NotInsured, null, other);
    }

    public static string Describe(IEnumerable<InsuranceTitle> titles) =>
        string.Join(", ", titles.Select(t => $"{KindName(t.RodzajTytulu)} (chorobowe {SicknessName(t.Chorobowe)})"));

    public static string KindName(TitleKind kind) => kind switch
    {
        TitleKind.Pracownik => "pracownik",
        TitleKind.Zleceniobiorca => "zleceniobiorca",
        TitleKind.Dzialalnosc => "prowadzący działalność",
        TitleKind.OsobaWspolpracujaca => "osoba współpracująca",
        TitleKind.PobierajacyZasilek => "osoba pobierająca zasiłek",
        _ => "inny tytuł",
    };

    public static string SicknessName(SicknessInsurance s) => s switch
    {
        SicknessInsurance.Obowiazkowe => "obowiązkowe",
        SicknessInsurance.Dobrowolne => "dobrowolne",
        _ => "brak",
    };

    public static int DaysBetween(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber;
}
