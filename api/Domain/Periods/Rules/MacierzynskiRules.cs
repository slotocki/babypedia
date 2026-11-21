using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public sealed class MacierzynskiWymiarRule : PeriodRule<int>
{
    public override string Id => "macierzynski_wymiar";

    public override string PodstawaPrawna => "KP art. 180 § 1";

    protected override RuleStep<int> Compute(PeriodsInputs i)
    {
        if (Step.MissingIf<int>(this, i.NeedEventDate(), i.NeedChildCount()) is { } missing)
        {
            return missing;
        }

        var count = i.ChildCount!.Value;
        if (count < 1)
        {
            return Step.Undecided<int>(this, "Liczba dzieci przy jednym porodzie musi wynosić co najmniej 1.");
        }

        var weeks = i.ParamInt(PeriodsParameterKeys.MacierzynskiTyg(count));
        return Step.Met(this, weeks,
            $"Przy liczbie dzieci {count} urlop macierzyński trwa {weeks} tyg. ({weeks * PeriodMath.DaysPerWeek} dni).",
            "wymiar zależy od liczby dzieci przy jednym porodzie");
    }
}

public sealed class MacierzynskiPrzedPorodemRule : PeriodRule<int>
{
    public override string Id => "macierzynski_przed_porodem";

    public override string PodstawaPrawna => "KP art. 180 § 2; art. 183¹ § 2";

    protected override RuleStep<int> Compute(PeriodsInputs i)
    {
        if (Step.MissingIf<int>(this, i.NeedEventDate()) is { } missing)
        {
            return missing;
        }

        var planned = i.Me.PlanMacierzynskiPrzedPorodemTyg?.Value;
        if (planned is null)
        {
            return Step.Met(this, 0, "Urlop macierzyński zaczyna się w dniu porodu — nie zaplanowano części przed porodem.",
                "pierwszym dniem urlopu jest dzień porodu, jeśli nie korzystasz z urlopu przed porodem");
        }

        var max = i.ParamInt(PeriodsParameterKeys.MacierzynskiPrzedPorodemMax);
        if (planned < 0 || planned > max)
        {
            return Step.SoftIssue(this, 0,
                $"Zaplanowano {planned} tyg. urlopu przed porodem, a przepis pozwala najwyżej na {max}. Liczę urlop od dnia porodu.",
                i.Issue("macierzynski_przed_porodem_ponad_limit",
                    $"Przed przewidywaną datą porodu można wykorzystać najwyżej {max} tyg. urlopu macierzyńskiego.",
                    FactKeys.PlanMacierzynskiPrzedPorodemTyg),
                $"nie więcej niż {max} tyg. przed przewidywaną datą porodu");
        }

        return Step.Met(this, planned.Value,
            planned == 0
                ? "Urlop macierzyński zaczyna się w dniu porodu."
                : $"Część urlopu macierzyńskiego ({planned} tyg.) przypada przed porodem; limit to {max} tyg.",
            $"nie więcej niż {max} tyg. przed przewidywaną datą porodu");
    }
}

public sealed class MacierzynskiOkresRule : PeriodRule<Period>
{
    public override string Id => "macierzynski_okres";

    public override string PodstawaPrawna => "KP art. 180 § 1–3; art. 183¹ § 1";

    protected override RuleStep<Period> Compute(PeriodsInputs i)
    {
        var weeks = i.MacierzynskiWymiar;
        if (!weeks.CanProceed)
        {
            return Step.Blocked<Period>(this, weeks);
        }

        var before = i.MacierzynskiPrzedPorodem;
        if (!before.CanProceed)
        {
            return Step.Blocked<Period>(this, before);
        }

        var start = i.EventDate!.Value.AddDays(-before.Value * PeriodMath.DaysPerWeek);
        var period = PeriodMath.ForWeeks(start, weeks.Value);
        return Step.Met(this, period,
            $"Urlop macierzyński: {PeriodMath.Describe(period)}. Tydzień urlopu to 7 dni kalendarzowych.",
            "okres liczony włącznie z dniem początkowym i końcowym");
    }
}

