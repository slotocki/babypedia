using BabyRadnik.Infrastructure.Parameters;

namespace BabyRadnik.Tests.Infrastructure.Parameters;

internal sealed class InMemoryParameterStore(IEnumerable<ParameterRow>? initial = null) : IParameterStore
{
    private readonly List<ParameterRow> rows = [.. initial ?? []];
    private readonly List<ParameterHistoryRow> history = [];
    private long nextId = 1000;

    public int LoadCount { get; private set; }

    public IReadOnlyList<ParameterRow> LoadAll()
    {
        LoadCount++;
        return [.. rows];
    }

    public Task<bool> SeedIfEmptyAsync(IReadOnlyList<ParameterRow> seed, CancellationToken ct)
    {
        if (rows.Count > 0)
        {
            return Task.FromResult(false);
        }

        foreach (var r in seed)
        {
            r.Id = nextId++;
            rows.Add(r);
        }

        return Task.FromResult(true);
    }

    public Task AddAsync(ParameterRow row, ParameterHistoryRow entry, CancellationToken ct)
    {
        row.Id = nextId++;
        rows.Add(row);
        history.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ParameterHistoryRow>> LoadHistoryAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ParameterHistoryRow>>([.. history]);
}

internal static class TestRows
{
    public static ParameterRow Number(string key, decimal value, string validFrom, string unit = "ulamek", long id = 0) => new()
    {
        Id = id,
        Key = key,
        ValueNumber = value,
        Unit = unit,
        ValidFrom = DateOnly.Parse(validFrom),
        LegalBasis = "podstawa testowa",
    };
}
