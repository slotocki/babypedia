using BabyRadnik.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Domain.Basis;

public static class BasisModule
{
    public static IServiceCollection AddBasis(this IServiceCollection services)
    {
        services.AddScoped<IBasisService>(provider => new BasisService(provider.GetRequiredService<IParameterProvider>()));
        return services;
    }
}
