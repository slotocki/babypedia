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

    public static bool IsFollowUp(string text) => FollowUp().IsMatch(Normalize(text));

    public static AssistantAnswer? Verify(ModelCitation[] selection, IReadOnlyList<LegalExcerpt> sources)
    {
        if (selection.Length is 0 or > 3) return null;

        var citations = new List<AssistantCitation>();
        foreach (var citation in selection)
        {
            if (citation is null || citation.DocumentIndex < 0 || citation.DocumentIndex >= sources.Count ||
                string.IsNullOrWhiteSpace(citation.CitedText) || citation.CitedText.Length > 600)
                return null;

            var source = sources[citation.DocumentIndex];
            var text = CollapseSpace(source.Text);
            var quote = CollapseSpace(citation.CitedText.Trim());
            var start = text.IndexOf(quote, StringComparison.Ordinal);
            if (start < 0 || quote.Length < 24 || HasSensitiveData(quote) || Money().IsMatch(Normalize(quote)))
                return null;

            // Render the text from the repository, even when the model returns a matching quote.
            var verified = source.Citation(text.Substring(start, quote.Length));
            if (!citations.Contains(verified)) citations.Add(verified);
        }

        return new AssistantAnswer(
            "Te fragmenty pasują do Twojego pytania.", citations,
            "Cytaty wybrał model językowy. Indywidualną sprawę ocenia kalkulator.",
            "ai");
    }

    private static string CollapseSpace(string text) => Whitespace().Replace(text, " ");

    [GeneratedRegex(@"\b\d(?:[\s.()-]*\d){8,}\b|[\p{L}\d._%+\-]+\s*@\s*[\p{L}\d.\-]+\.[\p{L}]{2,}|\b(?:oc_sk_|sk-ant-|sk-proj-)[a-z\d_-]+", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveData();

    [GeneratedRegex(@"\b(?:moj\w*\s+(?:pesel|pracodawc\w*|dzieck\w*|syn\w*|cork\w*|maz\w*|zon\w*|spraw\w*)|urodzilam|urodzilem|zarabiam|zarabialam|pracuje od|termin mam|przysluguje (?:mi|ci|pani|panu)|czy (?:dostane|mam prawo)|ile (?:dostane|otrzymam)|czy moge|przy pensji|nazywam sie|na imie|moj adres)\b")]
    private static partial Regex PersonalCase();

    [GeneratedRegex(@"(?:\d|\b(?:sto|tysiac\w*|tysiecy|milion\w*))\s*(?:zl\w*|pln\b|euro\b|eur\b|usd\b)|\b(?:pln|eur|usd)\s*\d|[€$]\s*\d", RegexOptions.IgnoreCase)]
    private static partial Regex Money();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^(?:a\s|(?:czy|jak|gdzie|kiedy|ile)\s+(?:to|tego|wtedy|tam)\b|ile (?:trwa|dni|tygodni)\s*\??$)")]
    private static partial Regex FollowUp();
}
