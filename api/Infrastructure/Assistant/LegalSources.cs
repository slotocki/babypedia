using System.Text.RegularExpressions;

namespace BabyRadnik.Infrastructure.Assistant;

/// <param name="Text">Treść do wyświetlenia: dosłowny tekst wyciągu z numeracją z indeksami górnymi i bez znaczników przypisów.</param>
/// <param name="Notes">Informacje z przypisów aktu o wejściu w życie lub brzmieniu jednostek tego artykułu.</param>
/// <param name="Key">Klucz numeru artykułu do wyszukiwania (<see cref="LegalNumbering.Key"/>) albo null.</param>
internal sealed record LegalExcerpt(
    string Document, string Article, string Text, bool IsLegalAct, IReadOnlyList<string> Notes, string? Key = null)
{
    public AssistantCitation Citation(string fragment) => new(Document, fragment, Article, Text, Notes);
}

/// <summary>Odczyt wyciągów z docs/prawo: podział na artykuły, przypisy, uwagi redakcyjne i numeracja do wyświetlenia.</summary>
internal static partial class LegalSources
{
    public const string CodeTitle = "Kodeks pracy";

    public static readonly (string File, string Title, bool IsLegalAct)[] Files =
    [
        ("ustawa-zasilkowa.md", "Ustawa zasiłkowa", true),
        ("kodeks-pracy.md", CodeTitle, true),
        ("rozporzadzenie-2025-03-11-wnioski-pracownicze.md", "Rozporządzenie w sprawie wniosków pracowniczych", true),
        ("rozporzadzenie-2026-09-27-dokumenty-zasilki.md", "Rozporządzenie w sprawie dokumentów do zasiłków", true),
        ("zalacznik-zus.md", "Załącznik ZUS (materiał pomocniczy)", false),
        ("poradnik-zus-2026.md", "Poradnik ZUS (materiał pomocniczy)", false),
    ];

    public sealed record Footnote(string Number, string Article, string Unit, string Note);

    private sealed record Section(string Document, string Title, string Text, bool IsLegalAct, IReadOnlyList<Footnote> Footnotes);

    public static (IReadOnlyList<LegalExcerpt> Excerpts, LegalNumbering Numbering) Read(string directory)
    {
        var sections = Files.SelectMany(file => ReadFile(directory, file)).ToArray();
        // Mapę numeracji budujemy z surowej treści KP (przed zamianą), bo kolejność „§ 1, § 11, § 2” jest widoczna tylko w niej.
        var numbering = new LegalNumbering(sections
            .Where(section => section.Document == CodeTitle && ArticleNumber(section.Title) is not null)
            .Select(section => (ArticleNumber(section.Title)!, section.Text)));
        return (sections.Select(section => ToExcerpt(section, numbering)).ToArray(), numbering);
    }

    private static LegalExcerpt ToExcerpt(Section section, LegalNumbering numbering)
    {
        var number = ArticleNumber(section.Title);
        var notes = new List<string>();
        var text = section.Text;
        if (number is not null && section.Footnotes.Count > 0)
            text = FootnoteMarker().Replace(text, match =>
            {
                var unit = match.Groups["unit"].Value;
                var footnote = section.Footnotes.FirstOrDefault(note => note.Number == match.Groups["mark"].Value);
                // Znacznik zdejmujemy tylko wtedy, gdy przypis o tym numerze dotyczy właśnie tej jednostki.
                if (footnote is null || footnote.Article != number || footnote.Unit != unit) return match.Value;
                notes.Add(footnote.Note);
                return unit + ". ";
            });

        if (section.IsLegalAct)
            text = numbering.Display(text, section.Document == CodeTitle ? number : null);

        return new LegalExcerpt(section.Document, section.Title, text, section.IsLegalAct, notes,
            number is null ? null : LegalNumbering.Key(number));
    }

    /// <summary>Numer z nagłówka „Art. 182¹a” → „182¹a”; inne nagłówki → null.</summary>
    public static string? ArticleNumber(string title)
    {
        var match = ArticleTitle().Match(title);
        return match.Success ? match.Groups["num"].Value : null;
    }

