using System.Text.Json;
using System.Text.RegularExpressions;
using BabyRadnik.Infrastructure.Common;

namespace BabyRadnik.Infrastructure.Assistant;

internal sealed record FaqSource(string Document, string Article, string Quote);

internal sealed record FaqEntry(
    string Id, bool Starter, string Question, string[] Alternatives, string Answer, FaqSource[] Sources);

internal sealed record FaqMatch(FaqEntry Entry, double Score);

/// <summary>
/// Gotowe odpowiedzi na najczęstsze pytania rodziców (docs/prawo/faq.json). Dopasowanie jest deterministyczne
/// (ważone słowami podobieństwo pytania do pytania i jego parafraz), a każdy cytat jest sprawdzany dosłownie
/// z wyciągami z aktów i materiałów ZUS już przy starcie.
/// </summary>
internal sealed partial class FaqCatalog
{
    /// <summary>Najniższe podobieństwo, przy którym odpowiadamy z FAQ zamiast szukać w przepisach.</summary>
    public const double MinScore = 0.55;

    /// <summary>Podobieństwo, od którego pytanie z „czy mogę…” nadal uznajemy za ogólne (nie o własną sprawę).</summary>
    public const double StrongScore = 0.8;

    public const string Disclaimer = "Odpowiedź ogólna na podstawie przepisów i materiałów ZUS. Swoją sytuację sprawdzisz w kalkulatorze.";

    private sealed record Phrase(FaqEntry Entry, HashSet<string> Terms);

    private readonly IReadOnlyList<FaqEntry> entries;
    private readonly Dictionary<string, AssistantCitation[]> citations = [];
    private readonly Dictionary<string, FaqEntry> byId;
    private readonly Phrase[] phrases;
    private readonly Dictionary<string, int> documentFrequency;

