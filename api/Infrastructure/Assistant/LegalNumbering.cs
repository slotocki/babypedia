using System.Text;
using System.Text.RegularExpressions;

namespace BabyRadnik.Infrastructure.Assistant;

/// <summary>
/// Numeracja z indeksami górnymi w wyciągach. W tekście z PDF indeksy spłaszczono do cyfr (art. 1821a = art. 182¹a,
/// § 11 po § 1 = § 1¹). Zamiana dotyczy wyłącznie wyświetlania i kluczy wyszukiwania. Jest odwracalna znak po znaku
/// (<see cref="Flatten"/>), więc cytat zawsze da się porównać z tekstem źródłowym. Niczego nie zgadujemy: zamieniamy
/// tylko numery z mapy zbudowanej ze struktury wyciągu (nagłówki artykułów, kolejność paragrafów); przypadek
/// niejednoznaczny zostaje w postaci surowej.
/// </summary>
internal sealed partial class LegalNumbering
{
    private const string Superscripts = "⁰¹²³⁴⁵⁶⁷⁸⁹";

    /// <summary>
    /// Artykuły KP przywoływane w wyciągach, których nie ma tam jako osobnych nagłówków. Kodeks pracy kończy się
    /// na art. 305, więc spłaszczone 1867, 1731 i 632 nie mogą oznaczać artykułu bez indeksu.
    /// art. 186⁷ — obniżenie wymiaru czasu pracy, art. 173¹ — urlop opiekuńczy, art. 63² — śmierć pracodawcy.
    /// </summary>
    private static readonly string[] ReferencedOnly = ["186⁷", "173¹", "63²"];

    private static readonly Dictionary<string, string> NoParagraphs = new(StringComparer.Ordinal);

    /// <summary>Spłaszczony numer artykułu KP → numer z indeksem (np. "1821a" → "182¹a").</summary>
    private readonly Dictionary<string, string> articles = new(StringComparer.Ordinal);

    /// <summary>Artykuł KP (numer z indeksem) → spłaszczony numer § → § z indeksem (np. "183" → {"42" → "4²"}).</summary>
    private readonly Dictionary<string, Dictionary<string, string>> paragraphs = new(StringComparer.Ordinal);

    /// <param name="codeArticles">Artykuły Kodeksu pracy z wyciągu: numer z nagłówka (z indeksem) i treść.</param>
    public LegalNumbering(IEnumerable<(string Number, string Text)> codeArticles)
    {
        foreach (var number in ReferencedOnly) articles[Flatten(number)] = number;
        foreach (var (number, text) in codeArticles)
        {
            if (Flatten(number) != number) articles[Flatten(number)] = number;
            var map = ParagraphMap(ParagraphLabels(text));
            if (map.Count > 0) paragraphs[number] = map;
        }
    }

    public IReadOnlyDictionary<string, string> Articles => articles;

    public IReadOnlyDictionary<string, string> ParagraphsOf(string article) =>
        paragraphs.TryGetValue(article, out var map) ? map : NoParagraphs;

    /// <summary>Indeksy górne → zwykłe cyfry. Zamiana znak na znak, więc pozycje w tekście się nie zmieniają.</summary>
    public static string Flatten(string text)
    {
        if (text.AsSpan().IndexOfAny(Superscripts) < 0) return text;
        var builder = new StringBuilder(text);
        for (var i = 0; i < builder.Length; i++)
        {
            var index = Superscripts.IndexOf(builder[i]);
            if (index >= 0) builder[i] = (char)('0' + index);
        }
        return builder.ToString();
    }

    private static string Superscript(string digits) => string.Concat(digits.Select(digit => Superscripts[digit - '0']));

