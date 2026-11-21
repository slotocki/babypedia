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
        if (lifeDays < weeksAfterBirth * PeriodMath.DaysPerWeek)
        {
            var afterBirthEnd = birth.AddDays(weeksAfterBirth * PeriodMath.DaysPerWeek - 1);
            var fromDeathEnd = death.AddDays(minDaysFromDeath - 1);
            var end = aliveEnd ?? (afterBirthEnd > fromDeathEnd ? afterBirthEnd : fromDeathEnd);
            var early = PeriodMath.Span(start, end);
            var earlyText = aliveEnd is null
                ? $"Dziecko urodziło się martwe albo zmarło przed upływem {weeksAfterBirth} tyg. życia: urlop trwa {weeksAfterBirth} tyg. po porodzie, nie krócej niż {minDaysFromDeath} dni od dnia zgonu."
                : $"Przy porodzie wielokrotnym urlop ma wymiar odpowiadający liczbie dzieci pozostałych przy życiu ({alive}).";
            return Step.Met(this, early, $"{earlyText} Okres: {PeriodMath.Describe(early)}.",
                $"zgon przed upływem {weeksAfterBirth} tyg. życia");
        }

        if (death > normalEnd)
        {
            var whole = PeriodMath.Span(start, normalEnd);
            return Step.Met(this, whole,
                $"Dziecko zmarło po zakończeniu urlopu macierzyńskiego — okres się nie zmienia: {PeriodMath.Describe(whole)}.",
                $"zgon po upływie {weeksAfterBirth} tyg. życia");
        }

        var keepEnd = death.AddDays(daysAfterLateDeath - 1);
        var lateEnd = keepEnd >= normalEnd ? normalEnd : aliveEnd is { } ae && ae > keepEnd ? ae : keepEnd;
        var late = PeriodMath.Span(death, lateEnd);
        var lateText = aliveEnd is null
            ? $"Dziecko zmarło po upływie {weeksAfterBirth} tyg. życia: prawo do urlopu trwa {daysAfterLateDeath} dni od dnia zgonu (nie dłużej niż do końca wymiaru urlopu)."
            : $"Dziecko zmarło po upływie {weeksAfterBirth} tyg. życia: urlop ma wymiar jak dla dzieci pozostałych przy życiu ({alive}), nie krócej niż {daysAfterLateDeath} dni od zgonu.";
        return Step.Met(this, late,
            $"{lateText} Okres po zgonie: {PeriodMath.Describe(late)}. Urlop od porodu ({PeriodMath.Format(start)}) do dnia poprzedzającego zgon przysługuje na zasadach ogólnych.",
            $"zgon po upływie {weeksAfterBirth} tyg. życia");
    }
}

public sealed class SwiadczeniaPoZgonieDzieckaRule : PeriodRule<bool>
{
    public override string Id => "swiadczenia_po_zgonie_dziecka";

    public override string PodstawaPrawna => "KP art. 180¹ § 1, art. 182¹a § 1, art. 182³ § 1";

    public const string StillbornText =
        "Dziecko urodziło się martwe. Urlop rodzicielski i urlop ojcowski przysługują w celu sprawowania opieki nad dzieckiem, " +
        "więc za ich okres zasiłek macierzyński nie przysługuje. Po urodzeniu martwego dziecka matce przysługuje zasiłek macierzyński " +
        "za 8 tygodni po porodzie (KP art. 180¹ § 1).";

    protected override RuleStep<bool> Compute(PeriodsInputs i)
    {
        if (i.StillbornWithNoChildAlive)
        {
            return Step.NotMet<bool>(this, StillbornText, "dziecko urodzone martwe, żadne dziecko z porodu nie żyje");
        }

        if (i.DeadCountUnknown)
        {
            return Step.Missing<bool>(this, new MissingFact(FactKeys.DzieckoLiczbaZmarlych,
                "Ile dzieci urodzonych przy jednym porodzie zmarło lub urodziło się martwych?", MissingFactType.Integer));
        }

        return Step.Undecided<bool>(this,
            "Przepisy o urlopie ojcowskim (KP art. 182³) nie odsyłają do art. 180¹ i nie mówią, co dzieje się z urlopem po zgonie dziecka. " +
            "Co do urlopu ojcowskiego nie rozstrzygam — skontaktuj się z ZUS.");
    }
}

