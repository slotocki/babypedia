using BabyRadnik.Api;
using BabyRadnik.Api.Endpoints;
using BabyRadnik.Domain.Amounts;
using BabyRadnik.Domain.Basis;
using BabyRadnik.Domain.Catalog;
using BabyRadnik.Domain.Deadlines;
using BabyRadnik.Domain.Eligibility;
using BabyRadnik.Domain.Periods;
using BabyRadnik.Domain.Scenarios;
using BabyRadnik.Infrastructure.Assistant;
using BabyRadnik.Infrastructure.Common;
using BabyRadnik.Infrastructure.Parameters;
using BabyRadnik.Infrastructure.Persistence;
using BabyRadnik.Infrastructure.Simulation;
using BabyRadnik.Infrastructure.Submissions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    if (ctx.Exception is BadHttpRequestException bad)
    {
        ctx.ProblemDetails.Title = "Nieprawidłowe dane wejściowe";
        ctx.ProblemDetails.Detail = bad.InnerException?.Message ?? bad.Message;
    }
});
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.ConfigureHttpJsonOptions(o => ApiJson.Configure(o.SerializerOptions));
builder.Services.AddSingleton(RepoPaths.Resolve(configuration, builder.Environment.ContentRootPath));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services
    .AddPeriods()
    .AddAmounts()
    .AddEligibility()
    .AddBasis()
    .AddCatalog()
    .AddDeadlines()
    .AddScenarios();

builder.Services
    .AddPersistence(configuration)
    .AddParameters(configuration)
    .AddSimulation(configuration)
    .AddAdminIntegrationSettings()
    .AddSubmissions(configuration)
    .AddAssistant(configuration);

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseCors();

await app.InitializePersistenceAsync();

app.MapHealthEndpoints();
app.MapAssessmentEndpoints();
app.MapCompareEndpoints();
app.MapParametersEndpoints();
app.MapSimulationEndpoints();
app.MapSubmissionsEndpoints();
app.MapAssistantEndpoints();

app.Run();

public partial class Program;
