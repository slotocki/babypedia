using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public sealed class RodzicielskiWymiarRule : PeriodRule<int>
{
    public override string Id => "rodzicielski_wymiar";

    public override string PodstawaPrawna => "KP art. 182¹a § 1–2";

    protected override RuleStep<int> Compute(PeriodsInputs i)
    {
        if (Step.MissingIf<int>(this, i.NeedEventDate(), i.NeedChildCount()) is { } missing)
        {
            return missing;
        }

        if (i.ChildCount!.Value < 1)
        {
            return Step.Undecided<int>(this, "Liczba dzieci przy jednym porodzie musi wynosić co najmniej 1.");
        }

        var count = i.ParentalChildCount!.Value;
        var weeks = i.ParamInt(PeriodsParameterKeys.RodzicielskiTyg(count, i.ZaZyciem));
        var certificate = i.ZaZyciem ? "dziecko z zaświadczeniem „Za życiem”" : "dziecko bez zaświadczenia „Za życiem”";
        var countText = i.SomeChildAlive ? $"liczba dzieci pozostałych przy życiu: {count}" : $"liczba dzieci: {count}";
        return Step.Met(this, weeks,
            $"Łączny wymiar urlopu rodzicielskiego obojga rodziców: {weeks} tyg. ({countText}, {certificate}).",
            "wymiar zależy od liczby dzieci i zaświadczenia „Za życiem”");
    }
}

public sealed class RodzicielskiNieprzenoszalneRule : PeriodRule<bool>
{
    public override string Id => "rodzicielski_nieprzenoszalne_tygodnie";

    public override string PodstawaPrawna => "KP art. 182¹a § 3–4, 6–7";

    protected override RuleStep<bool> Compute(PeriodsInputs i)
    {
        var total = i.RodzicielskiWymiar;
        if (!total.CanProceed)
        {
            return Step.Blocked<bool>(this, total);
        }

        var reserved = i.ParamInt(PeriodsParameterKeys.RodzicielskiNieprzenoszalneTyg);
        var perParentMax = total.Value - reserved;
        var ownDays = PeriodsInputs.RodzicielskiPlannedDays(i.Me);
        if (ownDays > perParentMax * PeriodMath.DaysPerWeek)
        {
            return Step.SoftIssue(this, false,
                $"Zaplanowano {PeriodMath.CeilWeeks(ownDays)} tyg. urlopu rodzicielskiego dla jednego rodzica, a może on wykorzystać najwyżej {perParentMax} tyg. z {total.Value}: {reserved} tyg. przysługuje wyłącznie drugiemu rodzicowi i nie można ich przenieść. Liczę wymiar bez tego planu.",
                i.Issue("rodzicielski_ponad_limit_jednego_rodzica",
                    $"Jeden rodzic może wykorzystać najwyżej {perParentMax} tyg. urlopu rodzicielskiego z {total.Value}; {reserved} tyg. przysługuje wyłącznie drugiemu rodzicowi.",
                    FactKeys.PlanRodzicielskiCzesci),
                $"nie więcej niż wymiar − {reserved} tyg. dla jednego rodzica");
        }

        var otherDays = PeriodsInputs.RodzicielskiPlannedDays(i.Other);
        if (ownDays + otherDays > total.Value * PeriodMath.DaysPerWeek)
        {
            return Step.SoftIssue(this, false,
                $"Plany obojga rodziców ({PeriodMath.CeilWeeks(ownDays + otherDays)} tyg. łącznie) przekraczają wymiar urlopu rodzicielskiego ({total.Value} tyg.). Liczę wymiar bez planu.",
                i.Issue("rodzicielski_ponad_wymiar_lacznie",
                    $"Łączny wymiar urlopu rodzicielskiego obojga rodziców to {total.Value} tyg.",
                    FactKeys.PlanRodzicielskiCzesci),
                "łączny wymiar obojga rodziców nie może być przekroczony");
        }

        return Step.Met(this, true,
            $"Z {total.Value} tyg. urlopu rodzicielskiego jeden rodzic może wykorzystać najwyżej {perParentMax} tyg.; {reserved} tyg. zostaje wyłącznie dla drugiego rodzica.",
            $"{reserved} tyg. nieprzenoszalne dla każdego z rodziców");
    }
}

public sealed class RodzicielskiCzesciRule : PeriodRule<int>
{
    public override string Id => "rodzicielski_max_czesci";

    public override string PodstawaPrawna => "KP art. 182¹c; art. 182¹d § 2";

