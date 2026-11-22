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

    public const string OtherBenefitAnswer =
        "Pytasz o świadczenie spoza ubezpieczenia chorobowego (np. 800+ albo becikowe). Pomocnik zna tylko przepisy " +
        "o zasiłkach z ubezpieczenia chorobowego (chorobowym, macierzyńskim, opiekuńczym) i o urlopach związanych " +
        "z rodzicielstwem, więc nie wskaże tu przepisu na ten temat.";

    public const string OffTopicAnswer =
        "Odpowiadam tylko na ogólne pytania o przepisy dotyczące zasiłków i urlopów związanych z rodzicielstwem. " +
        "Zapytaj na przykład o urlop, zasiłek albo dokumenty.";

    /// <summary>
    /// Pytanie wyraźnie spoza materiałów: inne świadczenia (800+, becikowe…) albo polecenie zmiany zadania
    /// („zignoruj instrukcje”, „napisz wiersz”). Zwraca odpowiedź bez cytatów albo null.
    /// </summary>
    public static string? OutOfScope(string text)
    {
        var normalized = Normalize(text);
        if (OtherBenefit().IsMatch(normalized)) return OtherBenefitAnswer;
        if (Injection().IsMatch(normalized)) return OffTopicAnswer;
        return null;
    }

    public const string GenericAnswer = "Te fragmenty pasują do Twojego pytania.";
    public const string QuotesOnlyDisclaimer = "Cytaty wybrał model językowy. Indywidualną sprawę ocenia kalkulator.";
    public const string ExplanationDisclaimer =
        "Wyjaśnienie napisał model językowy na podstawie przepisów poniżej — wiążące są przepisy. Indywidualną sprawę ocenia kalkulator.";

    private const int MaxExplanationLength = 700;

    public static AssistantAnswer? Verify(ModelReply reply, IReadOnlyList<LegalExcerpt> sources)
    {
        var citations = VerifyCitations(reply.Citations, sources);
        if (citations is null) return null;

        // Wyjaśnienie pokazujemy tylko obok zweryfikowanych cytatów i tylko, gdy przejdzie kontrole; inaczej — sam tekst ogólny.
        var explanation = CleanExplanation(reply.Explanation);
        return explanation is null
            ? new AssistantAnswer(GenericAnswer, citations, QuotesOnlyDisclaimer, "ai")
            : new AssistantAnswer(explanation, citations, ExplanationDisclaimer, "ai");
    }

    /// <summary>Wyjaśnienie modelu albo null: puste, za długie, z kwotą, linkiem lub danymi osobowymi.</summary>
    public static string? CleanExplanation(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var clean = CollapseSpace(text.Trim());
        if (clean.Length < 20 || clean.Length > MaxExplanationLength) return null;
        if (HasSensitiveData(clean) || Money().IsMatch(Normalize(clean)) || Link().IsMatch(clean)) return null;
        return clean;
    }

    private static List<AssistantCitation>? VerifyCitations(ModelCitation[] selection, IReadOnlyList<LegalExcerpt> sources)
    {
        const int maxCitations = 3;
        const int maxQuoteLength = 600;
        if (selection.Length == 0) return null;

        var citations = new List<AssistantCitation>();
        foreach (var citation in selection)
        {
            // Cytat, którego nie ma w wysłanym przepisie, unieważnia całą odpowiedź — to znak, że model coś dopisał.
            if (citation is null || citation.DocumentIndex < 0 || citation.DocumentIndex >= sources.Count ||
                string.IsNullOrWhiteSpace(citation.CitedText))
                return null;

            var source = sources[citation.DocumentIndex];
            var text = CollapseSpace(source.Text);
            var quote = CollapseSpace(citation.CitedText.Trim());
            var start = text.IndexOf(quote, StringComparison.Ordinal);
            // Wyświetlany tekst ma numerację z indeksami górnymi (§ 1¹), a źródło — spłaszczoną (§ 11). Zamiana jest
            // znak na znak, więc cytat w dowolnej z tych postaci porównujemy z tekstem źródłowym bez zmiany długości.
            if (start < 0) start = LegalNumbering.Flatten(text).IndexOf(LegalNumbering.Flatten(quote), StringComparison.Ordinal);
            if (start < 0) return null;

            // Modele oddają cytaty różnej długości i czasem więcej niż trzy (Haiku tnie przepis na zdania).
            // Za krótki albo z kwotą pomijamy, za długi skracamy, nadmiarowe odrzucamy — pokazywany tekst zawsze pochodzi z repozytorium.
            if (quote.Length < 24 || HasSensitiveData(quote) || Money().IsMatch(Normalize(quote))) continue;
            var length = quote.Length;
            if (length > maxQuoteLength)
            {
                var cut = text.LastIndexOf(' ', start + maxQuoteLength, maxQuoteLength);
                length = (cut > start ? cut : start + maxQuoteLength) - start;
            }

            var verified = source.Citation(text.Substring(start, length) + (length < quote.Length ? "…" : ""));
            if (!citations.Contains(verified)) citations.Add(verified);
            if (citations.Count == maxCitations) break;
        }

        return citations.Count == 0 ? null : citations;
    }

    [GeneratedRegex(@"https?://|www\.|\b[\p{L}\d-]+\.(?:pl|com|org|eu|net|gov)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Link();

    private static string CollapseSpace(string text) => Whitespace().Replace(text, " ");

    [GeneratedRegex(@"\b\d(?:[\s.()-]*\d){8,}\b|[\p{L}\d._%+\-]+\s*@\s*[\p{L}\d.\-]+\.[\p{L}]{2,}|\b(?:oc_sk_|sk-ant-|sk-proj-)[a-z\d_-]+", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveData();

    [GeneratedRegex(@"\b(?:moj\w*\s+(?:pesel|pracodawc\w*|dzieck\w*|syn\w*|cork\w*|maz\w*|zon\w*|spraw\w*)|urodzilam|urodzilem|zarabiam|zarabialam|pracuje od|termin mam|przysluguje (?:mi|ci|pani|panu)|czy (?:dostane|mam prawo)|ile (?:dostane|otrzymam)|czy moge|przy pensji|nazywam sie|na imie|moj adres)\b")]
    private static partial Regex PersonalCase();

    [GeneratedRegex(@"(?:\d|\b(?:sto|tysiac\w*|tysiecy|milion\w*))\s*(?:zl\w*|pln\b|euro\b|eur\b|usd\b)|\b(?:pln|eur|usd)\s*\d|[€$]\s*\d", RegexOptions.IgnoreCase)]
    private static partial Regex Money();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    // Świadczenia spoza ubezpieczenia chorobowego — nie ma ich w materiałach pomocnika.
    [GeneratedRegex(@"\bbecikow\w*|\b(?:300|500|800)\s*(?:\+|plus\b)|\bswiadczen\w*\s+wychowawcz\w*|\bdobry\s+start\b|\bbabciow\w*|\bkosiniakow\w*|\brodzinn\w*\s+kapital\w*|\bzasil\w*\s+rodzinn\w*|\bdodat\w*\s+mieszkaniow\w*|\bzapomog\w*\s+z\s+tytulu\s+urodzenia")]
    private static partial Regex OtherBenefit();

    // Próba zmiany zadania albo prośba o tekst twórczy.
    [GeneratedRegex(@"\b(?:zignoruj|ignoruj|zapomnij|pomin)\w*\b.{0,40}\b(?:instrukcj|polecen|zasad|regul|prompt)\w*|\bignore\b.{0,40}\binstructions?\b|\b(?:napisz|uloz|stworz|wymysl|zaspiewaj)\w*\b.{0,30}\b(?:wiersz|piosenk|rym|opowiadani|bajk|esej|limeryk|haiku|zart|dowcip)\w*|\budawaj\b|\bjestes\s+teraz\b|\bsystem\s+prompt\b|\bjailbreak\b")]
    private static partial Regex Injection();

    [GeneratedRegex(@"^(?:a\s|(?:czy|jak|gdzie|kiedy|ile)\s+(?:to|tego|wtedy|tam)\b|ile (?:trwa|dni|tygodni)\s*\??$)")]
    private static partial Regex FollowUp();
}
