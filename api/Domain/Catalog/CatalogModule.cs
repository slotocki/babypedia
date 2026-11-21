using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Domain.Catalog;

public static class CatalogModule
{
    /// <summary>
    /// Rejestruje silnik katalogu jako <see cref="IAssessmentEngine"/>. Ogniwa potoku podstawa → kwoty biorą moduły Basis i Amounts,
    /// jeśli są zarejestrowane (<see cref="IBasisService"/>, <see cref="IAmountsService"/>), a inaczej mają puste implementacje.
    /// Ustawienia katalogu (<see cref="ICatalogSettings"/>) domyślnie włączają wszystko; panel administratora podaje je per żądanie.
    /// </summary>
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddDeadlines();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICatalogSettings, AllEnabledCatalogSettings>();
        services.TryAddScoped<IBasisStep>(p =>
            p.GetService<IBasisService>() is { } basis ? new BasisServiceStep(basis) : new NoBasisStep());
        services.TryAddScoped<IAmountsStep>(p =>
            p.GetService<IAmountsService>() is { } amounts ? new AmountsServiceStep(amounts) : new NoAmountsStep());
        services.TryAddScoped<BenefitPipeline>();
        services.AddScoped<AssessmentEngine>();
        services.AddScoped<ICatalogAssessmentEngine>(p => p.GetRequiredService<AssessmentEngine>());
        services.AddScoped<IAssessmentEngine>(p => p.GetRequiredService<AssessmentEngine>());
        return services;
    }
}