    protected override RuleStep<int> Compute(PeriodsInputs i)
    {
        if (Step.MissingIf<int>(this, i.NeedEventDate()) is { } missing)
        {
            return missing;
        }

        var max = i.ParamInt(PeriodsParameterKeys.RodzicielskiMaxCzesci);
        var planned = PeriodsInputs.RodzicielskiPlan(i.Me).Count;
        var used = PeriodsInputs.RodzicielskiPobrane(i.Me).Count;
        var total = planned + used;
        if (total > max)
        {
            return Step.NotMet<int>(this,
                $"Urlop rodzicielski można wykorzystać jednorazowo albo w najwyżej {max} częściach. Wniosek oznaczałby {total}. część, więc nie przysługuje.",
                $"nie więcej niż {max} części urlopu rodzicielskiego",
                i.Issue("rodzicielski_ponad_limit_czesci",
                    $"Urlop rodzicielski można podzielić na najwyżej {max} części (liczą się też wcześniejsze wnioski o zasiłek).",
                    FactKeys.PlanRodzicielskiCzesci));
        }

        return Step.Met(this, total,
            total == 0
                ? $"Nie zaplanowano części urlopu rodzicielskiego; limit to {max} części."
                : $"Liczba części urlopu rodzicielskiego (zaplanowane i wcześniej wykorzystane): {total}, limit {max}.",
            $"nie więcej niż {max} części");
    }
}

public sealed class RodzicielskiTerminRule : PeriodRule<DateOnly?>
{
    public override string Id => "rodzicielski_termin_konca_roku";

    public override string PodstawaPrawna => "KP art. 182¹c";

    protected override RuleStep<DateOnly?> Compute(PeriodsInputs i)
    {
        if (i.IsAdoption)
        {
            return Step.Met<DateOnly?>(this, null,
                "Przy przyjęciu dziecka termin „do końca roku kalendarzowego, w którym dziecko kończy 6 lat” nie jest stosowany (art. 183 § 5 stosuje art. 182¹c tylko odpowiednio) — nie rozstrzygam tej granicy.");
        }

        if (Step.MissingIf<DateOnly?>(this, i.NeedEventDate()) is { } missing)
        {
            return missing;
        }

        var years = i.ParamInt(PeriodsParameterKeys.RodzicielskiDoKoncaRokuWieku);
        var last = PeriodMath.EndOfCalendarYear(i.EventDate!.Value.Year + years);
        return Step.Met<DateOnly?>(this, last,
            $"Urlop rodzicielski można wykorzystać najpóźniej do {PeriodMath.Format(last)} — końca roku kalendarzowego, w którym dziecko kończy {years} lat.",
            $"do końca roku kalendarzowego, w którym dziecko kończy {years} lat");
    }
}

public sealed class RodzicielskiPracaRule : PeriodRule<WorkExtension>
{
    public override string Id => "rodzicielski_wydluzenie_praca";

    public override string PodstawaPrawna => "KP art. 182¹e § 1; art. 182¹f § 1–2, 4";

