using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Tests.Reference;

/// <summary>Porównanie częściowe: sprawdzane są tylko pola podane w "oczekiwane" (contracts/reference.schema.json).</summary>
public static class ReferenceAssert
{
    private const decimal MoneyTolerance = 0.01m;

    public static void Matches(ReferenceCase referenceCase, AssessmentResult actual)
    {
        if (referenceCase.Oczekiwane is not { } expected)
        {
            return;
        }

        var id = referenceCase.Id;
        var actualJson = JsonSerializer.SerializeToElement(actual, ApiJson.Options);

        if (expected.TryGetProperty("benefits", out var benefits))
        {
            foreach (var eb in benefits.EnumerateArray())
            {
                var benefitId = eb.GetProperty("id").GetString()!;
                var person = eb.TryGetProperty("person", out var p) ? p.GetString()! : "wnioskodawca";
                var ab = actualJson.GetProperty("benefits").EnumerateArray()
                    .FirstOrDefault(b => b.GetProperty("id").GetString() == benefitId && b.GetProperty("person").GetString() == person);
                Assert.True(ab.ValueKind == JsonValueKind.Object, $"{id}: brak świadczenia {benefitId}/{person} w wyniku");
                MatchBenefit($"{id} {benefitId}/{person}", eb, ab);
            }
        }

        if (expected.TryGetProperty("missingFacts", out var missing))
        {
            var actualFacts = actual.MissingFacts.Select(m => m.Fact).ToHashSet();
            foreach (var fact in missing.EnumerateArray().Select(m => m.GetString()!))
            {
                Assert.True(actualFacts.Contains(fact), $"{id}: silnik nie zapytał o fakt {fact}");
            }
        }

        if (expected.TryGetProperty("validationErrors", out var errors))
        {
            foreach (var e in errors.EnumerateArray())
            {
                var code = e.TryGetProperty("code", out var c) ? c.GetString() : null;
                var fact = e.TryGetProperty("fact", out var f) ? f.GetString() : null;
                Assert.True(
                    actual.ValidationErrors.Any(v => (code is null || v.Code == code) && (fact is null || v.Fact == fact)),
                    $"{id}: brak błędu walidacji code={code} fact={fact}");
            }
        }

        if (expected.TryGetProperty("risks", out var risks))
        {
            var actualRisks = actual.Risks.Select(r => r.Id).ToHashSet();
            foreach (var risk in risks.EnumerateArray().Select(r => r.GetString()!))
            {
                Assert.True(actualRisks.Contains(risk), $"{id}: brak ryzyka {risk}");
            }
        }

        if (expected.TryGetProperty("nextStepDeadlineId", out var next))
        {
            Assert.Equal(next.GetString(), actual.NextStep?.DeadlineId);
        }
    }

