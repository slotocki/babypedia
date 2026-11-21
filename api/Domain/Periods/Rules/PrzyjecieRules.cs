using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Periods;

public sealed class PrzyjecieSciezkaRule : PeriodRule<AdoptionPath>
{
    public override string Id => "przyjecie_sciezka";

    public override string PodstawaPrawna => "KP art. 183 § 1, § 1¹; ustawa zasiłkowa art. 29 ust. 1 pkt 2–3";

    protected override RuleStep<AdoptionPath> Compute(PeriodsInputs i)
    {
        var path = i.Case.PrzyjecieSciezka?.Value;
        if (path is null)
        {
            return Step.Missing<AdoptionPath>(this,
                new MissingFact(FactKeys.PrzyjecieSciezka, "Na jakiej podstawie przyjęto dziecko na wychowanie?", MissingFactType.Enum,
                    Options: ["przysposobienie", "rodzina_zastepcza_niezawodowa", "rodzina_zastepcza_zawodowa"]));
        }

        switch (path)
        {
            case AdoptionPath.RodzinaZastepczaZawodowa:
                return Step.NotMet<AdoptionPath>(this,
                    "Rodzinie zastępczej zawodowej nie przysługuje urlop na warunkach urlopu macierzyńskiego ani zasiłek za ten okres.",
                    "rodzina zastępcza inna niż zawodowa");
            case AdoptionPath.Przysposobienie:
            {
                var motion = i.Case.PrzyjecieWniosekDoSadu?.Value;
                if (motion is null)
                {
                    return Step.Missing<AdoptionPath>(this,
                        new MissingFact(FactKeys.PrzyjecieWniosekDoSadu, "Czy wystąpiono do sądu opiekuńczego z wnioskiem o przysposobienie dziecka?", MissingFactType.Boolean));
                }

                return motion.Value
                    ? Step.Met(this, path.Value, "Dziecko przyjęto na wychowanie i wystąpiono do sądu opiekuńczego o przysposobienie.", "przyjęcie dziecka i wniosek do sądu opiekuńczego")
                    : Step.NotMet<AdoptionPath>(this,
                        "Urlop przy przysposobieniu przysługuje po wystąpieniu do sądu opiekuńczego z wnioskiem o przysposobienie dziecka.",
                        "przyjęcie dziecka i wniosek do sądu opiekuńczego");
            }

            default:
                return Step.Met(this, path.Value, "Dziecko przyjęto na wychowanie jako rodzina zastępcza niezawodowa.", "rodzina zastępcza inna niż zawodowa");
        }
    }
}

public sealed class PrzyjecieWiekRule : PeriodRule<AgeLimit>
{
    public override string Id => "przyjecie_wiek_dziecka";

    public override string PodstawaPrawna => "KP art. 183 § 1, § 1¹, § 3; ustawa zasiłkowa art. 29 ust. 1 pkt 2–3";

