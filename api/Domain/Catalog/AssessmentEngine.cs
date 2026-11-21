using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Domain.Periods;

namespace BabyRadnik.Domain.Catalog;

public interface ICatalogAssessmentEngine : IAssessmentEngine
{
    AssessmentResult Assess(AssessmentRequest request, ICatalogSettings settings);
}

/// <summary>
/// Silnik katalogu: ocenia każdą włączoną pozycję katalogu dla każdej osoby, składa wynik uprawnień i okresów
/// (<see cref="StatusComposer"/>), prowadzi go przez potok podstawa → kwoty, dopisuje terminy, ryzyka i pytania o brakujące fakty.
/// </summary>
public sealed class AssessmentEngine(
    IPeriodsService periods,
    IEligibilityService eligibility,
    IParameterProvider parameters,
    IDeadlineService deadlines,
    BenefitPipeline pipeline,
    ICatalogSettings settings,
    TimeProvider time) : ICatalogAssessmentEngine
{
    public const string RulesetVersion = "2026-11-21";

    private static readonly FuneralRule Funeral = new();

    public AssessmentResult Assess(AssessmentRequest request) => Assess(request, settings);

    public AssessmentResult Assess(AssessmentRequest request, ICatalogSettings catalogSettings)
    {
        var facts = request.Facts;
        var asOf = request.AsOf ?? DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var completed = new CompletedDeadlines(request.CompletedDeadlines);
        var persons = new List<PersonRef> { PersonRef.Wnioskodawca };
        if (facts.DrugiRodzic is not null)
        {
            persons.Add(PersonRef.DrugiRodzic);
        }

        var periodsResult = periods.Assess(facts, asOf);
        var benefits = new List<BenefitResult>();
        var rightsMissing = new List<(MissingFact Fact, BenefitId? Benefit)>();
        var otherMissing = new List<(MissingFact Fact, BenefitId? Benefit)>();

        foreach (var person in persons)
        {
            var cards = new List<(BenefitResult Card, bool Priced)>();
            foreach (var entry in BenefitCatalog.Entries.Where(e => catalogSettings.IsEnabled(e.Id)))
            {
                if (entry.Id == BenefitId.Pogrzebowy)
                {
                    var funeral = Funeral.Assess(facts, person, asOf, parameters, deadlines);
                    cards.Add((funeral.Result, false));
                    otherMissing.AddRange(funeral.Missing.Select(m => (m, (BenefitId?)BenefitId.Pogrzebowy)));
                    continue;
                }

                var rights = eligibility.Evaluate(entry.Id, facts, person, asOf);
                var period = periodsResult.Benefits.FirstOrDefault(b => b.Id == entry.Id && b.Person == person);
                var employee = InsuranceFacts.IsEmployee(facts.For(person), facts.Sprawa.ZdarzenieData?.Value, asOf);
                var composed = StatusComposer.Compose(entry, person, rights.Result, period, employee);
                if (composed.Status is BenefitStatus.BrakujeDanych or BenefitStatus.Przysluguje)
                {
                    rightsMissing.AddRange(rights.MissingFacts.Select(m => (m, (BenefitId?)entry.Id)));
                }

                cards.Add((composed, true));
            }

            var siblings = cards.Select(c => c.Card).ToList();
            foreach (var (card, priced) in cards)
            {
                if (!priced)
                {
                    benefits.Add(card);
                    continue;
                }

                var piped = pipeline.Run(card, siblings, facts, asOf, parameters);
                benefits.Add(WithDeadlines(piped.Benefit, facts, asOf));
                otherMissing.AddRange(piped.Missing.Select(m => (m, (BenefitId?)card.Id)));
            }
        }

        var missing = rightsMissing
            .Concat(PeriodsMissing(periodsResult.MissingFacts, benefits, facts))
            .Concat(otherMissing)
            .Concat(CatalogMissingFacts.For(facts, persons, asOf, parameters).SelectMany(Tagged))
            .Select(x => (Fact: FactQuestionBook.Normalize(x.Item1), Benefit: x.Item2))
            .GroupBy(x => (x.Fact.Fact, x.Fact.Person))
            .Select(g => g.First().Fact with
            {
                BenefitIds = [.. g.Select(x => x.Benefit).OfType<BenefitId>().Distinct().OrderBy(id => id)],
            })
            .ToList();

        var riskContext = new RiskContext(facts, asOf, parameters, benefits, completed);
        return new AssessmentResult
        {
            RulesetVersion = RulesetVersion,
            Benefits = benefits,
            MissingFacts = missing,
            ValidationErrors = periodsResult.ValidationErrors,
            NextStep = NextStepSelector.Select(benefits, asOf, completed),
            Risks = RiskRules.Merge(eligibility.Assess(facts, asOf).Risks, riskContext),
            DifficultSituation = DifficultSituation.Detect(facts, asOf, parameters),
        };
    }

    private BenefitResult WithDeadlines(BenefitResult benefit, FactSet facts, DateOnly asOf)
    {
        var outcome = deadlines.For(new DeadlineContext(facts, benefit.Person, asOf, parameters, benefit));
        return benefit with
        {
            Deadlines = [.. benefit.Deadlines.Concat(outcome.Deadlines).DistinctBy(d => d.Id)],
            Notes = [.. benefit.Notes.Concat(outcome.Notes).Distinct()],
        };
    }

    private static IEnumerable<(MissingFact, BenefitId?)> Tagged(MissingFact missing) =>
        CatalogMissingFacts.BenefitsOf(missing) is { Count: > 0 } ids ? ids.Select(id => (missing, (BenefitId?)id)) : [(missing, null)];

    private static IEnumerable<(MissingFact, BenefitId?)> PeriodsMissing(IReadOnlyList<MissingFact> missing, IReadOnlyList<BenefitResult> benefits, FactSet facts)
    {
        var waitingCards = benefits.Where(b => b.Status == BenefitStatus.BrakujeDanych).ToList();
        var waiting = waitingCards.Select(b => b.Person).ToHashSet();
        foreach (var m in missing.Where(m =>
            (m.Person is null && waiting.Count > 0)
            || (m.Person is { } person && waiting.Contains(person))
            || (m.Person == PersonRef.DrugiRodzic && facts.DrugiRodzic is null)))
        {
            var cards = waitingCards.Where(b => m.Person is null || b.Person == m.Person).ToList();
            if (cards.Count == 0)
            {
                yield return (m, null);
            }

            foreach (var card in cards)
            {
                yield return (m, card.Id);
            }
        }
    }
}
