using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Periods;
using Microsoft.Extensions.DependencyInjection;
using static BabyRadnik.Tests.Domain.Periods.PeriodsTestSupport;

namespace BabyRadnik.Tests.Domain.Periods;

public sealed class PeriodsModuleTests
{
    [Fact]
    public void AddPeriods_rejestruje_serwis_okresow()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Parameters());
        services.AddPeriods();
        using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<IPeriodsService>();

        var result = service.Assess(Facts(BirthCase("2027-03-15")), AsOf);
        Assert.Equal(BenefitStatus.Przysluguje, Get(result, BenefitId.Macierzynski).Status);
    }

    [Fact]
    public void Regula_ma_id_podstawe_prawna_i_ocenia_w_kontekscie()
    {
        IRule rule = new MacierzynskiWymiarRule();
        var context = new EvaluationContext(Facts(BirthCase("2027-03-15", 2)), PersonRef.Wnioskodawca, AsOf, Parameters());

        var result = rule.Evaluate(context);

        Assert.Equal("macierzynski_wymiar", rule.Id);
        Assert.Equal("KP art. 180 § 1", rule.PodstawaPrawna);
        Assert.Equal(RuleVerdict.Met, result.Verdict);
        Assert.Contains("31", result.Text);
    }

    [Fact]
    public void Regula_bez_faktu_zwraca_brak_danych_z_kluczem_faktu()
    {
        var sprawa = BirthCase("2027-03-15") with { DzieckoLiczba = null };
        var context = new EvaluationContext(Facts(sprawa), PersonRef.Wnioskodawca, AsOf, Parameters());

        var result = new MacierzynskiWymiarRule().Evaluate(context);

        Assert.Equal(RuleVerdict.MissingData, result.Verdict);
        Assert.Equal(FactKeys.DzieckoLiczba, Assert.Single(result.Missing!).Fact);
    }

    [Fact]
    public void Wszystkie_klucze_spoza_seed_sa_zdefiniowane_w_parametrach_testowych()
    {
        var provided = Supplemental.Select(r => r.Key).ToHashSet();

        Assert.True(PeriodsParameterKeys.NotInSeed.All(provided.Contains));
    }

    [Fact]
    public void Wynik_mozna_zlozyc_w_assessment_result()
    {
        var assessment = Assess(Facts(BirthCase("2027-03-15"), Mother(), Father()));

        var result = assessment.ToAssessmentResult("test");

        Assert.Equal("test", result.RulesetVersion);
        Assert.Contains(result.Benefits, b => b.Id == BenefitId.Ojcowski && b.Person == PersonRef.DrugiRodzic);
    }
}
