using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Common;

namespace BabyRadnik.Api.Endpoints;

/// <summary>Właściciel: gałąź be/scenarios.</summary>
public static class CompareEndpoints
{
    public static IEndpointRouteBuilder MapCompareEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/assessment/compare", (CompareRequest request, IServiceProvider services) =>
        {
            if (services.GetService<IAssessmentEngine>() is null)
            {
                return EndpointStubs.NotImplemented("be/catalog");
            }

            var comparer = services.GetService<IScenarioComparer>();
            return comparer is null ? EndpointStubs.NotImplemented("be/scenarios") : Results.Ok(comparer.Compare(request));
        }).WithTags("ocena");
        return app;
    }
}
