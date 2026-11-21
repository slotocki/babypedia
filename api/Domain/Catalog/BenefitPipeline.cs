using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Catalog;

public sealed record BenefitContext(BenefitResult Benefit, FactSet Facts, DateOnly AsOf, IParameterProvider Parameters)
{
    public PersonRef Person => Benefit.Person;
}

public sealed record BasisResolution(decimal Amount, BasisSource Source);

public sealed record BasisStepResult(BasisResolution? Basis, IReadOnlyList<string> Notes, IReadOnlyList<MissingFact> Missing)
{
    public static readonly BasisStepResult None = new(null, [], []);
}

public sealed record AmountsStepResult(
    decimal? Rate,
    Amount? Amount,
    IReadOnlyList<BenefitPart>? Parts,
    IReadOnlyList<string> Notes,
    IReadOnlyList<Deadline> Deadlines)
{
    public static readonly AmountsStepResult None = new(null, null, null, [], []);
}

/// <summary>Ogniwo „podstawa” potoku. Moduł Domain/Basis podmienia domyślną implementację (<see cref="NoBasisStep"/>).</summary>
public interface IBasisStep
{
    BasisStepResult Resolve(BenefitContext context);
}

/// <summary>Ogniwo „kwoty” potoku. Moduł Domain/Amounts podmienia domyślną implementację (<see cref="NoAmountsStep"/>).</summary>
public interface IAmountsStep
{
    AmountsStepResult Calculate(BenefitContext context, BasisStepResult basis);
}

public sealed class NoBasisStep : IBasisStep
{
    public BasisStepResult Resolve(BenefitContext context) => BasisStepResult.None;
}

public sealed class NoAmountsStep : IAmountsStep
{
    public const string MissingBasisNote = "Podaj podstawę, żeby zobaczyć kwotę.";

    public AmountsStepResult Calculate(BenefitContext context, BasisStepResult basis) =>
        context.Benefit.Period is null
            ? AmountsStepResult.None
            : AmountsStepResult.None with { Notes = [MissingBasisNote] };
}

public sealed record PipelineOutcome(BenefitResult Benefit, IReadOnlyList<MissingFact> Missing);

/// <summary>Potok: podstawa → kwoty. Działa tylko na świadczeniach ze statusem „przysługuje”.</summary>
public sealed class BenefitPipeline(IBasisStep basisStep, IAmountsStep amountsStep)
{
    public PipelineOutcome Run(BenefitResult benefit, FactSet facts, DateOnly asOf, IParameterProvider parameters)
    {
        if (benefit.Status != BenefitStatus.Przysluguje)
        {
            return new PipelineOutcome(benefit, []);
        }

        var context = new BenefitContext(benefit, facts, asOf, parameters);
        var basis = basisStep.Resolve(context);
        var amounts = amountsStep.Calculate(context, basis);
        var notes = benefit.Notes.Concat(basis.Notes).Concat(amounts.Notes).Distinct().ToList();
        var deadlines = benefit.Deadlines.Concat(amounts.Deadlines).DistinctBy(d => d.Id).ToList();
        var result = benefit with
        {
            Rate = amounts.Rate,
            Amount = amounts.Amount,
            Parts = amounts.Parts ?? benefit.Parts,
            Notes = notes,
            Deadlines = deadlines,
        };
        return new PipelineOutcome(result, basis.Missing);
    }
}
