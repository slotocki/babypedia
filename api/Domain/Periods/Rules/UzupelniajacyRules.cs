using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public sealed class UzupelniajacyPrzypadekRule : PeriodRule<UzupelniajacyCase>
{
    public override string Id => "uzupelniajacy_przypadek";

    public override string PodstawaPrawna => "KP art. 180¹ § 1, art. 180² § 1–2, 5";

    protected override RuleStep<UzupelniajacyCase> Compute(PeriodsInputs i)
    {
        if (i.Kind != EventKind.Urodzenie)
        {
            return Step.Inapplicable<UzupelniajacyCase>(this, "Uzupełniający urlop macierzyński ustala się po porodzie, gdy dziecko przebywa w szpitalu.");
        }

        if (i.StillbornWithNoChildAlive)
        {
            return Step.NotMet<UzupelniajacyCase>(this,
                "Dziecko urodziło się martwe. Uzupełniający urlop macierzyński przysługuje za okres pobytu dziecka w szpitalu po porodzie, " +
                "więc za jego okres zasiłek macierzyński nie przysługuje. Po urodzeniu martwego dziecka matce przysługuje zasiłek macierzyński " +
                "za 8 tygodni po porodzie (KP art. 180¹ § 1).",
                "dziecko urodzone martwe, żadne dziecko z porodu nie żyje");
        }

        if (i.ChildDeceased)
        {
            return Step.Undecided<UzupelniajacyCase>(this,
                "Przy zgonie dziecka nie rozstrzygam uzupełniającego urlopu macierzyńskiego — skontaktuj się z ZUS.");
        }

        if (i.HospitalDays is not { Count: > 0 })
        {
            return Step.Inapplicable<UzupelniajacyCase>(this, "Nie podano pobytu dziecka w szpitalu po porodzie, więc uzupełniający urlop macierzyński nie dotyczy.");
        }

        if (i.HasInvalidHospitalRange)
        {
            return Step.Undecided<UzupelniajacyCase>(this, "Okres pobytu dziecka w szpitalu ma datę końca wcześniejszą niż datę początku — nie rozstrzygam.");
        }

        var weeks = i.Case.DzieckoTydzienCiazyPrzyPorodzie?.Value;
        var mass = i.Case.DzieckoMasaUrodzeniowaG?.Value;
        var weekThresholdEarly = i.ParamInt(PeriodsParameterKeys.UzupelniajacyProgTydzienWczesniak);
        var weekThresholdFullTerm = i.ParamInt(PeriodsParameterKeys.UzupelniajacyProgTydzienDonoszony);
        var massThreshold = i.ParamInt(PeriodsParameterKeys.UzupelniajacyProgMasaG);

        if ((weeks is not null && weeks < weekThresholdEarly) || (mass is not null && mass <= massThreshold))
        {
            var max = i.ParamInt(PeriodsParameterKeys.UzupelniajacyMaxTygWczesniak);
            return Step.Met(this, new UzupelniajacyCase("wcześniak", max, 0, false),
                $"Poród przed ukończeniem {weekThresholdEarly}. tygodnia ciąży lub masa urodzeniowa nie większa niż {massThreshold} g: tydzień urlopu za każdy tydzień pobytu w szpitalu, do upływu {max}. tygodnia po porodzie.",
                $"poród przed {weekThresholdEarly}. tyg. ciąży lub masa ≤ {massThreshold} g");
        }

        var missing = new List<MissingFact?>
        {
            weeks is null
                ? new MissingFact(FactKeys.DzieckoTydzienCiazyPrzyPorodzie, "W którym tygodniu ciąży urodziło się dziecko?", MissingFactType.Integer)
                : null,
            mass is null
                ? new MissingFact(FactKeys.DzieckoMasaUrodzeniowaG, "Ile gramów ważyło dziecko po urodzeniu (przy porodzie mnogim — najlżejsze)?", MissingFactType.Integer)
                : null,
        };
        if (Step.MissingIf<UzupelniajacyCase>(this, [.. missing]) is { } missingStep)
        {
            return missingStep;
        }

        if (weeks >= weekThresholdFullTerm)
        {
            var max = i.ParamInt(PeriodsParameterKeys.UzupelniajacyMaxTygDonoszony);
            var from = i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyLiczenieOdDnia);
            return Step.Met(this, new UzupelniajacyCase("donoszony", max, from, true),
                $"Poród po ukończeniu {weekThresholdFullTerm}. tygodnia ciąży: tydzień urlopu za każdy tydzień pobytu w szpitalu od {from}. dnia do upływu {max}. tygodnia po porodzie, jeśli pobyt trwał co najmniej {i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyMinKolejneDni)} kolejne dni, a pierwszy z nich przypadał między {i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienOd)}. a {i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienDo)}. dniem po porodzie.",
                $"poród po ukończeniu {weekThresholdFullTerm}. tyg. ciąży");
        }

        var midMax = i.ParamInt(PeriodsParameterKeys.UzupelniajacyMaxTygPrzedwczesny);
        return Step.Met(this, new UzupelniajacyCase("przedwczesny", midMax, 0, false),
            $"Poród po ukończeniu {weekThresholdEarly}. i przed ukończeniem {weekThresholdFullTerm}. tygodnia ciąży, masa powyżej {massThreshold} g: tydzień urlopu za każdy tydzień pobytu w szpitalu, do upływu {midMax}. tygodnia po porodzie.",
            $"poród między {weekThresholdEarly}. a {weekThresholdFullTerm}. tyg. ciąży, masa > {massThreshold} g");
    }
}

