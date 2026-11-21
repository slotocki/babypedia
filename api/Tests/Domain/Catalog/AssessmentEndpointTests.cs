using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Parameters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BabyRadnik.Tests.Domain.Catalog;

public sealed class AssessmentFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IParameterProvider>();
            services.AddSingleton<IParameterProvider>(CatalogKit.Params());
            services.RemoveAll<ISettingsStore>();
            services.AddSingleton<ISettingsStore>(new AssessmentEndpointTests.MemorySettingsStore());
        });
}

public sealed class AssessmentEndpointTests(AssessmentFixture fixture) : IClassFixture<AssessmentFixture>
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private static readonly JsonElement Schema = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(TestPaths.Contracts, "assessment.schema.json"))).RootElement.Clone();

    internal sealed class MemorySettingsStore(string? catalogJson = null, bool fail = false) : ISettingsStore
    {
        public Task<string?> GetAsync(string name, CancellationToken ct) =>
            fail ? throw new InvalidOperationException("baza niedostępna") : Task.FromResult(name == "catalog" ? catalogJson : null);

        public Task SetAsync(string name, string json, DateTimeOffset now, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Capture(Messages);

        public void Dispose()
        {
        }

        private sealed class Capture(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (messages)
                {
                    messages.Add(formatter(state, exception) + exception);
                }
            }
        }
    }

    private HttpClient Client(ISettingsStore? settings = null, CapturingLoggerProvider? logs = null)
    {
        if (settings is null && logs is null)
        {
            return fixture.CreateClient();
        }

        return fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            if (settings is not null)
            {
                services.RemoveAll<ISettingsStore>();
                services.AddSingleton(settings);
            }

            if (logs is not null)
            {
                services.AddSingleton<ILoggerProvider>(logs);
            }
        })).CreateClient();
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string ExampleRequest() => File.ReadAllText(Path.Combine(TestPaths.Contracts, "examples", "assessment-request.example.json"));

    private static JsonObject PersonaFacts(string id)
    {
        var persona = JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.Fixtures, "personas", id + ".json")))!;
        return persona["fakty"]!.AsObject().DeepClone().AsObject();
    }

    private static string PersonaRequest(JsonObject facts, string asOf = "2026-11-21") =>
        new JsonObject { ["facts"] = facts.DeepClone(), ["asOf"] = asOf }.ToJsonString();

    private static async Task<JsonElement> Post(HttpClient client, string body)
    {
        var response = await client.PostAsync("/api/assessment", Json(body), Ct);
        var text = await response.Content.ReadAsStringAsync(Ct);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private static void AssertMatchesSchema(JsonElement result)
    {
        var errors = MiniSchema.Validate(Schema, "AssessmentResult", result);
        Assert.True(errors.Count == 0, string.Join("\n", errors.Take(10)));
    }

    private static JsonElement Benefit(JsonElement result, string id, string person = "wnioskodawca") =>
        result.GetProperty("benefits").EnumerateArray().Single(b => b.GetProperty("id").GetString() == id && b.GetProperty("person").GetString() == person);

    private static string Status(JsonElement result, string id, string person = "wnioskodawca") =>
        Benefit(result, id, person).GetProperty("status").GetString()!;

    private static readonly string[] CatalogIds =
    [
        "chorobowy_w_ciazy", "rehabilitacyjne", "zasilek_do_dnia_porodu", "macierzynski", "macierzynski_uzupelniajacy",
        "rodzicielski", "ojcowski", "przyjecie_dziecka", "przejecie_przez_ojca", "pogrzebowy",
    ];

    private static void AssertWholeCatalogPresent(JsonElement result, params string[] persons)
    {
        foreach (var person in persons)
        {
            var ids = result.GetProperty("benefits").EnumerateArray()
                .Where(b => b.GetProperty("person").GetString() == person)
                .Select(b => b.GetProperty("id").GetString()!)
                .Order()
                .ToList();
            Assert.Equal(CatalogIds.Order(), ids);
        }

        foreach (var benefit in result.GetProperty("benefits").EnumerateArray())
        {
            var status = benefit.GetProperty("status").GetString();
            if (status is "nie_przysługuje" or "nie_rozstrzygam")
            {
                Assert.False(string.IsNullOrWhiteSpace(benefit.GetProperty("reason").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(benefit.GetProperty("legalBasis").GetString()));
            }

            if (status == "przysługuje")
            {
                Assert.NotEmpty(benefit.GetProperty("trace").EnumerateArray());
                Assert.NotEmpty(benefit.GetProperty("links").EnumerateArray());
            }
        }
    }

    [Fact]
    public async Task Example_request_returns_a_real_result_that_matches_the_response_schema()
    {
        using var client = Client();

        var result = await Post(client, ExampleRequest());

        AssertMatchesSchema(result);
        Assert.Equal("2026-11-21", result.GetProperty("rulesetVersion").GetString());
        AssertWholeCatalogPresent(result, "wnioskodawca");
        var maternity = Benefit(result, "macierzynski");
        Assert.Equal("przysługuje", maternity.GetProperty("status").GetString());
        Assert.Equal("2027-03-15", maternity.GetProperty("period").GetProperty("from").GetString());
        Assert.Equal("2027-08-01", maternity.GetProperty("period").GetProperty("to").GetString());
        Assert.Equal(140, maternity.GetProperty("period").GetProperty("days").GetInt32());
        Assert.Equal("płatnik_składek", maternity.GetProperty("payer").GetString());
        Assert.Equal(1m, maternity.GetProperty("rate").GetDecimal());
        var amount = maternity.GetProperty("amount");
        Assert.Equal(6903.20m, amount.GetProperty("basis").GetDecimal());
        Assert.Equal(230.11m, amount.GetProperty("daily").GetDecimal());
        Assert.Equal(32215.40m, amount.GetProperty("total").GetDecimal());
        Assert.Equal("szacunek_zus", amount.GetProperty("basisSource").GetString());
        Assert.DoesNotContain("ATRAPA", result.GetRawText());
        Assert.Equal("wniosek_usredniona", result.GetProperty("nextStep").GetProperty("deadlineId").GetString());
    }

    [Fact]
    public async Task Missing_fact_comes_back_as_a_question_and_disappears_after_it_is_answered()
    {
        using var client = Client();
        var facts = PersonaFacts("klara-demo");
        facts["sprawa"]!.AsObject().Remove("dziecko.liczba");

        var asked = await Post(client, PersonaRequest(facts));
        facts["sprawa"]!["dziecko.liczba"] = new JsonObject { ["value"] = 1, ["source"] = "user" };
        var answered = await Post(client, PersonaRequest(facts));

        AssertMatchesSchema(asked);
        AssertMatchesSchema(answered);
        var question = asked.GetProperty("missingFacts").EnumerateArray().Single(m => m.GetProperty("fact").GetString() == "dziecko.liczba");
        Assert.Equal("integer", question.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Null, question.GetProperty("person").ValueKind);
        Assert.Equal("brakuje_danych", Status(asked, "macierzynski"));
        Assert.DoesNotContain(answered.GetProperty("missingFacts").EnumerateArray(), m => m.GetProperty("fact").GetString() == "dziecko.liczba");
        Assert.Equal("przysługuje", Status(answered, "macierzynski"));
    }

    [Fact]
    public async Task Klara_gets_granted_leave_benefits_paid_by_the_payer()
    {
        using var client = Client();

        var result = await Post(client, PersonaRequest(PersonaFacts("klara-demo")));

        AssertMatchesSchema(result);
        AssertWholeCatalogPresent(result, "wnioskodawca");
        Assert.Equal("przysługuje", Status(result, "macierzynski"));
        Assert.Equal("przysługuje", Status(result, "rodzicielski"));
        Assert.Equal("płatnik_składek", Benefit(result, "macierzynski").GetProperty("payer").GetString());
        Assert.Equal("nie_dotyczy", Status(result, "ojcowski"));
        Assert.Equal("macierzynski", result.GetProperty("nextStep").GetProperty("benefitId").GetString());
        Assert.False(result.GetProperty("difficultSituation").GetBoolean());
    }

    [Fact]
    public async Task Edyta_with_a_debt_is_denied_with_the_repayment_deadline_as_the_next_step()
    {
        using var client = Client();

        var result = await Post(client, PersonaRequest(PersonaFacts("edyta-demo")));

        AssertMatchesSchema(result);
        AssertWholeCatalogPresent(result, "wnioskodawca");
        var maternity = Benefit(result, "macierzynski");
        Assert.Equal("nie_przysługuje", maternity.GetProperty("status").GetString());
        Assert.Contains("120,00 zł", maternity.GetProperty("whatWouldChange").GetString());
        Assert.Equal("splata_zadluzenia", maternity.GetProperty("deadlines")[0].GetProperty("id").GetString());
        Assert.Equal("splata_zadluzenia", result.GetProperty("nextStep").GetProperty("deadlineId").GetString());
        Assert.Contains(result.GetProperty("risks").EnumerateArray(), r => r.GetProperty("id").GetString() == "zadluzenie_skladkowe");
        Assert.Equal(JsonValueKind.Null, maternity.GetProperty("period").ValueKind);
    }

    [Fact]
    public async Task Halina_with_a_sick_leave_gets_sickness_benefit_paid_by_zus()
    {
        using var client = Client();

        var result = await Post(client, PersonaRequest(PersonaFacts("halina-demo")));

        AssertMatchesSchema(result);
        AssertWholeCatalogPresent(result, "wnioskodawca");
        var sickness = Benefit(result, "chorobowy_w_ciazy");
        Assert.Equal("przysługuje", sickness.GetProperty("status").GetString());
        Assert.Equal("zus", sickness.GetProperty("payer").GetString());
        Assert.Equal("pracodawca", sickness.GetProperty("whereToApply").GetString());
        Assert.Contains(result.GetProperty("risks").EnumerateArray(), r => r.GetProperty("id").GetString() == "praca_w_czasie_zwolnienia");
    }

    [Fact]
    public async Task Monika_after_birth_sees_the_passed_averaged_variant_deadline_but_the_next_step_is_the_parental_request()
    {
        using var client = Client();

        var result = await Post(client, PersonaRequest(PersonaFacts("monika-demo")));

        AssertMatchesSchema(result);
        AssertWholeCatalogPresent(result, "wnioskodawca");
        var maternity = Benefit(result, "macierzynski");
        Assert.Equal("2026-10-08", maternity.GetProperty("period").GetProperty("from").GetString());
        var averaged = maternity.GetProperty("deadlines").EnumerateArray().Single(d => d.GetProperty("id").GetString() == "wniosek_usredniona");
        Assert.Equal("2026-10-29", averaged.GetProperty("date").GetString());
        Assert.Contains("minął", averaged.GetProperty("text").GetString());
        Assert.Equal("wniosek_rodzicielski", result.GetProperty("nextStep").GetProperty("deadlineId").GetString());
        Assert.DoesNotContain(result.GetProperty("risks").EnumerateArray(), r => r.GetProperty("id").GetString() == "termin_21_dni");
    }

    [Fact]
    public async Task Olga_without_insurance_is_denied_everywhere_with_reasons()
    {
        using var client = Client();

        var result = await Post(client, PersonaRequest(PersonaFacts("olga-demo")));

        AssertMatchesSchema(result);
        AssertWholeCatalogPresent(result, "wnioskodawca");
        Assert.Equal("nie_przysługuje", Status(result, "macierzynski"));
        Assert.Equal("nie_przysługuje", Status(result, "rodzicielski"));
        Assert.Equal("nie_przysługuje", Status(result, "chorobowy_w_ciazy"));
        Assert.Equal(JsonValueKind.Null, result.GetProperty("nextStep").ValueKind);
    }

    [Fact]
    public async Task Weronika_the_contractor_gets_maternity_but_not_sickness_benefit_and_a_renewal_warning()
    {
        using var client = Client();

        var result = await Post(client, PersonaRequest(PersonaFacts("weronika-demo")));

        AssertMatchesSchema(result);
        AssertWholeCatalogPresent(result, "wnioskodawca");
        Assert.Equal("nie_przysługuje", Status(result, "chorobowy_w_ciazy"));
        Assert.Equal("przysługuje", Status(result, "macierzynski"));
        Assert.Contains(result.GetProperty("risks").EnumerateArray(), r => r.GetProperty("id").GetString() == "ponowne_zgloszenie_do_chorobowego");
    }

    [Fact]
    public async Task Tadeusz_and_his_uninsured_partner_get_results_for_both_persons()
    {
        using var client = Client();
        var facts = PersonaFacts("tadeusz-demo");
        facts["drugiRodzic"] = PersonaFacts("olga-demo")["wnioskodawca"]!.DeepClone();

        var result = await Post(client, PersonaRequest(facts));

        AssertMatchesSchema(result);
        AssertWholeCatalogPresent(result, "wnioskodawca", "drugiRodzic");
        Assert.Equal("przysługuje", Status(result, "ojcowski"));
        Assert.Equal("przysługuje", Status(result, "rodzicielski"));
        Assert.Equal("nie_dotyczy", Status(result, "macierzynski"));
        Assert.Equal("nie_przysługuje", Status(result, "macierzynski", "drugiRodzic"));
        Assert.Equal("nie_dotyczy", Status(result, "pogrzebowy", "drugiRodzic"));
        Assert.Contains(result.GetProperty("risks").EnumerateArray(), r => r.GetProperty("id").GetString() == "niewykorzystany_ojcowski");
    }

    [Fact]
    public async Task Stillbirth_switches_on_the_difficult_situation_and_gives_the_funeral_information()
    {
        using var client = Client();
        var facts = PersonaFacts("monika-demo");
        facts["sprawa"]!["dziecko.martweUrodzenie"] = new JsonObject { ["value"] = true, ["source"] = "user" };
        facts["sprawa"]!["dziecko.dataPochowku"] = new JsonObject { ["value"] = "2026-10-12", ["source"] = "user" };

        var result = await Post(client, PersonaRequest(facts));

        AssertMatchesSchema(result);
        Assert.True(result.GetProperty("difficultSituation").GetBoolean());
        var funeral = Benefit(result, "pogrzebowy");
        Assert.Equal("przysługuje", funeral.GetProperty("status").GetString());
        Assert.Equal("2027-10-12", funeral.GetProperty("deadlines")[0].GetProperty("date").GetString());
        Assert.Contains(funeral.GetProperty("notes").EnumerateArray(), n => n.GetString()!.Contains("7 000,00 zł"));
        Assert.Equal(7000m, funeral.GetProperty("lumpSum").GetDecimal());
        Assert.Equal(JsonValueKind.Null, Benefit(result, "macierzynski").GetProperty("lumpSum").ValueKind);
    }

    [Fact]
    public async Task Completed_deadlines_from_the_request_move_the_next_step()
    {
        using var client = Client();
        var request = JsonNode.Parse(PersonaRequest(PersonaFacts("klara-demo")))!.AsObject();
        request["completedDeadlines"] = new JsonArray("wniosek_usredniona");

        var result = await Post(client, request.ToJsonString());

        AssertMatchesSchema(result);
        Assert.Equal("wniosek_rodzicielski", result.GetProperty("nextStep").GetProperty("deadlineId").GetString());
        Assert.Equal("rodzicielski", result.GetProperty("nextStep").GetProperty("benefitId").GetString());
    }

    [Fact]
    public async Task As_of_is_optional_and_defaults_to_today()
    {
        using var client = Client();
        var request = JsonNode.Parse(PersonaRequest(PersonaFacts("klara-demo")))!.AsObject();
        request.Remove("asOf");

        var result = await Post(client, request.ToJsonString());

        AssertMatchesSchema(result);
        Assert.Equal("przysługuje", Status(result, "macierzynski"));
    }

    [Fact]
    public async Task Entries_switched_off_in_the_admin_panel_are_skipped()
    {
        var catalog = """[{"id":"rodzicielski","nazwa":"Zasiłek rodzicielski","wlaczone":false},{"id":"macierzynski","nazwa":"x","wlaczone":true}]""";
        using var client = Client(new MemorySettingsStore(catalog));

        var result = await Post(client, ExampleRequest());

        AssertMatchesSchema(result);
        Assert.DoesNotContain(result.GetProperty("benefits").EnumerateArray(), b => b.GetProperty("id").GetString() == "rodzicielski");
        Assert.Equal(9, result.GetProperty("benefits").GetArrayLength());
        Assert.Equal("przysługuje", Status(result, "macierzynski"));
    }

    [Fact]
    public async Task Unreadable_admin_settings_do_not_break_the_assessment_and_nothing_personal_is_logged()
    {
        var logs = new CapturingLoggerProvider();
        using var client = Client(new MemorySettingsStore(fail: true), logs);

        var result = await Post(client, PersonaRequest(PersonaFacts("klara-demo")));

        Assert.Equal(10, result.GetProperty("benefits").GetArrayLength());
        string[] personal = ["Klara", "Wzorcowa", "Duża Firma", "92031810170", "platnik-demo-duzy", "1992-03-18"];
        lock (logs.Messages)
        {
            Assert.Contains(logs.Messages, m => m.Contains("ustawień katalogu"));
            Assert.All(logs.Messages, m => Assert.DoesNotContain(personal, m.Contains));
        }
    }

    [Theory]
    [InlineData("""{"facts":{"sprawa":{},"wnioskodawca":{}},"asOf":"1999-12-31"}""", "asOf")]
    [InlineData("""{"facts":{"sprawa":{},"wnioskodawca":{}},"asOf":"2101-01-01"}""", "asOf")]
    [InlineData("""{"facts":{"sprawa":{},"wnioskodawca":{}},"completedDeadlines":["Zły Id!"]}""", "completedDeadlines")]
    [InlineData("""{"facts":{"sprawa":{},"wnioskodawca":{}},"completedDeadlines":["inna/wniosek_usredniona"]}""", "completedDeadlines")]
    public async Task Invalid_request_values_are_rejected_with_problem_details(string body, string field)
    {
        using var client = Client();

        var response = await client.PostAsync("/api/assessment", Json(body), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(field, await response.Content.ReadAsStringAsync(Ct));
    }

    [Theory]
    [InlineData("""{"asOf":"2026-11-21"}""")]
    [InlineData("""{"facts":{"sprawa":{"zdarzenie.rodzaj":{"value":"burza","source":"user"}},"wnioskodawca":{}}}""")]
    [InlineData("""{"facts":{"sprawa":{"nieznany.klucz":{"value":1,"source":"user"}},"wnioskodawca":{}}}""")]
    [InlineData("""{"facts":{"sprawa":{},"wnioskodawca":{}},"asOf":"jutro"}""")]
    [InlineData("""not json""")]
    public async Task Malformed_requests_are_rejected_with_bad_request(string body)
    {
        using var client = Client();

        var response = await client.PostAsync("/api/assessment", Json(body), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Too_long_fact_lists_are_rejected()
    {
        using var client = Client();
        var rows = string.Join(",", Enumerable.Repeat("""{"miesiac":"2026-01","kwota":100}""", 501));
        var body = "{\"facts\":{\"sprawa\":{},\"wnioskodawca\":{\"ubezpieczenie.podstawySkladek\":{\"value\":[" + rows + "],\"source\":\"zus\"}}}}";

        var response = await client.PostAsync("/api/assessment", Json(body), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Empty_facts_are_a_valid_request_that_asks_for_the_basics()
    {
        using var client = Client();

        var response = await client.PostAsJsonAsync("/api/assessment", new AssessmentRequest(new FactSet(), CatalogKit.AsOf), ApiJson.Options, Ct);

        response.EnsureSuccessStatusCode();
        var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement;
        AssertMatchesSchema(result);
        Assert.Contains(result.GetProperty("missingFacts").EnumerateArray(), m => m.GetProperty("fact").GetString() == "zdarzenie.rodzaj");
    }
}
