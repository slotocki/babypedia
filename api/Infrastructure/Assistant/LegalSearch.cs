using System.Text.RegularExpressions;
using BabyRadnik.Infrastructure.Common;

namespace BabyRadnik.Infrastructure.Assistant;

internal sealed record LegalExcerpt(string Document, string Article, string Text, bool IsLegalAct)
{
    public AssistantCitation Citation(string fragment) => new(Document, fragment, Article, Text);
}

internal sealed partial class LegalSearch
{
    private static readonly (string File, string Title, bool IsLegalAct)[] Sources =
    [
        ("ustawa-zasilkowa.md", "Ustawa zasiłkowa", true),
        ("kodeks-pracy.md", "Kodeks pracy", true),
        ("rozporzadzenie-2025-03-11-wnioski-pracownicze.md", "Rozporządzenie w sprawie wniosków pracowniczych", true),
        ("rozporzadzenie-2026-09-27-dokumenty-zasilki.md", "Rozporządzenie w sprawie dokumentów do zasiłków", true),
        ("zalacznik-zus.md", "Załącznik ZUS (materiał pomocniczy)", false),
        ("poradnik-zus-2026.md", "Poradnik ZUS (materiał pomocniczy)", false),
    ];

    private static readonly HashSet<string> StopWords = new(
        "a aby ale albo az bez bo by byc chce co czy dla do dostac gdzie gdy i ich ile im inny jak jaka jakie jakich jaki jakim jako jest jeszcze jezeli ktore ktory kto kiedy lub ma mam maja me mnie mi moze mozna na nad nie o od oraz po pod przy sie sa tak ten to tu u w we wg z za ze".Split(' '),
        StringComparer.Ordinal);

    private readonly IndexedExcerpt[] index;
    private readonly Dictionary<string, int> documentFrequency;
    private readonly double averageLength;

    public LegalSearch(RepoPaths paths)
    {
        var directory = Path.Combine(paths.Root, "docs", "prawo");
        index = Sources.SelectMany(source => Read(directory, source)).Select(excerpt =>
        {
            var words = LegalTerms.Tokenize(excerpt.Article + " " + excerpt.Text).ToArray();
            var lineEnd = excerpt.Text.IndexOf('\n');
            var firstLine = excerpt.Text.AsSpan(0, lineEnd < 0 ? excerpt.Text.Length : lineEnd).Trim();
            var title = firstLine.StartsWith("[") && firstLine.EndsWith("]") ? firstLine[1..^1].ToString() : excerpt.Article;
            return new IndexedExcerpt(excerpt, words.Length, words.CountBy(word => word).ToDictionary(),
                LegalTerms.Tokenize(title).ToHashSet());
        }).ToArray();
        documentFrequency = index.SelectMany(item => item.Terms.Keys).CountBy(word => word).ToDictionary();
        averageLength = index.Length == 0 ? 1 : index.Average(item => item.Length);
    }

    public IReadOnlyList<LegalExcerpt> Find(string question)
    {
        var words = LegalTerms.Tokenize(question).Distinct().ToArray();
        if (words.Length == 0) return [];

        var candidates = index.Select(item =>
        {
            var matches = words.Where(item.Terms.ContainsKey).ToArray();
            var score = matches.Sum(term => Score(item, term));
            return (item.Excerpt, Matches: matches.Length, Score: score);
        });

        return candidates
            .Where(hit => hit.Matches >= Math.Min(2, words.Length) && hit.Score >= 1.2)
            .OrderByDescending(hit => hit.Score * (hit.Excerpt.IsLegalAct ? 1.3 : 0.75))
            .Take(6)
            .Select(hit => hit.Excerpt)
            .ToArray();
    }

    private double Score(IndexedExcerpt item, string word)
    {
        const double k = 1.2;
        const double b = 0.75;
        var frequency = item.Terms[word];
        var idf = Math.Log(1 + (index.Length - documentFrequency[word] + 0.5) / (documentFrequency[word] + 0.5));
        var termWeight = item.TitleTerms.Contains(word) ? 2.1 : 1;
        return termWeight * idf * frequency * (k + 1) / (frequency + k * (1 - b + b * item.Length / averageLength));
    }

    private static IEnumerable<LegalExcerpt> Read(string directory, (string File, string Title, bool IsLegalAct) source)
    {
        var path = Path.Combine(directory, source.File);
        if (!File.Exists(path)) yield break;

        var markdown = HtmlComment().Replace(File.ReadAllText(path), "");
        var sections = SectionHeading().Split(markdown);
        for (var i = 1; i < sections.Length; i += 2)
        {
            var title = sections[i].Trim();
            if (title.StartsWith("Przypisy", StringComparison.OrdinalIgnoreCase)) yield break;
            if (source.IsLegalAct && !ArticleHeading().IsMatch(title)) continue;

            var content = sections[i + 1].Trim();
            if (content.Length > 0)
                yield return new LegalExcerpt(source.Title, title, content, source.IsLegalAct);
        }
    }

    private sealed record IndexedExcerpt(
        LegalExcerpt Excerpt, int Length, Dictionary<string, int> Terms, HashSet<string> TitleTerms);

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlComment();

    [GeneratedRegex(@"^#{2,6}[ \t]+([^\r\n]+)\r?$", RegexOptions.Multiline)]
    private static partial Regex SectionHeading();

    [GeneratedRegex(@"^(?:Art\.|§)\s*\d")]
    private static partial Regex ArticleHeading();

    private static partial class LegalTerms
    {
        private static readonly (string Pattern, string Replacement)[] Synonyms =
        [
            (@"\bl4\b", "zaswiadczenie lekarskie"),
            (@"\btacierzynski\w*\b", "ojciec urlop macierzynski"),
            (@"\bblizni\w*\b", "dwojga dzieci"),
            (@"\bwczesniak\w*\b", "uzupelniajacy urlop macierzynski hospitalizacja"),
            (@"\bwyczekiwani\w*\b", "okres ubezpieczenie chorobowe"),
            (@"\bnie pracuje\b", "nieubezpieczona"),
            (@"\btrwa\b", "wymiar"),
            (@"\bzaleg\w*\s+sklad\w*\b", "zadluzenie skladki"),
            (@"\bpracowac\b", "laczenie pracy"),
            (@"\bzus\b", "zaklad ubezpieczen spolecznych"),
        ];

        private static readonly string[] CommonStems =
            ["urlop", "zasil", "sklad", "wnios", "ubezpiecz", "okres", "ciaz", "martw", "dwoj", "lacz"];

        public static IEnumerable<string> Tokenize(string text)
        {
            var normalized = AssistantPolicy.Normalize(text);
            foreach (var (pattern, replacement) in Synonyms)
                normalized = Regex.Replace(normalized, pattern, replacement);

            foreach (Match match in Word().Matches(normalized))
            {
                var word = match.Value;
                if (StopWords.Contains(word)) continue;
                if (word.StartsWith("dziec", StringComparison.Ordinal))
                    yield return "dziecko";
                else
                    yield return CommonStems.FirstOrDefault(stem => word.StartsWith(stem, StringComparison.Ordinal))
                        ?? (word.Length > 6 ? word[..6] : word);
            }
        }

        [GeneratedRegex(@"[a-z]{2,}")]
        private static partial Regex Word();
    }
}
