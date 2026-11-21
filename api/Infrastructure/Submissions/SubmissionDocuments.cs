using System.Globalization;
using System.Text.RegularExpressions;
using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Submissions;

public sealed record DocumentRow(string Label, string Value, string? Basis);

public sealed record DocumentSection(string Title, IReadOnlyList<DocumentRow> Rows);

public sealed record DocumentAttachment(string Text, string? Basis);

public sealed record DocumentModel(
    string Title,
    string Number,
    DateOnly Date,
    string? Place,
    string? Addressee,
    string Banner,
    IReadOnlyList<DocumentSection> Sections,
    IReadOnlyList<DocumentAttachment> Attachments,
    IReadOnlyList<NoteSpec> Notes,
    bool SignatureLine,
    string SignatureCaption = "podpis wnioskodawcy");

public sealed record ValidationResult(IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static partial class SubmissionValidator
{
    public const int MaxFields = 40;

    public static ValidationResult Validate(TemplateSpec spec, IReadOnlyDictionary<string, string>? values)
    {
        var errors = new Dictionary<string, List<string>>();

        void Add(string key, string message)
        {
            if (!errors.TryGetValue(key, out var list))
            {
                errors[key] = list = [];
            }

            list.Add(message);
        }

        values ??= new Dictionary<string, string>();
        if (values.Count > MaxFields)
        {
            Add("fields", $"Za dużo pól (najwyżej {MaxFields}).");
        }

        var known = spec.Fields.ToDictionary(f => f.Key, StringComparer.Ordinal);
        foreach (var key in values.Keys.Where(k => !known.ContainsKey(k)))
        {
            Add(FieldKey(key), "Nieznane pole tego szablonu.");
        }

        foreach (var field in spec.Fields)
        {
            var value = Normalize(values, field.Key);
            if (value is not null && PeselLike().IsMatch(value))
            {
                Add(FieldKey(field.Key), "Nie wpisuj numeru PESEL ani innych 11-cyfrowych numerów — Kompas Rodzica ich nie przyjmuje.");
                continue;
            }

            if (!IsApplicable(field, values))
            {
                continue;
            }

            if (value is null)
            {
                if (field.Required)
                {
                    Add(FieldKey(field.Key), "To pole jest wymagane.");
                }

                continue;
            }

            if (value.Length > field.MaxLength)
            {
                Add(FieldKey(field.Key), $"Najwyżej {field.MaxLength} znaków.");
                continue;
            }

            switch (field.Kind)
            {
                case FieldKind.Date when !TryParseDate(value, out _):
                    Add(FieldKey(field.Key), "Podaj datę w formacie RRRR-MM-DD.");
                    break;
                case FieldKind.Integer when !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n > 99:
                    Add(FieldKey(field.Key), "Podaj liczbę całkowitą od 0 do 99.");
                    break;
                case FieldKind.Choice when field.Options!.All(o => o.Value != value):
                    Add(FieldKey(field.Key), $"Dozwolone wartości: {string.Join(", ", field.Options!.Select(o => o.Value))}.");
                    break;
            }
        }

        CheckRange(values, errors, ("ojcowskiOd", "ojcowskiDo"));
        CheckRange(values, errors, ("okresOd", "okresDo"));
        CheckRange(values, errors, ("rodzicielskiOd", "rodzicielskiDo"));
        CheckRange(values, errors, ("uzupelniajacyOd", "uzupelniajacyDo"));
        CheckRange(values, errors, ("laczenieOd", "laczenieDo"));

        return new ValidationResult(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }

    public static bool IsApplicable(FieldSpec field, IReadOnlyDictionary<string, string> values) =>
        field.When is null || ConditionMet(field.When, values);

    public static bool ConditionMet(FieldCondition condition, IReadOnlyDictionary<string, string> values)
    {
        var actual = Normalize(values, condition.Key);
        return condition.Value == "*" ? actual is not null : actual == condition.Value;
    }

    public static string? Normalize(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw) ? raw.Trim() : null;

    public static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
        && date.Year is >= 1900 and <= 2200;

    private static void CheckRange(IReadOnlyDictionary<string, string> values, Dictionary<string, List<string>> errors, (string From, string To) pair)
    {
        var from = Normalize(values, pair.From);
        var to = Normalize(values, pair.To);
        if (from is not null && to is not null && TryParseDate(from, out var f) && TryParseDate(to, out var t) && t < f)
        {
            if (!errors.TryGetValue(FieldKey(pair.To), out var list))
            {
                errors[FieldKey(pair.To)] = list = [];
            }

            list.Add("Data końcowa nie może być wcześniejsza niż początkowa.");
        }
    }

    private static string FieldKey(string key) => $"fields.{key}";

    [GeneratedRegex(@"(?<!\d)\d{11}(?!\d)")]
    private static partial Regex PeselLike();
}

public static class DocumentBuilder
{
    private const string SimulationBanner =
        "SYMULACJA — dokument wygenerowany w Kompasie Rodzica. Nic nie zostało wysłane do żadnej instytucji ani pracodawcy.";

    public static DocumentModel Build(TemplateSpec spec, IReadOnlyDictionary<string, string> values, string number, DateOnly date)
    {
        var sections = new List<DocumentSection>();
        foreach (var section in spec.Sections)
        {
            var rows = new List<DocumentRow>();
            foreach (var field in section.Fields)
            {
                if (!SubmissionValidator.IsApplicable(field, values) || field.Key is "pracodawca" or "miejscowosc")
                {
                    continue;
                }

                var value = SubmissionValidator.Normalize(values, field.Key);
                if (value is null && spec.Type != SubmissionType.DaneZam)
                {
                    continue;
                }

                rows.Add(new DocumentRow(field.Label, value is null ? "— (nie podano)" : Display(field, value), field.Basis));
            }

            if (rows.Count > 0)
            {
                sections.Add(new DocumentSection(section.Title, rows));
            }
        }

        var attachments = spec.Attachments
            .Where(a => a.When is null || SubmissionValidator.ConditionMet(a.When, values))
            .Select(a => new DocumentAttachment(a.Text, a.Basis))
            .ToList();

        return new DocumentModel(
            spec.Title,
            number,
            date,
            SubmissionValidator.Normalize(values, "miejscowosc"),
            SubmissionValidator.Normalize(values, "pracodawca"),
            SimulationBanner,
            sections,
            attachments,
            spec.Notes,
            spec.SignatureLine);
    }

    public static string Summarize(TemplateSpec spec, IReadOnlyDictionary<string, string> values)
    {
        string? Period(string from, string to)
        {
            var a = SubmissionValidator.Normalize(values, from);
            var b = SubmissionValidator.Normalize(values, to);
            return a is not null && b is not null && SubmissionValidator.TryParseDate(a, out var da) && SubmissionValidator.TryParseDate(b, out var db)
                ? $"{FormatDate(da)}–{FormatDate(db)}"
                : null;
        }

        var detail = spec.Id switch
        {
            LeaveTemplateIds.MacierzynskiIRodzicielski or LeaveTemplateIds.Rodzicielski => Period("rodzicielskiOd", "rodzicielskiDo"),
            LeaveTemplateIds.Ojcowski => Period("ojcowskiOd", "ojcowskiDo"),
            LeaveTemplateIds.Uzupelniajacy => Period("uzupelniajacyOd", "uzupelniajacyDo"),
            "dane_zam" => Period("okresOd", "okresDo"),
            "zapytanie_ezus" => spec.Fields.First(f => f.Key == "temat").Options!
                .FirstOrDefault(o => o.Value == SubmissionValidator.Normalize(values, "temat"))?.Label,
            "odpis_aktu" => $"{SubmissionValidator.Normalize(values, "liczbaOdpisow")} szt.",
            _ => null,
        };

        return detail is null ? spec.Short : $"{spec.Short}: {detail}";
    }

    public static string FormatDate(DateOnly date) => date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

    private static string Display(FieldSpec field, string value) => field.Kind switch
    {
        FieldKind.Date when SubmissionValidator.TryParseDate(value, out var d) => FormatDate(d),
        FieldKind.Choice => field.Options!.FirstOrDefault(o => o.Value == value)?.Label ?? value,
        _ => value,
    };
}
