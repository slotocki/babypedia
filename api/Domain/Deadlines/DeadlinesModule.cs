using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Domain.Deadlines;

public static class DeadlinesModule
{
    public static IServiceCollection AddDeadlines(this IServiceCollection services)
    {
        return services;
    }
}
