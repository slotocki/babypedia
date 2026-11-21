using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Reference;
using Microsoft.Extensions.DependencyInjection;
using static BabyRadnik.Tests.Domain.Basis.BasisKit;

namespace BabyRadnik.Tests.Domain.Basis;

public sealed class BasisIntegrationTests
{
    public static TheoryData<string> AmountCases()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(TestPaths.Fixtures, "reference"), "kwoty-*.json").Order())
        {
            var referenceCase = ReferenceCase.Load(file);
            if (referenceCase is { Fakty: not null, DoUzupelnienia: false } && ExpectedBasis(referenceCase) is not null)
            {
                data.Add(Path.GetFileName(file));
            }
        }

        return data;
    }

    private static JsonElement? ExpectedBasis(ReferenceCase referenceCase)
    {
        if (referenceCase.Oczekiwane is not { } expected || !expected.TryGetProperty("benefits", out var benefits) || benefits.GetArrayLength() == 0)
        {
            return null;
        }

        var amount = benefits[0].TryGetProperty("amount", out var a) ? a : default;
        return amount.ValueKind == JsonValueKind.Object && amount.TryGetProperty("basis", out _) ? amount : null;
    }

    [Theory]
    [MemberData(nameof(AmountCases))]
    public void Basis_matches_the_amount_reference_cases(string file)
    {
        var referenceCase = ReferenceCase.Load(Path.Combine(TestPaths.Fixtures, "reference", file));
        var expected = ExpectedBasis(referenceCase)!.Value;
        var facts = referenceCase.Fakty!;
        var service = new BasisService(SeedParameterProvider.Load([.. LowestBase, .. referenceCase.Parametry]));

        var result = service.Assess(facts, PersonRef.Wnioskodawca, facts.Sprawa.ZdarzenieData!.Value);

        Assert.Equal(BasisOutcome.Determined, result.Outcome);
        Assert.Equal(expected.GetProperty("basis").GetDecimal(), result.Basis);
        Assert.Equal(expected.GetProperty("basisSource").GetString(), JsonSerializer.Serialize(result.Source, ApiJson.Options).Trim('"'));
    }

    [Fact]
    public void Amount_reference_cases_are_found()
    {
        Assert.True(AmountCases().Count() >= 8);
    }

    [Fact]
    public void AddBasis_registers_the_service()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IParameterProvider>(SeedParameterProvider.Load());
        services.AddBasis();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IBasisService>();

        var result = service.Assess(Facts(FullTimeEmployee(8000)), PersonRef.Wnioskodawca, D(Birth));
        Assert.Equal(6903.20m, result.Basis);
    }

    [Fact]
    public void Second_parent_is_assessed_from_their_own_facts()
    {
        var facts = new FactSet { Wnioskodawca = FullTimeEmployee(8000), DrugiRodzic = FullTimeEmployee(1000) };

        var service = Service();

        Assert.Equal(6903.20m, service.Assess(facts, PersonRef.Wnioskodawca, D(Birth)).Basis);
        Assert.Equal(4147.10m, service.Assess(facts, PersonRef.DrugiRodzic, D(Birth)).Basis);
    }

    [Fact]
    public void Missing_second_parent_asks_for_titles()
    {
        var result = Service().Assess(Facts(FullTimeEmployee(8000)), PersonRef.DrugiRodzic, D(Birth));

        Assert.Equal(BasisOutcome.MissingData, result.Outcome);
    }

    [Fact]
    public void Basis_is_rounded_to_grosze_only_at_the_end()
    {
        var contributions = Contributions("2025-03", 12, 8333.33m);

        var result = Assess(Person([Employee("2020-01-01")], contributions, [], []));

        Assert.Equal(Math.Round(8333.33m * (1 - 0.1371m), 2, MidpointRounding.AwayFromZero), result.Basis);
    }
}
