using System.Text.RegularExpressions;

namespace BabyRadnik.Infrastructure.Assistant;

internal static partial class AssistantPolicy
{
    public static string Normalize(string text) =>
        text.ToLowerInvariant().Replace('ą', 'a').Replace('ć', 'c').Replace('ę', 'e')
            .Replace('ł', 'l').Replace('ń', 'n').Replace('ó', 'o').Replace('ś', 's')
            .Replace('ź', 'z').Replace('ż', 'z');

    public static bool HasSensitiveData(string text) => SensitiveData().IsMatch(text);

    public static bool IsPersonalCase(string text) => PersonalCase().IsMatch(Normalize(text));

    public static AssistantAnswer? Verify(ModelClaim[] claims, IReadOnlyList<LegalExcerpt> sources)
    {
        var paragraphs = new List<string>();
        var citations = new List<AssistantCitation>();
        foreach (var claim in claims)
        {
            if (claim is null || string.IsNullOrWhiteSpace(claim.Text)) continue;
            if (HasSensitiveData(claim.Text) || IsPersonalCase(claim.Text) || Money().IsMatch(Normalize(claim.Text)))
                return null;
            if (claim.Citations is not { Length: > 0 } || SentenceBreak().IsMatch(claim.Text.Trim())) return null;

            var references = new List<int>();
            foreach (var citation in claim.Citations)
            {
                if (citation is null || citation.DocumentIndex < 0 || citation.DocumentIndex >= sources.Count)
                    return null;
                var source = sources[citation.DocumentIndex];
                var quote = citation.CitedText?.Trim();
                if (string.IsNullOrEmpty(quote) || !CollapseSpace(source.Text).Contains(CollapseSpace(quote), StringComparison.Ordinal))
                    return null;

                var verified = source.Citation(quote);
                var index = citations.IndexOf(verified);
                if (index < 0)
                {
                    index = citations.Count;
                    citations.Add(verified);
                }
                references.Add(index + 1);
            }
            paragraphs.Add($"{claim.Text.Trim()} [{string.Join(", ", references.Distinct())}]");
        }

        return paragraphs.Count == 0 ? null : new AssistantAnswer(
            string.Join("\n", paragraphs), citations,
            "Odpowiedź wygenerował model językowy na podstawie pokazanych przepisów. Twoją sprawę sprawdzisz w kalkulatorze.",
            "ai");
    }

    private static string CollapseSpace(string text) => Whitespace().Replace(text, " ");

    [GeneratedRegex(@"\b\d(?:[\s.()-]*\d){8,}\b|[\p{L}\d._%+\-]+\s*@\s*[\p{L}\d.\-]+\.[\p{L}]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveData();

    [GeneratedRegex(@"\b(?:moj\w*\s+(?:pesel|pracodawc\w*|dzieck\w*|syn\w*|cork\w*|maz\w*|zon\w*|spraw\w*)|urodzilam|urodzilem|zarabiam|zarabialam|pracuje od|termin mam|przysluguje (?:mi|ci|pani|panu)|czy (?:dostane|mam prawo)|ile (?:dostane|otrzymam)|czy moge|przy pensji|nazywam sie|na imie|moj adres)\b")]
    private static partial Regex PersonalCase();

    [GeneratedRegex(@"(?:\d|\b(?:sto|tysiac\w*|tysiecy|milion\w*))\s*(?:zl\w*|pln\b|euro\b|eur\b|usd\b)|\b(?:pln|eur|usd)\s*\d|[€$]\s*\d", RegexOptions.IgnoreCase)]
    private static partial Regex Money();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[!?]\s+\p{Lu}|(?<!\b(?:art|np|ust|pkt|poz|lit|tj))\.\s+\p{Lu}", RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex SentenceBreak();
}