public sealed class MacierzynskiRezygnacjaRule : PeriodRule<Period>
{
    public override string Id => "macierzynski_rezygnacja";

    public override string PodstawaPrawna => "KP art. 180 § 4, 6; ustawa zasiłkowa art. 29 ust. 3, 5";

    protected override RuleStep<Period> Compute(PeriodsInputs i)
    {
        var block = i.MacierzynskiBlok;
        if (!block.CanProceed)
        {
            return Step.Blocked<Period>(this, block);
        }

        var full = block.Value!;
        var resignedAfter = i.Me.PlanRezygnacjaZMacierzynskiegoPoTyg?.Value;
        if (resignedAfter is null)
        {
            return Step.Met(this, full, "Nie planujesz rezygnacji z części urlopu macierzyńskiego.");
        }

        var special = i.Case.MatkaSytuacjaSzczegolna?.Value.Rodzaj == MotherSituationKind.NiezdolnoscDoSamodzielnejEgzystencji;
        var minimum = i.ParamInt(special ? PeriodsParameterKeys.MatkaMinPoPorodzieSzczegolneTyg : PeriodsParameterKeys.MatkaMinPoPorodzieTyg);
        if (resignedAfter < minimum)
        {
            return Step.SoftIssue(this, full,
                $"Rezygnacja po {resignedAfter} tyg. po porodzie jest zbyt wczesna — wymagane co najmniej {minimum} tyg. Pokazuję okres bez rezygnacji.",
                i.Issue("rezygnacja_z_macierzynskiego_przed_minimum",
                    $"Z pozostałej części urlopu macierzyńskiego można zrezygnować po wykorzystaniu co najmniej {minimum} tyg. po porodzie.",
                    FactKeys.PlanRezygnacjaZMacierzynskiegoPoTyg),
                $"rezygnacja najwcześniej po {minimum} tyg. po porodzie");
        }

        var birth = i.EventDate!.Value;
        var lastDayUsed = birth.AddDays(resignedAfter.Value * PeriodMath.DaysPerWeek - 1);
        if (lastDayUsed >= full.To)
        {
            return Step.Met(this, full, "Rezygnacja przypada po końcu urlopu macierzyńskiego — okres się nie zmienia.");
        }

        var shortened = PeriodMath.Span(full.From, lastDayUsed);
        return Step.Met(this, shortened,
            $"Po rezygnacji z reszty urlopu (po {resignedAfter} tyg. po porodzie, minimum {minimum}) okres matki to {PeriodMath.Describe(shortened)}.",
            $"rezygnacja najwcześniej po {minimum} tyg. po porodzie");
    }
}

public sealed class MartweUrodzenieZgonDzieckaRule : PeriodRule<Period>
{
    public override string Id => "martwe_urodzenie_zgon_dziecka";

    public override string PodstawaPrawna => "KP art. 180¹ § 1–2";

