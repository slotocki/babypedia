using BabyRadnik.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Domain.Eligibility;

public static class EligibilityModule
{
    public static IServiceCollection AddEligibility(this IServiceCollection services)
    {
        services.AddScoped<IEligibilityService>(provider => new EligibilityService(provider.GetRequiredService<IParameterProvider>()));
        return services;
    }
}
