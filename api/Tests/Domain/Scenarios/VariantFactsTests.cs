using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Scenarios;
using BabyRadnik.Tests.Domain.Catalog;
using static BabyRadnik.Tests.Domain.Scenarios.ScenarioKit;

namespace BabyRadnik.Tests.Domain.Scenarios;

public sealed class VariantFactsTests
{
    private static readonly IParameterProvider Params = CatalogKit.Params();

    private static ScenarioComparer Comparer() =>
        new(CatalogKit.FullEngine(Params), Params, ScenariosModule.BuiltIn());

    public static TheoryData<string, string?> Cases() => new()
    {
        { "odchodze_z_pracy", """{"data":"2026-12-15"}""" },
        { "umowa_nie_zostanie_przedluzona", null },
        { "przechodze_na_dzialalnosc", """{"data":"2026-12-01","dobrowolneChorobowe":true}""" },
        { "wariant_wyplaty", """{"wariant":"usredniony"}""" },
        { "praca_na_czesc_etatu", """{"wymiar":0.5,"tygodnie":10}""" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Assessing_variant_facts_gives_exactly_the_variant(string scenario, string? parameters)
    {
        var facts = PregnantEmployee(contractEnd: "2026-12-31");
        var change = JsonSerializer.Deserialize<ScenarioChange>(
            $$"""{"id":"{{scenario}}"{{(parameters is null ? "" : $",\"params\":{parameters}")}}}""", ApiJson.Options)!;

        var result = Comparer().Compare(new CompareRequest(facts, [change], AsOf));

        Assert.Empty(result.ValidationErrors);
        Assert.NotNull(result.VariantFacts);
        Assert.NotEmpty(result.Changes!);

        // Tak jak ekran: fakty wariantu przechodzą przez JSON i wracają jako nowe żądanie oceny.
        var wire = JsonSerializer.Serialize(result.VariantFacts, ApiJson.Options);
        var roundTripped = JsonSerializer.Deserialize<FactSet>(wire, ApiJson.Options)!;
        var reassessed = CatalogKit.FullEngine(Params).Assess(new AssessmentRequest(roundTripped, AsOf));

        Assert.Equal(
            JsonSerializer.Serialize(result.Variant, ApiJson.Options),
            JsonSerializer.Serialize(reassessed, ApiJson.Options));
    }

    [Fact]
    public void Odchodze_z_pracy_closes_title_and_sets_end_reason_in_variant_facts()
    {
        var change = new ScenarioChange(ScenarioId.OdchodzeZPracy, Params: P("""{"data":"2026-12-15"}"""));

        var result = Comparer().Compare(new CompareRequest(PregnantEmployee(), [change], AsOf));

        var person = result.VariantFacts!.Wnioskodawca;
        Assert.Equal(D("2026-12-15"), person.UbezpieczenieTytuly!.Value.Single().Do);
        Assert.Equal(InsuranceEndReason.WypowiedzeniePrzezPracownika, person.UbezpieczenieUstaniePrzyczyna!.Value);
        Assert.Null(person.UmowaPlanowaneOdejscieData);
        Assert.Contains(result.Changes!, c => c.Contains("15.12.2026"));
    }

    [Fact]
    public void Failed_scenario_has_no_variant_facts()
    {
        var change = new ScenarioChange(ScenarioId.OdchodzeZPracy, Params: P("""{"data":"nie-data"}"""));

        var result = Comparer().Compare(new CompareRequest(PregnantEmployee(), [change], AsOf));

        Assert.NotEmpty(result.ValidationErrors);
        Assert.Null(result.VariantFacts);
        Assert.Empty(result.Changes!);
    }
}