    protected override RuleStep<Period> Compute(PeriodsInputs i)
    {
        var birthDate = i.EventDate;
        var count = i.ChildCount;
        var deadCount = i.Case.DzieckoLiczbaZmarlych?.Value;
        var missing = new List<MissingFact?> { i.NeedEventDate(), i.NeedChildCount() };
        if (count > 1 && deadCount is null)
        {
            missing.Add(new MissingFact(FactKeys.DzieckoLiczbaZmarlych,
                "Ile dzieci urodzonych przy jednym porodzie zmarło lub urodziło się martwych?", MissingFactType.Integer));
        }

        if (!i.Stillborn && i.DeathDate is null)
        {
            missing.Add(new MissingFact(FactKeys.DzieckoDataZgonu, "Kiedy zmarło dziecko?", MissingFactType.Date));
        }

        if (Step.MissingIf<Period>(this, [.. missing]) is { } missingStep)
        {
            return missingStep;
        }

        var before = i.MacierzynskiPrzedPorodem;
        if (!before.CanProceed)
        {
            return Step.Blocked<Period>(this, before);
        }

        var birth = birthDate!.Value;
        var death = i.Stillborn ? birth : i.DeathDate!.Value;
        if (death < birth)
        {
            return Step.Undecided<Period>(this, "Data zgonu dziecka jest wcześniejsza niż data porodu — nie rozstrzygam.");
        }

        var children = count!.Value;
        var dead = deadCount ?? 1;
        if (children < 1 || dead < 1 || dead > children)
        {
            return Step.Undecided<Period>(this, "Liczba dzieci albo liczba zmarłych dzieci jest nieprawidłowa — nie rozstrzygam.");
        }

        var alive = children - dead;
        var weeksAfterBirth = i.ParamInt(PeriodsParameterKeys.MartweUrodzenieTygPoPorodzie);
        var minDaysFromDeath = i.ParamInt(PeriodsParameterKeys.MartweUrodzenieMinDniOdZgonu);
        var daysAfterLateDeath = i.ParamInt(PeriodsParameterKeys.ZgonPo8TygDni);
        var start = birth.AddDays(-before.Value * PeriodMath.DaysPerWeek);
        var normalEnd = PeriodMath.ForWeeks(start, i.ParamInt(PeriodsParameterKeys.MacierzynskiTyg(children))).To;
        var aliveEnd = alive > 0
            ? PeriodMath.ForWeeks(start, i.ParamInt(PeriodsParameterKeys.MacierzynskiTyg(alive))).To
            : (DateOnly?)null;

        var lifeDays = death.DayNumber - birth.DayNumber;
        DateOnly end;
        string text;
        if (lifeDays < weeksAfterBirth * PeriodMath.DaysPerWeek)
        {
            var afterBirthEnd = birth.AddDays(weeksAfterBirth * PeriodMath.DaysPerWeek - 1);
            var fromDeathEnd = death.AddDays(minDaysFromDeath - 1);
            end = aliveEnd ?? (afterBirthEnd > fromDeathEnd ? afterBirthEnd : fromDeathEnd);
            text = aliveEnd is null
                ? $"Dziecko urodziło się martwe albo zmarło przed upływem {weeksAfterBirth} tyg. życia: urlop trwa {weeksAfterBirth} tyg. po porodzie, nie krócej niż {minDaysFromDeath} dni od dnia zgonu."
                : $"Przy porodzie wielokrotnym urlop ma wymiar odpowiadający liczbie dzieci pozostałych przy życiu ({alive}).";
        }
        else
        {
            var keepEnd = death.AddDays(daysAfterLateDeath - 1);
            if (keepEnd >= normalEnd)
            {
                end = normalEnd;
                text = "Zgon nastąpił pod koniec urlopu — okres macierzyńskiego się nie zmienia.";
            }
            else
            {
                end = aliveEnd is { } ae && ae > keepEnd ? ae : keepEnd;
                text = aliveEnd is null
                    ? $"Dziecko zmarło po upływie {weeksAfterBirth} tyg. życia: prawo do urlopu trwa {daysAfterLateDeath} dni od dnia zgonu (nie dłużej niż wymiar urlopu)."
                    : $"Dziecko zmarło po upływie {weeksAfterBirth} tyg. życia: wymiar jak dla dzieci pozostałych przy życiu ({alive}), nie krócej niż {daysAfterLateDeath} dni od zgonu.";
            }
        }

        var period = PeriodMath.Span(start, end);
        return Step.Met(this, period, $"{text} Okres: {PeriodMath.Describe(period)}.",
            $"zgon przed upływem {weeksAfterBirth} tyg. życia albo po nim");
    }
}

public sealed class SwiadczeniaPoZgonieDzieckaRule : PeriodRule<bool>
{
    public override string Id => "swiadczenia_po_zgonie_dziecka";

    public override string PodstawaPrawna => "KP art. 180¹ (dotyczy wyłącznie urlopu macierzyńskiego)";

    protected override RuleStep<bool> Compute(PeriodsInputs i) =>
        Step.Undecided<bool>(this,
            "Przepisy użyte w tym module regulują po martwym urodzeniu lub zgonie dziecka tylko urlop macierzyński. Co do tego świadczenia nie rozstrzygam — skontaktuj się z ZUS.");
}
