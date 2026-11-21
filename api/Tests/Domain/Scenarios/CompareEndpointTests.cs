using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static BabyRadnik.Tests.Domain.Scenarios.FakeEngine;

namespace BabyRadnik.Tests.Domain.Scenarios;

public sealed class CompareEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Body = """
        {
          "facts": {
            "sprawa": { "zdarzenie.rodzaj": { "value": "ciaza", "source": "user" }, "zdarzenie.data": { "value": "2027-03-20", "source": "user" } },
            "wnioskodawca": {
              "osoba.rola": { "value": "matka", "source": "user" },
              "ubezpieczenie.tytuly": { "value": [ { "rodzajTytulu": "pracownik", "od": "2025-01-01", "do": null, "chorobowe": "obowiazkowe" } ], "source": "zus" }
            }
          },
          "scenarios": [ { "id": "odchodze_z_pracy", "params": { "data": "2026-11-15" } } ],
          "asOf": "2026-11-21"
        }
        """;

    private HttpClient ClientWith(FakeEngine engine) =>
        factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IAssessmentEngine>(engine)))).CreateClient();

    [Fact]
    public async Task Compare_returns_base_variant_and_diff()
    {
        var engine = new FakeEngine(
            Result(Benefit(BenefitId.Macierzynski, BenefitStatus.Przysluguje, weeks: 20, total: 10000m)),
            Result(Benefit(BenefitId.Macierzynski, BenefitStatus.NiePrzysluguje)));
        using var client = ClientWith(engine);

        var response = await client.PostAsync("/api/assessment/compare", new StringContent(Body, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var diff = doc.RootElement.GetProperty("diff")[0];
        Assert.Equal("macierzynski", diff.GetProperty("id").GetString());
        Assert.Equal("przysługuje", diff.GetProperty("statusBase").GetString());
        Assert.Equal("nie_przysługuje", diff.GetProperty("statusVariant").GetString());
        Assert.Equal(-20m, diff.GetProperty("weeksDelta").GetDecimal());
        Assert.Equal(-10000m, diff.GetProperty("amountDelta").GetDecimal());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("validationErrors").ValueKind);
        Assert.Equal("fake", doc.RootElement.GetProperty("base").GetProperty("rulesetVersion").GetString());
        Assert.Equal("fake", doc.RootElement.GetProperty("variant").GetProperty("rulesetVersion").GetString());
    }

    [Fact]
    public async Task Compare_reports_invalid_scenario_params_in_validation_errors()
    {
        using var client = ClientWith(new FakeEngine(Result()));
        var body = Body.Replace("""{ "data": "2026-11-15" }""", """{ "data": "nie-data" }""");

        var response = await client.PostAsync("/api/assessment/compare", new StringContent(body, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CompareResult>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Contains(result.ValidationErrors, e => e.Code == "scenariusz_zly_parametr");
        Assert.Empty(result.Diff);
    }

    [Fact]
    public async Task Compare_rejects_unknown_scenario_id_with_problem_details()
    {
        using var client = ClientWith(new FakeEngine(Result()));
        var body = Body.Replace("odchodze_z_pracy", "nieznany_scenariusz");

        var response = await client.PostAsync("/api/assessment/compare", new StringContent(body, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