    private static IEnumerable<Section> ReadFile(string directory, (string File, string Title, bool IsLegalAct) source)
    {
        var path = Path.Combine(directory, source.File);
        if (!File.Exists(path)) yield break;

        var markdown = HtmlComment().Replace(File.ReadAllText(path).Replace("\r\n", "\n"), "");
        var parts = SectionHeading().Split(markdown);
        var footnotes = new List<Footnote>();
        var found = new List<(string Title, string Text)>();
        string? parent = null;
        for (var i = 1; i < parts.Length; i += 2)
        {
            var title = parts[i].Trim();
            if (title.StartsWith("Przypisy", StringComparison.OrdinalIgnoreCase))
            {
                footnotes.AddRange(ParseFootnotes(parts[i + 1]));
                break;
            }
            if (source.IsLegalAct && !ArticleHeading().IsMatch(title)) continue;

            var content = CleanContent(parts[i + 1]);
            if (source.IsLegalAct)
            {
                if (content.Length > 0) found.Add((title, content));
                continue;
            }

            // W poradniku nagłówki są spłaszczone: pusty „Jak otrzymać zasiłek” stoi nad „Pracownicy” i „Osoby ubezpieczone…”.
            // Bez rodzica taki tytuł nic nie mówi ani wyszukiwarce, ani czytelnikowi, więc go doklejamy.
            if (content.Length == 0)
            {
                parent = AssistantPolicy.Normalize(title).StartsWith("jak ", StringComparison.Ordinal) ? title : null;
                continue;
            }
            var audience = parent is not null && AudienceHeading().IsMatch(AssistantPolicy.Normalize(title));
            found.Add((audience ? $"{parent} › {title}" : title, content));
            if (!audience) parent = null;
        }

        foreach (var (title, text) in found)
            yield return new Section(source.Title, title, text, source.IsLegalAct, footnotes);
    }

    /// <summary>Przypisy o dodaniu lub nowym brzmieniu ustępu, np. „3 Art. 9 ust. 4 dodany ustawą … która wchodzi w życie 1.01.2027 r.”.</summary>
    public static IEnumerable<Footnote> ParseFootnotes(string text)
    {
        foreach (Match match in FootnoteLine().Matches(text))
        {
            var unit = match.Groups["unit"].Value;
            var act = match.Groups["dz"].Value;
            var effective = match.Groups["eff"].Value;
            var note = match.Groups["kind"].Value.StartsWith("dodany", StringComparison.Ordinal)
                ? $"Art. {match.Groups["art"].Value} ust. {unit} wchodzi w życie {effective} r. (dodany ustawą z dnia {match.Groups["date"].Value} r., {act})."
                : $"Art. {match.Groups["art"].Value} ust. {unit} w brzmieniu od {effective} r. (ustawa z dnia {match.Groups["date"].Value} r., {act}).";
            yield return new Footnote(match.Groups["n"].Value, match.Groups["art"].Value, unit, note);
        }
    }

    /// <summary>Bez uwag redakcyjnych (blok „&gt; Uwaga redakcyjna…”) i nagłówków rozdziałów — nie są treścią aktu ani materiału.</summary>
    private static string CleanContent(string text)
    {
        var lines = new List<string>();
        var skippingNote = false;
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('>'))
            {
                var quote = trimmed.TrimStart('>').TrimStart();
                if (quote.StartsWith("Uwaga redakcyjna", StringComparison.Ordinal) ||
                    quote.StartsWith("[Uwaga redakcyjna", StringComparison.Ordinal))
                    skippingNote = true;
                if (skippingNote) continue;
            }
            else
            {
                skippingNote = false;
            }

            if (ChapterHeading().IsMatch(line)) continue;
            lines.Add(line);
        }
        return string.Join('\n', lines).Trim();
    }

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlComment();

    [GeneratedRegex(@"^#{2,6}[ \t]+([^\n]+)$", RegexOptions.Multiline)]
    private static partial Regex SectionHeading();

    [GeneratedRegex(@"^#[ \t]")]
    private static partial Regex ChapterHeading();

    [GeneratedRegex(@"^(?:Art\.|§)\s*\d")]
    private static partial Regex ArticleHeading();

    [GeneratedRegex(@"^(?:pracownic|ubezpieczeni\b|osoby ubezpieczone|zleceniobiorc|osoby prowadzace|przedsiebiorc)")]
    private static partial Regex AudienceHeading();

    [GeneratedRegex(@"^Art\.\s*(?<num>\d+[⁰¹²³⁴⁵⁶⁷⁸⁹]*[a-z]?)(?![\w⁰¹²³⁴⁵⁶⁷⁸⁹])")]
    private static partial Regex ArticleTitle();

    // Znacznik przypisu doklejony do numeru ustępu: „4.3 Niezależnie…” = ust. 4, przypis 3.
    [GeneratedRegex(@"^(?<unit>\d+[a-z]?)\.(?<mark>\d+) ", RegexOptions.Multiline)]
    private static partial Regex FootnoteMarker();

    [GeneratedRegex(@"^(?<n>\d+) Art\. (?<art>\d+[a-z]?) ust\. (?<unit>\d+[a-z]?) (?<kind>dodany ustawą|w brzmieniu ustawy) z dnia (?<date>[\d.]+) r\. \((?<dz>Dz\.U\.[^)]*)\), która wchodzi w życie (?<eff>[\d.]+) r\.[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex FootnoteLine();
}
