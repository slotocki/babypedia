using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Domain.Deadlines;

public static class DeadlinesModule
{
    public static IServiceCollection AddDeadlines(this IServiceCollection services)
    {
        services.TryAddSingleton<IDeadlineService, DeadlineService>();
        return services;
    }
}
