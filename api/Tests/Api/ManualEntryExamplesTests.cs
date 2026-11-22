using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Reference;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Tests.Api;

/// <summary>
/// Fakty zbudowane przez formularz „Wprowadź dane ręcznie” (web/src/features/intake/manualFacts.ts + extrasFacts.ts)
/// wysłane do /api/assessment. Pliki w Api/ManualEntry wygenerowano tym samym kodem, którego używa formularz.
/// Parametry z fixtures/parameters.seed.json zamiast bazy, żeby test nie wymagał Postgresa.
/// </summary>
public sealed class ManualEntryExamplesTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<JsonElement> Assess(string name)
    {
        var json = File.ReadAllText(Path.Combine(TestPaths.Root, "api", "Tests", "Api", "ManualEntry", $"{name}.json"));
        using var request = JsonDocument.Parse(json);
        var body = new Dictionary<string, object?>
        {
            ["facts"] = request.RootElement.GetProperty("facts"),
            ["asOf"] = "2026-11-22",
        };
        using var client = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IParameterProvider>(SeedParameterProvider.Load()))).CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var response = await client.PostAsJsonAsync("/api/assessment", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.IsSuccessStatusCode, text);
        var result = JsonDocument.Parse(text).RootElement.Clone();
        TestContext.Current.TestOutputHelper?.WriteLine(Summary(name, result));
        if (Environment.GetEnvironmentVariable("MANUAL_ENTRY_DUMP") is { Length: > 0 } dir)
        {
            File.WriteAllText(Path.Combine(dir, $"{name}.result.txt"), Summary(name, result) + "\n\n" + text);
        }

        return result;
    }

    private static string Summary(string name, JsonElement result)
    {
        var sb = new StringBuilder($"== {name}\n");
        foreach (var b in result.GetProperty("benefits").EnumerateArray())
        {
            var status = b.GetProperty("status").GetString();
            if (status == "nie_dotyczy")
            {
                continue;
            }

            sb.Append($"{b.GetProperty("person").GetString()} {b.GetProperty("id").GetString()}: {status}");
            if (b.TryGetProperty("period", out var p) && p.ValueKind == JsonValueKind.Object)
            {
                sb.Append($" {p.GetProperty("from").GetString()}..{p.GetProperty("to").GetString()}");
            }

            if (b.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Object)
            {
                sb.Append($" podstawa {a.GetProperty("basis").GetDecimal()} dziennie {a.GetProperty("daily").GetDecimal()}");
            }

            if (b.TryGetProperty("payer", out var payer) && payer.ValueKind == JsonValueKind.String)
            {
                sb.Append($" płatnik {payer.GetString()}");
            }

            if (b.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String)
            {
                sb.Append($" — {reason.GetString()}");
            }

            sb.AppendLine();
        }

        foreach (var v in result.GetProperty("validationErrors").EnumerateArray())
        {
            sb.AppendLine($"błąd: {v.GetProperty("code").GetString()} {v.GetProperty("message").GetString()}");
        }

        foreach (var m in result.GetProperty("missingFacts").EnumerateArray())
        {
            sb.AppendLine($"brakuje: {m.GetProperty("fact").GetString()}");
        }

        return sb.ToString();
    }

    private static JsonElement Benefit(JsonElement result, string id, string person = "wnioskodawca") =>
        result.GetProperty("benefits").EnumerateArray().Single(b => b.GetProperty("id").GetString() == id && b.GetProperty("person").GetString() == person);

    private static string Status(JsonElement benefit) => benefit.GetProperty("status").GetString()!;

    private static string? PeriodTo(JsonElement benefit) =>
        benefit.GetProperty("period") is { ValueKind: JsonValueKind.Object } p ? p.GetProperty("to").GetString() : null;

    /// <summary>(a) 0,63 etatu + działalność bez chorobowego — zbieg rozstrzyga inna część silnika; tu tylko: fakty przyjęte bez błędów.</summary>
    [Fact]
    public async Task Part_time_employee_with_business_without_sickness_insurance_is_assessed()
    {
        var result = await Assess("a");
        Assert.Empty(result.GetProperty("validationErrors").EnumerateArray());
        Assert.NotEqual("brakuje_danych", Status(Benefit(result, "macierzynski")));
    }

    /// <summary>(a') 0,63 etatu, urlop rodzicielski w 2 częściach po 16 tyg. i praca na 1/2 etatu przez 8 tyg.</summary>
    [Fact]
    public async Task Parental_leave_split_into_parts_is_accepted()
    {
        var result = await Assess("a2");
        Assert.Empty(result.GetProperty("validationErrors").EnumerateArray());
        Assert.Equal("przysługuje", Status(Benefit(result, "rodzicielski")));
    }

    /// <summary>(b) Zleceniobiorczyni z dobrowolnym chorobowym i dwoma e-ZLA z kodem B, termin porodu z przeszłości.</summary>
    [Fact]
    public async Task Pregnancy_sick_leaves_from_manual_entry_give_sickness_benefit()
    {
        var result = await Assess("b");
        Assert.Equal("przysługuje", Status(Benefit(result, "chorobowy_w_ciazy")));
        Assert.Equal("2024-12-27", PeriodTo(Benefit(result, "macierzynski")));
    }

    /// <summary>(c) Matka rezygnuje po 14 tyg., ojciec (drugi rodzic) przejmuje resztę urlopu macierzyńskiego.</summary>
    [Fact]
    public async Task Mother_resigning_after_14_weeks_lets_father_take_over()
    {
        var result = await Assess("c");
        Assert.Equal("2025-06-15", PeriodTo(Benefit(result, "macierzynski")));
        Assert.Equal("przysługuje", Status(Benefit(result, "przejecie_przez_ojca", "drugiRodzic")));
    }

    /// <summary>(d) Kolejne umowy o pracę (5/8 i 1/2 etatu, ten sam i inny pracodawca) z datą końca pierwszego tytułu.</summary>
    [Fact]
    public async Task Several_employment_titles_are_accepted()
    {
        var result = await Assess("d");
        Assert.Empty(result.GetProperty("validationErrors").EnumerateArray());
        Assert.NotEmpty(result.GetProperty("benefits").EnumerateArray());
    }
}
