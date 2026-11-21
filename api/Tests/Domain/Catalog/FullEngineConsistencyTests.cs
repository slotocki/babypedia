using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Tests.Reference;

namespace BabyRadnik.Tests.Domain.Catalog;

/// <summary>
/// Po złożeniu wyniku pełny silnik nie może dawać wyniku SPRZECZNEGO z „oczekiwane” przypadków uprawnienia-* i okresy-*
/// (ich własne ewaluatory sprawdzają moduły osobno). Sprzeczność = oczekiwano „przysługuje”, jest „nie przysługuje” (albo odwrotnie),
/// albo okres/terminy z przypadku nie zgadzają się, gdy pełny wynik jest „przysługuje”.
/// </summary>
public sealed class FullEngineConsistencyTests
{
    private static IEnumerable<string> FileNames() =>
        Directory.EnumerateFiles(Path.Combine(TestPaths.Fixtures, "reference"), "*.json")
            .Order()
            .Select(Path.GetFileName)
            .Where(n => n!.StartsWith("uprawnienia-") || n.StartsWith("okresy-"))!;

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (var name in FileNames())
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Full_engine_does_not_contradict_the_expected_outcome(string file)
    {
        var (contradictions, weaker) = Analyse(file);

        Assert.True(contradictions.Count == 0, $"{file}: {string.Join("; ", contradictions)}");
        Assert.True(weaker.All(w => w.Contains("brakuje_danych") || w.Contains("Brak parametru")), $"{file}: słabsze wyniki bez wyjaśnienia: {string.Join("; ", weaker)}");
    }

    [Fact]
    public void Summary_of_all_cases_has_no_contradictions()
    {
        var contradictions = FileNames()
            .SelectMany(f => Analyse(f).Contradictions.Select(c => $"{f}: {c}"))
            .ToList();

        Assert.Empty(contradictions);
    }

    public static (List<string> Contradictions, List<string> Weaker) Analyse(string file)
    {
        var referenceCase = ReferenceCase.Load(Path.Combine(TestPaths.Fixtures, "reference", file));
        var contradictions = new List<string>();
        var weaker = new List<string>();
        if (referenceCase.DoUzupelnienia || referenceCase.Fakty is null || referenceCase.Oczekiwane is not { } expected)
        {
            return (contradictions, weaker);
        }

        var provider = CatalogKit.Params([.. referenceCase.Parametry]);
        var request = referenceCase.Request() with { AsOf = referenceCase.AsOf ?? CatalogKit.AsOf };
        var result = CatalogKit.Engine(provider).Assess(request);
        var actualJson = JsonSerializer.SerializeToElement(result, ApiJson.Options);

        if (!expected.TryGetProperty("benefits", out var benefits))
        {
            return (contradictions, weaker);
        }

        foreach (var eb in benefits.EnumerateArray())
        {
            var id = eb.GetProperty("id").GetString()!;
            var person = eb.TryGetProperty("person", out var p) ? p.GetString()! : "wnioskodawca";
            var label = $"{id}/{person}";
            var ab = actualJson.GetProperty("benefits").EnumerateArray()
                .FirstOrDefault(b => b.GetProperty("id").GetString() == id && b.GetProperty("person").GetString() == person);
            if (ab.ValueKind != JsonValueKind.Object)
            {
                contradictions.Add($"{label}: brak w pełnym wyniku");
                continue;
            }

            var expectedStatus = eb.GetProperty("status").GetString()!;
            var actualStatus = ab.GetProperty("status").GetString()!;
            if (expectedStatus != actualStatus)
            {
                var definite = new[] { "przysługuje", "nie_przysługuje" };
                if (definite.Contains(expectedStatus) && definite.Contains(actualStatus))
                {
                    contradictions.Add($"{label}: oczekiwano {expectedStatus}, pełny silnik daje {actualStatus} ({ab.GetProperty("reason")})");
                }
                else
                {
                    weaker.Add($"{label}: oczekiwano {expectedStatus}, pełny silnik daje {actualStatus} ({ab.GetProperty("reason")})");
                }

                continue;
            }

            if (actualStatus != "przysługuje")
            {
                continue;
            }

            foreach (var name in new[] { "period", "payer", "whereToApply", "parts", "deadlineIds", "deadlines", "documentsContain" })
            {
                if (!eb.TryGetProperty(name, out var value))
                {
                    continue;
                }

                try
                {
                    var single = JsonSerializer.Deserialize<JsonElement>(
                        $$"""{"id":"{{id}}","person":"{{person}}","status":"{{expectedStatus}}","{{name}}":{{value.GetRawText()}}}""");
                    var fragment = JsonSerializer.Deserialize<JsonElement>(
                        $$"""{"benefits":[{{single.GetRawText()}}]}""");
                    ReferenceAssert.Matches(referenceCase with { Oczekiwane = fragment }, result);
                }
                catch (Exception ex) when (ex is Xunit.Sdk.XunitException)
                {
                    contradictions.Add($"{label}.{name}: {ex.Message}");
                }
            }
        }

        return (contradictions, weaker);
    }
}
