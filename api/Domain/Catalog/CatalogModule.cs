using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Domain.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        return services;
    }
}
