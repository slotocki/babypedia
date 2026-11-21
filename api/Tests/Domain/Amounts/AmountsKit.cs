using System.Globalization;
using System.Text.Json;
using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Periods;
using BabyRadnik.Tests.Domain.Periods;
using BabyRadnik.Tests.Reference;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Amounts;

public static class AmountsKit
{
    public static SeedRow TextRow(string key, string value, string validFrom = "2000-01-01", string basis = "test") =>
        new(key, JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement.Clone(), DateOnly.Parse(validFrom, CultureInfo.InvariantCulture), basis);

    /// <summary>Parametry wymagane przez silnik kwot, których nie ma w fixtures/parameters.seed.json (do dodania do seed przez be/params).</summary>
    public static IReadOnlyList<SeedRow> Supplemental { get; } =
    [
        Row(AmountsParameterKeys.PodstawaDzielnikDzienny, 30, basis: "ustawa art. 36 ust. 3 w zw. z art. 47"),
        Row(AmountsParameterKeys.MiesiacDni, 30, basis: "ustawa art. 11 ust. 5 w zw. z art. 22"),
        Row(AmountsParameterKeys.PierwszyRokZyciaMiesiecy, 12, basis: "ustawa art. 31 ust. 3g i 4"),
    ];

    public static IParameterProvider Params(params SeedRow[] extra) =>
        SeedParameterProvider.Load([.. PeriodsTestSupport.Supplemental, .. Supplemental, .. extra]);

    public static IParameterProvider Params(IEnumerable<SeedRow> extra) => Params(extra.ToArray());

    public static BasisInput BasisOf(decimal amount, BasisSource source = BasisSource.SzacunekZus) => new(amount, source);

    public static AmountsResult Run(FactSet facts, BasisInput? basis = null, DateOnly? asOf = null, params SeedRow[] extra)
    {
        var parameters = Params(extra);
        var date = asOf ?? AsOf;
        var periods = new PeriodsService(parameters).Assess(facts, date);
        return Calculate(parameters, facts, date, periods.Benefits, basis);
    }

    public static AmountsResult Calculate(IParameterProvider parameters, FactSet facts, DateOnly asOf, IReadOnlyList<BenefitResult> cards, BasisInput? basis = null) =>
        new AmountsService(parameters).Calculate(new AmountsRequest(facts, asOf, cards,
            basis is null ? null : new Dictionary<PersonRef, BasisInput> { [PersonRef.Wnioskodawca] = basis }));

    public static BenefitResult Get(AmountsResult result, BenefitId id, PersonRef person = PersonRef.Wnioskodawca) =>
        result.Benefits.Single(b => b.Id == id && b.Person == person);

    public static BenefitResult Card(BenefitId id, string from, int days, PersonRef person = PersonRef.Wnioskodawca) => new()
    {
        Id = id,
        Person = person,
        Status = BenefitStatus.Przysluguje,
        Period = AmountPeriod(D(from), days),
    };

    public static Period AmountPeriod(DateOnly from, int days) => new(from, from.AddDays(days - 1), days, days / 7m);

    public static PaymentVariant Variant(PersonFacts facts) => facts.PlanWariantWyplaty!.Value;

    public static PersonFacts WithVariant(PersonFacts person, string? filedOn) => person with
    {
        PlanWariantWyplaty = PeriodsTestSupport.F(PaymentVariant.Usredniony),
        PlanDataWnioskuUsrednionego = filedOn is null ? null : PeriodsTestSupport.F(D(filedOn)),
    };

    public static BasisInput? BasisFromFacts(IParameterProvider parameters, FactSet facts, DateOnly? when = null)
    {
        var person = facts.Wnioskodawca;
        if (person.PodstawaUstalonaPrzezPlatnika is not null)
        {
            return null;
        }

        var months = person.UbezpieczeniePodstawySkladek?.Value;
        var date = when ?? facts.Sprawa.ZdarzenieData?.Value;
        if (months is null || months.Count == 0 || date is null)
        {
            return null;
        }

        var title = person.UbezpieczenieTytuly?.Value
            .FirstOrDefault(t => t.Od <= date && (t.Do is null || t.Do >= date));
        var profile = title is null
            ? new BasisProfile(TitleKind.Inny, SicknessInsurance.Obowiazkowe)
            : new BasisProfile(title.RodzajTytulu, title.Chorobowe, title.WymiarEtatu ?? 1m);
        var adjusted = new BasisAdjuster(parameters).FromRevenue(months.Average(m => m.Kwota), date.Value, profile);
        return new BasisInput(adjusted.Amount, adjusted.Source);
    }
}
