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
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=babyradnik;Username=babyradnik;Password=babyradnik";
        services.AddDbContext<BabyRadnikDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }

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
    }
}
