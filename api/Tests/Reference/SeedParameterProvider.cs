using System.Text.Json;
using System.Text.Json.Serialization;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Tests.Reference;

public sealed record SeedRow(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("value")] JsonElement Value,
    [property: JsonPropertyName("validFrom")] DateOnly ValidFrom,
    [property: JsonPropertyName("legalBasis")] string LegalBasis);

/// <summary>
/// Dostawca parametrów dla testów: fixtures/parameters.seed.json + wiersze z przypadku referencyjnego.
/// Pozwala gałęziom silnika testować bez bazy i bez be/params.
/// </summary>
public sealed class SeedParameterProvider(IReadOnlyList<SeedRow> rows) : IParameterProvider
{
    private static readonly Lazy<IReadOnlyList<SeedRow>> Seed = new(() =>
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestPaths.Fixtures, "parameters.seed.json")));
        return doc.RootElement.GetProperty("parametry").Deserialize<List<SeedRow>>()!;
    });

    public static SeedParameterProvider Load(IEnumerable<SeedRow>? extra = null) => new([.. Seed.Value, .. extra ?? []]);

    public Parameter Get(string key, DateOnly date)
    {
        var row = rows.Where(r => r.Key == key && r.ValidFrom <= date).MaxBy(r => r.ValidFrom)
            ?? throw new ParameterNotFoundException(key, date);
        return row.Value.ValueKind switch
        {
            JsonValueKind.Number => new Parameter(key, row.Value.GetDecimal(), null, row.ValidFrom, row.LegalBasis),
            JsonValueKind.String => new Parameter(key, null, row.Value.GetString(), row.ValidFrom, row.LegalBasis),
            _ => new Parameter(key, null, null, row.ValidFrom, row.LegalBasis),
        };
    }
}
