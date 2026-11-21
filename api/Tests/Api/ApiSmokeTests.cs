using System.Net;
using System.Net.Http.Json;
using System.Text;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BabyRadnik.Tests.Api;

public sealed class ApiSmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static StringContent ExampleRequest() => new(
        File.ReadAllText(Path.Combine(TestPaths.Contracts, "examples", "assessment-request.example.json")),
        Encoding.UTF8,
        "application/json");

    [Fact]
    public async Task Assessment_rejects_unknown_fact_key_as_problem_details()
    {
        using var client = factory.CreateClient();
        var body = new StringContent(
            """{"facts":{"sprawa":{"nieznany.klucz":{"value":1,"source":"user"}},"wnioskodawca":{}}}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/assessment", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/sim/personas")]
    [InlineData("/api/cases")]
    [InlineData("/api/organizations")]
    public async Task Simulation_endpoints_respond(string path)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Submissions_require_a_session()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/submissions", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Assistant_is_disabled_by_default()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/assistant/ask", new { question = "Czym jest urlop rodzicielski?" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
