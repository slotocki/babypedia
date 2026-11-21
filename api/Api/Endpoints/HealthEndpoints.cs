using BabyRadnik.Infrastructure.Persistence;

namespace BabyRadnik.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (BabyRadnikDbContext db, CancellationToken ct) =>
        {
            bool database;
            try
            {
                database = await db.Database.CanConnectAsync(ct);
            }
            catch (Exception)
            {
                database = false;
            }

            var body = new { status = database ? "ok" : "degraded", api = "ok", database = database ? "ok" : "unavailable" };
            return database ? Results.Ok(body) : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
        }).WithTags("stan");
        return app;
    }
}
