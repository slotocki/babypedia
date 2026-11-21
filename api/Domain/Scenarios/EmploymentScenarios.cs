using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Domain.Scenarios.FactOverlay;

namespace BabyRadnik.Domain.Scenarios;

internal static class EmploymentSupport
{
    public static ScenarioOutcome? RequireTitles(ScenarioContext context, out IReadOnlyList<InsuranceTitle> titles)
    {
        titles = [];
        if (context.Facts.For(context.Person) is not { } person)
        {
            return ScenarioOutcome.Fail(NotApplicable($"Brak faktów o {PersonName(context.Person)}, więc scenariusz nie ma do czego się odnieść.", context.Person));
        }

        if (person.UbezpieczenieTytuly?.Value is not { } known)
        {
            return ScenarioOutcome.Fail(MissingFact("Scenariusz wymaga znanej historii tytułów ubezpieczenia.", context.Person, FactKeys.UbezpieczenieTytuly));
        }

        titles = known;
        return null;
    }

    public static ScenarioOutcome? SingleEmployeeTitle(ScenarioContext context, IReadOnlyList<InsuranceTitle> titles, DateOnly date, out InsuranceTitle title)
    {
        title = null!;
        var active = titles.Where(t => t.RodzajTytulu == TitleKind.Pracownik && ActiveOn(t, date)).ToList();
        if (active.Count == 0)
        {
            return ScenarioOutcome.Fail(NotApplicable(
                $"W dniu {Date(date)} nie ma tytułu pracownika, który mógłby się zakończyć.", context.Person, FactKeys.UbezpieczenieTytuly));
        }

        if (active.Count > 1)
        {
            return ScenarioOutcome.Fail(NotApplicable(
                $"W dniu {Date(date)} jest kilka tytułów pracownika naraz — nie wiadomo, który kończy się w scenariuszu.", context.Person, FactKeys.UbezpieczenieTytuly));
        }

        title = active[0];
        return null;
    }
}

public sealed class OdchodzeZPracyScenario : IScenario
{
    public ScenarioId Id => ScenarioId.OdchodzeZPracy;

    public string Description => "Odchodzę z pracy: ubezpieczenie z tytułu zatrudnienia kończy się w podanym dniu po moim wypowiedzeniu.";

    public ScenarioOutcome Apply(ScenarioContext context)
    {
        var args = new ScenarioParams(context.Params, context.Person, "data");
        var date = args.Date("data");
        if (!args.Valid || date is null)
        {
            return ScenarioOutcome.Fail(args.Issues);
        }

        if (EmploymentSupport.RequireTitles(context, out var titles) is { } missing)
        {
            return missing;
        }

        if (EmploymentSupport.SingleEmployeeTitle(context, titles, date.Value, out var title) is { } failed)
        {
            return failed;
        }

        var facts = context.Facts.WithPerson(context.Person, p => p with
        {
            UbezpieczenieTytuly = Assume(Replace(titles, title, title with { Do = date })),
            UbezpieczenieUstaniePrzyczyna = Assume(InsuranceEndReason.WypowiedzeniePrzezPracownika),
        });
        return ScenarioOutcome.Ok(
            facts,
            $"Ubezpieczenie z tytułu pracy (od {Date(title.Od)}) kończy się {Date(date.Value)}.",
            "Przyczyna ustania ubezpieczenia: wypowiedzenie przez pracownika.");
    }
}

public sealed class UmowaNieZostaniePrzedluzonaScenario : IScenario
{
    public ScenarioId Id => ScenarioId.UmowaNieZostaniePrzedluzona;

    public string Description => "Umowa na czas określony nie zostanie przedłużona: ubezpieczenie kończy się w dniu końca umowy.";

