using System.Text.RegularExpressions;
using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Tests.Domain.Catalog.CatalogKit;

namespace BabyRadnik.Tests.Domain.Catalog;

/// <summary>
/// Teksty, które widzi użytkownik (ścieżka decyzji, podstawy prawne, uwagi, terminy, ryzyka), nie zawierają odwołań do
/// dokumentów roboczych, nazw ustawień ani dat w formacie RRRR-MM-DD.
/// </summary>
public sealed partial class UserFacingTextTests
{
    private const string Pregnancy = """{ "sprawa": { "zdarzenie.rodzaj": { "value": "ciaza", "source": "user" }, "zdarzenie.data": { "value": "2027-03-15", "source": "user" }, "zdarzenie.dlaKogo": { "value": "ja", "source": "user" }, "dziecko.liczba": { "value": 1, "source": "user" } } }""";
    private const string Born = """{ "sprawa": { "zdarzenie.rodzaj": { "value": "urodzenie", "source": "user" }, "zdarzenie.data": { "value": "2026-10-08", "source": "user" }, "zdarzenie.dlaKogo": { "value": "ja", "source": "user" }, "dziecko.liczba": { "value": 1, "source": "user" } } }""";

    private static readonly string[] Forbidden =
    [
        "instrukcj", "kontrakt:", "ROZBIEZNOSCI", "do weryfikacji", "do potwierdzenia", "pobrane,", "wg treści zadania", "Przyjmuję ostatnie znane saldo",
    ];

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}")]
    private static partial Regex IsoDate();

    [GeneratedRegex(@"(?i)parametr\w* „[a-z0-9_.]+”")]
    private static partial Regex ParameterKey();

    public static TheoryData<string, string> Cases() => new()
    {
        { "anna-przyklad", Pregnancy },
        { "edyta-demo", Pregnancy },
        { "monika-demo", Born },
        { "weronika-demo", Pregnancy },
        { "klara-demo", Pregnancy },
        { "tadeusz-demo", Born },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Persona_results_use_plain_language_and_polish_dates(string persona, string sprawa)
    {
        var facts = LoadPersona(persona, sprawa);
        var me = facts.Wnioskodawca with { OsobaRola = F(persona == "tadeusz-demo" ? ParentRole.Ojciec : ParentRole.Matka) };
        var result = FullEngine().Assess(new AssessmentRequest(facts with { Wnioskodawca = me }, AsOf));

        AssertPlain(result);
    }

    [Fact]
    public void Stillbirth_result_uses_plain_language_and_polish_dates()
    {
        var sprawa = Case(EventKind.Urodzenie, "2026-11-10") with { DzieckoMartweUrodzenie = F(true) };
        var me = Person(ParentRole.Matka, Title(from: "2021-02-01"), 150, b =>
            b.Bases = [.. Enumerable.Range(0, 12).Select(i => new ContributionBase(new DateOnly(2025, 10, 1).AddMonths(i).ToString("yyyy-MM"), 6000m))]);

        AssertPlain(FullEngine().Assess(new AssessmentRequest(Facts(sprawa, me), AsOf)));
    }

    private static void AssertPlain(AssessmentResult result)
    {
        var texts = result.Benefits.SelectMany(b => new[] { b.Reason, b.LegalBasis, b.WhatWouldChange }
                .Concat(b.Notes)
                .Concat(b.Documents)
                .Concat(b.Deadlines.SelectMany(d => new[] { d.Text, d.Basis, d.Consequence }))
                .Concat(b.Trace.SelectMany(t => new[] { t.Text, t.Basis, t.Condition })))
            .Concat(result.Risks.SelectMany(r => new[] { r.Text, r.Basis }))
            .OfType<string>()
            .Distinct()
            .ToList();
        Assert.NotEmpty(texts);

        var problems = texts.Where(t => Forbidden.Any(f => t.Contains(f, StringComparison.OrdinalIgnoreCase)) || IsoDate().IsMatch(t) || ParameterKey().IsMatch(t)).ToList();
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }
}
