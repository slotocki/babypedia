using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public sealed class InsuredMonthsRule : BasisRule<MonthSet>
{
    public override string Id => "pelne_miesiace_ubezpieczenia";

    public override string PodstawaPrawna => "ustawa art. 36 ust. 1–2, art. 48 ust. 1, art. 3 pkt 3–4";

    protected override BasisStep<MonthSet> Compute(BasisInputs i)
    {
        if (!i.Title.Ok)
        {
            return BasisSteps.Blocked<MonthSet>(this, i.Title);
        }

        var reference = i.ReferenceDay;
        var window = MonthMath.Window(reference);
        var insured = window.Where(m => m >= i.Title.Value!.InsuredFrom).ToList();
        var range = $"{Fmt.Months([window[0]])}–{Fmt.Months([window[^1]])}";
        if (insured.Count == 0)
        {
            return BasisSteps.Met(this, new MonthSet([], new Dictionary<DateOnly, decimal>()),
                $"Okres obliczeniowy {range}: brak pełnego miesiąca kalendarzowego ubezpieczenia chorobowego.",
                "12 miesięcy kalendarzowych poprzedzających miesiąc powstania prawa");
        }

        var data = i.Me?.UbezpieczeniePodstawySkladek?.Value;
        if (data is null)
        {
            return BasisSteps.Missing<MonthSet>(this, "Brak danych o podstawach składek za okres obliczeniowy.",
                BasisAsk.Contributions(i.Person, "Jakie były podstawy składki chorobowej za 12 miesięcy poprzedzających miesiąc powstania prawa?"));
        }

        var byMonth = data
            .Select(c => (Parsed: MonthMath.TryParse(c.Miesiac, out var month), Month: month, c.Kwota))
            .Where(x => x.Parsed)
            .ToLookup(x => x.Month, x => x.Kwota);
        var missing = insured.Where(m => !byMonth.Contains(m)).ToList();
        if (missing.Count > 0)
        {
            return BasisSteps.Missing<MonthSet>(this,
                $"Dane z ZUS nie zawierają podstaw składek za miesiące: {Fmt.Months(missing)} (okres obliczeniowy {range}).",
                BasisAsk.Contributions(i.Person, $"Jakie były podstawy składki chorobowej za miesiące: {Fmt.Months(missing)}?"));
        }

        var duplicated = insured.Where(m => byMonth[m].Count() > 1).ToList();
        if (duplicated.Count > 0)
        {
            return BasisSteps.Undecided<MonthSet>(this,
                $"Dane z ZUS zawierają więcej niż jedną podstawę składek za miesiące: {Fmt.Months(duplicated)} (np. kilku płatników).");
        }

        var contributions = insured.ToDictionary(m => m, m => byMonth[m].Single());
        if (contributions.Any(c => c.Value < 0))
        {
            return BasisSteps.Undecided<MonthSet>(this, "Dane z ZUS zawierają ujemną podstawę składek.");
        }

        return BasisSteps.Met(this, new MonthSet(insured, contributions),
            $"Okres obliczeniowy {range}: {insured.Count} pełnych miesięcy ubezpieczenia chorobowego ({Fmt.Months(insured)}).",
            "12 miesięcy kalendarzowych poprzedzających miesiąc powstania prawa");
    }
}

public sealed class AbsentMonthsRule : BasisRule<MonthPlan>
{
    public override string Id => "miesiace_z_nieobecnoscia";

    public override string PodstawaPrawna => "ustawa art. 38 ust. 2–3 (pracownik), art. 50 (niebędący pracownikiem), art. 47, art. 52";

