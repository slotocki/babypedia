using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Periods;
using static BabyRadnik.Domain.Scenarios.FactOverlay;

namespace BabyRadnik.Domain.Scenarios;

public sealed class DrugiRodzicBierzeScenario : IScenario
{
    public ScenarioId Id => ScenarioId.DrugiRodzicBierze;

    /// <remarks>
    /// <c>person</c> = osoba, która bierze podaną liczbę tygodni (zwykle <c>drugiRodzic</c>); druga osoba dostaje resztę wymiaru.
    /// Limit „wymiar − 9 tyg. dla jednego rodzica” (KP art. 182¹a) sprawdza moduł okresów osobno dla każdego rodzica.
    /// </remarks>
    public string Description => "Podział urlopu rodzicielskiego: wskazana osoba (person, zwykle drugi rodzic) bierze podaną liczbę tygodni, druga osoba — resztę wymiaru.";

    public ScenarioOutcome Apply(ScenarioContext context)
    {
        var args = new ScenarioParams(context.Params, context.Person, "tygodnie");
        var weeks = args.Integer("tygodnie");
        var errors = args.Issues.ToList();
        if (weeks is < 0)
        {
            errors.Add(new ValidationIssue("scenariusz_zly_parametr", "Parametr „tygodnie” nie może być ujemny.", null, context.Person));
        }

        if (errors.Count > 0 || weeks is null)
        {
            return ScenarioOutcome.Fail(errors);
        }

        var taker = context.Person;
        var other = taker.Other();
        if (context.Facts.For(taker) is null || context.Facts.For(other) is null)
        {
            return ScenarioOutcome.Fail(NotApplicable("Podział urlopu rodzicielskiego wymaga faktów o obojgu rodzicach; dodaj drugiego rodzica.", taker));
        }

        var inputs = new PeriodsInputs(new EvaluationContext(context.Facts, taker, context.Today, context.Parameters));
        var total = inputs.RodzicielskiWymiar;
        if (!total.CanProceed)
        {
            var missing = total.Result.Missing?
                .Select(m => MissingFact($"Do podziału urlopu rodzicielskiego potrzebna jest informacja: {m.Question}", taker, m.Fact))
                .ToList() ?? [];
            return ScenarioOutcome.Fail(missing.Count > 0
                ? missing
                : [NotApplicable($"Nie da się ustalić wymiaru urlopu rodzicielskiego do podziału: {total.Result.Text}", taker)]);
        }

        var weeksFor = new Dictionary<PersonRef, int>
        {
            [taker] = weeks.Value,
            [other] = Math.Max(total.Value - weeks.Value, 0),
        };
        var starts = SequentialStarts(context, weeksFor);
        var updated = context.Facts;
        foreach (var (person, count) in weeksFor)
        {
            IReadOnlyList<LeavePart> parts = count > 0 ? [new LeavePart(count, starts?.StartOf(person))] : [];
            updated = updated.WithPerson(person, p => p with { PlanRodzicielskiCzesci = Assume(parts) });
        }

        return ScenarioOutcome.Ok(
            updated,
            $"Urlop rodzicielski ({total.Value} tyg. łącznie): część {PersonName(taker)} — {weeksFor[taker]} tyg. w jednej części{StartText(starts, taker, weeksFor)}.",
            $"Urlop rodzicielski: część {PersonName(other)} — {weeksFor[other]} tyg. w jednej części (reszta wymiaru){StartText(starts, other, weeksFor)}.");
    }

    private sealed record LeaveStarts(PersonRef Mother, DateOnly MotherStart, DateOnly? FatherStart)
    {
        public DateOnly? StartOf(PersonRef person) => person == Mother ? MotherStart : FatherStart;
    }

    /// <summary>
    /// Początki części, gdy da się je ustalić bez zgadywania: część matki zaczyna się bezpośrednio po urlopie macierzyńskim
    /// (KP art. 180² § 1 — tak liczy moduł okresów). Część ojca to jawne założenie scenariusza: bezpośrednio po części matki
    /// (rodzice mogą też korzystać z urlopu jednocześnie — KP art. 182¹a § 6 — albo w innym terminie). Bez znanego końca
    /// urlopu macierzyńskiego dat nie ustalamy i moduł okresów dalej pyta o plan.
    /// </summary>
    private static LeaveStarts? SequentialStarts(ScenarioContext context, IReadOnlyDictionary<PersonRef, int> weeksFor)
    {
        var mother = weeksFor.Keys.Where(p => context.Facts.For(p)?.OsobaRola?.Value == ParentRole.Matka).ToList();
        if (mother.Count != 1)
        {
            return null;
        }

        var anchor = new PeriodsInputs(new EvaluationContext(context.Facts, mother[0], context.Today, context.Parameters)).RodzicielskiPoczatek;
        if (!anchor.CanProceed || anchor.Value is not { } maternityEnd)
        {
            return null;
        }

        var motherStart = maternityEnd.AddDays(1);
        DateOnly? fatherStart = context.Facts.For(mother[0].Other())?.OsobaRola?.Value == ParentRole.Ojciec
            ? motherStart.AddDays(weeksFor[mother[0]] * PeriodMath.DaysPerWeek)
            : null;
        return new LeaveStarts(mother[0], motherStart, fatherStart);
    }

