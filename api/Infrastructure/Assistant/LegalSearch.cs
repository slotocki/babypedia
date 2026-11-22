using System.Text.RegularExpressions;
using BabyRadnik.Infrastructure.Common;

namespace BabyRadnik.Infrastructure.Assistant;

/// <param name="Preview">Najlepiej pasujący ustęp (dosłowny fragment <see cref="LegalExcerpt.Text"/>), skrócony do podglądu.</param>
internal sealed record LegalHit(LegalExcerpt Excerpt, string Preview);

internal sealed partial class LegalSearch
{
    private const int MaxHits = 6;
    private const int MaxPreview = 260;

    /// <summary>Najniższy wynik BM25 trafienia. Niższe to przypadkowa zbieżność pojedynczych słów.</summary>
    private const double MinScore = 2.0;

    private static readonly HashSet<string> StopWords = new(
        "a aby ale albo az bez bo by byc chce co czy dla do dostac gdzie gdy i ich ile im inny jak jaka jakie jakich jaki jakim jako jest jeszcze jezeli ktore ktory kto kiedy lub ma mam maja me mnie mi moze mozna na nad nie o od oraz po pod przy sie sa tak ten to tu u w we wg z za ze".Split(' '),
        StringComparer.Ordinal);

    /// <summary>Słowa o przepisach w ogóle — same nie wskazują tematu pytania.</summary>
    private static readonly string[] GenericStems =
        ["art", "przepi", "praw", "zasad", "ustaw", "kodeks", "paragr", "podaj", "napisz", "prosz", "pytan", "odpow",
         "inform", "instru", "wyjasn", "powiedz", "pomoc", "przysl", "wynosi"];

    private readonly IndexedExcerpt[] index;
    private readonly Dictionary<string, int> documentFrequency;
    private readonly double averageLength;
    private readonly double averageUnitLength;
    private readonly LegalNumbering numbering;

    public LegalSearch(RepoPaths paths)
    {
        var directory = Path.Combine(paths.Root, "docs", "prawo");
        (var excerpts, numbering) = LegalSources.Read(directory);
        index = excerpts.Select(Index).ToArray();
        documentFrequency = index.SelectMany(item => item.Terms.Keys).CountBy(word => word).ToDictionary();
        averageLength = index.Length == 0 ? 1 : index.Average(item => item.Length);
        var units = index.SelectMany(item => item.Units).ToArray();
        averageUnitLength = units.Length == 0 ? 1 : units.Average(unit => unit.Length);
    }

    public IReadOnlyList<LegalExcerpt> Excerpts => index.Select(item => item.Excerpt).ToArray();

    public LegalNumbering Numbering => numbering;

    /// <summary>
    /// Najlepiej pasujące fragmenty. <paramref name="minSupporting"/> rezerwuje miejsca dla materiałów pomocniczych ZUS:
    /// akty prawne mają w rankingu przewagę, a poradnik tłumaczy to samo prostym językiem — model dostaje jedno i drugie.
    /// </summary>
    public IReadOnlyList<LegalHit> Find(string question, int limit = MaxHits, int minSupporting = 0)
    {
        var referenced = Referenced(question);
        var asked = LegalTerms.Tokenize(question).Distinct().ToArray();
        var known = asked.Where(documentFrequency.ContainsKey).ToArray();

        // Pytanie spoza materiałów: żadne słowo nie wskazuje tematu albo większości słów nie ma w żadnym przepisie.
        // Oceniamy samo pytanie — słowa dodane przez rozwinięcie zawsze są w przepisach i zamazałyby ten test.
        if (!known.Any(word => !IsGeneric(word)) || known.Length * 2 < asked.Length)
            return referenced.Take(limit).Select(item => Hit(item, known.Where(word => !IsGeneric(word)).ToArray())).ToArray();

        var words = LegalTerms.Tokenize(LegalTerms.ExpandQuery(question)).Distinct().ToArray();
        var informative = words.Where(word => documentFrequency.ContainsKey(word) && !IsGeneric(word)).ToArray();
        var found = index.Select(item =>
            {
                var matches = informative.Count(item.Terms.ContainsKey);
                var articleScore = words.Where(item.Terms.ContainsKey).Sum(term => Score(item.Terms[term], item.Length, averageLength, term, item));
                var unitScore = item.Units.Count == 0 ? 0 : item.Units.Max(unit => UnitScore(unit, words, item));
                return (Item: item, Matches: matches, Score: articleScore + 0.6 * unitScore);
            })
            .Where(hit => hit.Matches >= Math.Min(2, informative.Length) && hit.Score >= MinScore)
            .OrderByDescending(hit => hit.Score * (hit.Item.Excerpt.IsLegalAct ? 1.3 : 0.75))
            .Select(hit => hit.Item);

        var ranked = referenced.Concat(found).Distinct().ToArray();
        var reserved = ranked.Where(item => !item.Excerpt.IsLegalAct).Take(Math.Min(minSupporting, limit)).ToHashSet();
        var rest = ranked.Where(item => !reserved.Contains(item)).Take(limit - reserved.Count).ToHashSet();
        return ranked.Where(item => reserved.Contains(item) || rest.Contains(item))
            .Select(item => Hit(item, informative)).ToArray();
    }

