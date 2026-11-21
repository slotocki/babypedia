using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using BabyRadnik.Infrastructure.Common;
using BabyRadnik.Infrastructure.Parameters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Tests.Infrastructure.Parameters;

/// <summary>Publiczny katalog wsparcia = zweryfikowane organizacje zapisane w panelu administratora.</summary>
public sealed class OrganizationsEndpointTests : IDisposable
{
    private const string Password = "test-haslo";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
        .ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ADMIN_PASSWORD"] = Password }))
        .ConfigureTestServices(s =>
        {
            s.RemoveAll<ISettingsStore>();
            s.AddSingleton<ISettingsStore, Store>();
        }));

    public void Dispose() => factory.Dispose();

    private sealed class Store : ISettingsStore
    {
        private readonly ConcurrentDictionary<string, string> data = new();

        public Task<string?> GetAsync(string name, CancellationToken ct) => Task.FromResult(data.TryGetValue(name, out var v) ? v : null);

        public Task SetAsync(string name, string json, DateTimeOffset now, CancellationToken ct)
        {
            data[name] = json;
            return Task.CompletedTask;
        }
    }

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/organizations")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(AdminPasswordFilter.HeaderName, Password);
        return client.SendAsync(request, Ct);
    }

    [Fact]
    public async Task Empty_without_organizations_in_admin_settings()
    {
        using var client = factory.CreateClient();

        Assert.Equal("[]", await client.GetStringAsync("/api/organizations", Ct));
    }

    [Fact]
    public async Task Returns_only_verified_organizations_from_admin_panel_filtered_by_situation()
    {
        using var client = factory.CreateClient();
        var put = await PutAsync(client, """
            [
              { "id": "a", "nazwa": "Testowa Fundacja A", "opis": "x", "sytuacje": ["martwe_urodzenie"], "zweryfikowano": true },
              { "id": "b", "nazwa": "Testowa Fundacja B", "opis": "x", "sytuacje": ["martwe_urodzenie"], "zweryfikowano": false },
              { "id": "c", "nazwa": "Testowa Fundacja C", "opis": "x", "sytuacje": ["wczesniak"], "zweryfikowano": true }
            ]
            """);
        put.EnsureSuccessStatusCode();

        var all = await client.GetFromJsonAsync<List<OrganizationDto>>("/api/organizations", Ct);
        Assert.Equal(["a", "c"], all!.Select(o => o.Id).ToArray());

        var filtered = await client.GetFromJsonAsync<List<OrganizationDto>>("/api/organizations?situation=martwe_urodzenie", Ct);
        Assert.Equal(["a"], filtered!.Select(o => o.Id).ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/organizations?situation=nieznana", Ct)).StatusCode);
    }

    [Fact]
    public async Task Admin_panel_accepts_only_known_situation_ids()
    {
        using var client = factory.CreateClient();

        var response = await PutAsync(client, """[{ "id": "a", "nazwa": "n", "opis": "o", "sytuacje": ["dowolny tekst"], "zweryfikowano": true }]""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("[0].sytuacje", await response.Content.ReadAsStringAsync(Ct));
    }
}
