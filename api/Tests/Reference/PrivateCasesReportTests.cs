using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Domain.Catalog;

namespace BabyRadnik.Tests.Reference;

/// <summary>
/// Porównanie silnika z decyzjami ZUS dla spraw z katalogu prywatnego (poza repozytorium).
/// Uruchamiane tylko, gdy ustawiono CASES_REPORT_DIR (katalog z cases.json i personas/); wynik: raport-silnika.md w tym katalogu.
/// Test niczego nie rozstrzyga asercją — rozbieżności ocenia człowiek.
/// </summary>
public sealed class PrivateCasesReportTests(AssessmentFixture fixture) : IClassFixture<AssessmentFixture>
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Writes_engine_report_for_private_cases()
    {
        var dir = Environment.GetEnvironmentVariable("CASES_REPORT_DIR");
        if (string.IsNullOrWhiteSpace(dir) || !File.Exists(Path.Combine(dir, "cases.json")))
        {
            Assert.Skip("Brak CASES_REPORT_DIR — raport dla spraw prywatnych pominięty.");
        }

        using var client = fixture.CreateClient();
        using var cases = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dir, "cases.json"), Ct));
        var report = new StringBuilder("# Silnik a decyzje ZUS\n\n");
        var summary = new List<string>();

        foreach (var entry in cases.RootElement.GetProperty("cases").EnumerateArray())
        {
            var id = entry.GetProperty("id").GetString()!;
            var expected = entry.GetProperty("oczekiwanaDecyzja").GetString();
            using var persona = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dir, "personas", id + ".json"), Ct));
            var facts = persona.RootElement.GetProperty("fakty").Deserialize<FactSet>(ApiJson.Options)!;

            report.Append(CultureInfo.InvariantCulture, $"## {id} — {entry.GetProperty("tytul").GetString()}\n\nZUS: {expected}\n\n");
            var response = await client.PostAsJsonAsync("/api/assessment", new AssessmentRequest(facts), ApiJson.Options, Ct);
            if (!response.IsSuccessStatusCode)
            {
                report.Append(CultureInfo.InvariantCulture, $"BŁĄD {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(Ct)}\n\n");
                summary.Add($"| {id} | {expected} | błąd {(int)response.StatusCode} | |");
                continue;
            }

            var result = (await response.Content.ReadFromJsonAsync<AssessmentResult>(ApiJson.Options, Ct))!;
            var confirmed = entry.TryGetProperty("potwierdzoneOkresy", out var periods) ? periods.EnumerateArray().ToList() : [];
            var matched = 0;
            var granted = new List<string>();
            foreach (var b in result.Benefits)
            {
                var benefitId = JsonSerializer.Serialize(b.Id, ApiJson.Options).Trim('"');
                var status = JsonSerializer.Serialize(b.Status, ApiJson.Options).Trim('"');
                var period = b.Period is null ? "" : $" {b.Period.From:yyyy-MM-dd}–{b.Period.To:yyyy-MM-dd} ({b.Period.Days} dni)";
                var amount = b.Amount is null ? "" : string.Create(CultureInfo.InvariantCulture, $" podstawa {b.Amount.Basis}, dziennie {b.Amount.Daily}, razem {b.Amount.Total}");
                var zus = confirmed.FirstOrDefault(p => p.GetProperty("swiadczenie").GetString() == benefitId);
                var compare = "";
                if (zus.ValueKind == JsonValueKind.Object)
                {
                    var same = b.Period is not null
                        && zus.GetProperty("od").GetString() == b.Period.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        && zus.GetProperty("do").GetString() == b.Period.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    matched += same ? 1 : 0;
                    compare = same ? " — ZGODNE z ZUS" : $" — ZUS: {zus.GetProperty("od").GetString()}–{zus.GetProperty("do").GetString()} ({zus.GetProperty("dni").GetInt32()} dni)";
                }

                if (b.Status == BenefitStatus.Przysluguje)
                {
                    granted.Add(benefitId);
                }

                report.Append(CultureInfo.InvariantCulture, $"- {benefitId} ({b.Person}): {status}{period}{amount}{compare}\n");
                if (b.Status is not BenefitStatus.Przysluguje and not BenefitStatus.NieDotyczy && !string.IsNullOrWhiteSpace(b.Reason))
                {
                    report.Append(CultureInfo.InvariantCulture, $"  - powód: {b.Reason}\n");
                }
            }

            if (result.MissingFacts.Count > 0)
            {
                report.Append(CultureInfo.InvariantCulture, $"- pytania: {string.Join("; ", result.MissingFacts.Select(m => m.Fact))}\n");
            }

            if (result.ValidationErrors.Count > 0)
            {
                report.Append(CultureInfo.InvariantCulture, $"- walidacja: {string.Join("; ", result.ValidationErrors.Select(v => v.Message))}\n");
            }

            report.Append('\n');
            summary.Add($"| {id} | {expected} | {(granted.Count == 0 ? "nic nie przysługuje" : string.Join(", ", granted))} | {matched}/{confirmed.Count} | {result.MissingFacts.Count} |");
        }

        var header = "| sprawa | ZUS | silnik: przysługuje | okresy zgodne | pytań |\n|---|---|---|---|---|\n";
        await File.WriteAllTextAsync(Path.Combine(dir, "raport-silnika.md"), report + "## Podsumowanie\n\n" + header + string.Join('\n', summary) + "\n", Ct);
    }
}
