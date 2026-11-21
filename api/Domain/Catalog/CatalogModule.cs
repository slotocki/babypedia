using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Domain.Catalog;

public static class CatalogModule
{
    /// <summary>
    /// Rejestruje silnik katalogu jako <see cref="IAssessmentEngine"/>. Ogniwa potoku (<see cref="IBasisStep"/>, <see cref="IAmountsStep"/>)
    /// i ustawienia katalogu (<see cref="ICatalogSettings"/>) mają domyślne, puste implementacje (TryAdd) — moduły Basis/Amounts
    /// i panel administratora podmieniają je własną rejestracją.
    /// </summary>
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddDeadlines();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICatalogSettings, AllEnabledCatalogSettings>();
        services.TryAddSingleton<IBasisStep, NoBasisStep>();
        services.TryAddSingleton<IAmountsStep, NoAmountsStep>();
        services.TryAddScoped<BenefitPipeline>();
        services.AddScoped<AssessmentEngine>();
        services.AddScoped<ICatalogAssessmentEngine>(p => p.GetRequiredService<AssessmentEngine>());
        services.AddScoped<IAssessmentEngine>(p => p.GetRequiredService<AssessmentEngine>());
        return services;
    }
}
