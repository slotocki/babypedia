using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Domain.Catalog;
using static BabyRadnik.Tests.Domain.Eligibility.Kit;

namespace BabyRadnik.Tests.Domain.Eligibility;

/// <summary>Ryzyko „koniec ubezpieczenia przed porodem” tylko, gdy w dniu zdarzenia nie ma aktywnego tytułu z chorobowym.</summary>
public sealed class RiskCatalogTests
{
    private const string RiskId = "koniec_ubezpieczenia_przed_porodem";
    private static readonly DateOnly Birth = D(2027, 3, 20);

    private static FactSet Pregnant(params InsuranceTitle[] titles) =>
        Facts(Case(EventKind.Ciaza, Birth), Person(ParentRole.Matka, titles));

    [Fact]
    public void Ended_past_title_with_another_active_sickness_title_is_not_a_risk()
    {
        var facts = Pregnant(
            Title(TitleKind.Pracownik, D(2018, 1, 1), D(2021, 12, 31)),
            Title(TitleKind.Pracownik, D(2022, 1, 1)));

        Assert.DoesNotContain(Service().Assess(facts, AsOf).Risks, r => r.Id == RiskId);
    }

    [Fact]
    public void Title_ending_before_birth_without_other_insurance_is_a_risk()
    {
        var facts = Pregnant(Title(TitleKind.Pracownik, D(2022, 1, 1), D(2027, 1, 31)));

        Assert.Contains(Service().Assess(facts, AsOf).Risks, r => r.Id == RiskId);
    }

    [Fact]
    public void Ended_title_followed_by_title_without_sickness_insurance_is_a_risk()
    {
        var facts = Pregnant(
            Title(TitleKind.Pracownik, D(2022, 1, 1), D(2027, 1, 31)),
            Title(TitleKind.Dzialalnosc, D(2027, 2, 1), sickness: SicknessInsurance.Brak));

        Assert.Contains(Service().Assess(facts, AsOf).Risks, r => r.Id == RiskId);
    }

    [Fact]
    public void Full_assessment_has_no_duplicate_risk_ids()
    {
        var facts = Pregnant(Title(TitleKind.Pracownik, D(2022, 1, 1), D(2027, 1, 31)));

        var risks = CatalogKit.Assess(facts with { Sprawa = facts.Sprawa with { ZdarzenieDlaKogo = F(CalculationScope.Ja) } }, engine: CatalogKit.FullEngine()).Risks;

        Assert.Single(risks, r => r.Id == RiskId);
        Assert.Equal(risks.Count, risks.Select(r => r.Id).Distinct().Count());
    }
}
