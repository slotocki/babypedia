using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain;

/// <summary>Pilnuje zgodności typów C# z contracts/*.schema.json i czystości projektu Domain.</summary>
public sealed class ContractTests
{
    [Theory]
    [InlineData(typeof(CaseFacts), "faktySprawy")]
    [InlineData(typeof(PersonFacts), "faktyOsoby")]
    public void Fact_keys_match_facts_schema(Type type, string schemaDef)
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestPaths.Contracts, "facts.schema.json")));
        var schemaKeys = schema.RootElement.GetProperty("$defs").GetProperty(schemaDef).GetProperty("properties")
            .EnumerateObject().Select(p => p.Name).Order().ToList();
        var csharpKeys = type.GetProperties()
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name)
            .OfType<string>().Order().ToList();

        Assert.Equal(schemaKeys, csharpKeys);
    }

    [Fact]
    public void FactKeys_constants_cover_all_fact_properties()
    {
        var constants = typeof(FactKeys).GetFields().Select(f => (string)f.GetRawConstantValue()!).Order().ToList();
        var properties = typeof(CaseFacts).GetProperties().Concat(typeof(PersonFacts).GetProperties())
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name).OfType<string>().Order().ToList();

        Assert.Equal(properties, constants);
    }

    [Fact]
    public void Example_request_and_persona_deserialize_strictly()
    {
        var request = JsonSerializer.Deserialize<AssessmentRequest>(
            File.ReadAllText(Path.Combine(TestPaths.Contracts, "examples", "assessment-request.example.json")), ApiJson.Options);

        Assert.NotNull(request);
        Assert.Equal(EventKind.Ciaza, request.Facts.Sprawa.ZdarzenieRodzaj?.Value);
        Assert.Equal(TitleKind.Pracownik, request.Facts.Wnioskodawca.UbezpieczenieTytuly?.Value[0].RodzajTytulu);
    }

    [Fact]
    public void Example_assessment_roundtrips_through_csharp_types()
    {
        var json = File.ReadAllText(Path.Combine(TestPaths.Contracts, "examples", "assessment.example.json"));
        var result = JsonSerializer.Deserialize<AssessmentResult>(json, ApiJson.Options)!;
        var again = JsonSerializer.Deserialize<AssessmentResult>(JsonSerializer.Serialize(result, ApiJson.Options), ApiJson.Options)!;

        Assert.Equal(result.Benefits.Count, again.Benefits.Count);
        Assert.Equal(Enum.GetValues<BenefitStatus>().Length, result.Benefits.Select(b => b.Status).Distinct().Count());
    }

    [Fact]
    public void Domain_has_no_dependency_on_database_http_or_assistant()
    {
        var references = typeof(FactSet).Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.DoesNotContain(references, r => r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.StartsWith("Npgsql", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.StartsWith("System.Net.Http", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.StartsWith("BabyRadnik.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void Seed_parameters_resolve_by_date()
    {
        var provider = SeedParameterProvider.Load();

        Assert.Equal(20m, provider.GetDecimal("macierzynski_tyg.dzieci_1", new DateOnly(2026, 6, 1)));
        Assert.Equal(4806.00m, provider.GetDecimal("placa_minimalna", new DateOnly(2026, 1, 1)));
        Assert.Throws<ParameterNotFoundException>(() => provider.Get("placa_minimalna", new DateOnly(2025, 12, 31)));
        Assert.Null(provider.Get("kwota_swiadczenia_rodzicielskiego", new DateOnly(2026, 6, 1)).Value);
    }
}
