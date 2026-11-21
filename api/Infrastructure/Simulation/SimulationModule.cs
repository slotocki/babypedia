using BabyRadnik.Infrastructure.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Infrastructure.Simulation;

public static class SimulationModule
{
    private const string Branch = "be/sim";

    public static IServiceCollection AddSimulation(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        var sim = app.MapGroup("/api/sim").WithTags("symulacja");
        sim.MapPost("/login", () => EndpointStubs.NotImplemented(Branch));
        sim.MapGet("/personas", () => EndpointStubs.NotImplemented(Branch));
        sim.MapGet("/me/facts", () => EndpointStubs.NotImplemented(Branch));
        sim.MapGet("/me/zus", () => EndpointStubs.NotImplemented(Branch));
        sim.MapGet("/me/debt-certificate", () => EndpointStubs.NotImplemented("be/submissions"));

        app.MapGet("/api/cases", () => EndpointStubs.NotImplemented(Branch)).WithTags("sprawy");
        app.MapGet("/api/cases/by-hash/{sha256}", (string sha256) => EndpointStubs.NotImplemented(Branch)).WithTags("sprawy");
        app.MapGet("/api/organizations", (string? situation) => EndpointStubs.NotImplemented(Branch)).WithTags("wsparcie");
        return app;
    }
}
