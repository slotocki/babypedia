using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BabyRadnik.Infrastructure.Assistant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Tests.Api;

public sealed class AssistantModelTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("valid", "ai")]
    [InlineData("invented_quote", "search")]
    [InlineData("no_citation", "search")]
    [InlineData("money", "search")]
    [InlineData("provider_error", "search")]
    [InlineData("daily_limit", "search")]
    public async Task Only_supported_cited_answers_are_shown(string providerCase, string expectedMode)
    {
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASSISTANT_ENABLED", "true");
            builder.UseSetting("ASSISTANT_PROVIDER", "anthropic");
            builder.UseSetting("ANTHROPIC_API_KEY", "test-placeholder");
            builder.UseSetting("ASSISTANT_DAILY_LIMIT", providerCase == "daily_limit" ? "0" : "10");
            builder.ConfigureTestServices(services => services.ConfigureHttpClientDefaults(client =>
                client.ConfigurePrimaryHttpMessageHandler(() => new ClaudeStub(providerCase))));
        });

        using var client = app.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var response = await client.PostAsJsonAsync("/api/assistant/ask", new { question = "Urlop rodzicielski" }, ct);
        response.EnsureSuccessStatusCode();
        var answer = await response.Content.ReadFromJsonAsync<AssistantAnswer>(ct);
        Assert.NotNull(answer);
        Assert.Equal(expectedMode, answer.Mode);
        Assert.NotEmpty(answer.Citations);
        if (expectedMode == "ai")
        {
            Assert.Contains("[1]", answer.Answer);
            Assert.All(answer.Citations, citation => Assert.Contains(citation.Fragment, citation.ArticleText));
        }
    }

    private sealed class ClaudeStub(string providerCase) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.NotEqual("daily_limit", providerCase);
            Assert.Equal("https://api.anthropic.com/v1/messages", request.RequestUri?.AbsoluteUri);
            using var body = await request.Content!.ReadFromJsonAsync<JsonDocument>(ct);
            var blocks = body!.RootElement.GetProperty("messages")[0].GetProperty("content");
            var document = blocks[0];
            Assert.Equal("document", document.GetProperty("type").GetString());
            Assert.True(document.GetProperty("citations").GetProperty("enabled").GetBoolean());
            Assert.Equal("Urlop rodzicielski", blocks[blocks.GetArrayLength() - 1].GetProperty("text").GetString());

            if (providerCase == "provider_error") return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

            var source = document.GetProperty("source").GetProperty("data").GetString()!;
            var quote = providerCase == "invented_quote" ? "Cytat spoza dokumentu." : source.Split('\n')[0].Trim();
            var text = providerCase == "money" ? "Przepis przewiduje 6000 zł." : "Kodeks pracy opisuje urlop rodzicielski.";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    stop_reason = "end_turn",
                    content = new[]
                    {
                        new
                        {
                            type = "text", text,
                            citations = providerCase == "no_citation" ? null : new[] { new { document_index = 0, cited_text = quote } },
                        },
                    },
                }),
            };
        }
    }
}
