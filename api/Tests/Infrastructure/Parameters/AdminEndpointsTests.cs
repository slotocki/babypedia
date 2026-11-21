using System.Net;
using System.Text;
using BabyRadnik.Infrastructure.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BabyRadnik.Tests.Infrastructure.Parameters;

public sealed class AdminEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "test-haslo";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    public static TheoryData<string, string> Routes => new()
    {
        { "GET", "/api/admin/parameters" },
        { "PUT", "/api/admin/parameters" },
        { "GET", "/api/admin/parameters/history" },
        { "GET", "/api/admin/catalog" },
        { "PUT", "/api/admin/catalog" },
        { "GET", "/api/admin/integrations" },
        { "PUT", "/api/admin/integrations" },
        { "GET", "/api/admin/organizations" },
        { "PUT", "/api/admin/organizations" },
    };

    private HttpClient Client(string? adminPassword) => factory.WithWebHostBuilder(b =>
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ADMIN_PASSWORD"] = adminPassword }))).CreateClient();

    private static HttpRequestMessage Request(string method, string path, string? password, string body = "{}")
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT")
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        if (password is not null)
        {
            request.Headers.Add(AdminPasswordFilter.HeaderName, password);
        }

        return request;
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Missing_configured_password_returns_503(string method, string path)
    {
        using var client = Client(null);
        var response = await client.SendAsync(Request(method, path, Password), Ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Missing_or_wrong_password_returns_401(string method, string path)
    {
        using var client = Client(Password);

        foreach (var given in new string?[] { null, "zle-haslo", "" })
        {
            var response = await client.SendAsync(Request(method, path, given), Ct);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Theory]
    [InlineData("/api/admin/parameters", "{")]
    [InlineData("/api/admin/catalog", "{}")]
    [InlineData("/api/admin/catalog", """[{"id":"nieznane","nazwa":"x","wlaczone":true}]""")]
    [InlineData("/api/admin/integrations", """[{"source":"zus","fact":"nie.istnieje","wlaczone":false}]""")]
    [InlineData("/api/admin/organizations", """[{"id":"a"}]""")]
    [InlineData("/api/admin/organizations", """[{"id":"a","nazwa":"n","opis":"o","sytuacje":[],"url":"ftp://x","zweryfikowano":true}]""")]
    public async Task Invalid_body_returns_400_problem(string path, string body)
    {
        using var client = Client(Password);
        var response = await client.SendAsync(Request("PUT", path, Password, body), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
