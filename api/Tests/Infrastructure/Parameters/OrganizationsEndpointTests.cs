using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BabyRadnik.Infrastructure.Common;
using BabyRadnik.Infrastructure.Parameters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Tests.Infrastructure.Parameters;

/// <summary>Sprawdza katalog startowy, filtrowanie oraz zapisy z panelu administratora.</summary>
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
    public async Task Uses_verified_free_fixture_entries_until_a_catalog_is_saved()
    {
        using var client = factory.CreateClient();

        var actual = await client.GetFromJsonAsync<List<OrganizationDto>>("/api/organizations", Ct);
        Assert.NotNull(actual);
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(TestPaths.Fixtures, "organizations.json"), Ct));
        var expected = fixture.RootElement.EnumerateArray()
            .Where(entry => entry.GetProperty("zweryfikowano").GetBoolean() && entry.GetProperty("bezplatna").GetBoolean())
            .ToDictionary(entry => entry.GetProperty("id").GetString()!);
        Assert.Equal(expected.Keys.Order(), actual.Select(entry => entry.Id!).Order());
        foreach (var entry in actual)
        {
            Assert.True(entry.Zweryfikowano);
            Assert.Equal(expected[entry.Id!].GetProperty("zakres").GetString(), entry.Opis);
            Assert.Equal(expected[entry.Id!].GetProperty("www").GetString(), entry.Url);
        }

        foreach (var situation in new[] { "poronienie", "martwe_urodzenie", "zgon_dziecka", "wczesniak",
                     "ciezka_choroba_dziecka", "sytuacja_matki" })
        {
            var filtered = await client.GetFromJsonAsync<List<OrganizationDto>>($"/api/organizations?situation={situation}", Ct);
            Assert.NotNull(filtered);
            Assert.True(filtered.Count >= 2, situation);
            Assert.All(filtered, entry => Assert.Contains(situation, entry.Sytuacje!));
        }

        using var put = await PutAsync(client, "[]");
        put.EnsureSuccessStatusCode();
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