    /// <summary>Artykuły wskazane w pytaniu numerem („art. 180¹”, „art. 1821a”), z uwzględnieniem wskazanego aktu.</summary>
    private IEnumerable<IndexedExcerpt> Referenced(string question)
    {
        var keys = numbering.QueryKeys(question);
        if (keys.Count == 0) return [];
        var normalized = AssistantPolicy.Normalize(question);
        var document = CodeHint().IsMatch(normalized) ? LegalSources.CodeTitle
            : ActHint().IsMatch(normalized) ? LegalSources.Files[0].Title
            : null;
        foreach (var key in keys)
        {
            var matches = index.Where(item => item.Excerpt.IsLegalAct && item.Excerpt.Key == key &&
                                              (document is null || item.Excerpt.Document == document)).ToArray();
            if (matches.Length > 0) return matches;
        }
        return [];
    }

    private LegalHit Hit(IndexedExcerpt item, IReadOnlyList<string> words)
    {
        // Podgląd z ustępu, który zawiera najwięcej słów pytania; przy zbliżonym wyniku — z wcześniejszego (zwykle ogólnej reguły).
        var text = item.Excerpt.Text;
        var scores = item.Units
            .Select(unit => (Unit: unit, Words: words.Count(unit.Terms.ContainsKey), Score: UnitScore(unit, words, item)))
            .ToArray();
        var most = scores.Length == 0 ? 0 : scores.Max(score => score.Words);
        scores = scores.Where(score => score.Words == most).ToArray();
        var top = scores.Length == 0 ? 0 : scores.Max(score => score.Score);
        var best = top > 0 ? scores.First(score => score.Score >= 0.75 * top).Unit : null;
        var preview = best is null ? text : text.Substring(best.Start, best.Count);
        return new LegalHit(item.Excerpt, Preview(preview.Trim()));
    }

    private static string Preview(string text)
    {
        if (text.Length <= MaxPreview) return text;
        var end = text.LastIndexOf(' ', MaxPreview);
        return text[..(end > 0 ? end : MaxPreview)].TrimEnd() + "…";
    }

    private static bool IsGeneric(string word) => GenericStems.Any(stem => word.StartsWith(stem, StringComparison.Ordinal));

    private double UnitScore(Unit unit, IEnumerable<string> words, IndexedExcerpt item) =>
        words.Where(unit.Terms.ContainsKey).Sum(term => Score(unit.Terms[term], unit.Length, averageUnitLength, term, item));

    private double Score(int frequency, int length, double average, string word, IndexedExcerpt item)
    {
        const double k = 1.2;
        const double b = 0.75;
        var idf = Math.Log(1 + (index.Length - documentFrequency[word] + 0.5) / (documentFrequency[word] + 0.5));
        var termWeight = item.TitleTerms.Contains(word) ? 2.1 : 1;
        return termWeight * idf * frequency * (k + 1) / (frequency + k * (1 - b + b * length / average));
    }

    private static IndexedExcerpt Index(LegalExcerpt excerpt)
    {
        var words = LegalTerms.Tokenize(excerpt.Article + " " + excerpt.Text).ToArray();
        var lineEnd = excerpt.Text.IndexOf('\n');
        var firstLine = excerpt.Text.AsSpan(0, lineEnd < 0 ? excerpt.Text.Length : lineEnd).Trim();
        var title = firstLine.StartsWith("[") && firstLine.EndsWith("]") ? firstLine[1..^1].ToString() : excerpt.Article;
        return new IndexedExcerpt(excerpt, words.Length, words.CountBy(word => word).ToDictionary(),
            LegalTerms.Tokenize(title).ToHashSet(), excerpt.IsLegalAct ? Units(excerpt.Text) : []);
    }

