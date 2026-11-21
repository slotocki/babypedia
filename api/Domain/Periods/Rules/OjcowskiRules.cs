using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public sealed class OjcowskiOknoRule : PeriodRule<DateOnly>
{
    public override string Id => "ojcowski_okno";

    public override string PodstawaPrawna => "KP art. 182³ § 1";

    protected override RuleStep<DateOnly> Compute(PeriodsInputs i)
    {
        if (Step.MissingIf<DateOnly>(this, i.NeedEventDate()) is { } missing)
        {
            return missing;
        }

        var months = i.ParamInt(PeriodsParameterKeys.OjcowskiDoMiesiacaZycia);
        if (!i.IsAdoption)
        {
            var birth = i.EventDate!.Value;
            var last = birth.AddMonths(months).AddDays(-1);
            return Step.Met(this, last,
                $"Urlop ojcowski można wykorzystać do ukończenia przez dziecko {months}. miesiąca życia — najpóźniej {PeriodMath.Format(last)}.",
                $"do ukończenia przez dziecko {months}. miesiąca życia");
        }

        var path = i.Case.PrzyjecieSciezka?.Value;
        if (path is null)
        {
            return Step.Missing<DateOnly>(this,
                new MissingFact(FactKeys.PrzyjecieSciezka, "Na jakiej podstawie przyjęto dziecko na wychowanie?", MissingFactType.Enum,
                    Options: ["przysposobienie", "rodzina_zastepcza_niezawodowa", "rodzina_zastepcza_zawodowa"]));
        }

        if (path != AdoptionPath.Przysposobienie)
        {
            return Step.Undecided<DateOnly>(this,
                "Przepis o urlopie ojcowskim wskazuje okres wykorzystania tylko dla urodzenia dziecka i przysposobienia. Dla rodziny zastępczej nie rozstrzygam — skontaktuj się z ZUS.");
        }

        var ruling = i.Case.PrzyjecieDataUprawomocnieniaPostanowienia?.Value;
        var childBirth = i.Case.DzieckoDataUrodzenia?.Value;
        var needs = new[]
        {
            ruling is null ? new MissingFact(FactKeys.PrzyjecieDataUprawomocnieniaPostanowienia, "Kiedy uprawomocniło się postanowienie sądu o przysposobieniu?", MissingFactType.Date) : null,
            childBirth is null ? new MissingFact(FactKeys.DzieckoDataUrodzenia, "Jaka jest data urodzenia przyjmowanego dziecka?", MissingFactType.Date) : null,
        };
        if (Step.MissingIf<DateOnly>(this, needs) is { } missingStep)
        {
            return missingStep;
        }

        var fromRuling = ruling!.Value.AddMonths(months).AddDays(-1);
        var ageLimit = PeriodMath.LastDayBeforeBirthday(childBirth!.Value, i.ParamInt(PeriodsParameterKeys.WiekPrzysposobienieMax));
        var window = fromRuling < ageLimit ? fromRuling : ageLimit;
        return Step.Met(this, window,
            $"Przy przysposobieniu urlop ojcowski można wykorzystać do upływu {months} miesięcy od uprawomocnienia postanowienia, nie dłużej niż do ukończenia przez dziecko {i.ParamInt(PeriodsParameterKeys.WiekPrzysposobienieMax)}. roku życia — najpóźniej {PeriodMath.Format(window)}.",
            $"{months} miesięcy od uprawomocnienia postanowienia, nie dłużej niż do granicy wieku");
    }
}

public sealed class OjcowskiCzesciRule : PeriodRule<IReadOnlyList<LeavePart>>
{
    public override string Id => "ojcowski_czesci";

    public override string PodstawaPrawna => "KP art. 182³ § 1, § 1¹";