/// <summary>
/// Zgon dziecka w trakcie urlopu rodzicielskiego albo uzupełniającego: prawo do urlopu zachowuje się przez 7 dni od dnia zgonu
/// (KP art. 180¹ § 2, stosowany odpowiednio przez art. 182¹g i art. 180² § 7). Zgon przed rozpoczęciem urlopu — urlop nie przysługuje.
/// </summary>
public sealed class ZgonWTrakcieUrlopuRule(BenefitId leave) : PeriodRule<Period>
{
    private bool Parental => leave == BenefitId.Rodzicielski;

    private string LeaveName => Parental ? "urlopu rodzicielskiego" : "uzupełniającego urlopu macierzyńskiego";

    public override string Id => Parental ? "zgon_dziecka_w_trakcie_rodzicielskiego" : "zgon_dziecka_w_trakcie_uzupelniajacego";

    public override string PodstawaPrawna => Parental ? "KP art. 180¹ § 2 w zw. z art. 182¹g" : "KP art. 180¹ § 2 w zw. z art. 180² § 7";

    protected override RuleStep<Period> Compute(PeriodsInputs i)
    {
        var period = Parental ? i.RodzicielskiOkres.Value?.Period : i.UzupelniajacyOkres.Value;
        if (period is null || i.DeathDate is not { } death)
        {
            return Step.Undecided<Period>(this, $"Nie ustalono okresu {LeaveName}, więc nie da się ocenić skutków zgonu dziecka — nie rozstrzygam.");
        }

        var days = i.ParamInt(PeriodsParameterKeys.ZgonPo8TygDni);
        if (death < period.From)
        {
            return Step.NotMet<Period>(this,
                $"Dziecko zmarło {PeriodMath.Format(death)}, przed rozpoczęciem {LeaveName} ({PeriodMath.Format(period.From)}). " +
                $"Urlop służy opiece nad dzieckiem, więc zasiłek za jego okres nie przysługuje. Prawo do urlopu zachowuje się przez {days} dni od zgonu " +
                "tylko wtedy, gdy dziecko zmarło w trakcie urlopu.",
                "zgon dziecka przed rozpoczęciem urlopu");
        }

        var keepEnd = death.AddDays(days - 1);
        if (keepEnd >= period.To)
        {
            return Step.Met(this, period,
                $"Dziecko zmarło {PeriodMath.Format(death)}; prawo do urlopu zachowuje się przez {days} dni od zgonu, a urlop kończy się wcześniej — okres się nie zmienia: {PeriodMath.Describe(period)}.",
                "zgon dziecka pod koniec urlopu albo po nim");
        }

        var cut = PeriodMath.Span(period.From, keepEnd);
        return Step.Met(this, cut,
            $"Dziecko zmarło {PeriodMath.Format(death)} w trakcie {LeaveName}. Prawo do urlopu, a więc i do zasiłku, zachowuje się przez {days} dni od dnia zgonu: {PeriodMath.Describe(cut)}.",
            $"zgon dziecka w trakcie urlopu — {days} dni od zgonu");
    }

    /// <summary>Przycina części urlopu do ostatniego dnia okresu po zgonie.</summary>
    public static IReadOnlyList<BenefitPart> Trim(IReadOnlyList<BenefitPart> parts, DateOnly lastDay) =>
        [.. parts
            .Where(p => p.Period.From <= lastDay)
            .Select(p => p.Period.To <= lastDay ? p : p with { Period = PeriodMath.Span(p.Period.From, lastDay) })];
}
