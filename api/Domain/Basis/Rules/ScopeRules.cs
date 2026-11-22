using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public sealed class PayerBasisRule : BasisRule<decimal?>
{
    public override string Id => "podstawa_ustalona_przez_platnika";

    public override string PodstawaPrawna => "instrukcja 6.1 i 8.7; kontrakt: podstawa.ustalonaPrzezPlatnika";

    protected override BasisStep<decimal?> Compute(BasisInputs i)
    {
        var value = i.Me?.PodstawaUstalonaPrzezPlatnika?.Value;
        if (value is null)
        {
            return BasisSteps.NotMet<decimal?>(this, "Nie podano podstawy ustalonej przez płatnika — przechodzę do szacunku z danych ZUS.");
        }

        if (value <= 0)
        {
            return BasisSteps.NotMet<decimal?>(this,
                $"Podana podstawa ustalona przez płatnika ({Fmt.Money(value.Value)}) nie jest dodatnia — pomijam ją i przechodzę do szacunku z danych ZUS.");
        }

        var rounded = Fmt.Round(value.Value);
        return BasisSteps.Met<decimal?>(this, rounded,
            $"Przyjmuję podstawę ustaloną przez płatnika: {Fmt.Money(rounded)}. Traktuję ją jako gotową podstawę wymiaru zasiłku (po odliczeniach i z zastosowaniem minimum oraz limitu przez płatnika) — nie pomniejszam jej ponownie o 13,71%.",
            "podstawa ustalona przez płatnika ma pierwszeństwo przed szacunkiem z danych ZUS");
    }
}

public sealed class ReferenceDateRule : BasisRule<ReferenceDate>
{
    public override string Id => "art43_brak_przeliczenia";

    public override string PodstawaPrawna => "ustawa art. 43, art. 47, art. 52";

    protected override BasisStep<ReferenceDate> Compute(BasisInputs i)
    {
        var benefits = i.Me?.UbezpieczeniePobraneSwiadczenia?.Value;
        if (benefits is null)
        {
            return BasisSteps.Met(this, new ReferenceDate(i.Date, [], false),
                $"Brak danych o pobranych świadczeniach — nie sprawdzam art. 43. Przyjmuję dzień powstania prawa {Fmt.Date(i.Date)}.");
        }

        var relevant = benefits
            .Where(b => b.Rodzaj is not (ReceivedBenefitKind.WynagrodzenieChorobowe or ReceivedBenefitKind.Inne) && b.Do >= b.Od)
            .ToList();
        var chain = new List<ReceivedBenefit>();
        var current = i.Date;
        while (relevant
                   .Where(b => b.Od < current && MonthMath.GapShorterThanMonth(b.Do, current) && !chain.Contains(b))
                   .OrderBy(b => b.Od)
                   .FirstOrDefault() is { } previous)
        {
            chain.Add(previous);
            current = previous.Od;
        }

        if (chain.Count == 0)
        {
            return BasisSteps.Met(this, new ReferenceDate(i.Date, [], true),
                $"Żaden zasiłek nie poprzedza tego prawa bez przerwy ani z przerwą krótszą niż miesiąc kalendarzowy — podstawę ustalam od dnia {Fmt.Date(i.Date)}.",
                "przerwa między zasiłkami krótsza niż miesiąc kalendarzowy");
        }

        var list = string.Join(", ", chain.OrderBy(b => b.Od).Select(b => $"{b.Od:yyyy-MM-dd}–{b.Do:yyyy-MM-dd}"));
        return BasisSteps.Met(this, new ReferenceDate(current, chain, true),
            $"Zasiłek następuje po innym zasiłku bez przerwy albo z przerwą krótszą niż miesiąc kalendarzowy ({list}), więc podstawy nie ustala się na nowo: liczę ją tak jak dla pierwszego zasiłku w ciągu — od dnia {Fmt.Date(current)}.",
            "przerwa między zasiłkami krótsza niż miesiąc kalendarzowy");
    }
}

public sealed class InsuredTitleRule : BasisRule<InsuredTitle>
{
    private const int MaxBreakDays = 31;

    public override string Id => "tytul_ubezpieczenia_podstawy";

