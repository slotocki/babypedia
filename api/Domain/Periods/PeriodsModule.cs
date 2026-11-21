using BabyRadnik.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Domain.Periods;

public static class PeriodsModule
{
    public static IServiceCollection AddPeriods(this IServiceCollection services)
    {
        services.AddScoped<IPeriodsService>(provider => new PeriodsService(provider.GetRequiredService<IParameterProvider>()));
        return services;
    }
}
