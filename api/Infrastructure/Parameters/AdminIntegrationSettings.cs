using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BabyRadnik.Infrastructure.Parameters;

/// <summary>
/// Przełączniki pól integracji z panelu administratora (`/api/admin/integrations`) dla symulatora.
/// Wyłączone pole znika z `GET /api/sim/me/facts`, więc silnik zgłasza je jako pytanie (instrukcja 6.2).
/// Krótki cache, żeby nie czytać bazy przy każdym polu; błąd odczytu → wszystkie pola włączone.
/// </summary>
public sealed class AdminIntegrationSettings(AdminSettingsService admin, TimeProvider time, ILogger<AdminIntegrationSettings> logger) : IIntegrationSettings
{
    public static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(5);

    private readonly Lock gate = new();
    private HashSet<string> disabled = [];
    private DateTimeOffset loadedAt = DateTimeOffset.MinValue;

    public static string Key(string source, string factKey) => $"{source}/{factKey}";

    public static string SourceName(FactSource source) => JsonSerializer.Serialize(source).Trim('"');

    public bool IsFieldEnabled(FactSource source, string factKey) => !Disabled().Contains(Key(SourceName(source), factKey));

    private HashSet<string> Disabled()
    {
        var now = time.GetUtcNow();
        lock (gate)
        {
            if (now - loadedAt < CacheFor)
            {
                return disabled;
            }
        }

        HashSet<string> fresh;
        try
        {
            var fields = admin.GetIntegrationsAsync(CancellationToken.None).GetAwaiter().GetResult();
            fresh = [.. fields.Where(f => f.Wlaczone == false && f.Source is not null && f.Fact is not null).Select(f => Key(f.Source!, f.Fact!))];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nie udało się odczytać ustawień integracji; wszystkie pola włączone.");
            fresh = [];
        }

        lock (gate)
        {
            disabled = fresh;
            loadedAt = now;
            return disabled;
        }
    }
}

public static class AdminIntegrationSettingsModule
{
    /// <summary>Wywołać po <c>AddSimulation</c> — zastępuje domyślne „wszystko włączone”.</summary>
    public static IServiceCollection AddAdminIntegrationSettings(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IIntegrationSettings, AdminIntegrationSettings>());
        return services;
    }
}
