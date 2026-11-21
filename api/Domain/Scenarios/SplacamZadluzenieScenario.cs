using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Domain.Scenarios.FactOverlay;

namespace BabyRadnik.Domain.Scenarios;

public sealed class SplacamZadluzenieScenario : IScenario
{
    public ScenarioId Id => ScenarioId.SplacamZadluzenie;

    public string Description => "Spłacam zadłużenie z tytułu składek w podanym dniu.";

    public ScenarioOutcome Apply(ScenarioContext context)
    {
        var args = new ScenarioParams(context.Params, context.Person, "data");
        var date = args.Date("data");
        if (!args.Valid || date is null)
        {
            return ScenarioOutcome.Fail(args.Issues);
        }

        if (context.Facts.For(context.Person) is not { } person)
        {
            return ScenarioOutcome.Fail(NotApplicable($"Brak faktów o {PersonName(context.Person)}, więc scenariusz nie ma do czego się odnieść.", context.Person));
        }

        if (person.SkladkiZadluzenie?.Value is not { } debt)
        {
            return ScenarioOutcome.Fail(MissingFact("Scenariusz wymaga znanego salda zadłużenia składkowego — bez długu nie ma czego spłacać.", context.Person, FactKeys.SkladkiZadluzenie));
        }

        var facts = context.Facts.WithPerson(context.Person, p => p with { SkladkiDataSplaty = Assume(date.Value) });
        return ScenarioOutcome.Ok(facts, $"Całe zadłużenie ({debt.Kwota:0.00} zł na {Date(debt.NaDzien)}) zostaje spłacone {Date(date.Value)}.");
    }
}
