using BabyRadnik.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Domain.Amounts;

public static class AmountsModule
{
    public static IServiceCollection AddAmounts(this IServiceCollection services)
    {
        services.AddScoped<IAmountsService>(provider => new AmountsService(provider.GetRequiredService<IParameterProvider>()));
        services.AddScoped(provider => new BasisAdjuster(provider.GetRequiredService<IParameterProvider>()));
        return services;
    }
}