    protected override RuleStep<IReadOnlyList<LeavePart>> Compute(PeriodsInputs i)
    {
        if (Step.MissingIf<IReadOnlyList<LeavePart>>(this, i.NeedEventDate()) is { } missing)
        {
            return missing;
        }

        var parts = i.Me.PlanOjcowskiCzesci?.Value ?? [];
        var maxParts = i.ParamInt(PeriodsParameterKeys.OjcowskiMaxCzesci);
        var minWeeks = i.ParamInt(PeriodsParameterKeys.OjcowskiMinCzescTyg);
        var weeks = i.ParamInt(PeriodsParameterKeys.OjcowskiTyg);
        if (parts.Count == 0)
        {
            return Step.Met<IReadOnlyList<LeavePart>>(this, [],
                $"Urlop ojcowski trwa do {weeks} tyg., jednorazowo albo w najwyżej {maxParts} częściach, każda co najmniej {minWeeks} tyg.");
        }

        if (parts.Count > maxParts)
        {
            return Step.NotMet<IReadOnlyList<LeavePart>>(this,
                $"Urlop ojcowski można wykorzystać jednorazowo albo w najwyżej {maxParts} częściach; plan zakłada {parts.Count} części, więc nie przysługuje.",
                $"nie więcej niż {maxParts} części",
                i.Issue("ojcowski_ponad_limit_czesci", $"Urlop ojcowski można podzielić na najwyżej {maxParts} części.", FactKeys.PlanOjcowskiCzesci));
        }

        if (parts.Any(p => p.Tygodnie < minWeeks))
        {
            return Step.SoftIssue<IReadOnlyList<LeavePart>>(this, [],
                $"Żadna część urlopu ojcowskiego nie może być krótsza niż {minWeeks} tyg. Liczę wymiar bez planu.",
                i.Issue("ojcowski_czesc_zbyt_krotka", $"Każda część urlopu ojcowskiego musi trwać co najmniej {minWeeks} tyg.", FactKeys.PlanOjcowskiCzesci),
                $"każda część co najmniej {minWeeks} tyg.");
        }

        var planned = parts.Sum(p => p.Tygodnie);
        if (planned > weeks)
        {
            return Step.SoftIssue<IReadOnlyList<LeavePart>>(this, [],
                $"Plan zakłada {planned} tyg. urlopu ojcowskiego, a przysługuje najwyżej {weeks} tyg. Liczę wymiar bez planu.",
                i.Issue("ojcowski_ponad_wymiar", $"Urlop ojcowski przysługuje w wymiarze do {weeks} tyg.", FactKeys.PlanOjcowskiCzesci),
                $"łącznie nie więcej niż {weeks} tyg.");
        }

        return Step.Met(this, parts,
            $"Plan urlopu ojcowskiego: {parts.Count} cz. po {string.Join(" i ", parts.Select(p => $"{p.Tygodnie} tyg."))}, łącznie {planned} z {weeks} tyg.",
            $"do {maxParts} części, każda co najmniej {minWeeks} tyg., łącznie do {weeks} tyg.");
    }
}

public sealed class OjcowskiOkresRule : PeriodRule<OjcowskiOutcome>
{
    public override string Id => "ojcowski_okres";

    public override string PodstawaPrawna => "KP art. 182³ § 1, § 1¹";

    protected override RuleStep<OjcowskiOutcome> Compute(PeriodsInputs i)
    {
        var window = i.OjcowskiOkno;
        if (!window.CanProceed)
        {
            return Step.Blocked<OjcowskiOutcome>(this, window);
        }

        var parts = i.OjcowskiCzesci;
        if (!parts.CanProceed)
        {
            return Step.Blocked<OjcowskiOutcome>(this, parts);
        }

        var last = window.Value;
        var weeks = i.ParamInt(PeriodsParameterKeys.OjcowskiTyg);
        var plan = parts.Value!;
        var notes = new List<string>();

        if (plan.Count > 0 && plan.All(p => p.Od is not null))
        {
            var built = plan
                .Select((p, index) => new BenefitPart(
                    $"Część {index + 1} urlopu ojcowskiego",
                    PeriodMath.ForWeeks(p.Od!.Value, p.Tygodnie),
                    i.ParamAt(PeriodsParameterKeys.StawkaOjcowski, p.Od!.Value)))
                .OrderBy(p => p.Period.From)
                .ToList();

            if (built[^1].Period.To > last)
            {
                return Step.NotMet<OjcowskiOutcome>(this,
                    $"Część urlopu ojcowskiego kończyłaby się {PeriodMath.Format(built[^1].Period.To)}, po terminie {PeriodMath.Format(last)}. Po tym terminie prawo do urlopu ojcowskiego wygasa.",
                    "urlop ojcowski wykorzystany w terminie");
            }

            if (built.Count > 1 && built[1].Period.From <= built[0].Period.To)
            {
                return Step.SoftIssue(this, new OjcowskiOutcome(null, [], []),
                    "Części urlopu ojcowskiego nachodzą na siebie. Nie ustalam okresu.",
                    i.Issue("ojcowski_czesci_nachodza", "Części urlopu ojcowskiego nie mogą na siebie nachodzić.", FactKeys.PlanOjcowskiCzesci));
            }

            var days = built.Sum(p => p.Period.Days);
            var period = new Period(built[0].Period.From, built[^1].Period.To, days, days / (decimal)PeriodMath.DaysPerWeek);
            return Step.Met(this, new OjcowskiOutcome(period, built, notes),
                $"Urlop ojcowski w {built.Count} cz.: {PeriodMath.Describe(period)}; mieści się w terminie do {PeriodMath.Format(last)}.",
                "urlop ojcowski wykorzystany w terminie");
        }

        if (i.AsOf > last)
        {
            return Step.NotMet<OjcowskiOutcome>(this,
                $"Termin na urlop ojcowski minął {PeriodMath.Format(last)}. Po tym terminie prawo do urlopu ojcowskiego wygasa.",
                "urlop ojcowski wykorzystany w terminie");
        }

        notes.Add($"Urlop ojcowski trwa do {weeks} tyg. i trzeba go wykorzystać do {PeriodMath.Format(last)}. Podaj daty części w planie, żeby ustalić okres.");
        return Step.Met(this, new OjcowskiOutcome(null, [], notes),
            $"Urlop ojcowski przysługuje w wymiarze do {weeks} tyg.; termin wykorzystania upływa {PeriodMath.Format(last)}. Daty części nie zostały wskazane.",
            "urlop ojcowski wykorzystany w terminie");
    }
}
