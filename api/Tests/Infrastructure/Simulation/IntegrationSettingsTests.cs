using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Common;
using BabyRadnik.Infrastructure.Parameters;
using BabyRadnik.Infrastructure.Simulation;
using BabyRadnik.Tests.Domain.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Tests.Infrastructure.Simulation;

/// <summary>Wyłączenie pola integracji w /admin usuwa fakt z GET /api/sim/me/facts, a silnik pyta o niego (instrukcja 6.2).</summary>
public sealed class IntegrationSettingsTests : IDisposable
{
    private const string Password = "test-haslo";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly WebApplicationFactory<Program> factory;

    public IntegrationSettingsTests()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ADMIN_PASSWORD"] = Password }))
            .ConfigureTestServices(services =>
            {
                services.RemoveAll<ISettingsStore>();
                services.AddSingleton<ISettingsStore, Store>();
                services.RemoveAll<IParameterProvider>();
                services.AddSingleton<IParameterProvider>(CatalogKit.Params());
            }));
    }

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

    private static async Task<JsonElement> FactsAsync(HttpClient client, string personaId)
    {
        var login = await client.PostAsJsonAsync("/api/sim/login", new { personaId }, Ct);
        login.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync(Ct));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sim/me/facts");
        request.Headers.Add(SimulationModule.SessionHeader, doc.RootElement.GetProperty("sessionId").GetString());
        var response = await client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement.Clone();
    }

    private static async Task DisableAsync(HttpClient client, string source, string fact)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/integrations")
        {
            Content = new StringContent($$"""[{"source":"{{source}}","fact":"{{fact}}","wlaczone":false}]""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(AdminPasswordFilter.HeaderName, Password);
        (await client.SendAsync(request, Ct)).EnsureSuccessStatusCode();
    }

    [Fact]
    public void Registered_settings_read_admin_panel()
    {
        Assert.IsType<AdminIntegrationSettings>(factory.Services.GetRequiredService<IIntegrationSettings>());
    }

    [Fact]
    public async Task Disabled_field_disappears_from_sim_facts_and_engine_asks_for_it()
    {
        using var client = factory.CreateClient();
        var before = await FactsAsync(client, "klara-demo");
        Assert.True(before.GetProperty("wnioskodawca").TryGetProperty(FactKeys.UbezpieczenieTytuly, out _));

        await DisableAsync(client, "zus", FactKeys.UbezpieczenieTytuly);
        await Task.Delay(AdminIntegrationSettings.CacheFor + TimeSpan.FromMilliseconds(200), Ct);

        var after = await FactsAsync(client, "klara-demo");
        Assert.False(after.GetProperty("wnioskodawca").TryGetProperty(FactKeys.UbezpieczenieTytuly, out _));
        Assert.True(after.GetProperty("wnioskodawca").TryGetProperty(FactKeys.UbezpieczeniePodstawySkladek, out _));

        var fromSim = after.Deserialize<FactSet>(ApiJson.Options)!;
        var facts = fromSim with
        {
            Sprawa = CatalogKit.Case(EventKind.Ciaza, "2027-03-20"),
            Wnioskodawca = fromSim.Wnioskodawca with { OsobaRola = CatalogKit.F(ParentRole.Matka) },
        };
        var assessment = await client.PostAsJsonAsync("/api/assessment", new AssessmentRequest(facts, CatalogKit.AsOf), ApiJson.Options, Ct);
        assessment.EnsureSuccessStatusCode();
        var result = (await assessment.Content.ReadFromJsonAsync<AssessmentResult>(ApiJson.Options, Ct))!;
        Assert.True(
            result.MissingFacts.Any(m => m.Fact == FactKeys.UbezpieczenieTytuly && m.Person == PersonRef.Wnioskodawca),
            "missingFacts: " + string.Join(", ", result.MissingFacts.Select(m => $"{m.Person}/{m.Fact}")));
    }

    [Fact]
    public async Task Admin_panel_field_keys_cover_exactly_the_facts_the_simulator_emits()
    {
        using var client = factory.CreateClient();
        var emitted = new HashSet<string>();
        foreach (var persona in new[] { "monika-demo", "edyta-demo", "halina-demo", "klara-demo", "tadeusz-demo" })
        {
            var facts = await FactsAsync(client, persona);
            foreach (var bucket in new[] { "sprawa", "wnioskodawca" })
            {
                // Odpowiedzi osoby demo (źródło „user”) nie pochodzą z integracji, więc panel ich nie przełącza.
                foreach (var p in facts.GetProperty(bucket).EnumerateObject().Where(p => p.Value.GetProperty("source").GetString() != "user"))
                {
                    emitted.Add($"{p.Value.GetProperty("source").GetString()}/{p.Name}");
                }
            }
        }

        var fields = await factory.Services.GetRequiredService<AdminSettingsService>().GetIntegrationsAsync(Ct);
        var panel = fields.Select(f => AdminIntegrationSettings.Key(f.Source!, f.Fact!)).ToHashSet();
        var factKeys = typeof(FactKeys).GetFields().Select(f => (string)f.GetRawConstantValue()!).ToHashSet();
        // zdarzenie.* z rejestru stanu cywilnego wynika z wpisu urodzenia — włącza je i wyłącza pole usc/dziecko.dataUrodzenia.
        emitted.ExceptWith([$"usc/{FactKeys.ZdarzenieRodzaj}", $"usc/{FactKeys.ZdarzenieData}"]);
        Assert.Subset(panel, emitted);
        Assert.All(panel, key => Assert.Contains(key.Split('/')[1], factKeys));
    }
}