    public override string PodstawaPrawna => "ustawa art. 3 pkt 1, art. 36 ust. 4, art. 40, art. 47, art. 48 ust. 2, art. 52; instrukcja 8.2";

    private static bool IsBusiness(InsuranceTitle t) => t.RodzajTytulu is TitleKind.Dzialalnosc or TitleKind.OsobaWspolpracujaca;

    /// <summary>Obaj płatnicy są znani i różni (brak identyfikatora = nie wiadomo, traktujemy jak tego samego płatnika).</summary>
    private static bool OtherPayer(InsuranceTitle a, InsuranceTitle b) =>
        a.PlatnikId is { } x && b.PlatnikId is { } y && !string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

    /// <summary><paramref name="previous"/> kończy się najpóźniej dzień przed <paramref name="next"/>, jest tego samego rodzaju, z chorobowym i u tego samego płatnika.</summary>
    private static bool Continues(InsuranceTitle previous, InsuranceTitle next) =>
        previous.RodzajTytulu == next.RodzajTytulu
        && (next.RodzajTytulu == TitleKind.Pracownik ? previous.Chorobowe == SicknessInsurance.Obowiazkowe : previous.Chorobowe != SicknessInsurance.Brak)
        && previous.Od < next.Od
        && previous.Do is { } end && end >= next.Od.AddDays(-1)
        && !OtherPayer(previous, next);

