using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Periods;
using static BabyRadnik.Domain.Scenarios.FactOverlay;

namespace BabyRadnik.Domain.Scenarios;

public sealed class DrugiRodzicBierzeScenario : IScenario
{
    public ScenarioId Id => ScenarioId.DrugiRodzicBierze;

    public string Description => "Drugi rodzic bierze część urlopu rodzicielskiego: podana liczba tygodni dla drugiego rodzica, reszta wymiaru dla mnie.";

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

        var other = context.Person.Other();
        if (context.Facts.For(context.Person) is null || context.Facts.For(other) is null)
        {
            return ScenarioOutcome.Fail(NotApplicable("Podział urlopu rodzicielskiego wymaga faktów o obojgu rodzicach; dodaj drugiego rodzica.", context.Person));
        }

        var inputs = new PeriodsInputs(new EvaluationContext(context.Facts, context.Person, context.Today, context.Parameters));
        var total = inputs.RodzicielskiWymiar;
        if (!total.CanProceed)
        {
            var missing = total.Result.Missing?
                .Select(m => MissingFact($"Do podziału urlopu rodzicielskiego potrzebna jest informacja: {m.Question}", context.Person, m.Fact))
                .ToList() ?? [];
            return ScenarioOutcome.Fail(missing.Count > 0
                ? missing
                : [NotApplicable($"Nie da się ustalić wymiaru urlopu rodzicielskiego do podziału: {total.Result.Text}", context.Person)]);
        }

        var remainder = total.Value - weeks.Value;
        IReadOnlyList<LeavePart> ownParts = remainder > 0 ? [new LeavePart(remainder)] : [];
        IReadOnlyList<LeavePart> otherParts = weeks.Value > 0 ? [new LeavePart(weeks.Value)] : [];
        var updated = context.Facts
            .WithPerson(context.Person, p => p with { PlanRodzicielskiCzesci = Assume(ownParts) })
            .WithPerson(other, p => p with { PlanRodzicielskiCzesci = Assume(otherParts) });
        return ScenarioOutcome.Ok(
            updated,
            $"Urlop rodzicielski ({total.Value} tyg. łącznie): {PersonName(other)} — {weeks.Value} tyg. w jednej części.",
            $"Urlop rodzicielski: {PersonName(context.Person)} — {Math.Max(remainder, 0)} tyg. w jednej części (reszta wymiaru).");
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
        return ScenarioOutcome.Ok(
            facts,
            variant == PaymentVariant.Usredniony
                ? "Wariant wypłaty: uśredniony (81,5% za urlop macierzyński i rodzicielski)."
                : "Wariant wypłaty: standardowy (100% za urlop macierzyński, 70% za rodzicielski).");
    }
}
