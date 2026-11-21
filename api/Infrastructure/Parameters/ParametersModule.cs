using BabyRadnik.Infrastructure.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Infrastructure.Parameters;

public static class ParametersModule
{
    private const string Branch = "be/params";

    public static IServiceCollection AddParameters(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapParametersEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").AddEndpointFilter<AdminPasswordFilter>().WithTags("admin");
        admin.MapGet("/parameters", () => EndpointStubs.NotImplemented(Branch));
        admin.MapPut("/parameters", () => EndpointStubs.NotImplemented(Branch));
        admin.MapGet("/parameters/history", () => EndpointStubs.NotImplemented(Branch));
        admin.MapGet("/catalog", () => EndpointStubs.NotImplemented(Branch));
        admin.MapPut("/catalog", () => EndpointStubs.NotImplemented(Branch));
        admin.MapGet("/integrations", () => EndpointStubs.NotImplemented(Branch));
        admin.MapPut("/integrations", () => EndpointStubs.NotImplemented(Branch));
        admin.MapGet("/organizations", () => EndpointStubs.NotImplemented(Branch));
        admin.MapPut("/organizations", () => EndpointStubs.NotImplemented(Branch));
        return app;
    }
}