    protected override BasisStep<InsuredTitle> Compute(BasisInputs i)
    {
        var titles = i.Me?.UbezpieczenieTytuly?.Value;
        if (titles is null)
        {
            return BasisSteps.Missing<InsuredTitle>(this, "Brak danych o tytułach ubezpieczenia.", BasisAsk.Titles(i.Person));
        }

        var day = i.ReferenceDay;
        var activeAll = titles.Where(t => t.Od <= day && (t.Do is null || t.Do >= day)).ToList();
        var active = activeAll.Where(t => t.Chorobowe != SicknessInsurance.Brak).ToList();
        var condition = $"jeden tytuł z ubezpieczeniem chorobowym w dniu {Fmt.Date(day)}";

        if (active.Count == 0)
        {
            return BasisSteps.NotMet<InsuredTitle>(this,
                $"W dniu {Fmt.Date(day)} żaden tytuł nie obejmuje ubezpieczenia chorobowego — podstawy nie liczymy (zasiłek po ustaniu tytułu: art. 30, art. 46, poza poziomem 1).",
                condition);
        }

        var title = active[0];
        if (active.Count > 1)
        {
            // Tytuł bez ubezpieczenia chorobowego (np. działalność tylko ze składką zdrowotną) nie wpływa na podstawę — liczy się tylko zbieg tytułów z chorobowym.
            return BasisSteps.Undecided<InsuredTitle>(this,
                "W tym dniu obowiązuje kilka tytułów z ubezpieczeniem chorobowym (zbieg tytułów). Zasiłek przysługuje z każdego z nich, a podstawę ustala się odrębnie dla każdego tytułu z wynagrodzenia lub przychodu u danego płatnika (art. 36 ust. 4, art. 48 ust. 2) — poziom 1 nie liczy kilku podstaw naraz.");
        }

        if (title.RodzajTytulu is not (TitleKind.Pracownik or TitleKind.Zleceniobiorca or TitleKind.Dzialalnosc or TitleKind.OsobaWspolpracujaca))
        {
            return BasisSteps.Undecided<InsuredTitle>(this,
                "Tytuł ubezpieczenia nie jest ani zatrudnieniem, ani zleceniem, ani działalnością — przepisy z wyciągu nie pozwalają ustalić z niego podstawy.");
        }

        var employee = title.RodzajTytulu == TitleKind.Pracownik;
        var chain = new List<InsuranceTitle> { title };
        var from = title.Od;
        var current = title;
        if (employee || IsBusiness(title))
        {
            // Kolejne tytuły tego samego rodzaju bez przerwy i u tego samego płatnika to jedno nieprzerwane ubezpieczenie
            // (np. nowa umowa u tego samego pracodawcy albo zmiana kodu tytułu tej samej działalności, 0590 → 0510).
            while (titles
                       .Where(t => !chain.Contains(t) && Continues(t, current))
                       .OrderBy(t => t.Od)
                       .FirstOrDefault() is { } previous)
            {
                chain.Add(previous);
                from = previous.Od;
                current = previous;
            }
        }

        foreach (var gap in i.Me?.UbezpieczeniePrzerwy?.Value ?? [])
        {
            if (gap.Do >= from && gap.Od < day)
            {
                from = gap.Do.AddDays(1) > from ? gap.Do.AddDays(1) : from;
            }
        }

        var windowStart = MonthMath.Window(day)[0];
        var effectiveFrom = from > windowStart ? from : windowStart;
        foreach (var other in titles.Where(t => !chain.Contains(t) && t.Chorobowe != SicknessInsurance.Brak))
        {
            var concurrent = other.Od < day && (other.Do is null || other.Do >= effectiveFrom);
            var closePredecessor = other.Do is { } end && end < from && from.DayNumber - end.DayNumber <= MaxBreakDays && from > windowStart;
            if (employee && !concurrent && from > windowStart && other.Do is { } otherEnd && otherEnd >= from.AddDays(-1) && other.RodzajTytulu == TitleKind.Pracownik && OtherPayer(other, current))
            {
                return BasisSteps.Undecided<InsuredTitle>(this,
                    $"Zmiana pracodawcy bez przerwy w ubezpieczeniu: poprzednie zatrudnienie u innego płatnika ({other.PlatnikNazwa ?? other.PlatnikId}) trwało do {otherEnd:yyyy-MM-dd}. Podstawę ustala się z wynagrodzenia u płatnika, u którego trwa ubezpieczenie (art. 36 ust. 4), więc wynagrodzeń od poprzedniego płatnika nie wliczam; przy krótkim zatrudnieniu u obecnego płatnika potrzebne są dane z umowy (art. 37), a przy przejęciu zakładu pracy (art. 23¹ KP) zasady są inne — poziom 1 tego nie liczy.");
            }

            if (concurrent || closePredecessor)
            {
                return BasisSteps.Undecided<InsuredTitle>(this,
                    $"W okresie obliczeniowym występuje inny tytuł z ubezpieczeniem chorobowym ({other.Od:yyyy-MM-dd}–{(other.Do is { } d ? d.ToString("yyyy-MM-dd") : "bez końca")}): zmiana tytułu albo przerwa do 30 dni (art. 36 ust. 4, art. 48a ust. 2–4, art. 49 ust. 2–3) wymaga zasad spoza poziomu 1.");
            }
        }

        var workingTimes = chain
            .Where(t => (t.Do ?? DateOnly.MaxValue) >= windowStart && t.WymiarEtatu is not null)
            .Select(t => t.WymiarEtatu!.Value)
            .Distinct()
            .ToList();
        if (employee && workingTimes.Count > 1)
        {
            return BasisSteps.Undecided<InsuredTitle>(this,
                "W okresie obliczeniowym zmieniał się wymiar czasu pracy (art. 40) — wynagrodzenie dla nowego wymiaru wymaga danych z umowy, których nie ma w danych z ZUS.");
        }

        var kind = employee ? "pracownik" : title.RodzajTytulu switch
        {
            TitleKind.Zleceniobiorca => "zleceniobiorca",
            TitleKind.Dzialalnosc => "prowadzący działalność",
            _ => "osoba współpracująca",
        };
        var payerAssumed = chain.Count > 1 && chain.Any(t => t.PlatnikId is null)
            ? " Kolejne tytuły połączono jako jedno ubezpieczenie u tego samego płatnika (w danych brak identyfikatora płatnika) — jeśli to była zmiana pracodawcy, podstawę liczy się tylko z wynagrodzenia u obecnego (art. 36 ust. 4)."
            : "";
        return BasisSteps.Met(this, new InsuredTitle(title, employee, from, chain),
            $"Tytuł: {kind}; ubezpieczenie chorobowe nieprzerwanie od {Fmt.Date(from)} — stosuję zasady dla {(employee ? "pracowników (art. 36–42, 45)" : "osób niebędących pracownikami (art. 48–50)")}.{payerAssumed}",
            condition);
    }
}
