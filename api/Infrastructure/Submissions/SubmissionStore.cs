using BabyRadnik.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Infrastructure.Submissions;

/// <summary>Wpis listy „Moje zgłoszenia”. Bez PESEL i bez danych osobowych: samo streszczenie, numer i skrót identyfikatora sesji.</summary>
public sealed class SubmissionRow
{
    public Guid Id { get; init; }
    public string Number { get; init; } = "";
    public string Type { get; init; } = "";
    public string Recipient { get; init; } = "";
    public DateTimeOffset SubmittedAt { get; init; }
    public string Status { get; init; } = SubmissionStatus.Zlozone;
    public string Summary { get; init; } = "";
    public string SessionHash { get; init; } = "";
}

public static class SubmissionStatus
{
    public const string Zlozone = "zlozone";
}

internal sealed class SubmissionRowConfiguration : IEntityTypeConfiguration<SubmissionRow>
{
    public void Configure(EntityTypeBuilder<SubmissionRow> b)
    {
        b.ToTable("submissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(60).IsRequired();
        b.Property(x => x.Type).HasMaxLength(30).IsRequired();
        b.Property(x => x.Recipient).HasMaxLength(30).IsRequired();
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(300).IsRequired();
        b.Property(x => x.SessionHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.SessionHash, x.SubmittedAt });
        b.HasIndex(x => x.Number);
    }
}

public interface ISubmissionStore
{
    Task AddAsync(SubmissionRow row, CancellationToken ct);
    Task<IReadOnlyList<SubmissionRow>> ListAsync(string sessionHash, int limit, CancellationToken ct);
    Task<SubmissionRow?> FindAsync(Guid id, string sessionHash, CancellationToken ct);
}

internal sealed class EfSubmissionStore(IServiceScopeFactory scopes) : ISubmissionStore
{
    public async Task AddAsync(SubmissionRow row, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>();
        db.Set<SubmissionRow>().Add(row);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SubmissionRow>> ListAsync(string sessionHash, int limit, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>()
            .Set<SubmissionRow>().AsNoTracking()
            .Where(x => x.SessionHash == sessionHash)
            .OrderByDescending(x => x.SubmittedAt).ThenByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<SubmissionRow?> FindAsync(Guid id, string sessionHash, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>()
            .Set<SubmissionRow>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.SessionHash == sessionHash, ct);
    }
}
