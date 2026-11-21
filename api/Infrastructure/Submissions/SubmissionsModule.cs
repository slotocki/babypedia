using BabyRadnik.Infrastructure.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Infrastructure.Submissions;

public static class SubmissionsModule
{
    private const string Branch = "be/submissions";

    public static IServiceCollection AddSubmissions(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSubmissionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/submissions").WithTags("zgłoszenia");
        group.MapPost("", () => EndpointStubs.NotImplemented(Branch));
        group.MapGet("", () => EndpointStubs.NotImplemented(Branch));
        return app;
    }
}
