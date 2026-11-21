using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Infrastructure.Parameters;

public static class ParametersModule
{
    private static readonly JsonSerializerOptions BodyOptions = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddParameters(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IParameterStore, EfParameterStore>();
        services.AddSingleton<ISettingsStore, EfSettingsStore>();
        services.AddSingleton<CachedParameterProvider>();
        services.AddSingleton<IParameterProvider>(sp => sp.GetRequiredService<CachedParameterProvider>());
        services.AddSingleton<ParameterAdminService>();
        services.AddSingleton<AdminSettingsService>();
        services.AddHostedService<ParameterSeeder>();
        return services;
    }

    public static IEndpointRouteBuilder MapParametersEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").AddEndpointFilter<AdminPasswordFilter>().WithTags("admin");

        admin.MapGet("/parameters", async (ParameterAdminService s) => Results.Ok(await s.ListAsync()));
        admin.MapPut("/parameters", async (HttpRequest request, ParameterAdminService s, CancellationToken ct) =>
        {
            var (body, error) = await ReadBodyAsync<ParameterChangeRequest>(request, ct);
            if (body is null)
            {
                return error!;
            }

            var outcome = await s.ChangeAsync(body, ct);
            return outcome.Succeeded ? Results.Ok(outcome.Parameter) : Invalid(outcome.Errors);
        });
        admin.MapGet("/parameters/history", async (ParameterAdminService s, CancellationToken ct) =>
            Results.Ok(await s.HistoryAsync(ct)));

        admin.MapGet("/catalog", async (AdminSettingsService s, CancellationToken ct) => Results.Ok(await s.GetCatalogAsync(ct)));
        admin.MapPut("/catalog", (HttpRequest request, AdminSettingsService s, CancellationToken ct) =>
            PutAsync<CatalogEntryDto>(request, ct, s.PutCatalogAsync));

        admin.MapGet("/integrations", async (AdminSettingsService s, CancellationToken ct) => Results.Ok(await s.GetIntegrationsAsync(ct)));
        admin.MapPut("/integrations", (HttpRequest request, AdminSettingsService s, CancellationToken ct) =>
            PutAsync<IntegrationFieldDto>(request, ct, s.PutIntegrationsAsync));

        admin.MapGet("/organizations", async (AdminSettingsService s, CancellationToken ct) => Results.Ok(await s.GetOrganizationsAsync(ct)));
        admin.MapPut("/organizations", (HttpRequest request, AdminSettingsService s, CancellationToken ct) =>
            PutAsync<OrganizationDto>(request, ct, s.PutOrganizationsAsync));

        return app;
    }

    private static async Task<IResult> PutAsync<T>(
        HttpRequest request, CancellationToken ct, Func<List<T>?, CancellationToken, Task<SettingsOutcome<T>>> put)
    {
        var (body, error) = await ReadBodyAsync<List<T>>(request, ct);
        if (body is null)
        {
            return error!;
        }

        var outcome = await put(body, ct);
        return outcome.Succeeded ? Results.Ok(outcome.Value) : Invalid(outcome.Errors);
    }

    private static IResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors.ToDictionary(e => e.Key, e => e.Value), title: "Nieprawidłowe dane wejściowe");

    private static async Task<(T? Body, IResult? Error)> ReadBodyAsync<T>(HttpRequest request, CancellationToken ct) where T : class
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<T>(request.Body, BodyOptions, ct);
            return body is null
                ? (null, Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Nieprawidłowe dane wejściowe", detail: "Brak treści żądania."))
                : (body, null);
        }
        catch (JsonException ex)
        {
            return (null, Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Nieprawidłowe dane wejściowe", detail: ex.Message));
        }
    }
}