    private static void MatchBenefit(string label, JsonElement expected, JsonElement actual)
    {
        Assert.True(
            expected.GetProperty("status").GetString() == actual.GetProperty("status").GetString(),
            $"{label}: status oczekiwany {expected.GetProperty("status")}, jest {actual.GetProperty("status")}");

        foreach (var name in new[] { "rate", "payer", "whereToApply" })
        {
            if (expected.TryGetProperty(name, out var e))
            {
                AssertValue($"{label}.{name}", e, actual.GetProperty(name));
            }
        }

        if (expected.TryGetProperty("period", out var period))
        {
            MatchObject($"{label}.period", period, actual.GetProperty("period"));
        }

        if (expected.TryGetProperty("amount", out var amount))
        {
            var a = actual.GetProperty("amount");
            Assert.True(a.ValueKind == JsonValueKind.Object, $"{label}: brak kwoty");
            foreach (var prop in amount.EnumerateObject())
            {
                if (prop.Name == "monthly")
                {
                    var monthly = a.GetProperty("basis").GetDecimal() * actual.GetProperty("rate").GetDecimal();
                    AssertMoney($"{label}.amount.monthly", prop.Value.GetDecimal(), Math.Round(monthly, 2, MidpointRounding.AwayFromZero));
                }
                else
                {
                    AssertValue($"{label}.amount.{prop.Name}", prop.Value, a.GetProperty(prop.Name));
                }
            }
        }

        if (expected.TryGetProperty("parts", out var parts))
        {
            var actualParts = actual.GetProperty("parts").EnumerateArray().ToList();
            var expectedParts = parts.EnumerateArray().ToList();
            Assert.True(expectedParts.Count == actualParts.Count, $"{label}: liczba części {expectedParts.Count} ≠ {actualParts.Count}");
            for (var i = 0; i < expectedParts.Count; i++)
            {
                var ap = actualParts[i];
                foreach (var prop in expectedParts[i].EnumerateObject())
                {
                    var actualValue = prop.Name switch
                    {
                        "from" or "to" or "days" or "weeks" => ap.GetProperty("period").GetProperty(prop.Name),
                        "total" => ap.GetProperty("amount").GetProperty("total"),
                        _ => ap.GetProperty(prop.Name),
                    };
                    AssertValue($"{label}.parts[{i}].{prop.Name}", prop.Value, actualValue);
                }
            }
        }

        if (expected.TryGetProperty("deadlineIds", out var deadlineIds))
        {
            var ids = actual.GetProperty("deadlines").EnumerateArray().Select(d => d.GetProperty("id").GetString()).ToHashSet();
            foreach (var d in deadlineIds.EnumerateArray())
            {
                Assert.True(ids.Contains(d.GetString()), $"{label}: brak terminu {d}");
            }
        }

        if (expected.TryGetProperty("deadlines", out var deadlines))
        {
            foreach (var d in deadlines.EnumerateArray())
            {
                var did = d.GetProperty("id").GetString();
                var ad = actual.GetProperty("deadlines").EnumerateArray().FirstOrDefault(x => x.GetProperty("id").GetString() == did);
                Assert.True(ad.ValueKind == JsonValueKind.Object, $"{label}: brak terminu {did}");
                MatchObject($"{label}.deadlines.{did}", d, ad);
            }
        }

        if (expected.TryGetProperty("documentsContain", out var docs))
        {
            var actualDocs = actual.GetProperty("documents").EnumerateArray().Select(x => x.GetString()!).ToList();
            foreach (var doc in docs.EnumerateArray().Select(x => x.GetString()!))
            {
                Assert.True(actualDocs.Any(x => x.Contains(doc, StringComparison.OrdinalIgnoreCase)), $"{label}: brak dokumentu „{doc}”");
            }
        }

        ContainsText(label, expected, actual, "reasonContains", "reason");
        ContainsText(label, expected, actual, "legalBasisContains", "legalBasis");
        ContainsText(label, expected, actual, "whatWouldChangeContains", "whatWouldChange");

        if (expected.TryGetProperty("traceRules", out var rules))
        {
            var actualRules = actual.GetProperty("trace").EnumerateArray().Select(t => t.GetProperty("rule").GetString()).ToHashSet();
            foreach (var rule in rules.EnumerateArray())
            {
                Assert.True(actualRules.Contains(rule.GetString()), $"{label}: brak reguły {rule} w ścieżce decyzji");
            }
        }
    }

    private static void ContainsText(string label, JsonElement expected, JsonElement actual, string expectedName, string actualName)
    {
        if (expected.TryGetProperty(expectedName, out var e))
        {
            var text = actual.GetProperty(actualName).ValueKind == JsonValueKind.String ? actual.GetProperty(actualName).GetString()! : "";
            Assert.True(text.Contains(e.GetString()!, StringComparison.OrdinalIgnoreCase), $"{label}: {actualName} „{text}” nie zawiera „{e}”");
        }
    }

    private static void MatchObject(string label, JsonElement expected, JsonElement actual)
    {
        Assert.True(actual.ValueKind == JsonValueKind.Object, $"{label}: brak w wyniku");
        foreach (var prop in expected.EnumerateObject())
        {
            AssertValue($"{label}.{prop.Name}", prop.Value, actual.GetProperty(prop.Name));
        }
    }

    private static void AssertValue(string label, JsonElement expected, JsonElement actual)
    {
        if (expected.ValueKind == JsonValueKind.Number && actual.ValueKind == JsonValueKind.Number)
        {
            AssertMoney(label, expected.GetDecimal(), actual.GetDecimal());
            return;
        }

        Assert.True(expected.ToString() == actual.ToString(), $"{label}: oczekiwano {expected}, jest {actual}");
    }

    private static void AssertMoney(string label, decimal expected, decimal actual) =>
        Assert.True(Math.Abs(expected - actual) < MoneyTolerance, $"{label}: oczekiwano {expected}, jest {actual}");
}
