using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Common;

namespace BabyRadnik.Api.Endpoints;

/// <summary>Właściciel: gałąź be/catalog. Dopóki nie ma silnika, zwraca atrapę z contracts/examples/assessment.example.json.</summary>
public static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/assessment", (AssessmentRequest request, IServiceProvider services, RepoPaths paths) =>
        {
            var engine = services.GetService<IAssessmentEngine>();
            return Results.Ok(engine is null ? LoadExample(paths) : engine.Assess(request));
        }).WithTags("ocena");
        return app;
    }

    public static AssessmentResult LoadExample(RepoPaths paths)
    {
        var json = File.ReadAllText(Path.Combine(paths.Contracts, "examples", "assessment.example.json"));
        return JsonSerializer.Deserialize<AssessmentResult>(json, ApiJson.Options)
            ?? throw new InvalidOperationException("Pusty plik atrapy assessment.example.json.");
    }
}
