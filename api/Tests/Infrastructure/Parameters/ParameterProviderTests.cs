using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Parameters;

namespace BabyRadnik.Tests.Infrastructure.Parameters;

public sealed class ParameterProviderTests
{
    private const string Rate = "stawka_macierzynski";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private static (InMemoryParameterStore Store, CachedParameterProvider Provider, ParameterAdminService Admin) Create()
    {
        var store = new InMemoryParameterStore();
        store.SeedIfEmptyAsync(ParameterSeed.Parse(File.ReadAllText(Path.Combine(TestPaths.Fixtures, "parameters.seed.json"))), Ct)
            .GetAwaiter().GetResult();
        var provider = new CachedParameterProvider(store);
        return (store, provider, new ParameterAdminService(store, provider, TimeProvider.System));
    }

    private static JsonElement Json(decimal value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task Rate_valid_from_2027_does_not_change_2026_but_changes_2027()
    {
        var (_, provider, admin) = Create();
        var before = provider.GetDecimal(Rate, new DateOnly(2026, 6, 1));

        var outcome = await admin.ChangeAsync(new ParameterChangeRequest(Rate, Json(0.75m), "2027-01-01", "ustawa testowa", null), Ct);

        Assert.True(outcome.Succeeded, string.Join("; ", outcome.Errors.Values.SelectMany(v => v)));
        Assert.Equal(before, provider.GetDecimal(Rate, new DateOnly(2026, 6, 1)));
        Assert.Equal(before, provider.GetDecimal(Rate, new DateOnly(2026, 12, 31)));
        Assert.Equal(0.75m, provider.GetDecimal(Rate, new DateOnly(2027, 1, 1)));
        Assert.Equal(0.75m, provider.GetDecimal(Rate, new DateOnly(2027, 8, 15)));
    }

    [Fact]
    public void Selects_row_with_greatest_valid_from_not_after_date()
    {
        var store = new InMemoryParameterStore([
            TestRows.Number("k", 1m, "2000-01-01", id: 1),
            TestRows.Number("k", 3m, "2028-01-01", id: 2),
            TestRows.Number("k", 2m, "2026-01-01", id: 3),
        ]);
        var provider = new CachedParameterProvider(store);

        Assert.Equal(1m, provider.GetDecimal("k", new DateOnly(2025, 12, 31)));
        Assert.Equal(2m, provider.GetDecimal("k", new DateOnly(2026, 1, 1)));
        Assert.Equal(2m, provider.GetDecimal("k", new DateOnly(2027, 12, 31)));
        Assert.Equal(3m, provider.GetDecimal("k", new DateOnly(2028, 1, 1)));
    }

    [Fact]
    public void Missing_key_or_date_before_first_row_throws()
    {
        var provider = new CachedParameterProvider(new InMemoryParameterStore([TestRows.Number("k", 1m, "2026-01-01", id: 1)]));

        Assert.Throws<ParameterNotFoundException>(() => provider.Get("k", new DateOnly(2025, 12, 31)));
        Assert.Throws<ParameterNotFoundException>(() => provider.Get("inny", new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void Same_valid_from_later_entry_wins()
    {
        var store = new InMemoryParameterStore([
            TestRows.Number("k", 1m, "2026-01-01", id: 1),
            TestRows.Number("k", 2m, "2026-01-01", id: 2),
        ]);

        Assert.Equal(2m, new CachedParameterProvider(store).GetDecimal("k", new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void Snapshot_is_cached_until_invalidated()
    {
        var store = new InMemoryParameterStore([TestRows.Number("k", 1m, "2026-01-01", id: 1)]);
        var provider = new CachedParameterProvider(store);

        provider.Get("k", new DateOnly(2026, 6, 1));
        provider.Get("k", new DateOnly(2026, 7, 1));
        Assert.Equal(1, store.LoadCount);

        provider.Invalidate();
        provider.Get("k", new DateOnly(2026, 6, 1));
        Assert.Equal(2, store.LoadCount);
    }

    [Fact]
    public async Task Change_adds_row_keeps_old_one_and_records_history()
    {
        var (store, _, admin) = Create();
        var rowsBefore = store.LoadAll().Count;

        await admin.ChangeAsync(new ParameterChangeRequest(Rate, Json(0.75m), "2027-01-01", null, "nowelizacja"), Ct);

        Assert.Equal(rowsBefore + 1, store.LoadAll().Count);
        var entry = Assert.Single(await admin.HistoryAsync(Ct));
        Assert.Equal(Rate, entry.Key);
        Assert.Equal(0.75m, entry.Value);
        Assert.Equal(1m, entry.PreviousValue);
        Assert.Equal(new DateOnly(2027, 1, 1), entry.ValidFrom);
        Assert.Equal("nowelizacja", entry.Reason);
    }

    [Fact]
    public void Seed_loads_all_rows_from_fixture()
    {
        var rows = ParameterSeed.Parse(File.ReadAllText(Path.Combine(TestPaths.Fixtures, "parameters.seed.json")));

        Assert.Contains(rows, r => r.Key == Rate && r.ValueNumber == 1m);
        Assert.Contains(rows, r => r.ValueText is not null);
        Assert.Contains(rows, r => r.ValueNumber is null && r.ValueText is null);
    }
}
