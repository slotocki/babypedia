using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Common;
using BabyRadnik.Infrastructure.Simulation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Tests.Infrastructure.Simulation;

public sealed class SimulationTests : IDisposable
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly List<string> _tempDirs = [];
    private readonly List<WebApplicationFactory<Program>> _factories = [];

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }

        foreach (var dir in _tempDirs.Where(Directory.Exists))
        {
            Directory.Delete(dir, true);
        }
    }

    private WebApplicationFactory<Program> CreateFactory(Action<IServiceCollection>? configure = null)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            configure?.Invoke(services);
        }));
        _factories.Add(factory);
        return factory;
    }

    private string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "babyradnik-sim-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private static Action<IServiceCollection> WithPaths(RepoPaths paths) => services =>
    {
        services.RemoveAll<RepoPaths>();
        services.AddSingleton(paths);
    };

    private static async Task<(string SessionId, string Body)> LoginAsync(HttpClient client, string personaId)
    {
        var response = await client.PostAsJsonAsync("/api/sim/login", new { personaId }, Ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(Ct);
        using var doc = JsonDocument.Parse(body);
        return (doc.RootElement.GetProperty("sessionId").GetString()!, body);
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string path, string sessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(SimulationModule.SessionHeader, sessionId);
        var response = await client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
    }

    private static string ValidPesel(string firstTen)
    {
        int[] weights = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
        var sum = firstTen.Select((c, i) => (c - '0') * weights[i]).Sum();
        return firstTen + (10 - sum % 10) % 10;
    }

    private static string PersonaJson(string id, string status, string pesel) => $$"""
        {
          "id": "{{id}}", "opis": "osoba testowa", "status": "{{status}}",
          "tozsamosc": { "imie": "Test", "nazwisko": "Prywatny", "pesel": "{{pesel}}", "dataUrodzenia": "1990-01-01" },
          "fakty": { "sprawa": {}, "wnioskodawca": { "ubezpieczenie.tytuly": { "value": [], "source": "zus" } } }
        }
        """;

    [Fact]
    public async Task Personas_include_all_demo_people()
    {
        using var client = CreateFactory().CreateClient();
        var personas = await client.GetFromJsonAsync<List<PersonaListItem>>("/api/sim/personas", SimJson.Options, Ct);

        Assert.NotNull(personas);
        string[] expected = ["klara-demo", "halina-demo", "weronika-demo", "edyta-demo", "olga-demo", "tadeusz-demo", "monika-demo"];
        Assert.All(expected, id => Assert.Contains(personas, p => p.Id == id));
    }

    [Fact]
    public async Task Simulation_responses_carry_the_simulation_marker()
    {
        using var client = CreateFactory().CreateClient();
        var response = await client.GetAsync("/api/sim/personas", Ct);

        Assert.Equal("true", Assert.Single(response.Headers.GetValues(SimulationModule.SimulationHeader)));
        var (sessionId, body) = await LoginAsync(client, "klara-demo");
        Assert.True(JsonDocument.Parse(body).RootElement.GetProperty("simulated").GetBoolean());
        using var zus = await GetJsonAsync(client, "/api/sim/me/zus", sessionId);
        Assert.True(zus.RootElement.GetProperty("simulated").GetBoolean());
    }

    [Fact]
    public async Task Uninsured_person_gets_empty_insurance_registry()
    {
        var factory = CreateFactory();
        var registry = factory.Services.GetRequiredService<IInsuranceRegistry>();
        var account = await registry.GetAccountAsync("olga-demo", Ct);

        Assert.Empty(account.Tytuly);
        Assert.Empty(account.PodstawySkladek);

        using var client = factory.CreateClient();
        var (sessionId, _) = await LoginAsync(client, "olga-demo");
        using var zus = await GetJsonAsync(client, "/api/sim/me/zus", sessionId);
        Assert.Equal(0, zus.RootElement.GetProperty("tytuly").GetArrayLength());
    }

    [Fact]
    public async Task Facts_carry_the_source_of_each_integration()
    {
        using var client = CreateFactory().CreateClient();
        var (sessionId, _) = await LoginAsync(client, "halina-demo");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);
        var applicant = facts.RootElement.GetProperty("wnioskodawca");

        Assert.Equal("mobywatel", applicant.GetProperty("osoba.dataUrodzenia").GetProperty("source").GetString());
        Assert.Equal("1989-07-02", applicant.GetProperty("osoba.dataUrodzenia").GetProperty("value").GetString());
        Assert.Equal("zus", applicant.GetProperty("ubezpieczenie.tytuly").GetProperty("source").GetString());
        Assert.Equal("zus", applicant.GetProperty("platnik.liczbaUbezpieczonych").GetProperty("source").GetString());
        Assert.Equal(12, applicant.GetProperty("platnik.liczbaUbezpieczonych").GetProperty("value").GetInt32());
        var leave = applicant.GetProperty("ezla.zwolnienia");
        Assert.Equal("ezla", leave.GetProperty("source").GetString());
        Assert.Equal("B", leave.GetProperty("value")[0].GetProperty("kod").GetString());
        // Rola nie pochodzi z rejestru — to odpowiedź osoby demo (źródło „user”), do poprawienia na „Sprawdź dane”.
        Assert.Equal("user", applicant.GetProperty("osoba.rola").GetProperty("source").GetString());
    }

    [Fact]
    public async Task Postpartum_person_has_child_facts_from_civil_registry()
    {
        using var client = CreateFactory().CreateClient();
        var (sessionId, _) = await LoginAsync(client, "monika-demo");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);
        var caseFacts = facts.RootElement.GetProperty("sprawa");

        Assert.Equal("usc", caseFacts.GetProperty("dziecko.dataUrodzenia").GetProperty("source").GetString());
        Assert.Equal("2026-10-08", caseFacts.GetProperty("dziecko.dataUrodzenia").GetProperty("value").GetString());
        Assert.Equal(1, caseFacts.GetProperty("dziecko.liczba").GetProperty("value").GetInt32());
        Assert.Equal("urodzenie", caseFacts.GetProperty("zdarzenie.rodzaj").GetProperty("value").GetString());
        Assert.Equal("usc", caseFacts.GetProperty("zdarzenie.rodzaj").GetProperty("source").GetString());
        Assert.Equal("2026-10-08", caseFacts.GetProperty("zdarzenie.data").GetProperty("value").GetString());
        Assert.Equal("usc", caseFacts.GetProperty("zdarzenie.data").GetProperty("source").GetString());
        Assert.Equal("user", caseFacts.GetProperty("zdarzenie.dlaKogo").GetProperty("source").GetString());
    }

    [Fact]
    public async Task Case_persona_carries_case_facts_from_documents_in_sim_facts()
    {
        var dir = TempDir();
        Directory.CreateDirectory(Path.Combine(dir, "personas"));
        await File.WriteAllTextAsync(Path.Combine(dir, "personas", "julia.json"), """
            {
              "id": "julia", "opis": "x", "status": "gotowe", "sprawa": "julia",
              "tozsamosc": { "imie": "J", "nazwisko": "K", "pesel": "90010100000", "dataUrodzenia": "1990-01-01" },
              "fakty": {
                "sprawa": {
                  "zdarzenie.rodzaj": { "value": "przyjecie", "source": "zus", "page": 2 },
                  "zdarzenie.data": { "value": "2026-05-04", "source": "zus", "page": 2 },
                  "zdarzenie.dlaKogo": { "value": "ja", "source": "user" },
                  "przyjecie.sciezka": { "value": "przysposobienie", "source": "zus", "page": 3 }
                },
                "wnioskodawca": {}
              }
            }
            """, Ct);
        using var client = CreateFactory(WithPaths(new RepoPaths { Root = TestPaths.Root, CasesPrivateDir = dir })).CreateClient();
        var (sessionId, _) = await LoginAsync(client, "julia");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);
        var caseFacts = facts.RootElement.GetProperty("sprawa");

        Assert.Equal("przyjecie", caseFacts.GetProperty("zdarzenie.rodzaj").GetProperty("value").GetString());
        Assert.Equal(2, caseFacts.GetProperty("zdarzenie.data").GetProperty("page").GetInt32());
        Assert.Equal("przysposobienie", caseFacts.GetProperty("przyjecie.sciezka").GetProperty("value").GetString());
        Assert.False(caseFacts.TryGetProperty("zdarzenie.dlaKogo", out _));
    }

    [Fact]
    public async Task Pregnant_person_has_no_child_facts_yet()
    {
        using var client = CreateFactory().CreateClient();
        var (sessionId, _) = await LoginAsync(client, "klara-demo");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);

        Assert.False(facts.RootElement.GetProperty("sprawa").TryGetProperty("dziecko.dataUrodzenia", out _));
    }

    [Fact]
    public async Task Demo_person_answers_arrive_as_user_facts()
    {
        using var client = CreateFactory().CreateClient();
        var (sessionId, _) = await LoginAsync(client, "anna-przyklad");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);
        var caseFacts = facts.RootElement.GetProperty("sprawa");
        var applicant = facts.RootElement.GetProperty("wnioskodawca");

        foreach (var (key, value) in new[] { ("zdarzenie.rodzaj", "ciaza"), ("zdarzenie.data", "2027-03-15"), ("zdarzenie.dlaKogo", "ja") })
        {
            Assert.Equal(value, caseFacts.GetProperty(key).GetProperty("value").GetString());
            Assert.Equal("user", caseFacts.GetProperty(key).GetProperty("source").GetString());
        }

        Assert.Equal(1, caseFacts.GetProperty("dziecko.liczba").GetProperty("value").GetInt32());
        Assert.Equal("matka", applicant.GetProperty("osoba.rola").GetProperty("value").GetString());
        Assert.Equal("user", applicant.GetProperty("osoba.rola").GetProperty("source").GetString());
        // Dane z rejestrów nadal z integracji, nie z pliku osoby.
        Assert.Equal("zus", applicant.GetProperty("ubezpieczenie.tytuly").GetProperty("source").GetString());
    }

    [Fact]
    public async Task Partner_answers_of_demo_person_are_returned_with_partner_titles()
    {
        using var client = CreateFactory().CreateClient();
        var (sessionId, _) = await LoginAsync(client, "tadeusz-demo");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);
        var partner = facts.RootElement.GetProperty("drugiRodzic");

        Assert.Equal("matka", partner.GetProperty("osoba.rola").GetProperty("value").GetString());
        Assert.Equal("user", partner.GetProperty("osoba.rola").GetProperty("source").GetString());
        Assert.Equal("zus", partner.GetProperty("ubezpieczenie.tytuly").GetProperty("source").GetString());
    }

    [Fact]
    public async Task Facts_of_self_employed_include_debt_and_father_gets_partner_titles()
    {
        using var client = CreateFactory().CreateClient();
        var (edyta, _) = await LoginAsync(client, "edyta-demo");
        using var debtFacts = await GetJsonAsync(client, "/api/sim/me/facts", edyta);
        var debt = debtFacts.RootElement.GetProperty("wnioskodawca").GetProperty("skladki.zadluzenie").GetProperty("value");
        Assert.Equal(120.00m, debt.GetProperty("kwota").GetDecimal());

        var (tadeusz, _) = await LoginAsync(client, "tadeusz-demo");
        using var fatherFacts = await GetJsonAsync(client, "/api/sim/me/facts", tadeusz);
        var partner = fatherFacts.RootElement.GetProperty("drugiRodzic").GetProperty("ubezpieczenie.tytuly");
        Assert.Equal("zus", partner.GetProperty("source").GetString());
        Assert.Equal(0, partner.GetProperty("value").GetArrayLength());
    }

    [Fact]
    public async Task Disabled_integration_field_is_not_returned()
    {
        using var client = CreateFactory(s => s.Replace(ServiceDescriptor.Singleton<IIntegrationSettings>(new DisabledFieldsSettings(FactKeys.EzlaZwolnienia, FactKeys.PlatnikLiczbaUbezpieczonych)))).CreateClient();
        var (sessionId, _) = await LoginAsync(client, "halina-demo");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);
        var applicant = facts.RootElement.GetProperty("wnioskodawca");

        Assert.False(applicant.TryGetProperty(FactKeys.EzlaZwolnienia, out _));
        Assert.False(applicant.TryGetProperty(FactKeys.PlatnikLiczbaUbezpieczonych, out _));
        Assert.True(applicant.TryGetProperty(FactKeys.UbezpieczenieTytuly, out _));
    }

    [Theory]
    [InlineData("/api/sim/me/facts")]
    [InlineData("/api/sim/me/zus")]
    public async Task Missing_or_unknown_session_returns_401_problem(string path)
    {
        using var client = CreateFactory().CreateClient();

        var missing = await client.GetAsync(path, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(SimulationModule.SessionHeader, "nieznana-sesja");
        var unknown = await client.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
    }

    [Theory]
    [InlineData("""{"personaId":"nie-ma-takiej"}""", HttpStatusCode.NotFound)]
    [InlineData("""{"personaId":""}""", HttpStatusCode.BadRequest)]
    [InlineData("""{}""", HttpStatusCode.BadRequest)]
    public async Task Login_validates_input_with_problem_details(string json, HttpStatusCode expected)
    {
        using var client = CreateFactory().CreateClient();
        var response = await client.PostAsync("/api/sim/login", new StringContent(json, System.Text.Encoding.UTF8, "application/json"), Ct);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Pesel_appears_only_in_login_response()
    {
        var factory = CreateFactory();
        using var client = factory.CreateClient();
        var store = factory.Services.GetRequiredService<PersonaStore>();
        var sessions = factory.Services.GetRequiredService<SessionStore>();
        var checkedPersonas = 0;

        foreach (var persona in store.All)
        {
            var pesel = persona.Tozsamosc.Pesel;
            var (sessionId, loginBody) = await LoginAsync(client, persona.Id);
            Assert.Contains(pesel, loginBody);

            var factsBody = await GetBodyAsync(client, "/api/sim/me/facts", sessionId);
            var zusBody = await GetBodyAsync(client, "/api/sim/me/zus", sessionId);
            var personasBody = await client.GetStringAsync("/api/sim/personas", Ct);
            var casesBody = await client.GetStringAsync("/api/cases", Ct);

            foreach (var body in new[] { factsBody, zusBody, personasBody, casesBody })
            {
                Assert.DoesNotContain(pesel, body);
            }

            var state = JsonSerializer.Serialize(sessions.Snapshot(), SimJson.Options);
            Assert.DoesNotContain(pesel, state);
            Assert.DoesNotContain(persona.Tozsamosc.Nazwisko, state);
            checkedPersonas++;
        }

        Assert.True(checkedPersonas >= 8);
    }

    [Fact]
    public async Task Facts_expose_only_birth_date_from_identity()
    {
        using var client = CreateFactory().CreateClient();
        var (sessionId, _) = await LoginAsync(client, "klara-demo");
        using var facts = await GetJsonAsync(client, "/api/sim/me/facts", sessionId);
        var text = facts.RootElement.GetRawText();

        Assert.DoesNotContain("pesel", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("imie", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nazwisko", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1992-03-18", facts.RootElement.GetProperty("wnioskodawca").GetProperty("osoba.dataUrodzenia").GetProperty("value").GetString());
    }

    [Fact]
    public void Builtin_personas_have_non_checksum_pesel_and_valid_status()
    {
        var store = CreateFactory().Services.GetRequiredService<PersonaStore>();

        Assert.All(store.All, p => Assert.False(PersonaStore.PeselChecksumValid(p.Tozsamosc.Pesel)));
    }

    [Fact]
    public async Task Private_directory_adds_personas_skips_unfinished_and_checksum_valid_pesel()
    {
        var dir = TempDir();
        Directory.CreateDirectory(Path.Combine(dir, "personas"));
        await File.WriteAllTextAsync(Path.Combine(dir, "personas", "prywatna.json"), PersonaJson("prywatna", "gotowe", "90010100000"), Ct);
        await File.WriteAllTextAsync(Path.Combine(dir, "personas", "wstepna.json"), PersonaJson("wstepna", "do_uzupelnienia", "90010100000"), Ct);
        await File.WriteAllTextAsync(Path.Combine(dir, "personas", "prawdziwy.json"), PersonaJson("prawdziwy", "gotowe", ValidPesel("9001010123")), Ct);
        await File.WriteAllTextAsync(Path.Combine(dir, "personas", "uszkodzona.json"), "{ nie json", Ct);

        var paths = new RepoPaths { Root = TestPaths.Root, CasesPrivateDir = dir };
        using var client = CreateFactory(WithPaths(paths)).CreateClient();
        var personas = await client.GetFromJsonAsync<List<PersonaListItem>>("/api/sim/personas", SimJson.Options, Ct);

        Assert.NotNull(personas);
        Assert.Contains(personas, p => p.Id == "prywatna");
        Assert.DoesNotContain(personas, p => p.Id == "wstepna");
        Assert.DoesNotContain(personas, p => p.Id == "prawdziwy");
        Assert.Contains(personas, p => p.Id == "klara-demo");
    }

    [Fact]
    public async Task Missing_private_directory_is_normal()
    {
        var paths = new RepoPaths { Root = TestPaths.Root, CasesPrivateDir = Path.Combine(TempDir(), "nie-istnieje") };
        using var client = CreateFactory(WithPaths(paths)).CreateClient();
        var response = await client.GetAsync("/api/sim/personas", Ct);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Cases_list_is_empty_without_private_case_data()
    {
        // Sprawy ZUS są wyłącznie w katalogu prywatnym (poza repozytorium); samo repozytorium nie zawiera żadnej.
        var paths = new RepoPaths { Root = TestPaths.Root, CasesPrivateDir = Path.Combine(TempDir(), "nie-istnieje") };
        using var client = CreateFactory(WithPaths(paths)).CreateClient();
        var cases = await client.GetFromJsonAsync<List<CaseSummary>>("/api/cases", SimJson.Options, Ct);

        Assert.NotNull(cases);
        Assert.Empty(cases);
    }

    [Fact]
    public async Task Case_passes_confirmed_periods_from_records_in_list_and_by_hash()
    {
        var dir = TempDir();
        var hash = new string('d', 64);
        await File.WriteAllTextAsync(Path.Combine(dir, "cases.json"), $$"""
            { "cases": [
              { "id": "test-okresy", "tytul": "fikcyjna sprawa", "oczekiwanaDecyzja": "przyznanie",
                "plik": { "nazwa": "x.pdf", "liczbaStron": 3, "sha256": ["{{hash}}"] },
                "potwierdzoneOkresy": [ { "swiadczenie": "zasilek_macierzynski", "od": "2030-01-10", "do": "2030-07-09", "dni": 181, "tygodnie": 26 } ] },
              { "id": "test-bez", "tytul": "bez okresow", "oczekiwanaDecyzja": "odmowa" } ] }
            """, Ct);

        var paths = new RepoPaths { Root = TestPaths.Root, CasesPrivateDir = dir };
        using var client = CreateFactory(WithPaths(paths)).CreateClient();

        var list = await client.GetFromJsonAsync<List<CaseSummary>>("/api/cases", SimJson.Options, Ct);
        var withPeriods = Assert.Single(list!, c => c.Id == "test-okresy");
        var period = Assert.Single(withPeriods.PotwierdzoneOkresy!);
        Assert.Equal(("zasilek_macierzynski", "2030-01-10", "2030-07-09", 181), (period.Swiadczenie, period.Od, period.Do, period.Dni));
        Assert.Null(list!.Single(c => c.Id == "test-bez").PotwierdzoneOkresy);

        var byHash = await client.GetFromJsonAsync<CaseSummary>($"/api/cases/by-hash/{hash}", SimJson.Options, Ct);
        Assert.Single(byHash!.PotwierdzoneOkresy!);
    }

    [Fact]
    public async Task Case_is_recognised_by_hash_then_by_file_name_and_pages_else_404()
    {
        var dir = TempDir();
        Directory.CreateDirectory(Path.Combine(dir, "personas"));
        await File.WriteAllTextAsync(Path.Combine(dir, "personas", "ewa.json"), PersonaJson("ewa", "gotowe", "90010100000"), Ct);
        var hash = new string('a', 64);
        await File.WriteAllTextAsync(Path.Combine(dir, "cases.json"), $$"""
            { "cases": [
              { "id": "ewa", "tytul": "sprawa Ewy", "oczekiwanaDecyzja": "odmowa", "persona": "ewa",
                "plik": { "nazwa": "ewa.pdf", "liczbaStron": 7, "sha256": ["{{hash}}"] } } ] }
            """, Ct);

        var paths = new RepoPaths { Root = TestPaths.Root, CasesPrivateDir = dir };
        using var client = CreateFactory(WithPaths(paths)).CreateClient();

        var byHash = await client.GetFromJsonAsync<CaseSummary>($"/api/cases/by-hash/{hash.ToUpperInvariant()}", SimJson.Options, Ct);
        Assert.Equal("ewa", byHash?.Id);
        Assert.True(byHash?.Dostepna);

        var byName = await client.GetFromJsonAsync<CaseSummary>($"/api/cases/by-hash/{new string('b', 64)}?fileName=EWA.pdf&pages=7", SimJson.Options, Ct);
        Assert.Equal("ewa", byName?.Id);

        var wrongPages = await client.GetAsync($"/api/cases/by-hash/{new string('b', 64)}?fileName=ewa.pdf&pages=8", Ct);
        Assert.Equal(HttpStatusCode.NotFound, wrongPages.StatusCode);
        Assert.Equal("application/problem+json", wrongPages.Content.Headers.ContentType?.MediaType);

        var unknown = await client.GetAsync($"/api/cases/by-hash/{new string('c', 64)}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var malformed = await client.GetAsync("/api/cases/by-hash/nie-hash", Ct);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }

    private static async Task<string> GetBodyAsync(HttpClient client, string path, string sessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(SimulationModule.SessionHeader, sessionId);
        using var response = await client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(Ct);
    }

    private sealed class DisabledFieldsSettings(params string[] disabled) : IIntegrationSettings
    {
        public bool IsFieldEnabled(FactSource source, string factKey) => !disabled.Contains(factKey);
    }
}
