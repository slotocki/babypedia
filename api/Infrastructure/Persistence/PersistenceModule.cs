using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BabyRadnik.Infrastructure.Persistence;

public static class PersistenceModule
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Bez ConnectionStrings__Default (albo z DATABASE=memory) API działa na bazie w pamięci procesu:
        // demo uruchamia się samym `dotnet run`, bez Dockera i PostgreSQL. Zmiany z panelu /admin i zgłoszenia
        // giną po restarcie. Docker Compose i serwer zawsze podają connection string, więc używają PostgreSQL.
        var connectionString = configuration.GetConnectionString("Default");
        if (UsesMemory(configuration))
            services.AddDbContext<BabyRadnikDbContext>(options => options.UseInMemoryDatabase("babyradnik"));
        else
            services.AddDbContext<BabyRadnikDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }

    public static bool UsesMemory(IConfiguration configuration) =>
        string.Equals(configuration["DATABASE"], "memory", StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(configuration.GetConnectionString("Default"));

    /// <summary>Do czasu wprowadzenia migracji schemat tworzy EnsureCreated; zmiana encji = `docker compose down -v`.</summary>
    public static async Task InitializePersistenceAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BabyRadnikDbContext>();
        try
        {
            await db.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Baza danych niedostępna przy starcie; /health pokaże stan.");
        }
        if (db.Database.IsInMemory())
        {
            app.Logger.LogWarning("Brak ConnectionStrings__Default: baza w pamięci procesu (tryb demo). Dane giną po restarcie.");
        }
    }
}