    /// <summary>
    /// Kolejność paragrafów w artykule rozstrzyga, czy surowe "§ 11" to § 11, czy § 1¹: po § 1 (albo § 1¹), a przed § 2
    /// stoi indeks. Numer, który nie pasuje ani do następnego paragrafu, ani do kolejnego indeksu, zostaje bez zmian
    /// i wstrzymuje odczyt do następnego jednoznacznego (jednocyfrowego) numeru.
    /// </summary>
    public static Dictionary<string, string> ParagraphMap(IEnumerable<string> labels)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        int? lastBase = 0;
        var lastSup = 0;
        foreach (var label in labels)
        {
            var value = int.Parse(label);
            var baseText = lastBase?.ToString();
            var nextSup = (lastSup + 1).ToString();
            var isNext = value == lastBase + 1;
            var isSup = baseText is not null && label == baseText + nextSup;
            if (label.Length == 1 || isNext)
            {
                lastBase = value;
                lastSup = 0;
            }
            else if (isSup)
            {
                map[label] = baseText + Superscript(nextSup);
                lastSup++;
            }
            else
            {
                lastBase = null;
            }
        }
        return map;
    }

    /// <summary>Numery § otwierających ustępy artykułu, w kolejności.</summary>
    public static IEnumerable<string> ParagraphLabels(string text) =>
        ParagraphStart().Matches(text).Select(match => match.Groups["num"].Value);

    /// <summary>
    /// Tekst do wyświetlenia: numery paragrafów i odwołania do artykułów KP z indeksami górnymi.
    /// <paramref name="ownArticle"/> to numer artykułu KP, którego treść zamieniamy; null dla innych aktów —
    /// wtedy zamieniamy tylko odwołania wyraźnie wskazujące Kodeks pracy.
    /// </summary>
    public string Display(string text, string? ownArticle)
    {
        var own = ownArticle is null ? NoParagraphs : ParagraphsOf(ownArticle);
        if (own.Count > 0)
            text = ParagraphStart().Replace(text, match =>
                own.TryGetValue(match.Groups["num"].Value, out var label) ? $"§ {label}." : match.Value);

        return Reference().Replace(text, match =>
        {
            if (match.Groups["art"].Success) return ArticleReference(match, ownArticle is not null);
            // „§ 11” w zdaniu dotyczy własnego artykułu, chyba że tuż przed nim przywołano inny artykuł.
            if (own.Count == 0 || ArticleBefore().IsMatch(text[Math.Max(0, match.Index - 60)..match.Index])) return match.Value;
            return MapNumbers(match.Value, own);
        });
    }

    private string ArticleReference(Match match, bool insideCode)
    {
        var tail = match.Groups["tail"].Value;
        var pointsToCode = CodeContext().IsMatch(tail);
        if (insideCode ? OtherAct().IsMatch(tail) && !pointsToCode : !pointsToCode) return match.Value;

        var number = articles.GetValueOrDefault(match.Groups["num"].Value, match.Groups["num"].Value);
        var range = match.Groups["range"].Value;
        return match.Groups["prefix"].Value + number + match.Groups["dash"].Value + articles.GetValueOrDefault(range, range) +
               MapNumbers(match.Groups["paras"].Value, ParagraphsOf(number)) + match.Groups["rest"].Value;
    }

    private static string MapNumbers(string text, IReadOnlyDictionary<string, string> map) =>
        map.Count == 0 ? text : Number().Replace(text, match => map.GetValueOrDefault(match.Value, match.Value));

    /// <summary>Klucz artykułu do wyszukiwania: indeks górny zapisany po "^" (182¹a → "182^1a", 2a → "2a").</summary>
    public static string Key(string number)
    {
        var builder = new StringBuilder();
        var inSuperscript = false;
        foreach (var c in number.Trim().ToLowerInvariant())
        {
            var index = Superscripts.IndexOf(c);
            if (index >= 0 && !inSuperscript) builder.Append('^');
            builder.Append(index >= 0 ? (char)('0' + index) : c);
            inSuperscript = index >= 0;
        }
        return builder.ToString();
    }

    /// <summary>
    /// Odwołania do artykułów w pytaniu: „art. 180¹”, „art. 180^1”, „art 180 1”, spłaszczone „art. 1801”, samo „182¹a”.
    /// Zwraca możliwe klucze (<see cref="Key"/>) od najbardziej pewnego.
    /// </summary>
    public IReadOnlyList<string> QueryKeys(string question)
    {
        var keys = new List<string>();
        var text = ArticleWord().Replace(question.ToLowerInvariant(), "art.");
        foreach (Match match in QueryReference().Matches(text))
        {
            var number = match.Groups["num"].Value;
            var letter = match.Groups["letter"].Value;
            var hasArt = match.Groups["art"].Success;
            if (match.Groups["sup"].Success) keys.Add($"{number}^{Flatten(match.Groups["sup"].Value)}{letter}");
            else if (match.Groups["caret"].Success) keys.Add($"{number}^{match.Groups["caret"].Value}{letter}");
            else if (!hasArt) continue;
            else if (match.Groups["spaced"].Success)
            {
                keys.Add($"{number}^{match.Groups["spaced"].Value}{letter}");
                keys.Add(number + letter);
            }
            else
            {
                if (articles.TryGetValue(number + letter, out var article)) keys.Add(Key(article));
                keys.Add(number + letter);
            }
        }
        return keys.Distinct().ToArray();
    }

    [GeneratedRegex(@"^§ (?<num>\d+)\.", RegexOptions.Multiline)]
    private static partial Regex ParagraphStart();

    // Odwołanie „art. 1821a § 3-7 i § 11 pkt 2” albo samo „§ 41 albo 42”. Kontekst po odwołaniu (Kodeks pracy / inna ustawa)
    // jest sprawdzany w podglądzie, aby nie pochłonąć dalszych odwołań.
    [GeneratedRegex(@"(?<art>(?<prefix>\b[Aa]rt\.\s*)(?<num>\d+[a-z]?)(?:(?<dash>\s*[-–]\s*)(?<range>\d+[a-z]?)\b)?(?<paras>\s*§\s*\d+(?:\s*[-–]\s*\d+)?(?:(?:\s+(?:i|oraz|albo|lub)|,)\s*(?:§\s*)?\d+(?:\s*[-–]\s*\d+)?(?![\d.]|\s*(?:pkt|ust|r\.|dni|tyg|lat|rok|miesi)))*)?(?<rest>(?:\s+(?:pkt|zdanie)\s+[\w-]+)?)(?=(?<tail>\s+(?:Kodeksu pracy|ust\.|ustawy[^;]{0,60}))?))|§\s*\d+(?:\s*[-–]\s*\d+)?(?:(?:\s+(?:i|oraz|albo|lub)|,)\s*(?:§\s*)?\d+(?:\s*[-–]\s*\d+)?(?![\d.]|\s*(?:pkt|ust|r\.|dni|tyg|lat|rok|miesi)))*")]
    private static partial Regex Reference();

    [GeneratedRegex(@"^\s+(?:Kodeksu pracy|ustawy z dnia 26 czerwca 1974 r\.\s*[-–]\s*Kodeks pracy)")]
    private static partial Regex CodeContext();

    [GeneratedRegex(@"^\s+(?:ust\.|ustawy)")]
    private static partial Regex OtherAct();

    [GeneratedRegex(@"\b[Aa]rt\.\s*\d")]
    private static partial Regex ArticleBefore();

    [GeneratedRegex(@"\d+")]
    private static partial Regex Number();

    [GeneratedRegex(@"\bartyku[lł]\w*")]
    private static partial Regex ArticleWord();

    [GeneratedRegex(@"(?:(?<art>\bart\.?)\s*)?(?<!\d)(?<num>\d{1,4})(?:(?<sup>[⁰¹²³⁴⁵⁶⁷⁸⁹]+)|\s*\^\s*(?<caret>\d{1,2})|(?(art)\s(?<spaced>\d)(?![\d,.%])|(?!)))?(?<letter>[a-h](?![a-zęóąśłżźćń]))?")]
    private static partial Regex QueryReference();
}