    private static string StartText(LeaveStarts? starts, PersonRef person, IReadOnlyDictionary<PersonRef, int> weeksFor)
    {
        if (starts?.StartOf(person) is not { } start || weeksFor[person] == 0)
        {
            return "";
        }

        if (person == starts.Mother)
        {
            return $", od {Date(start)} — bezpośrednio po urlopie macierzyńskim";
        }

        return weeksFor[starts.Mother] > 0
            ? $", od {Date(start)} — założenie: bezpośrednio po części matki (rodzice mogą też korzystać z urlopu jednocześnie albo w innym terminie)"
            : $", od {Date(start)} — założenie: bezpośrednio po urlopie macierzyńskim matki (można wybrać inny termin)";
    }
}

public sealed class PracaNaCzescEtatuScenario : IScenario
{
    public ScenarioId Id => ScenarioId.PracaNaCzescEtatu;

    public string Description => "Praca na część etatu w czasie urlopu rodzicielskiego: podany wymiar etatu przez podaną liczbę tygodni.";

    public ScenarioOutcome Apply(ScenarioContext context)
    {
        var args = new ScenarioParams(context.Params, context.Person, "wymiar", "tygodnie");
        var size = args.Number("wymiar");
        var weeks = args.Integer("tygodnie");
        var errors = args.Issues.ToList();
        if (weeks is <= 0)
        {
            errors.Add(new ValidationIssue("scenariusz_zly_parametr", "Parametr „tygodnie” musi być dodatni.", null, context.Person));
        }

        if (size is <= 0)
        {
            errors.Add(new ValidationIssue("scenariusz_zly_parametr", "Parametr „wymiar” musi być większy od zera (np. 0,5 = pół etatu).", null, context.Person));
        }

        if (size is > 0)
        {
            var date = context.Facts.Sprawa.ZdarzenieData?.Value ?? context.Today;
            try
            {
                var max = context.Parameters.GetDecimal(PeriodsParameterKeys.PracaNaRodzicielskimMaxEtat, date);
                if (size > max)
                {
                    errors.Add(new ValidationIssue(
                        "praca_na_rodzicielskim_wymiar",
                        $"Z urlopem rodzicielskim można łączyć pracę w wymiarze najwyżej {max:0.##} etatu (KP art. 182¹e).",
                        FactKeys.PlanPracaNaCzescEtatu,
                        context.Person));
                }
            }
            catch (ParameterNotFoundException e)
            {
                errors.Add(new ValidationIssue("scenariusz_brak_parametru_prawnego", $"Brakuje parametru prawnego „{e.Key}” na dzień {Date(e.Date)}.", null, context.Person));
            }
        }

        if (errors.Count == 0 && context.Facts.For(context.Person) is null)
        {
            errors.Add(NotApplicable($"Brak faktów o {PersonName(context.Person)}, więc scenariusz nie ma do czego się odnieść.", context.Person));
        }

        if (errors.Count > 0 || size is null || weeks is null)
        {
            return ScenarioOutcome.Fail(errors);
        }

        IReadOnlyList<PartTimeWork> work = [new PartTimeWork(size.Value, weeks.Value)];
        var facts = context.Facts.WithPerson(context.Person, p => p with { PlanPracaNaCzescEtatu = Assume(work) });
        return ScenarioOutcome.Ok(facts, $"Praca w wymiarze {size.Value:0.##} etatu przez {weeks.Value} tyg. w czasie urlopu rodzicielskiego.");
    }
}

public sealed class WariantWyplatyScenario : IScenario
{
    public ScenarioId Id => ScenarioId.WariantWyplaty;

    public string Description => "Wariant wypłaty: standardowy albo uśredniony (zasiłek rodzicielski 81,5% przez cały okres).";

    public ScenarioOutcome Apply(ScenarioContext context)
    {
        var args = new ScenarioParams(context.Params, context.Person, "wariant");
        var text = args.Text("wariant", "standardowy", "usredniony");
        if (!args.Valid || text is null)
        {
            return ScenarioOutcome.Fail(args.Issues);
        }

        if (context.Facts.For(context.Person) is null)
        {
            return ScenarioOutcome.Fail(NotApplicable($"Brak faktów o {PersonName(context.Person)}, więc scenariusz nie ma do czego się odnieść.", context.Person));
        }

        var variant = text == "usredniony" ? PaymentVariant.Usredniony : PaymentVariant.Standardowy;
        var facts = context.Facts.WithPerson(context.Person, p => p with { PlanWariantWyplaty = Assume(variant) });
        var changes = new List<string>
        {
            variant == PaymentVariant.Usredniony
                ? "Wariant wypłaty: uśredniony (81,5% za urlop macierzyński i rodzicielski)."
                : "Wariant wypłaty: standardowy (100% za urlop macierzyński, 70% za rodzicielski).",
        };
        if (variant == PaymentVariant.Usredniony && LateWithoutDate(context) is { } deadline)
        {
            changes.Add($"Termin wniosku o wariant uśredniony minął {Date(deadline)}, a nie podano daty wniosku złożonego w terminie — wariant liczy stawki standardowe. Jeśli wniosek złożono w terminie, podaj jego datę na ekranie wyniku.");
        }

        return new ScenarioOutcome(facts, [], changes);
    }

    private static DateOnly? LateWithoutDate(ScenarioContext context)
    {
        var sprawa = context.Facts.Sprawa;
        if (sprawa.ZdarzenieRodzaj?.Value is not (EventKind.Urodzenie or EventKind.Przyjecie)
            || sprawa.ZdarzenieData?.Value is not { } eventDate
            || context.Facts.For(context.Person)?.PlanDataWnioskuUsrednionego is not null)
        {
            return null;
        }

        try
        {
            var deadline = eventDate.AddDays(context.Parameters.GetInt("termin_wniosku_usredniona_dni", eventDate));
            return context.Today > deadline ? deadline : null;
        }
        catch (ParameterNotFoundException)
        {
            return null;
        }
    }
}