    protected override RuleStep<WorkExtension> Compute(PeriodsInputs i)
    {
        var entries = i.Me.PlanPracaNaCzescEtatu?.Value ?? [];
        if (entries.Count == 0)
        {
            return Step.Met(this, new WorkExtension(0, []), "Nie planujesz łączenia urlopu rodzicielskiego z pracą.");
        }

        var total = i.RodzicielskiWymiar;
        if (!total.CanProceed)
        {
            return Step.Blocked<WorkExtension>(this, total);
        }

        var maxShare = i.Param(PeriodsParameterKeys.PracaNaRodzicielskimMaxEtat);
        var invalid = entries.FirstOrDefault(e => e.Wymiar <= 0 || e.Wymiar > maxShare || e.Tygodnie <= 0);
        if (invalid is not null)
        {
            return Step.SoftIssue(this, new WorkExtension(0, []),
                $"Praca w wymiarze {BabyRadnik.Domain.Eligibility.Text.Number(invalid.Wymiar)} etatu przez {invalid.Tygodnie} tyg. nie spełnia warunków: pracować można w wymiarze nie wyższym niż {BabyRadnik.Domain.Eligibility.Text.Number(maxShare)} etatu. Liczę urlop bez łączenia z pracą.",
                i.Issue("praca_na_rodzicielskim_wymiar",
                    $"Z urlopem rodzicielskim można łączyć pracę w wymiarze najwyżej {BabyRadnik.Domain.Eligibility.Text.Number(maxShare)} pełnego etatu; liczba tygodni musi być dodatnia.",
                    FactKeys.PlanPracaNaCzescEtatu),
                $"praca w wymiarze nie wyższym niż {BabyRadnik.Domain.Eligibility.Text.Number(maxShare)} etatu");
        }

        var children = i.ParentalChildCount ?? 1;
        if (i.IsAdoption && i.PrzyjecieOkres.Value?.Case == AdoptionLeaveCase.Minimum)
        {
            return Step.Undecided<WorkExtension>(this,
                "Przepisy nie wskazują limitu wydłużenia urlopu rodzicielskiego przy zmniejszonym wymiarze (przyjęcie dziecka na minimalny okres) — nie rozstrzygam łączenia z pracą.");
        }

        var limit = i.ParamInt(PeriodsParameterKeys.RodzicielskiZPracaMaxTyg(children, i.ZaZyciem));
        var computedDays = (int)decimal.Floor(entries.Sum(e => e.Tygodnie * PeriodMath.DaysPerWeek * e.Wymiar));
        var capDays = Math.Max(0, (limit - total.Value) * PeriodMath.DaysPerWeek);
        var days = Math.Min(computedDays, capDays);
        var notes = new List<string>();
        if (days < computedDays)
        {
            notes.Add($"Wydłużenie ograniczono do limitu {limit} tyg. łącznie (art. 182¹f § 1).");
        }

        var weeksText = entries.Sum(e => e.Tygodnie);
        return Step.Met(this, new WorkExtension(days, notes),
            $"Praca na część etatu przez {weeksText} tyg. wydłuża urlop rodzicielski o {days} dni ({days / (decimal)PeriodMath.DaysPerWeek:0.##} tyg.): tygodnie łączenia × wymiar etatu, limit łącznie {limit} tyg.",
            "wydłużenie = tygodnie łączenia × wymiar czasu pracy");
    }
}

public sealed class RodzicielskiPoczatekRule : PeriodRule<DateOnly?>
{
    public override string Id => "rodzicielski_poczatek";

    public override string PodstawaPrawna => "KP art. 180² § 1; art. 182¹a; art. 183 § 4";

    protected override RuleStep<DateOnly?> Compute(PeriodsInputs i)
    {
        if (i.Role is null)
        {
            return Step.Missing<DateOnly?>(this, PeriodMissing.Role(i.Person));
        }

        if (i.Role != ParentRole.Matka)
        {
            return Step.Met<DateOnly?>(this, null,
                "Urlop rodzicielski ojca nie ma ustawowego początku — termin wybierasz w planie (części z datą początku).");
        }

        if (i.IsAdoption)
        {
            var leave = i.PrzyjecieOkres;
            return leave.CanProceed
                ? Step.Met<DateOnly?>(this, leave.Value!.Period.To,
                    $"Urlop rodzicielski zaczyna się bezpośrednio po urlopie na warunkach urlopu macierzyńskiego, czyli {PeriodMath.Format(leave.Value.Period.To.AddDays(1))}.")
                : Step.Blocked<DateOnly?>(this, leave);
        }

        var block = i.MacierzynskiBlok;
        if (!block.CanProceed)
        {
            return Step.Blocked<DateOnly?>(this, block);
        }

        var anchor = block.Value!.To;
        var supplementary = i.UzupelniajacyOkres;
        if (supplementary.CanProceed)
        {
            anchor = supplementary.Value!.To;
        }
        else if (!supplementary.NotApplicable && supplementary.Result.Verdict is RuleVerdict.MissingData or RuleVerdict.Undecided)
        {
            return Step.Blocked<DateOnly?>(this, supplementary);
        }

        return Step.Met<DateOnly?>(this, anchor,
            $"Urlop rodzicielski matki zaczyna się bezpośrednio po urlopie macierzyńskim{(supplementary.CanProceed ? " i uzupełniającym" : "")}, czyli {PeriodMath.Format(anchor.AddDays(1))}.",
            "rodzicielski po macierzyńskim (i uzupełniającym)");
    }
}

public sealed class RodzicielskiOkresRule : PeriodRule<RodzicielskiOutcome>
{
    public override string Id => "rodzicielski_okres";

    public override string PodstawaPrawna => "KP art. 182¹a, 182¹c, 182¹f; art. 183 § 4–5";

