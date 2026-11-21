using System.Net;
using System.Net.Http.Json;
using System.Text;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Assistant;
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
    public async Task Assistant_returns_sources_without_a_model()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/assistant/ask", new { question = "Czym jest urlop rodzicielski?" },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var answer = await response.Content.ReadFromJsonAsync<AssistantAnswer>(TestContext.Current.CancellationToken);
        Assert.NotNull(answer);
        Assert.Equal("search", answer.Mode);
        Assert.Contains(answer.Citations.Take(3),
            citation => citation.Document == "Kodeks pracy" && citation.Article.StartsWith("Art. 182¹"));
    }

    [Theory]
    [InlineData("Ile dostanę przy pensji 6000?", "calculator")]
    [InlineData("Jaka jest jutro pogoda?", "no_results")]
    public async Task Assistant_handles_questions_outside_general_legal_search(string question, string mode)
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var response = await client.PostAsJsonAsync("/api/assistant/ask", new { question }, ct);
        response.EnsureSuccessStatusCode();
        var answer = await response.Content.ReadFromJsonAsync<AssistantAnswer>(ct);
        Assert.NotNull(answer);
        Assert.Equal(mode, answer.Mode);
        Assert.Empty(answer.Citations);
    }

    [Theory]
    [InlineData("Mój PESEL to 00000000000")]
    [InlineData("Mój e-mail to demo@example.com")]
    public async Task Assistant_rejects_identifiers(string question)
    {
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var response = await client.PostAsJsonAsync("/api/assistant/ask", new { question }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