    protected override RuleStep<AgeLimit> Compute(PeriodsInputs i)
    {
        var path = i.PrzyjecieSciezka;
        if (!path.CanProceed)
        {
            return Step.Blocked<AgeLimit>(this, path);
        }

        var birth = i.Case.DzieckoDataUrodzenia?.Value;
        if (Step.MissingIf<AgeLimit>(this, i.NeedEventDate(),
                birth is null ? new MissingFact(FactKeys.DzieckoDataUrodzenia, "Jaka jest data urodzenia przyjmowanego dziecka?", MissingFactType.Date) : null) is { } missing)
        {
            return missing;
        }

        var born = birth!.Value;
        var taken = i.EventDate!.Value;
        if (taken < born)
        {
            return Step.Undecided<AgeLimit>(this, "Data przyjęcia dziecka jest wcześniejsza niż data jego urodzenia — nie rozstrzygam.");
        }

        var age = PeriodMath.FullYears(born, taken);
        if (path.Value == AdoptionPath.Przysposobienie)
        {
            var limit = i.ParamInt(PeriodsParameterKeys.WiekPrzysposobienieMax);
            return taken < born.AddYears(limit)
                ? Step.Met(this, new AgeLimit(PeriodMath.LastDayBeforeBirthday(born, limit), false),
                    $"W dniu przyjęcia dziecko miało {age} lat — przysposobienie dotyczy dzieci do ukończenia {limit}. roku życia.",
                    $"dziecko do {limit}. roku życia")
                : Step.NotMet<AgeLimit>(this,
                    $"W dniu przyjęcia dziecko miało {age} lat. Urlop przy przysposobieniu przysługuje, gdy dziecko nie ukończyło {limit}. roku życia.",
                    $"dziecko do {limit}. roku życia");
        }

        var standard = i.ParamInt(PeriodsParameterKeys.WiekRodzinaZastepczaMax);
        var deferred = i.ParamInt(PeriodsParameterKeys.WiekRodzinaZastepczaMaxOdroczenie);
        var deferral = i.Case.PrzyjecieOdroczenieObowiazkuSzkolnego?.Value;
        var condition = $"dziecko do {standard}. roku życia, a przy odroczeniu obowiązku szkolnego do {deferred}. roku życia";

        if (taken >= born.AddYears(deferred))
        {
            return Step.NotMet<AgeLimit>(this,
                $"W dniu przyjęcia dziecko miało {age} lat — to więcej niż granica {deferred} lat nawet przy odroczeniu obowiązku szkolnego.", condition);
        }

        if (taken >= born.AddYears(standard))
        {
            if (deferral is null)
            {
                return Step.Missing<AgeLimit>(this,
                    new MissingFact(FactKeys.PrzyjecieOdroczenieObowiazkuSzkolnego, "Czy wobec dziecka podjęto decyzję o odroczeniu obowiązku szkolnego?", MissingFactType.Boolean));
            }

            return deferral.Value
                ? Step.Met(this, new AgeLimit(PeriodMath.LastDayBeforeBirthday(born, deferred), false),
                    $"W dniu przyjęcia dziecko miało {age} lat i podjęto decyzję o odroczeniu obowiązku szkolnego — granica wieku to {deferred} lat.", condition)
                : Step.NotMet<AgeLimit>(this,
                    $"W dniu przyjęcia dziecko miało {age} lat, a bez odroczenia obowiązku szkolnego granica wieku to {standard} lat.", condition);
        }

        return deferral == true
            ? Step.Met(this, new AgeLimit(PeriodMath.LastDayBeforeBirthday(born, deferred), false),
                $"W dniu przyjęcia dziecko miało {age} lat; przy odroczeniu obowiązku szkolnego granica wieku to {deferred} lat.", condition)
            : Step.Met(this, new AgeLimit(PeriodMath.LastDayBeforeBirthday(born, standard), deferral is null),
                $"W dniu przyjęcia dziecko miało {age} lat — mieści się w granicy {standard} lat.", condition);
    }
}

public sealed class PrzyjecieOkresRule : PeriodRule<AdoptionLeave>
{
    public override string Id => "przyjecie_okres";

    public override string PodstawaPrawna => "KP art. 183 § 1, § 1¹, § 3, § 6; art. 183¹ § 1";

