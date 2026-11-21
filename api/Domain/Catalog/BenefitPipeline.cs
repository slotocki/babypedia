using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Catalog;

/// <summary>Kontekst kroku potoku: karta świadczenia, wszystkie karty tej samej osoby (przed potokiem), fakty i parametry.</summary>
public sealed record BenefitContext(
    BenefitResult Benefit,
    IReadOnlyList<BenefitResult> Siblings,
    FactSet Facts,
    DateOnly AsOf,
    IParameterProvider Parameters)
{
    public PersonRef Person => Benefit.Person;

    public DateOnly EventDate => Facts.Sprawa.ZdarzenieData?.Value ?? AsOf;
}

public sealed record BasisResolution(decimal Amount, BasisSource Source);

public sealed record BasisStepResult(
    BasisResolution? Basis,
    IReadOnlyList<string> Notes,
    IReadOnlyList<MissingFact> Missing,
    IReadOnlyList<TraceStep> Trace)
{
    public static readonly BasisStepResult None = new(null, [], [], []);
}

/// <summary>Ogniwo „podstawa” potoku. Domyślnie puste (<see cref="NoBasisStep"/>); z modułem Basis — <see cref="BasisServiceStep"/>.</summary>
public interface IBasisStep
{
    BasisStepResult Resolve(BenefitContext context);
}

/// <summary>Ogniwo „kwoty” potoku: zwraca kartę ze stawką, częściami i kwotą. Domyślnie puste (<see cref="NoAmountsStep"/>); z modułem Amounts — <see cref="AmountsServiceStep"/>.</summary>
public interface IAmountsStep
{
    BenefitResult Apply(BenefitContext context, BasisStepResult basis);
}

public sealed class NoBasisStep : IBasisStep
{
    public BasisStepResult Resolve(BenefitContext context) => BasisStepResult.None;
}

public sealed class NoAmountsStep : IAmountsStep
{
    public const string MissingBasisNote = "Podaj podstawę, żeby zobaczyć kwotę.";

    public BenefitResult Apply(BenefitContext context, BasisStepResult basis) =>
        context.Benefit.Period is null ? context.Benefit : context.Benefit with { Notes = [.. context.Benefit.Notes, MissingBasisNote] };
}

public sealed record PipelineOutcome(BenefitResult Benefit, IReadOnlyList<MissingFact> Missing);

/// <summary>Potok: podstawa → kwoty. Działa tylko na świadczeniach ze statusem „przysługuje”.</summary>
public sealed class BenefitPipeline(IBasisStep basisStep, IAmountsStep amountsStep)
{
    public PipelineOutcome Run(BenefitResult benefit, IReadOnlyList<BenefitResult> siblings, FactSet facts, DateOnly asOf, IParameterProvider parameters)
    {
        if (benefit.Status != BenefitStatus.Przysluguje)
        {
            return new PipelineOutcome(benefit, []);
        }

        var context = new BenefitContext(benefit, siblings, facts, asOf, parameters);
        var basis = basisStep.Resolve(context);
        var withBasis = benefit with
        {
            Notes = [.. benefit.Notes.Concat(basis.Notes).Distinct()],
            Trace = [.. benefit.Trace, .. basis.Trace],
        };
        var priced = amountsStep.Apply(context with { Benefit = withBasis }, basis);
        return new PipelineOutcome(priced with { Notes = [.. priced.Notes.Distinct()] }, basis.Missing);
    }
}