    protected override RuleStep<RodzicielskiOutcome> Compute(PeriodsInputs i)
    {
        var total = i.RodzicielskiWymiar;
        if (!total.CanProceed)
        {
            return Step.Blocked<RodzicielskiOutcome>(this, total);
        }

        var anchorStep = i.RodzicielskiPoczatek;
        if (!anchorStep.CanProceed)
        {
            return Step.Blocked<RodzicielskiOutcome>(this, anchorStep);
        }

        var deadline = i.RodzicielskiTermin;
        if (!deadline.CanProceed)
        {
            return Step.Blocked<RodzicielskiOutcome>(this, deadline);
        }

        var work = i.RodzicielskiPraca;
        if (!work.CanProceed)
        {
            return Step.Blocked<RodzicielskiOutcome>(this, work);
        }

        var planOk = i.RodzicielskiNieprzenoszalne.Value;
        var lastDay = deadline.Value ?? AdoptionAgeCap(i);
        var anchor = anchorStep.Value;
        var parts = planOk ? PeriodsInputs.RodzicielskiPlan(i.Me) : [];
        var notes = new List<string>(work.Value!.Notes);

        if (parts.Count > 0)
        {
            var placed = TryPlaceParts(i, parts, anchor, work.Value.Days, lastDay, notes);
            if (placed.Failure is { } failure)
            {
                if (failure.Hard)
                {
                    return Step.NotMet<RodzicielskiOutcome>(this, failure.Text,
                        "części urlopu rodzicielskiego w terminie", failure.Issue);
                }

                var fallback = DefaultBlock(i, total.Value, work.Value.Days, anchor, lastDay, notes);
                return fallback with
                {
                    Issues = [failure.Issue],
                    Soft = true,
                    Result = RuleResult.NotMet($"{failure.Text} Liczę wymiar bez planu.", "części urlopu rodzicielskiego w planie"),
                };
            }

            if (placed.Outcome is not null)
            {
                return Step.Met(this, placed.Outcome,
                    $"Urlop rodzicielski w {placed.Outcome.Parts.Count} częściach: {PeriodMath.Describe(placed.Outcome.Period!)}.",
                    "części planu mieszczą się w terminie i nie nachodzą na siebie");
            }

            notes.Add("Nie podano początku każdej części urlopu rodzicielskiego — nie ustalam okresu. Uzupełnij daty w planie.");
            return Step.Met(this, new RodzicielskiOutcome(null, [], notes),
                "Plan urlopu rodzicielskiego nie ma dat początku wszystkich części, więc okres nie jest ustalony.");
        }

        return DefaultBlock(i, total.Value, work.Value.Days, anchor, lastDay, notes);
    }

    private static DateOnly? AdoptionAgeCap(PeriodsInputs i)
    {
        if (!i.IsAdoption)
        {
            return null;
        }

        var leave = i.PrzyjecieOkres.Value;
        return i.PrzyjecieSciezka.Value == AdoptionPath.Przysposobienie && leave?.Case == AdoptionLeaveCase.Full
            ? leave.AgeLimitLastDay
            : null;
    }

    private RuleStep<RodzicielskiOutcome> DefaultBlock(
        PeriodsInputs i, int weeks, int extensionDays, DateOnly? anchor, DateOnly? lastDay, List<string> notes)
    {
        var perParent = weeks - i.ParamInt(PeriodsParameterKeys.RodzicielskiNieprzenoszalneTyg);
        notes.Add($"Pokazany jest łączny wymiar {weeks} tyg.; jeden rodzic może wykorzystać najwyżej {perParent} tyg.");
        if (anchor is null)
        {
            notes.Add("Początek urlopu rodzicielskiego wybierasz w planie — bez daty początku nie ustalam okresu.");
            return Step.Met(this, new RodzicielskiOutcome(null, [], notes),
                $"Wymiar urlopu rodzicielskiego to {weeks} tyg. Bez daty początku nie ustalam okresu.");
        }

        var start = anchor.Value.AddDays(1);
        var days = weeks * PeriodMath.DaysPerWeek + extensionDays;
        if (lastDay is { } limit)
        {
            if (start > limit)
            {
                return Step.NotMet<RodzicielskiOutcome>(this,
                    $"Urlop rodzicielski zaczynałby się {PeriodMath.Format(start)}, po granicy {PeriodMath.Format(limit)} — nie przysługuje.",
                    "urlop rodzicielski w ustawowym terminie");
            }

            if (start.AddDays(days - 1) > limit)
            {
                days = PeriodMath.DaysInclusive(start, limit);
                notes.Add($"Okres skrócono do {PeriodMath.Format(limit)} (granica terminu lub wieku dziecka).");
            }
        }

        var period = PeriodMath.ForDays(start, days);
        if (extensionDays > 0)
        {
            notes.Add($"Okres obejmuje wydłużenie o {extensionDays} dni z tytułu łączenia z pracą (art. 182¹f).");
        }

        return Step.Met(this, new RodzicielskiOutcome(period, [], notes),
            $"Urlop rodzicielski: {PeriodMath.Describe(period)}.",
            "rodzicielski bezpośrednio po macierzyńskim, w całości");
    }