    protected override RuleStep<AdoptionLeave> Compute(PeriodsInputs i)
    {
        var path = i.PrzyjecieSciezka;
        if (!path.CanProceed)
        {
            return Step.Blocked<AdoptionLeave>(this, path);
        }

        var age = i.PrzyjecieWiek;
        if (!age.CanProceed)
        {
            return Step.Blocked<AdoptionLeave>(this, age);
        }

        if (Step.MissingIf<AdoptionLeave>(this, i.NeedChildCount()) is { } missing)
        {
            return missing;
        }

        var count = i.ChildCount!.Value;
        if (count < 1)
        {
            return Step.Undecided<AdoptionLeave>(this, "Liczba przyjmowanych dzieci musi wynosić co najmniej 1.");
        }

        var weeks = i.ParamInt(PeriodsParameterKeys.MacierzynskiTyg(count));
        var start = i.EventDate!.Value;
        var full = PeriodMath.ForWeeks(start, weeks);
        var lastDay = age.Value!.LastDay;

        if (age.Value.Provisional && full.To > lastDay)
        {
            return Step.Missing<AdoptionLeave>(this,
                new MissingFact(FactKeys.PrzyjecieOdroczenieObowiazkuSzkolnego, "Czy wobec dziecka podjęto decyzję o odroczeniu obowiązku szkolnego?", MissingFactType.Boolean));
        }

        const string note = " Początek urlopu przyjmuję w dniu przyjęcia dziecka (we wniosku można wskazać późniejszy początek, w ustawowym terminie po przyjęciu).";
        if (full.To <= lastDay)
        {
            return Step.Met(this, new AdoptionLeave(full, AdoptionLeaveCase.Full, lastDay),
                $"Urlop na warunkach urlopu macierzyńskiego przy {count} dz.: {weeks} tyg., {PeriodMath.Describe(full)}.{note}",
                "wymiar jak urlop macierzyński, nie dłużej niż do granicy wieku dziecka");
        }

        var minimumDays = i.ParamInt(PeriodsParameterKeys.PrzyjecieMinTyg) * PeriodMath.DaysPerWeek;
        var remaining = PeriodMath.DaysInclusive(start, lastDay);
        if (remaining < minimumDays)
        {
            var minimum = PeriodMath.ForDays(start, minimumDays);
            return Step.Met(this, new AdoptionLeave(minimum, AdoptionLeaveCase.Minimum, lastDay),
                $"Do granicy wieku dziecka ({PeriodMath.Format(lastDay)}) zostaje {remaining} dni, mniej niż minimum {minimumDays / PeriodMath.DaysPerWeek} tyg., więc urlop trwa minimum: {PeriodMath.Describe(minimum)}.{note}",
                "nie krócej niż minimum, nawet gdy granica wieku wypada wcześniej");
        }

        var truncated = PeriodMath.ForDays(start, remaining);
        return Step.Met(this, new AdoptionLeave(truncated, AdoptionLeaveCase.Truncated, lastDay),
            $"Urlop skrócono do granicy wieku dziecka ({PeriodMath.Format(lastDay)}): {PeriodMath.Describe(truncated)}.{note}",
            "wymiar jak urlop macierzyński, nie dłużej niż do granicy wieku dziecka");
    }
}

public sealed class PrzyjecieRodzicielskiWymiarRule : PeriodRule<int>
{
    public override string Id => "przyjecie_rodzicielski_wymiar";

    public override string PodstawaPrawna => "KP art. 183 § 4, § 4¹–4⁶";

    protected override RuleStep<int> Compute(PeriodsInputs i)
    {
        var leave = i.PrzyjecieOkres;
        if (!leave.CanProceed)
        {
            return Step.Blocked<int>(this, leave);
        }

        var count = i.ChildCount!.Value;
        switch (leave.Value!.Case)
        {
            case AdoptionLeaveCase.Full:
            {
                var weeks = i.ParamInt(PeriodsParameterKeys.RodzicielskiTyg(count, i.ZaZyciem));
                return Step.Met(this, weeks,
                    $"Urlop rodzicielski po przyjęciu dziecka (liczba dzieci: {count}{(i.ZaZyciem ? ", zaświadczenie „Za życiem”" : "")}): do {weeks} tyg.",
                    "wymiar jak przy urodzeniu dziecka");
            }

            case AdoptionLeaveCase.Minimum:
            {
                var weeks = i.ParamInt(i.ZaZyciem
                    ? PeriodsParameterKeys.PrzyjecieRodzicielskiPrzyMinimumZaZyciem
                    : PeriodsParameterKeys.PrzyjecieRodzicielskiPrzyMinimum);
                return Step.Met(this, weeks,
                    $"Urlop na warunkach urlopu macierzyńskiego trwa minimum ({i.ParamInt(PeriodsParameterKeys.PrzyjecieMinTyg)} tyg.), więc urlop rodzicielski to {weeks} tyg.{(i.ZaZyciem ? " (zaświadczenie „Za życiem”)" : "")}.",
                    "urlop w wymiarze minimalnym → rodzicielski w wymiarze zmniejszonym");
            }

            default:
                return Step.Undecided<int>(this,
                    "Urlop skrócono do granicy wieku dziecka, ale jest dłuższy niż minimum. Przepis nie wskazuje wprost wymiaru urlopu rodzicielskiego w takim przypadku — nie rozstrzygam. Skontaktuj się z ZUS.");
        }
    }
}