    protected override BasisStep<MonthPlan> Compute(BasisInputs i)
    {
        if (!i.Months.Ok)
        {
            return BasisSteps.Blocked<MonthPlan>(this, i.Months);
        }

        var insured = i.Months.Value!.Insured;
        var employee = i.Title.Value!.IsEmployee;
        var benefits = i.Me?.UbezpieczeniePobraneSwiadczenia?.Value;
        var leaves = i.Me?.EzlaZwolnienia?.Value;
        var known = benefits is not null || leaves is not null;

        var intervals = new List<(DateOnly From, DateOnly To)>();
        intervals.AddRange((benefits ?? [])
            .Where(b => b.Rodzaj != ReceivedBenefitKind.Inne && (employee || b.Rodzaj != ReceivedBenefitKind.WynagrodzenieChorobowe))
            .Select(b => (b.Od, b.Do)));
        intervals.AddRange((leaves ?? []).Select(l => (l.Od, l.Do)));

        var used = new List<DateOnly>();
        var omitted = new List<DateOnly>();
        var partial = new List<DateOnly>();
        foreach (var month in insured)
        {
            var days = DateTime.DaysInMonth(month.Year, month.Month);
            var covered = CoveredDays(intervals, month);
            if (covered >= days)
            {
                omitted.Add(month);
                continue;
            }

            used.Add(month);
            if (covered > 0)
            {
                partial.Add(month);
            }
        }

        if (omitted.Count == 0 && insured.Count > 0)
        {
            return BasisSteps.Met(this, new MonthPlan(used, omitted, partial, false, known),
                partial.Count == 0
                    ? (known ? "Żaden miesiąc okresu obliczeniowego nie jest w całości objęty zasiłkiem ani zwolnieniem e-ZLA." : "Brak danych o zwolnieniach i pobranych świadczeniach — żadnego miesiąca nie wyłączam.")
                    : $"Żaden miesiąc nie jest w całości objęty nieobecnością; w miesiącach {Fmt.Months(partial)} nieobecność jest częściowa — nie rozpoznaję, czy przepracowano połowę czasu pracy, więc je zostawiam.");
        }

        if (insured.Count == 0)
        {
            return BasisSteps.Met(this, new MonthPlan([], [], [], false, known), "Brak miesięcy do oceny nieobecności.");
        }

        if (used.Count == 0)
        {
            if (employee)
            {
                return BasisSteps.Undecided<MonthPlan>(this,
                    "Każdy miesiąc okresu obliczeniowego jest w całości objęty nieobecnością: art. 38 ust. 3 każe wtedy przyjąć wynagrodzenie za wszystkie miesiące po uzupełnieniu składników stałych (art. 37 ust. 2), czego nie da się zrobić z danych ZUS.");
            }

            return BasisSteps.Met(this, new MonthPlan(insured, [], [], true, known),
                $"Każdy miesiąc okresu obliczeniowego jest w całości objęty zasiłkiem lub zwolnieniem — przyjmuję przychód za wszystkie miesiące ({Fmt.Months(insured)}).",
                "art. 50 ust. 2");
        }

        return BasisSteps.Met(this, new MonthPlan(used, omitted, partial, false, known),
            $"Pomijam miesiące w całości objęte pobranymi świadczeniami lub zwolnieniami e-ZLA: {Fmt.Months(omitted)} (przepracowano w nich mniej niż połowę czasu).{(partial.Count > 0 ? $" Miesiące z częściową nieobecnością ({Fmt.Months(partial)}) zostawiam bez zmian." : "")}",
            employee ? "wyłączenie miesięcy z przepracowanym czasem poniżej połowy" : "wyłączenie miesięcy z działalnością poniżej połowy miesiąca");
    }

    private static int CoveredDays(List<(DateOnly From, DateOnly To)> intervals, DateOnly month)
    {
        var merged = new List<(DateOnly From, DateOnly To)>();
        foreach (var interval in intervals.Where(x => x.To >= x.From).OrderBy(x => x.From))
        {
            if (merged.Count > 0 && interval.From <= merged[^1].To.AddDays(1))
            {
                merged[^1] = (merged[^1].From, interval.To > merged[^1].To ? interval.To : merged[^1].To);
            }
            else
            {
                merged.Add(interval);
            }
        }

        return merged.Sum(x => MonthMath.OverlapDays(x.From, x.To, month));
    }
}
