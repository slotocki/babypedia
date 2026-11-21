using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public sealed class BasisService(IParameterProvider parameters) : IBasisService
{
    public BasisAssessment Assess(FactSet facts, PersonRef person, DateOnly dzienPowstaniaPrawa)
    {
        var inputs = new BasisInputs(facts, person, dzienPowstaniaPrawa, parameters);
        var path = new List<BasisStep>();

        bool Run(BasisStep step)
        {
            path.Add(step);
            return step.Ok;
        }

        if (Run(inputs.Payer))
        {
            return FromPayer(inputs, path);
        }

        if (!Run(inputs.Reference) || !Run(inputs.Title))
        {
            return Failed(inputs, path);
        }

        var employee = inputs.Title.Value!.IsEmployee;
        var final = employee
            ? Run(inputs.Months) && Run(inputs.Absence) && Run(inputs.EmployeeAverage) && Run(inputs.EmployeeMinimum) ? inputs.EmployeeMinimum : null
            : Run(inputs.Months) && Run(inputs.Absence) && Run(inputs.NonEmployeeEstimate) && Run(inputs.VoluntaryLimit) ? inputs.VoluntaryLimit : null;

        return final is { Value: { } value } ? FromEstimate(inputs, path, value, employee) : Failed(inputs, path);
    }

    private static BasisAssessment FromPayer(BasisInputs i, List<BasisStep> path) => new(
        BasisOutcome.Determined,
        i.Payer.Value,
        BasisSource.UstalonaPrzezPlatnika,
        false,
        i.Date,
        "Podstawa ustalona przez płatnika składek — przyjęta bez zmian.",
        [],
        [],
        [],
        path.Select(s => s.Trace).ToList(),
        []);

    private static BasisAssessment FromEstimate(BasisInputs i, List<BasisStep> path, BasisValue value, bool employee)
    {
        var notCounted = new List<string>();
        if (employee)
        {
            notCounted.Add(BasisTexts.NotCountedPartialAbsence);
            notCounted.Add(BasisTexts.NotCountedQuarterlyAnnual);
            notCounted.Add(BasisTexts.NotCountedExcludedComponents);
        }
        else
        {
            notCounted.Add(BasisTexts.NotCountedPartialActivity);
        }

        var reference = i.Reference.Value!;
        if (!reference.BenefitsKnown)
        {
            notCounted.Add(BasisTexts.NotCountedBenefitsUnknown);
        }
        else if (reference.Chain.Count > 0)
        {
            notCounted.Add(BasisTexts.NotCountedWorkingTimeChange);
        }

        if (!i.Absence.Value!.AbsencesKnown)
        {
            notCounted.Add(BasisTexts.NotCountedAbsencesUnknown);
        }

        notCounted.Add(BasisTexts.NotCountedEstimate);

        var notes = new List<string>(value.Notes)
        {
            $"Szacunek z danych ZUS: podstawa wymiaru zasiłku {Fmt.Money(value.Amount)} (kwoty brutto, przed zaliczką na podatek; odliczono tylko składki {Fmt.Percent(i.Param(BasisParameterKeys.OdliczenieSkladek, i.ReferenceDay))}).",
        };

        var months = BuildMonths(i);
        return new BasisAssessment(
            BasisOutcome.Determined,
            value.Amount,
            value.Source,
            true,
            i.ReferenceDay,
            $"Szacunek podstawy z danych ZUS: {Fmt.Money(value.Amount)}. Dokładną podstawę ustala płatnik składek — możesz ją wpisać w polu „podstawa ustalona przez płatnika”.",
            notes,
            notCounted,
            months,
            path.Select(s => s.Trace).ToList(),
            []);
    }

    private static IReadOnlyList<BasisMonth> BuildMonths(BasisInputs i)
    {
        var months = i.Months.Value!;
        var plan = i.Absence.Value!;
        return months.Insured
            .Select(m => new BasisMonth(
                Fmt.Months([m]),
                months.Contributions[m],
                plan.Used.Contains(m),
                plan.Omitted.Contains(m) ? "pominięty: w całości objęty nieobecnością"
                : plan.Partial.Contains(m) ? "częściowa nieobecność — nie rozpoznano"
                : null))
            .ToList();
    }

    private static BasisAssessment Failed(BasisInputs i, List<BasisStep> path)
    {
        var step = path[^1];
        var outcome = step.Result.Verdict switch
        {
            RuleVerdict.MissingData => BasisOutcome.MissingData,
            RuleVerdict.NotMet => BasisOutcome.NoTitle,
            _ => BasisOutcome.Undecided,
        };
        var missing = new List<MissingFact>(step.Result.Missing ?? []) { BasisAsk.PayerBasis(i.Person) };
        var message = outcome == BasisOutcome.MissingData
            ? $"Brakuje danych do szacunku podstawy: {step.Result.Text} {BasisTexts.OutOfScope} {BasisTexts.Referral}"
            : BasisTexts.Fail(step.Result.Text);
        return new BasisAssessment(
            outcome,
            null,
            null,
            false,
            i.ReferenceDay,
            message,
            [],
            BasisTexts.OutOfScopeItems,
            [],
            path.Select(s => s.Trace).ToList(),
            missing);
    }
}
