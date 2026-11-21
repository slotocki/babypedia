using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using BabyRadnik.Domain.Periods;

namespace BabyRadnik.Domain.Catalog;

public sealed class CompletedDeadlines(IEnumerable<string>? entries)
{
    private readonly HashSet<string> entries = [.. entries ?? []];

    public static string PersonName(PersonRef person) => person == PersonRef.Wnioskodawca ? "wnioskodawca" : "drugiRodzic";

    /// <summary>Wpis „id” oznacza termin wykonany dla obu osób; „wnioskodawca/id” albo „drugiRodzic/id” — tylko dla wskazanej osoby.</summary>
    public bool Contains(PersonRef person, string id) => entries.Contains(id) || entries.Contains($"{PersonName(person)}/{id}");
}

/// <summary>Tryb trudnej sytuacji (sekcja 5): martwe urodzenie, zgon dziecka, wcześniactwo z hospitalizacją, ciężka choroba („Za życiem”), sytuacja szczególna matki.</summary>
public static class DifficultSituation
{
    public static bool Detect(FactSet facts, DateOnly asOf, IParameterProvider parameters)
    {
        var child = facts.Sprawa;
        if (child.DzieckoMartweUrodzenie?.Value == true
            || child.DzieckoDataZgonu is not null
            || child.DzieckoLiczbaZmarlych?.Value > 0
            || child.DzieckoZaZyciem?.Value == true
            || child.MatkaSytuacjaSzczegolna is not null)
        {
            return true;
        }

        return child.DzieckoPobytyWSzpitalu?.Value is { Count: > 0 } && Premature(child, child.ZdarzenieData?.Value ?? asOf, parameters);
    }

    private static bool Premature(CaseFacts child, DateOnly date, IParameterProvider parameters)
    {
        return Below(child.DzieckoTydzienCiazyPrzyPorodzie?.Value, PeriodsParameterKeys.UzupelniajacyProgTydzienDonoszony, false)
            || Below(child.DzieckoMasaUrodzeniowaG?.Value, PeriodsParameterKeys.UzupelniajacyProgMasaG, true);

        bool Below(int? value, string key, bool inclusive)
        {
            if (value is not { } v)
            {
                return false;
            }

            try
            {
                var limit = parameters.GetDecimal(key, date);
                return inclusive ? v <= limit : v < limit;
            }
            catch (ParameterNotFoundException)
            {
                return false;
            }
        }
    }
}

/// <summary>Pytania wynikające z tabeli 6.2 („kiedy pytać”), których nie zgłaszają moduły uprawnień i okresów.</summary>
public static class CatalogMissingFacts
{
    public static IReadOnlyList<MissingFact> For(FactSet facts, IReadOnlyList<PersonRef> persons, DateOnly asOf, IParameterProvider? parameters = null)
    {
        var missing = new List<MissingFact>();
        if (facts.Sprawa.ZdarzenieDlaKogo is null)
        {
            missing.Add(FactQuestionBook.Ask(FactKeys.ZdarzenieDlaKogo));
        }

        if (parameters is not null)
        {
            missing.AddRange(AveragedApplicationDate(facts, persons, asOf, parameters));
        }

        if (facts.Sprawa.ZdarzenieRodzaj?.Value is not (EventKind.Planuje or EventKind.Ciaza))
        {
            return missing;
        }

        var date = facts.Sprawa.ZdarzenieData?.Value;
        foreach (var person in persons)
        {
            var personFacts = facts.For(person);
            var active = InsuranceFacts.SicknessTitlesOn(personFacts, date ?? asOf);
            if (personFacts is null || active is not [{ RodzajTytulu: TitleKind.Pracownik or TitleKind.Zleceniobiorca }])
            {
                continue;
            }

            if (personFacts.UmowaNaCzasOkreslony is null)
            {
                missing.Add(FactQuestionBook.Ask(FactKeys.UmowaNaCzasOkreslony, person));
            }
            else if (personFacts.UmowaNaCzasOkreslony.Value && personFacts.UmowaDataKonca is null)
            {
                missing.Add(FactQuestionBook.Ask(FactKeys.UmowaDataKonca, person));
            }
        }

        return missing;
    }

    /// <summary>
    /// Wariant uśredniony zadeklarowany, termin wniosku (art. 30a) minął, a brak daty wniosku: silnik kwot liczy wtedy
    /// stawki standardowe (reguły nie zmieniamy) — pytamy o datę wniosku, żeby wynik nie był cichym 70%.
    /// </summary>
    private static IEnumerable<MissingFact> AveragedApplicationDate(FactSet facts, IReadOnlyList<PersonRef> persons, DateOnly asOf, IParameterProvider parameters)
    {
        var kind = facts.Sprawa.ZdarzenieRodzaj?.Value;
        if (kind is not (EventKind.Urodzenie or EventKind.Przyjecie) || facts.Sprawa.ZdarzenieData?.Value is not { } eventDate)
        {
            yield break;
        }

        int days;
        try
        {
            days = parameters.GetInt(DeadlineParameterKeys.WniosekUsredniona, eventDate);
        }
        catch (ParameterNotFoundException)
        {
            yield break;
        }

        if (asOf <= eventDate.AddDays(days))
        {
            yield break;
        }

        foreach (var person in persons)
        {
            var personFacts = facts.For(person);
            if (personFacts?.PlanWariantWyplaty?.Value != PaymentVariant.Usredniony || personFacts.PlanDataWnioskuUsrednionego is not null)
            {
                continue;
            }

            if (kind == EventKind.Urodzenie && personFacts.OsobaRola?.Value != ParentRole.Matka)
            {
                continue;
            }

            yield return FactQuestionBook.Ask(FactKeys.PlanDataWnioskuUsrednionego, person);
        }
    }
}

public static class NextStepSelector
{
    public static NextStep? Select(IReadOnlyList<BenefitResult> benefits, DateOnly asOf, CompletedDeadlines completed) =>
        benefits
            .Where(b => b.Status != BenefitStatus.NieDotyczy)
            .SelectMany((b, order) => b.Deadlines
                .Where(d => d.Type is DeadlineType.NajpozniejDo or DeadlineType.NajpozniejPrzed
                    && d.Date >= asOf
                    && !completed.Contains(b.Person, d.Id))
                .Select(d => (Deadline: d, Benefit: b, Order: order)))
            .OrderBy(x => x.Deadline.Date)
            .ThenBy(x => x.Order)
            .Select(x => new NextStep(x.Deadline.Id, x.Benefit.Id, x.Benefit.Person))
            .FirstOrDefault();
}
