using BabyRadnik.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Infrastructure.Parameters;

public interface IParameterStore
{
    IReadOnlyList<ParameterRow> LoadAll();
    Task<bool> SeedIfEmptyAsync(IReadOnlyList<ParameterRow> rows, CancellationToken ct);
    Task AddAsync(ParameterRow row, ParameterHistoryRow history, CancellationToken ct);
    Task<IReadOnlyList<ParameterHistoryRow>> LoadHistoryAsync(CancellationToken ct);
}

public interface ISettingsStore
{
    Task<string?> GetAsync(string name, CancellationToken ct);
    Task SetAsync(string name, string json, DateTimeOffset now, CancellationToken ct);
}

internal sealed class EfParameterStore(IServiceScopeFactory scopes) : IParameterStore
{
    public IReadOnlyList<ParameterRow> LoadAll()
    {
        using var scope = scopes.CreateScope();
        return scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>()
            .Set<ParameterRow>().AsNoTracking().ToList();
    }

    public async Task<bool> SeedIfEmptyAsync(IReadOnlyList<ParameterRow> rows, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>();
        if (await db.Set<ParameterRow>().AnyAsync(ct))
        {
            return false;
        }

        db.Set<ParameterRow>().AddRange(rows);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AddAsync(ParameterRow row, ParameterHistoryRow history, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>();
        db.Set<ParameterRow>().Add(row);
        db.Set<ParameterHistoryRow>().Add(history);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ParameterHistoryRow>> LoadHistoryAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>()
            .Set<ParameterHistoryRow>().AsNoTracking()
            .OrderByDescending(x => x.ChangedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
    }
}

internal sealed class EfSettingsStore(IServiceScopeFactory scopes) : ISettingsStore
{
    public async Task<string?> GetAsync(string name, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>()
            .Set<AdminSettingRow>().AsNoTracking().FirstOrDefaultAsync(x => x.Name == name, ct);
        return row?.Json;
    }

    public async Task SetAsync(string name, string json, DateTimeOffset now, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>();
        var row = await db.Set<AdminSettingRow>().FirstOrDefaultAsync(x => x.Name == name, ct);
        if (row is null)
        {
            db.Set<AdminSettingRow>().Add(new AdminSettingRow { Name = name, Json = json, UpdatedAt = now });
        }
        else
        {
            row.Json = json;
            row.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }
}
