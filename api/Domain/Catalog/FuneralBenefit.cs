using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;

namespace BabyRadnik.Domain.Catalog;

public sealed record FuneralEvaluation(BenefitResult Result, IReadOnlyList<MissingFact> Missing);

/// <summary>Zasiłek pogrzebowy: tylko informacja i kwota z parametru; bez wyliczeń. Dotyczy faktów wskazujących martwe urodzenie lub zgon dziecka.</summary>
public sealed class FuneralRule : IRule
{
    public const string RuleId = "pogrzebowy_martwe_urodzenie_lub_zgon";

    public string Id => RuleId;

    public string PodstawaPrawna => "Poradnik ZUS 2026 s. 35–36";

    public RuleResult Evaluate(EvaluationContext context)
    {
        var child = context.Facts.Sprawa;
        if (child.DzieckoMartweUrodzenie?.Value == true)
        {
            return RuleResult.Met("Dziecko urodziło się martwe — przysługuje zasiłek pogrzebowy w stałej kwocie, bez względu na czas trwania ciąży.");
        }

        return child.DzieckoDataZgonu is not null
            ? RuleResult.Met("Dziecko zmarło — możesz ubiegać się o zasiłek pogrzebowy w stałej kwocie.")
            : RuleResult.NotMet("Fakty nie wskazują na martwe urodzenie ani zgon dziecka.");
    }

    public FuneralEvaluation Assess(FactSet facts, PersonRef person, DateOnly asOf, IParameterProvider parameters, IDeadlineService deadlines)
    {
        var entry = BenefitCatalog.Get(BenefitId.Pogrzebowy);
        if (person != PersonRef.Wnioskodawca)
        {
            return new FuneralEvaluation(new BenefitResult { Id = entry.Id, Person = person, Status = BenefitStatus.NieDotyczy }, []);
        }

        var context = new EvaluationContext(facts, person, asOf, parameters);
        var verdict = Evaluate(context);
        if (verdict.Verdict != RuleVerdict.Met)
        {
            return new FuneralEvaluation(new BenefitResult { Id = entry.Id, Person = person, Status = BenefitStatus.NieDotyczy }, []);
        }

        var trace = new[] { this.ToTrace(verdict) };
        var eventDate = facts.Sprawa.ZdarzenieData?.Value ?? asOf;
        decimal amount;
        try
        {
            amount = parameters.GetDecimal(DeadlineParameterKeys.ZasilekPogrzebowy, eventDate);
        }
        catch (ParameterNotFoundException e)
        {
            var reason = $"Brakuje parametru „{e.Key}” na dzień {e.Date:yyyy-MM-dd}, więc nie podaję kwoty zasiłku pogrzebowego. Skontaktuj się z ZUS.";
            var undecided = new BenefitResult
            {
                Id = entry.Id,
                Person = person,
                Status = BenefitStatus.NieRozstrzygam,
                Reason = reason,
                LegalBasis = PodstawaPrawna,
                Notes = [StatusComposer.ContactNote],
                Links = [.. entry.Links, CatalogLinks.ZusContact],
                Trace = trace,
            };
            return new FuneralEvaluation(undecided, []);
        }

        var missing = new List<MissingFact>();
        var notes = new List<string>
        {
            $"Kwota zasiłku pogrzebowego: {Money(amount)}. Przysługuje pełna kwota, nawet jeśli koszty pogrzebu były niższe.",
            "Zasiłek otrzymuje osoba, która zorganizowała pogrzeb i pokryła jego koszty.",
        };
        if (facts.Sprawa.DzieckoMartweUrodzenie?.Value != true)
        {
            notes.Add("Zasady z Poradnika ZUS opisują zasiłek pogrzebowy przy martwym urodzeniu; przy zgonie dziecka po porodzie potwierdź warunki w ZUS.");
        }

        if (facts.Sprawa.DzieckoDataPochowku is null)
        {
            missing.Add(FactQuestionBook.Ask(FactKeys.DzieckoDataPochowku));
            notes.Add("Podaj datę pochówku, żeby zobaczyć termin wniosku Z-12.");
        }

        var result = new BenefitResult
        {
            Id = entry.Id,
            Person = person,
            Status = BenefitStatus.Przysluguje,
            LumpSum = amount,
            Payer = Payer.Zus,
            WhereToApply = ApplicationPlace.Zus,
            Documents = entry.Documents,
            Notes = notes,
            Links = entry.Links,
            Trace = trace,
        };
        var outcome = deadlines.For(new DeadlineContext(facts, person, asOf, parameters, result));
        return new FuneralEvaluation(
            result with { Deadlines = outcome.Deadlines, Notes = [.. result.Notes, .. outcome.Notes] },
            missing);
    }

    private static string Money(decimal value) =>
        value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',') + " zł";
}
