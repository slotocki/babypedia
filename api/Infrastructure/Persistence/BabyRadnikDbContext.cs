using Microsoft.EntityFrameworkCore;

namespace BabyRadnik.Infrastructure.Persistence;

/// <summary>
/// Jeden kontekst dla całego monolitu. Moduły nie edytują tego pliku: każdy dodaje encje
/// przez własne klasy IEntityTypeConfiguration&lt;T&gt; w swoim katalogu (wykrywane automatycznie).
/// </summary>
public sealed class BabyRadnikDbContext(DbContextOptions<BabyRadnikDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BabyRadnikDbContext).Assembly);
    }
}
