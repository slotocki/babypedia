using System.Text.Json;
using System.Text.Json.Serialization;
using BabyRadnik.Infrastructure.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BabyRadnik.Infrastructure.Parameters;

internal sealed record SeedFile(List<SeedEntry> Parametry);

internal sealed record SeedEntry(
    string Key,
    string? Wiersz,
    JsonElement Value,
    string? Unit,
    DateOnly ValidFrom,
    string LegalBasis,
    string Status,
    string? Note);

public static class ParameterSeed
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<ParameterRow> Parse(string json)
    {
        var file = JsonSerializer.Deserialize<SeedFile>(json, Options)
            ?? throw new JsonException("Pusty plik parametrów startowych.");
        return file.Parametry.Select(e => new ParameterRow
        {
            Key = e.Key,
            Wiersz = e.Wiersz,
            ValueNumber = e.Value.ValueKind == JsonValueKind.Number ? e.Value.GetDecimal() : null,
            ValueText = e.Value.ValueKind == JsonValueKind.String ? e.Value.GetString() : null,
            Unit = e.Unit,
            ValidFrom = e.ValidFrom,
            LegalBasis = e.LegalBasis,
            Status = e.Status,
            Note = e.Note,
        }).ToList();
    }
}

internal sealed class ParameterSeeder(IParameterStore store, RepoPaths paths, CachedParameterProvider provider, ILogger<ParameterSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var path = Path.Combine(paths.Fixtures, "parameters.seed.json");
            var rows = ParameterSeed.Parse(await File.ReadAllTextAsync(path, cancellationToken));
            if (await store.SeedIfEmptyAsync(rows, cancellationToken))
            {
                provider.Invalidate();
                logger.LogInformation("Zasilono tabelę parametrów: {Count} wierszy.", rows.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Nie udało się zasilić parametrów z fixtures/parameters.seed.json.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
