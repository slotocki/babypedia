using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public sealed record ReferenceDate(DateOnly Date, IReadOnlyList<ReceivedBenefit> Chain, bool BenefitsKnown);

public sealed record InsuredTitle(InsuranceTitle Title, bool IsEmployee, DateOnly InsuredFrom, IReadOnlyList<InsuranceTitle> Chain);

public sealed record MonthSet(IReadOnlyList<DateOnly> Insured, IReadOnlyDictionary<DateOnly, decimal> Contributions);

public sealed record MonthPlan(
    IReadOnlyList<DateOnly> Used,
    IReadOnlyList<DateOnly> Omitted,
    IReadOnlyList<DateOnly> Partial,
    bool UsedAllBecauseEveryMonthAbsent,
    bool AbsencesKnown);

public sealed record BasisValue(decimal Amount, BasisSource Source, IReadOnlyList<string> Notes)
{
    public BasisValue With(decimal amount, BasisSource? source = null, params string[] notes) =>
        new(amount, source ?? Source, [.. Notes, .. notes]);
}

public static class BasisRules
{
    public static readonly PayerBasisRule Payer = new();
    public static readonly ReferenceDateRule Reference = new();
    public static readonly InsuredTitleRule Title = new();
    public static readonly InsuredMonthsRule Months = new();
    public static readonly AbsentMonthsRule Absence = new();
    public static readonly EmployeeAverageRule EmployeeAverage = new();
    public static readonly EmployeeMinimumRule EmployeeMinimum = new();
    public static readonly NonEmployeeAverageRule NonEmployeeAverage = new();
    public static readonly ShortInsuranceRule ShortInsurance = new();
    public static readonly FirstMonthRule FirstMonth = new();
    public static readonly VoluntaryLimitRule VoluntaryLimit = new();
}

public sealed class BasisInputs(FactSet facts, PersonRef person, DateOnly date, IParameterProvider parameters)
{
    private BasisStep<decimal?>? payer;
    private BasisStep<ReferenceDate>? reference;
    private BasisStep<InsuredTitle>? title;
    private BasisStep<MonthSet>? months;
    private BasisStep<MonthPlan>? absence;
    private BasisStep<BasisValue>? employeeAverage;
    private BasisStep<BasisValue>? employeeMinimum;
    private BasisStep<BasisValue>? nonEmployeeEstimate;
    private BasisStep<BasisValue>? limit;

    public FactSet Facts => facts;

    public PersonRef Person => person;

    public DateOnly Date => date;

    public PersonFacts? Me => facts.For(person);

    public decimal Param(string key, DateOnly on) => parameters.GetDecimal(key, on);

    public DateOnly ReferenceDay => Reference.Value?.Date ?? date;

    public BasisStep<decimal?> Payer => payer ??= BasisRules.Payer.Calculate(this);

    public BasisStep<ReferenceDate> Reference => reference ??= BasisRules.Reference.Calculate(this);

    public BasisStep<InsuredTitle> Title => title ??= BasisRules.Title.Calculate(this);

    public BasisStep<MonthSet> Months => months ??= BasisRules.Months.Calculate(this);

    public BasisStep<MonthPlan> Absence => absence ??= BasisRules.Absence.Calculate(this);

    public BasisStep<BasisValue> EmployeeAverage => employeeAverage ??= BasisRules.EmployeeAverage.Calculate(this);

    public BasisStep<BasisValue> EmployeeMinimum => employeeMinimum ??= BasisRules.EmployeeMinimum.Calculate(this);

    public BasisStep<BasisValue> NonEmployeeEstimate => nonEmployeeEstimate ??= SelectNonEmployeeRule().Calculate(this);

    public BasisStep<BasisValue> VoluntaryLimit => limit ??= BasisRules.VoluntaryLimit.Calculate(this);

    private BasisRule<BasisValue> SelectNonEmployeeRule()
    {
        var insured = Months.Value?.Insured.Count ?? 0;
        return insured >= 12 ? BasisRules.NonEmployeeAverage : insured > 0 ? BasisRules.ShortInsurance : BasisRules.FirstMonth;
    }
}