    public ScenarioOutcome Apply(ScenarioContext context)
    {
        var args = new ScenarioParams(context.Params, context.Person);
        if (!args.Valid)
        {
            return ScenarioOutcome.Fail(args.Issues);
        }

        if (EmploymentSupport.RequireTitles(context, out var titles) is { } missing)
        {
            return missing;
        }

        var person = context.Facts.For(context.Person)!;
        if (person.UmowaNaCzasOkreslony?.Value == false)
        {
            return ScenarioOutcome.Fail(NotApplicable("Umowa nie jest zawarta na czas określony, więc nie ma końca umowy do rozważenia.", context.Person, FactKeys.UmowaNaCzasOkreslony));
        }

        if (person.UmowaDataKonca?.Value is not { } end)
        {
            return ScenarioOutcome.Fail(MissingFact("Scenariusz wymaga daty końca umowy na czas określony.", context.Person, FactKeys.UmowaDataKonca));
        }

        if (EmploymentSupport.SingleEmployeeTitle(context, titles, end, out var title) is { } failed)
        {
            return failed;
        }

        var facts = context.Facts.WithPerson(context.Person, p => p with
        {
            UbezpieczenieTytuly = Assume(Replace(titles, title, title with { Do = end })),
            UbezpieczenieUstaniePrzyczyna = Assume(InsuranceEndReason.KoniecUmowyNaCzasOkreslony),
            UmowaNaCzasOkreslony = Assume(true),
        });
        return ScenarioOutcome.Ok(
            facts,
            $"Umowa na czas określony kończy się {Date(end)} i nie jest przedłużana; ubezpieczenie z tytułu pracy kończy się tego dnia.",
            "Przyczyna ustania ubezpieczenia: koniec umowy na czas określony.");
    }
}

public sealed class PrzechodzeNaDzialalnoscScenario : IScenario
{
    public ScenarioId Id => ScenarioId.PrzechodzeNaDzialalnosc;

    public string Description => "Przechodzę na działalność: tytuł pracownika kończy się dzień przed podaną datą, od tej daty jest działalność z dobrowolnym ubezpieczeniem chorobowym albo bez niego.";

    public ScenarioOutcome Apply(ScenarioContext context)
    {
        var args = new ScenarioParams(context.Params, context.Person, "data", "dobrowolneChorobowe");
        var date = args.Date("data");
        var voluntary = args.Bool("dobrowolneChorobowe");
        if (!args.Valid || date is null || voluntary is null)
        {
            return ScenarioOutcome.Fail(args.Issues);
        }

        if (EmploymentSupport.RequireTitles(context, out var titles) is { } missing)
        {
            return missing;
        }

        if (EmploymentSupport.SingleEmployeeTitle(context, titles, date.Value, out var title) is { } failed)
        {
            return failed;
        }

        if (title.Od >= date)
        {
            return ScenarioOutcome.Fail(NotApplicable(
                $"Działalność ma się zacząć {Date(date.Value)}, a zatrudnienie zaczęło się {Date(title.Od)} — zatrudnienie musi trwać co najmniej dzień przed zmianą.",
                context.Person,
                FactKeys.UbezpieczenieTytuly));
        }

        var sickness = voluntary.Value ? SicknessInsurance.Dobrowolne : SicknessInsurance.Brak;
        var business = new InsuranceTitle(TitleKind.Dzialalnosc, date.Value, null, sickness);
        var updated = Replace(titles, title, title with { Do = date.Value.AddDays(-1) }).Append(business).ToList();
        var facts = context.Facts.WithPerson(context.Person, p => p with
        {
            UbezpieczenieTytuly = Assume<IReadOnlyList<InsuranceTitle>>(updated),
            UbezpieczenieUstaniePrzyczyna = Assume(InsuranceEndReason.Inna),
        });
        return ScenarioOutcome.Ok(
            facts,
            $"Zatrudnienie (od {Date(title.Od)}) kończy się {Date(date.Value.AddDays(-1))}.",
            $"Od {Date(date.Value)} działalność, ubezpieczenie chorobowe: {(voluntary.Value ? "dobrowolne" : "brak")}.",
            "Przyczyna ustania ubezpieczenia z zatrudnienia: inna (zmiana tytułu, bez wyjątku z art. 30 ustawy).");
    }
}
