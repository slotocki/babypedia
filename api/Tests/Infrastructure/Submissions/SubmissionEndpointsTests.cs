using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Simulation;
using BabyRadnik.Infrastructure.Submissions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Tests.Infrastructure.Submissions;

public sealed partial class SubmissionEndpointsTests : IDisposable
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly InMemorySubmissionStore _store = new();
    private readonly WebApplicationFactory<Program> _factory;

    public SubmissionEndpointsTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<ISubmissionStore>();
            services.AddSingleton<ISubmissionStore>(_store);
        }));
    }

    public void Dispose() => _factory.Dispose();

    [GeneratedRegex(@"^BR-SYM-(URL|ZAM|EZUS|ODP)-\d{8}-[0-9A-HJKMNP-TV-Z]{8}$")]
    private static partial Regex NumberFormat();

    private static async Task<string> LoginAsync(HttpClient client, string personaId = "klara-demo")
    {
        var response = await client.PostAsJsonAsync("/api/sim/login", new { personaId }, Ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.GetProperty("sessionId").GetString()!;
    }

    private static HttpRequestMessage Request(HttpMethod method, string path, string? session, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (session is not null)
        {
            request.Headers.Add(SimulationModule.SessionHeader, session);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    [Theory]
    [MemberData(nameof(Samples.TemplateIds), MemberType = typeof(Samples))]
    public async Task Post_creates_a_simulated_receipt_with_number_and_pdf(string templateId)
    {
        using var client = _factory.CreateClient();
        var session = await LoginAsync(client);

        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", session, Samples.Request(templateId)), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("true", Assert.Single(response.Headers.GetValues(SimulationModule.SimulationHeader)));
        using var receipt = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var root = receipt.RootElement;
        var spec = TemplateCatalog.All.Single(t => t.Id == templateId);
        Assert.True(root.GetProperty("simulated").GetBoolean());
        Assert.Matches(NumberFormat(), root.GetProperty("number").GetString()!);
        Assert.Equal(SubmissionNames.ToWire(spec.Type), root.GetProperty("type").GetString());
        Assert.Equal(SubmissionNames.ToWire(spec.Recipient), root.GetProperty("recipient").GetString());
        Assert.True(DateTimeOffset.TryParse(root.GetProperty("submittedAt").GetString(), out _));
        Assert.Equal("zlozone", root.GetProperty("status").GetString());

        var pdfUrl = root.GetProperty("pdfUrl").GetString()!;
        var pdf = await client.SendAsync(Request(HttpMethod.Get, pdfUrl, session), Ct);
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        var bytes = await pdf.Content.ReadAsByteArrayAsync(Ct);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        PdfText.AssertHas(PdfText.Extract(bytes), root.GetProperty("number").GetString()!);
    }

    [Fact]
    public async Task Stored_rows_contain_neither_personal_data_nor_the_raw_session_id()
    {
        using var client = _factory.CreateClient();
        var session = await LoginAsync(client);
        foreach (var id in new[] { LeaveTemplateIds.Rodzicielski, "dane_zam", "odpis_aktu", "zapytanie_ezus" })
        {
            var response = await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", session, Samples.Request(id)), Ct);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var serialized = JsonSerializer.Serialize(_store.Rows);
        Assert.Equal(4, _store.Rows.Count);
        Assert.DoesNotContain("Ćwikła", serialized);
        Assert.DoesNotContain("Łódź", serialized);
        Assert.DoesNotContain(session, serialized);
    }

    [Fact]
    public async Task My_submissions_are_listed_per_session_newest_first()
    {
        using var client = _factory.CreateClient();
        var first = await LoginAsync(client);
        var second = await LoginAsync(client, "halina-demo");

        (await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", first, Samples.Request("odpis_aktu")), Ct)).EnsureSuccessStatusCode();
        await Task.Delay(20, Ct);
        (await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", first, Samples.Request("zapytanie_ezus")), Ct)).EnsureSuccessStatusCode();
        (await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", second, Samples.Request("dane_zam")), Ct)).EnsureSuccessStatusCode();

        var firstList = await client.SendAsync(Request(HttpMethod.Get, "/api/submissions", first), Ct);
        using var firstDoc = JsonDocument.Parse(await firstList.Content.ReadAsStringAsync(Ct));
        var items = firstDoc.RootElement.EnumerateArray().ToList();
        Assert.Equal(["zapytanie_ezus", "odpis_aktu"], items.Select(i => i.GetProperty("type").GetString()));
        Assert.All(items, i => Assert.True(i.GetProperty("simulated").GetBoolean()));
        Assert.Contains("Zapytanie do ZUS", items[0].GetProperty("summary").GetString());

        var secondList = await client.SendAsync(Request(HttpMethod.Get, "/api/submissions", second), Ct);
        using var secondDoc = JsonDocument.Parse(await secondList.Content.ReadAsStringAsync(Ct));
        Assert.Equal("dane_zam", Assert.Single(secondDoc.RootElement.EnumerateArray()).GetProperty("type").GetString());
    }

    [Fact]
    public async Task Another_session_cannot_download_the_pdf()
    {
        using var client = _factory.CreateClient();
        var owner = await LoginAsync(client);
        var other = await LoginAsync(client, "halina-demo");
        var created = await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", owner, Samples.Request("odpis_aktu")), Ct);
        using var doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync(Ct));
        var pdfUrl = doc.RootElement.GetProperty("pdfUrl").GetString()!;

        var stolen = await client.SendAsync(Request(HttpMethod.Get, pdfUrl, other), Ct);
        Assert.Equal(HttpStatusCode.NotFound, stolen.StatusCode);
        Assert.Equal("application/problem+json", stolen.Content.Headers.ContentType?.MediaType);

        var unknown = await client.SendAsync(Request(HttpMethod.Get, $"/api/submissions/{Guid.NewGuid()}/pdf", owner), Ct);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var malformed = await client.SendAsync(Request(HttpMethod.Get, "/api/submissions/nie-guid/pdf", owner), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/submissions")]
    [InlineData("POST", "/api/submissions")]
    [InlineData("GET", "/api/submissions/00000000-0000-0000-0000-000000000001/pdf")]
    [InlineData("GET", "/api/sim/me/debt-certificate")]
    public async Task Missing_or_unknown_session_returns_401_problem(string method, string path)
    {
        using var client = _factory.CreateClient();
        var body = method == "POST" ? Samples.Request("odpis_aktu") : null;

        foreach (var session in new string?[] { null, "nieistniejaca-sesja" })
        {
            var response = await client.SendAsync(Request(new HttpMethod(method), path, session, body), Ct);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Invalid_requests_return_400_problem_details_with_field_errors()
    {
        using var client = _factory.CreateClient();
        var session = await LoginAsync(client);

        async Task<JsonDocument> Post(object body, HttpStatusCode expected = HttpStatusCode.BadRequest)
        {
            var response = await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", session, body), Ct);
            Assert.Equal(expected, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        }

        using (var unknownType = await Post(new { type = "pozew", fields = new Dictionary<string, string>() }))
        {
            Assert.True(unknownType.RootElement.GetProperty("errors").TryGetProperty("type", out _));
        }

        using (var noTemplate = await Post(new { type = "wniosek_urlop", fields = new Dictionary<string, string>() }))
        {
            Assert.True(noTemplate.RootElement.GetProperty("errors").TryGetProperty("template", out _));
        }

        using (var noName = await Post(new { type = "wniosek_urlop", template = "ojcowski", fields = new Dictionary<string, string> { ["ojcowskiOd"] = "2027-02-15" } }))
        {
            var errors = noName.RootElement.GetProperty("errors");
            Assert.True(errors.TryGetProperty("fields.imieNazwisko", out _));
            Assert.True(errors.TryGetProperty("fields.ojcowskiDo", out _));
        }

        var pesel = new Dictionary<string, string>(Samples.Fields("zapytanie_ezus")) { ["tresc"] = "PESEL 12345678901" };
        using (var withPesel = await Post(new { type = "zapytanie_ezus", fields = pesel }))
        {
            Assert.Contains("PESEL", withPesel.RootElement.GetProperty("errors").GetProperty("fields.tresc")[0].GetString());
        }

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/submissions")
        {
            Headers = { { SimulationModule.SessionHeader, session } },
            Content = new StringContent("{ nie json", Utf8Encoding, "application/json"),
        }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var numberField = new StringContent("""{"type":"odpis_aktu","fields":{"liczbaOdpisow":2}}""", Utf8Encoding, "application/json");
        var wrongType = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/submissions")
        {
            Headers = { { SimulationModule.SessionHeader, session } },
            Content = numberField,
        }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);
        Assert.Empty(_store.Rows);
    }

    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    [Fact]
    public async Task Templates_endpoint_describes_every_field_with_its_basis()
    {
        using var client = _factory.CreateClient();
        var templates = await client.GetFromJsonAsync<JsonElement>("/api/submissions/templates", Ct);

        Assert.Equal(TemplateCatalog.All.Count, templates.GetArrayLength());
        foreach (var template in templates.EnumerateArray())
        {
            Assert.All(template.GetProperty("fields").EnumerateArray(), f => Assert.False(string.IsNullOrEmpty(f.GetProperty("basis").GetString())));
        }
    }

    [Fact]
    public async Task Debt_certificate_is_a_pdf_with_balance_and_simulation_banner()
    {
        using var client = _factory.CreateClient();
        var session = await LoginAsync(client, "edyta-demo");

        var response = await client.SendAsync(Request(HttpMethod.Get, "/api/sim/me/debt-certificate?date=2027-05-05", session), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("true", Assert.Single(response.Headers.GetValues(SimulationModule.SimulationHeader)));
        var bytes = await response.Content.ReadAsByteArrayAsync(Ct);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        var text = PdfText.Extract(bytes);
        PdfText.AssertHas(text, "SYMULACJA — dokument nie jest zaświadczeniem ZUS");
        PdfText.AssertHas(text, "120,00 zł");
        PdfText.AssertHas(text, "05.05.2027");
        PdfText.AssertHas(text, "Edyta Pokazowa");
        PdfText.AssertHas(text, PdfRenderer.FooterText);
        Assert.Matches(@"BR-SYM-ZAS-\d{8}-[0-9A-HJKMNP-TV-Z]{8}", PdfText.Compact(text));
    }

    [Fact]
    public async Task Debt_certificate_without_account_says_so_and_validates_the_date()
    {
        using var client = _factory.CreateClient();
        var session = await LoginAsync(client, "klara-demo");

        var ok = await client.SendAsync(Request(HttpMethod.Get, "/api/sim/me/debt-certificate", session), Ct);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        PdfText.AssertHas(PdfText.Extract(await ok.Content.ReadAsByteArrayAsync(Ct)), "brak danych w symulatorze");

        foreach (var bad in new[] { "jutro", "2027-13-40", "1850-01-01" })
        {
            var response = await client.SendAsync(Request(HttpMethod.Get, $"/api/sim/me/debt-certificate?date={bad}", session), Ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Gateway_numbers_are_deterministic_human_readable_and_channel_specific()
    {
        var gateway = new SimulatedSubmissionGateway(new FixedTime(new DateTimeOffset(2026, 11, 21, 8, 30, 0, TimeSpan.Zero)));
        var fields = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };

        var a = await gateway.SubmitAsync(new Submission(SubmissionType.WniosekUrlop, SubmissionRecipient.Pracodawca, "klara-demo", fields), Ct);
        var b = await gateway.SubmitAsync(new Submission(SubmissionType.WniosekUrlop, SubmissionRecipient.Pracodawca, "klara-demo", fields.Reverse().ToDictionary()), Ct);
        var c = await gateway.SubmitAsync(new Submission(SubmissionType.WniosekUrlop, SubmissionRecipient.Pracodawca, "halina-demo", fields), Ct);
        var zus = await gateway.SubmitAsync(new Submission(SubmissionType.DaneZam, SubmissionRecipient.Zus, "klara-demo", fields), Ct);

        Assert.Equal(a.Number, b.Number);
        Assert.NotEqual(a.Number, c.Number);
        Assert.StartsWith("BR-SYM-URL-20261121-", a.Number);
        Assert.StartsWith("BR-SYM-ZAM-20261121-", zus.Number);
        Assert.Matches(NumberFormat(), a.Number);
        Assert.Equal(SubmissionRecipient.Zus, zus.Recipient);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            gateway.SubmitAsync(new Submission(SubmissionType.OdpisAktu, SubmissionRecipient.Zus, "klara-demo", fields), Ct));
    }

    [Fact]
    public async Task Every_channel_is_reachable_through_the_api()
    {
        using var client = _factory.CreateClient();
        var session = await LoginAsync(client);
        var recipients = new HashSet<string>();
        foreach (var spec in TemplateCatalog.All)
        {
            var response = await client.SendAsync(Request(HttpMethod.Post, "/api/submissions", session, Samples.Request(spec.Id)), Ct);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
            recipients.Add(doc.RootElement.GetProperty("recipient").GetString()!);
        }

        Assert.Equal(new HashSet<string> { "pracodawca", "zus", "ezus", "urzad_stanu_cywilnego" }, recipients);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
