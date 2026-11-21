using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Domain.Periods;
using BabyRadnik.Domain.Scenarios;
using BabyRadnik.Tests.Domain.Eligibility;
using BabyRadnik.Tests.Domain.Periods;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Scenarios;

internal static class ScenarioKit
{
    public static readonly DateOnly AsOf = new(2026, 11, 21);

    public static IParameterProvider Parameters() =>
        SeedParameterProvider.Load([.. Kit.SupplementRows(), .. PeriodsTestSupport.Supplemental]);

    public static DateOnly D(string date) => DateOnly.Parse(date);

    public static Fact<T> F<T>(T value) => new(value, FactSource.User);

    public static JsonElement? P(string json) => JsonDocument.Parse(json).RootElement.Clone();

    public static ScenarioContext Context(FactSet facts, string? json, PersonRef person = PersonRef.Wnioskodawca) =>
        new(facts, person, json is null ? null : P(json), AsOf, Parameters());

    public static InsuranceTitle Employee(string from, string? to = null, decimal etat = 1) =>
        new(TitleKind.Pracownik, D(from), to is null ? null : D(to), SicknessInsurance.Obowiazkowe, WymiarEtatu: etat);

    public static FactSet PregnantEmployee(string? contractEnd = null, string? titleEnd = null)
    {
        var person = new PersonFacts
        {
            OsobaRola = F(ParentRole.Matka),
            UbezpieczenieTytuly = F<IReadOnlyList<InsuranceTitle>>([Employee("2025-01-01", titleEnd)]),
        };
        if (contractEnd is not null)
        {
            person = person with
            {
                UmowaNaCzasOkreslony = F(true),
                UmowaDataKonca = F(D(contractEnd)),
            };
        }

        return new FactSet
        {
            Sprawa = new CaseFacts { ZdarzenieRodzaj = F(EventKind.Ciaza), ZdarzenieData = F(D("2027-03-20")) },
            Wnioskodawca = person,
        };
    }

    public static FactSet BirthWithBothParents(string birth = "2026-03-02") => new()
    {
        Sprawa = new CaseFacts
        {
            ZdarzenieRodzaj = F(EventKind.Urodzenie),
            ZdarzenieData = F(D(birth)),
            DzieckoLiczba = F(1),
            DzieckoDataUrodzenia = F(D(birth)),
        },
        Wnioskodawca = new PersonFacts
        {
            OsobaRola = F(ParentRole.Matka),
            UbezpieczenieTytuly = F<IReadOnlyList<InsuranceTitle>>([Employee("2020-01-01")]),
        },
        DrugiRodzic = new PersonFacts
        {
            OsobaRola = F(ParentRole.Ojciec),
            UbezpieczenieTytuly = F<IReadOnlyList<InsuranceTitle>>([Employee("2020-01-01")]),
        },
    };

    public static FactSet MotherWithDebt() => new()
    {
        Sprawa = new CaseFacts
        {
            ZdarzenieRodzaj = F(EventKind.Urodzenie),
            ZdarzenieData = F(D("2026-08-10")),
            DzieckoLiczba = F(1),
            DzieckoDataUrodzenia = F(D("2026-08-10")),
        },
        Wnioskodawca = new PersonFacts
        {
            OsobaRola = F(ParentRole.Matka),
            UbezpieczenieTytuly = F<IReadOnlyList<InsuranceTitle>>(
                [new InsuranceTitle(TitleKind.Dzialalnosc, D("2020-01-01"), null, SicknessInsurance.Dobrowolne)]),
            SkladkiZadluzenie = F(new ContributionDebt(D("2026-08-10"), 120m)),
        },
    };

    public static FactSet Apply(IScenario scenario, FactSet facts, string? json, PersonRef person = PersonRef.Wnioskodawca)
    {
        var outcome = scenario.Apply(Context(facts, json, person));
        Assert.True(outcome.Succeeded, string.Join("; ", outcome.Errors.Select(e => e.Message)));
        return outcome.Facts!;
    }

    public static EligibilityService EligibilityEngine() => new(Parameters());

    public static PeriodsService PeriodsEngine() => new(Parameters());

    public static BenefitResult Benefit(EligibilityAssessment assessment, BenefitId id, PersonRef person = PersonRef.Wnioskodawca) =>
        assessment.Benefits.Single(b => b.Id == id && b.Person == person);
}