    public FaqCatalog(RepoPaths paths, LegalSearch search)
    {
        var path = Path.Combine(paths.Root, "docs", "prawo", "faq.json");
        var loaded = File.Exists(path)
            ? JsonSerializer.Deserialize<FaqEntry[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? []
            : [];

        var problems = new List<string>();
        var valid = new List<FaqEntry>();
        foreach (var entry in loaded)
        {
            var resolved = new List<AssistantCitation>();
            foreach (var source in entry.Sources)
            {
                var citation = Resolve(search.Excerpts, source);
                if (citation is null) problems.Add($"{entry.Id}: {source.Document}, {source.Article}: „{source.Quote}”");
                else resolved.Add(citation);
            }
            if (resolved.Count == 0 || resolved.Count != entry.Sources.Length) continue;
            citations[entry.Id] = resolved.ToArray();
            valid.Add(entry);
        }

        Problems = problems;
        entries = valid;
        byId = valid.ToDictionary(entry => entry.Id);
        phrases = valid.SelectMany(entry => entry.Alternatives.Prepend(entry.Question)
            .Select(text => new Phrase(entry, Terms(text)))).Where(phrase => phrase.Terms.Count > 0).ToArray();
        documentFrequency = phrases.SelectMany(phrase => phrase.Terms).CountBy(term => term).ToDictionary();
    }

    /// <summary>Źródła, których cytatu nie udało się znaleźć dosłownie we wskazanym artykule (puste przy poprawnych danych).</summary>
    public IReadOnlyList<string> Problems { get; }

    public IReadOnlyList<FaqEntry> Entries => entries;

    public IReadOnlyList<string> Starters => entries.Where(entry => entry.Starter).Select(entry => entry.Question).ToArray();

    public FaqMatch? Match(string question)
    {
        if (!LooksLikeQuestion(question)) return null;
        var asked = Terms(question);
        // Pytanie musi dotyczyć świadczeń, urlopów lub dziecka — samo „liczy” czy „emerytura” nie wystarczy.
        if (!asked.Any(term => Anchors.Any(anchor => term.StartsWith(anchor, StringComparison.Ordinal)))) return null;

        FaqMatch? best = null;
        var askedWeight = asked.Sum(Weight);
        foreach (var phrase in phrases)
        {
            var common = asked.Where(phrase.Terms.Contains).ToArray();
            if (common.Length < 2 && !(common.Length == 1 && asked.Count == 1)) continue;
            var score = 2 * common.Sum(Weight) / (askedWeight + phrase.Terms.Sum(Weight));
            if (best is null || score > best.Score) best = new FaqMatch(phrase.Entry, score);
        }

        return best is { Score: >= MinScore } ? best : null;
    }

    public AssistantAnswer ToAnswer(FaqEntry entry) => new(entry.Answer, citations[entry.Id], Disclaimer, "faq");

    private double Weight(string term) =>
        Math.Log(1 + (phrases.Length + 0.5) / (documentFrequency.GetValueOrDefault(term) + 0.5));

    // „ZUS” rozwija się w tokenizerze do „zakładu ubezpieczeń społecznych”, a „muszę/trzeba” nic nie mówią o temacie.
    private static readonly HashSet<string> Noise = ["zaklad", "spolec", "musze", "musi", "trzeba", "nalezy", "powinn", "moge", "mozna"];

    private static readonly string[] Anchors =
    [
        "zasil", "urlop", "macier", "rodzic", "ojciec", "matka", "dziecko", "ciaz", "chorob", "opieku", "porod", "poro",
        "adopc", "przysp", "martw", "wczesn", "zlecen", "dzialal", "przedsi", "zaleg", "zadlu", "zwolni", "lekars",
        "ezla", "wyczek", "wychowa", "szpita", "ulga", "bliz", "wymiar", "wnios",
    ];

    private static HashSet<string> Terms(string text) =>
        LegalSearch.LegalTerms.Tokenize(text).Where(term => !LegalSearch.IsGeneric(term) && !Noise.Contains(term)).ToHashSet();

    // Hasło tematu („Urlop rodzicielski”) to nie pytanie — takie wpisy trafiają do wyszukiwarki i modelu.
    private static bool LooksLikeQuestion(string text)
    {
        var normalized = AssistantPolicy.Normalize(text).Trim();
        return normalized.EndsWith('?') || QuestionStart().IsMatch(normalized) ||
               LegalSearch.LegalTerms.Tokenize(text).Count() >= 4;
    }

    [GeneratedRegex(@"^(?:czy|jak|jaki|jaka|jakie|jakich|ile|kiedy|kto|komu|co|gdzie|do kiedy|od kiedy|na ile|w jakim|po ile|dlaczego|czemu|skad|z czego|ktory|ktora|ktore)\b")]
    private static partial Regex QuestionStart();

    private static AssistantCitation? Resolve(IReadOnlyList<LegalExcerpt> excerpts, FaqSource source)
    {
        foreach (var excerpt in excerpts.Where(item => item.Document == source.Document && item.Article == source.Article))
            if (Locate(excerpt, source.Quote) is { } fragment)
                return excerpt.Citation(fragment);
        return null;
    }

    /// <summary>Dosłowny fragment tekstu wyciągu (po ujednoliceniu białych znaków jak w AssistantPolicy) albo null.</summary>
    internal static string? Locate(LegalExcerpt excerpt, string quote)
    {
        var text = AssistantPolicy.CollapseSpace(excerpt.Text);
        var wanted = AssistantPolicy.CollapseSpace(quote.Trim());
        if (wanted.Length == 0) return null;
        var start = text.IndexOf(wanted, StringComparison.Ordinal);
        // Jak w AssistantPolicy: wyświetlany tekst ma numerację z indeksami górnymi, zamiana jest znak na znak.
        if (start < 0) start = LegalNumbering.Flatten(text).IndexOf(LegalNumbering.Flatten(wanted), StringComparison.Ordinal);
        return start < 0 ? null : text.Substring(start, wanted.Length);
    }
}
