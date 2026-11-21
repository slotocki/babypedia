using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BabyRadnik.Infrastructure.Simulation;

public sealed record LoginRequest(string? PersonaId);

public sealed record LoginResponse(string SessionId, Identity Identity, bool Simulated);

public sealed record PersonaListItem(string Id, string Opis, string Imie, string? CaseId);

public sealed record ZusBalance(DateOnly NaDzien, decimal Kwota);

public sealed record ZusAccountResponse(
    bool Simulated,
    IReadOnlyList<InsuranceTitle> Tytuly,
    IReadOnlyList<ContributionBase> PodstawySkladek,
    IReadOnlyList<InsuranceBreak> Przerwy,
    IReadOnlyList<ReceivedBenefit> PobraneSwiadczenia,
    IReadOnlyList<SickLeave> Zwolnienia,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] ZusBalance? Saldo);

public static partial class SimulationModule
{
    public const string SessionHeader = "X-Sim-Session";
    public const string SimulationHeader = "X-Simulation";

    public static IServiceCollection AddSimulation(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IIntegrationSettings, AllFieldsEnabledSettings>();
        services.AddSingleton(sp => PersonaStore.Load(sp.GetRequiredService<RepoPaths>(), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Simulation")));
        services.AddSingleton<SessionStore>();
        services.AddSingleton(sp => CaseCatalog.Load(sp.GetRequiredService<RepoPaths>(), sp.GetRequiredService<PersonaStore>(), sp.GetRequiredService<ILoggerFactory>().CreateLogger("Simulation")));
        services.AddSingleton<IIdentityProvider, SimulatedIdentityProvider>();
        services.AddSingleton<IInsuranceRegistry, SimulatedInsuranceRegistry>();
        services.AddSingleton<IContributionAccount, SimulatedContributionAccount>();
        services.AddSingleton<IMedicalCertificates, SimulatedMedicalCertificates>();
        services.AddSingleton<ICivilRegistry, SimulatedCivilRegistry>();
        services.AddSingleton<SimulatedFactsAssembler>();
        return services;
    }

    public static IEndpointRouteBuilder MapSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        var sim = app.MapGroup("/api/sim").WithTags("symulacja");
        sim.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers[SimulationHeader] = "true";
            return await next(context);
        });

        sim.MapPost("/login", LoginAsync);
        sim.MapGet("/personas", ListPersonas);
        sim.MapGet("/me/facts", GetFactsAsync);
        sim.MapGet("/me/zus", GetZusAsync);

        app.MapGet("/api/cases", (CaseCatalog cases) => Json(cases.List())).WithTags("sprawy");
        app.MapGet("/api/cases/by-hash/{sha256}", FindCaseByHash).WithTags("sprawy");
        return app;
    }

    private static async Task<IResult> LoginAsync(LoginRequest? request, IIdentityProvider identity, SessionStore sessions)
    {
        if (string.IsNullOrWhiteSpace(request?.PersonaId) || request.PersonaId.Length > 100)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Nieprawidłowe dane wejściowe",
                detail: "Podaj personaId wybranej osoby.");
        }

        var who = await identity.LoginAsync(request.PersonaId);
        if (who is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Nie ma takiej osoby",
                detail: "Wybierz osobę z listy GET /api/sim/personas.");
        }

        return Json(new LoginResponse(sessions.Create(request.PersonaId), who, true));
    }

    private static IResult ListPersonas(PersonaStore store) =>
        Json(store.All.Select(p => new PersonaListItem(p.Id, p.Opis, p.Tozsamosc.Imie, p.Sprawa)).ToList());

    private static async Task<IResult> GetFactsAsync(HttpContext http, SessionStore sessions, SimulatedFactsAssembler facts, CancellationToken ct)
    {
        var personaId = sessions.FindPersonaId(http.Request.Headers[SessionHeader].FirstOrDefault());
        if (personaId is null)
        {
            return Unauthorized();
        }

        var set = await facts.AssembleAsync(personaId, ct);
        return set is null ? Unauthorized() : Json(set);
    }

    private static async Task<IResult> GetZusAsync(
        HttpContext http,
        SessionStore sessions,
        IInsuranceRegistry insurance,
        IMedicalCertificates certificates,
        IContributionAccount contributions,
        TimeProvider time,
        CancellationToken ct)
    {
        var personaId = sessions.FindPersonaId(http.Request.Headers[SessionHeader].FirstOrDefault());
        if (personaId is null)
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var account = await insurance.GetAccountAsync(personaId, ct);
        var leaves = await certificates.GetSickLeavesAsync(personaId, ct);
        var balance = await contributions.GetBalanceAsync(personaId, today, ct);
        return Json(new ZusAccountResponse(
            true,
            account.Tytuly,
            account.PodstawySkladek,
            account.Przerwy,
            account.PobraneSwiadczenia,
            leaves,
            balance is null ? null : new ZusBalance(balance.NaDzien, balance.Kwota)));
    }

    private static IResult FindCaseByHash(string sha256, string? fileName, int? pages, CaseCatalog cases)
    {
        var hash = sha256.ToLowerInvariant();
        if (!HashPattern().IsMatch(hash) || fileName is { Length: > 260 } || pages is < 1 or > 100_000)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Nieprawidłowe dane wejściowe",
                detail: "sha256 to 64 znaki szesnastkowe; fileName do 260 znaków; pages liczba dodatnia.");
        }

        var found = cases.FindByHash(hash, fileName, pages);
        return found is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Nie znam tej sprawy",
                detail: "Plik nie pasuje do żadnej sprawy testowej. Wybierz sprawę z listy albo wprowadź dane ręcznie.")
            : Json(found);
    }

    private static IResult Json<T>(T value) => Results.Json(value, SimJson.Options);

    private static IResult Unauthorized() => Results.Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Brak sesji symulacji",
        detail: $"Zaloguj się (POST /api/sim/login) i przekaż nagłówek {SessionHeader}.");

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex HashPattern();
}