    private sealed record Placement(RodzicielskiOutcome? Outcome, PlacementFailure? Failure);

    private sealed record PlacementFailure(string Text, ValidationIssue Issue, bool Hard);

    private Placement TryPlaceParts(
        PeriodsInputs i, IReadOnlyList<LeavePart> parts, DateOnly? anchor, int extensionDays, DateOnly? lastDay, List<string> notes)
    {
        var ranges = new List<(DateOnly Start, DateOnly End)>();
        for (var index = 0; index < parts.Count; index++)
        {
            var part = parts[index];
            if (part.Tygodnie < 1)
            {
                return Fail(i, "Część urlopu rodzicielskiego musi trwać co najmniej 1 tydzień.", "rodzicielski_zla_dlugosc_czesci", false);
            }

            var start = part.Od ?? (index == 0 && anchor is { } a ? a.AddDays(1) : (DateOnly?)null);
            if (start is null)
            {
                return new Placement(null, null);
            }

            ranges.Add((start.Value, start.Value.AddDays(part.Tygodnie * PeriodMath.DaysPerWeek - 1)));
        }

        if (extensionDays > 0)
        {
            var workStart = i.Me.PlanPracaNaCzescEtatu?.Value.Select(w => w.Od).FirstOrDefault(d => d is not null);
            var target = workStart is { } w
                ? ranges.FindIndex(r => r.Start <= w && w <= r.End)
                : -1;
            if (target < 0)
            {
                target = ranges.Count - 1;
                notes.Add("Wydłużenie z tytułu pracy dodano do ostatniej części urlopu rodzicielskiego (nie wskazano daty pracy w obrębie części).");
            }

            ranges[target] = (ranges[target].Start, ranges[target].End.AddDays(extensionDays));
        }

        var ordered = ranges.OrderBy(r => r.Start).ToList();
        for (var k = 1; k < ordered.Count; k++)
        {
            if (ordered[k].Start <= ordered[k - 1].End)
            {
                return Fail(i, "Części urlopu rodzicielskiego nachodzą na siebie.", "rodzicielski_czesci_nachodza", false);
            }
        }

        if (anchor is { } before && ordered[0].Start <= before)
        {
            return Fail(i, $"Urlop rodzicielski matki nie może zaczynać się przed {PeriodMath.Format(before.AddDays(1))} — wcześniej trwa urlop macierzyński (i uzupełniający).",
                "rodzicielski_przed_koncem_macierzynskiego", false);
        }

        if (lastDay is { } limit && ordered[^1].End > limit)
        {
            return Fail(i, $"Część urlopu rodzicielskiego kończyłaby się {PeriodMath.Format(ordered[^1].End)}, po terminie {PeriodMath.Format(limit)} — nie przysługuje.",
                "rodzicielski_po_terminie", true);
        }

        var built = ranges.Select((r, index) => new BenefitPart(
            $"Część {index + 1} urlopu rodzicielskiego",
            PeriodMath.Span(r.Start, r.End),
            i.ParamAt(PeriodsParameterKeys.StawkaRodzicielski, r.Start))).OrderBy(p => p.Period.From).ToList();
        var daysTotal = built.Sum(p => p.Period.Days);
        var period = new Period(built[0].Period.From, built[^1].Period.To, daysTotal, daysTotal / (decimal)PeriodMath.DaysPerWeek);
        if (extensionDays > 0)
        {
            notes.Add($"Okres obejmuje wydłużenie o {extensionDays} dni z tytułu łączenia z pracą (art. 182¹f). Stawka w częściach to stawka podstawowa — korekta za pracę na część etatu należy do wyliczenia kwot.");
        }

        return new Placement(new RodzicielskiOutcome(period, built, notes), null);
    }

    private static Placement Fail(PeriodsInputs i, string text, string code, bool hard) =>
        new(null, new PlacementFailure(text, i.Issue(code, text, FactKeys.PlanRodzicielskiCzesci), hard));
}
