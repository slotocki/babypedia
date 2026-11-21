using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;

namespace BabyRadnik.Domain.Catalog;

/// <summary>Ogniwo „podstawa” oparte na module Basis. Dzień powstania prawa: początek pierwszego okresu zasiłku macierzyńskiego tej osoby (urlopy po nim traktuję jako ciąg — art. 43), inaczej początek okresu świadczenia, inaczej dzień zdarzenia; dla prawa w przyszłości podstawę szacuję na dzień oceny.</summary>
public sealed class BasisServiceStep(IBasisService basis) : IBasisStep
{
    public static DateOnly RightsDay(BenefitContext context)
    {
        var continuous = context.Benefit.Id is BenefitId.Macierzynski or BenefitId.MacierzynskiUzupelniajacy or BenefitId.Rodzicielski or BenefitId.PrzyjecieDziecka;
        if (continuous)
        {
            var anchors = context.Siblings
                .Where(b => b.Status == BenefitStatus.Przysluguje && b.Id is BenefitId.Macierzynski or BenefitId.PrzyjecieDziecka && b.Period is not null)
                .Select(b => b.Period!.From)
                .ToList();
            if (anchors.Count > 0)
            {
                return anchors.Min();
            }
        }

        return context.Benefit.Period?.From ?? context.EventDate;
    }

    public BasisStepResult Resolve(BenefitContext context)
    {
        var rightsDay = RightsDay(context);
        var future = rightsDay > context.AsOf;
        var assessment = basis.Assess(context.Facts, context.Person, future ? context.AsOf : rightsDay);
        string[] extra = future
            ? [$"Prawo powstanie {PolishDate.Words(rightsDay)}, więc podstawę szacuję z 12 miesięcy poprzedzających dzień oceny ({PolishDate.Words(context.AsOf)})."]
            : [];
        if (assessment.Basis is not { } amount || assessment.Source is not { } source)
        {
            return new BasisStepResult(null, [assessment.Message, .. extra], assessment.MissingFacts, assessment.Trace);
        }

        return new BasisStepResult(new BasisResolution(amount, source), [.. assessment.Notes, .. extra], assessment.MissingFacts, assessment.Trace);
    }
}

/// <summary>Ogniwo „kwoty” oparte na module Amounts: karta idzie przez <see cref="IAmountsService"/> razem z podstawą (z modułu Basis albo faktem „podstawa ustalona przez płatnika”).</summary>
public sealed class AmountsServiceStep(IAmountsService amounts) : IAmountsStep
{
    public BenefitResult Apply(BenefitContext context, BasisStepResult basis)
    {
        IReadOnlyDictionary<PersonRef, BasisInput>? bases = basis.Basis is { } resolved
            ? new Dictionary<PersonRef, BasisInput> { [context.Person] = new BasisInput(resolved.Amount, resolved.Source) }
            : null;
        var result = amounts.Calculate(new AmountsRequest(context.Facts, context.AsOf, [context.Benefit], bases));
        return result.Benefits[0];
    }
}