    /// <summary>Ustępy i paragrafy artykułu — długie artykuły (np. art. 180 KP) trafiają dzięki pojedynczemu ustępowi.</summary>
    private static IReadOnlyList<Unit> Units(string text)
    {
        var starts = UnitStart().Matches(text).Select(match => match.Index).ToList();
        if (starts.Count == 0) return [];
        var units = new List<Unit>();
        for (var i = 0; i < starts.Count; i++)
        {
            var end = i + 1 < starts.Count ? starts[i + 1] : text.Length;
            var words = LegalTerms.Tokenize(text[starts[i]..end]).ToArray();
            if (words.Length > 0)
                units.Add(new Unit(starts[i], end - starts[i], words.Length, words.CountBy(word => word).ToDictionary()));
        }
        return units;
    }

    private sealed record Unit(int Start, int Count, int Length, Dictionary<string, int> Terms);

    private sealed record IndexedExcerpt(
        LegalExcerpt Excerpt, int Length, Dictionary<string, int> Terms, HashSet<string> TitleTerms, IReadOnlyList<Unit> Units);

    [GeneratedRegex(@"^(?:§ \d+[⁰¹²³⁴⁵⁶⁷⁸⁹]*[a-z]?\.|\d+[a-z]?\.)[ \t]", RegexOptions.Multiline)]
    private static partial Regex UnitStart();

    [GeneratedRegex(@"\b(?:kp|kodeks\w*)\b")]
    private static partial Regex CodeHint();

    [GeneratedRegex(@"\b(?:ustaw\w*\s+zasilkow\w*|ustawy)\b")]
    private static partial Regex ActHint();

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
            (@"\bprzej(?:ac|mie|muje|mowac|al|ela|ecie)\w*\b", "wykorzysta czesc"),
        ];

        // Tylko dla pytań: rodzic pyta „jakie dokumenty”, a przepisy mówią „dokumentem niezbędnym… jest zaświadczenie / odpis”,
        // poradnik — „jak otrzymać zasiłek… wystarczy złożyć wniosek”. Do tekstów przepisów tego nie stosujemy.
        private static readonly (string Pattern, string Expansion)[] QueryExpansions =
        [
            (@"\b(?:dokument\w*|potrzebn\w*|dostarcz\w*|zalacz\w*|przynies\w*|formalnosc\w*)\b",
                "dokumentem niezbednym zaswiadczenie odpis wniosek otrzymac"),
        ];

        public static string ExpandQuery(string question)
        {
            var normalized = AssistantPolicy.Normalize(question);
            foreach (var (pattern, expansion) in QueryExpansions)
                if (Regex.IsMatch(normalized, pattern))
                    normalized += " " + expansion;
            return normalized;
        }

        private static readonly string[] CommonStems =
            ["urlop", "zasil", "sklad", "wnios", "ubezpiecz", "okres", "ciaz", "martw", "dwoj", "lacz"];

        public static IEnumerable<string> Tokenize(string text)
        {
            var normalized = AssistantPolicy.Normalize(text);
            foreach (var (pattern, replacement) in Synonyms)
                normalized = Regex.Replace(normalized, pattern, replacement);

            // Procent podstawy (np. 81,5%) wskazuje konkretną wysokość zasiłku.
            foreach (Match match in Percent().Matches(normalized))
                yield return "pct" + match.Groups[1].Value.Replace('.', ',');

            foreach (Match match in Word().Matches(normalized))
            {
                var word = match.Value;
                if (StopWords.Contains(word)) continue;
                if (word.StartsWith("dziec", StringComparison.Ordinal))
                    yield return "dziecko";
                else if (word.StartsWith("ojc", StringComparison.Ordinal))
                    yield return "ojciec";
                else if (word.StartsWith("matk", StringComparison.Ordinal) || word is "matce")
                    yield return "matka";
                else
                    yield return CommonStems.FirstOrDefault(stem => word.StartsWith(stem, StringComparison.Ordinal))
                        ?? (word.Length > 6 ? word[..6] : word);
            }
        }

        [GeneratedRegex(@"[a-z]{2,}")]
        private static partial Regex Word();

        [GeneratedRegex(@"(?<![\d,.])(\d{1,3}(?:[,.]\d+)?)\s?%")]
        private static partial Regex Percent();
    }
}