public sealed class UzupelniajacyWymiarRule : PeriodRule<int>
{
    public override string Id => "uzupelniajacy_wymiar";

    public override string PodstawaPrawna => "KP art. 180² § 1–3";

    protected override RuleStep<int> Compute(PeriodsInputs i)
    {
        var caseStep = i.UzupelniajacyPrzypadek;
        if (!caseStep.CanProceed)
        {
            return Step.Blocked<int>(this, caseStep);
        }

        if (Step.MissingIf<int>(this, i.NeedEventDate()) is { } missing)
        {
            return missing;
        }

        var rule = caseStep.Value!;
        var birth = i.EventDate!.Value;
        var days = i.HospitalDays!;

        if (rule.RequiresConsecutiveStay)
        {
            var needed = i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyMinKolejneDni);
            var earliest = birth.AddDays(i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienOd));
            var latest = birth.AddDays(i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienDo));
            var qualifies = days.Any(first => first >= earliest && first <= latest
                && Enumerable.Range(1, needed - 1).All(offset => days.Contains(first.AddDays(offset))));
            if (!qualifies)
            {
                var fullTerm = i.ParamInt(PeriodsParameterKeys.UzupelniajacyProgTydzienDonoszony);
                var firstFrom = i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienOd);
                var firstTo = i.ParamInt(PeriodsParameterKeys.UzupelniajacyDonoszonyPierwszyDzienDo);
                return Step.NotMet<int>(this,
                    $"Przy porodzie po ukończeniu {fullTerm}. tygodnia ciąży uzupełniający urlop przysługuje tylko wtedy, gdy dziecko przebywało w szpitalu co najmniej {needed} kolejne dni, a pierwszy z nich wypadł między {PeriodMath.Format(earliest)} a {PeriodMath.Format(latest)}.",
                    $"pobyt co najmniej {needed} kolejne dni, pierwszy między {firstFrom}. a {firstTo}. dniem po porodzie");
            }
        }

        var windowStart = birth.AddDays(rule.CountFromDay);
        var windowEnd = birth.AddDays(rule.MaxWeeks * PeriodMath.DaysPerWeek - 1);
        var counted = days.Count(d => d >= windowStart && d <= windowEnd);
        if (counted == 0)
        {
            return Step.NotMet<int>(this,
                $"Pobyt dziecka w szpitalu nie mieści się w okresie liczonym do uzupełniającego urlopu ({PeriodMath.Format(windowStart)}–{PeriodMath.Format(windowEnd)}).",
                $"pobyt w szpitalu w okresie do upływu {rule.MaxWeeks}. tygodnia po porodzie");
        }

        var weeks = Math.Min(PeriodMath.CeilWeeks(counted), rule.MaxWeeks);
        return Step.Met(this, weeks,
            $"Dni pobytu dziecka w szpitalu w okresie {PeriodMath.Format(windowStart)}–{PeriodMath.Format(windowEnd)}: {counted}. Dzielone przez 7 i zaokrąglone w górę to {PeriodMath.CeilWeeks(counted)} tyg.; limit {rule.MaxWeeks} tyg. Uzupełniający urlop: {weeks} tyg.",
            "tydzień urlopu za każdy tydzień pobytu, niepełny tydzień w górę");
    }
}

public sealed class UzupelniajacyOkresRule : PeriodRule<Period>
{
    public override string Id => "uzupelniajacy_okres";

    public override string PodstawaPrawna => "KP art. 180² § 1 (bezpośrednio po urlopie macierzyńskim)";

    protected override RuleStep<Period> Compute(PeriodsInputs i)
    {
        var weeks = i.UzupelniajacyWymiar;
        if (!weeks.CanProceed)
        {
            return Step.Blocked<Period>(this, weeks);
        }

        var block = i.MacierzynskiBlok;
        if (!block.CanProceed)
        {
            return Step.Blocked<Period>(this, block);
        }

        var period = PeriodMath.ForWeeks(block.Value!.To.AddDays(1), weeks.Value);
        return Step.Met(this, period,
            $"Uzupełniający urlop macierzyński przypada bezpośrednio po urlopie macierzyńskim (przed rodzicielskim): {PeriodMath.Describe(period)}.",
            "uzupełniający urlop bezpośrednio po macierzyńskim");
    }
}
