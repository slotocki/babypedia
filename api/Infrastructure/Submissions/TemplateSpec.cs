using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Submissions;

public enum FieldKind
{
    Text,
    LongText,
    Date,
    Integer,
    Choice,
}

public sealed record FieldOption(string Value, string Label);

public sealed record FieldCondition(string Key, string Value);

public sealed record FieldSpec(
    string Key,
    string Label,
    FieldKind Kind,
    string Basis,
    bool Required = false,
    FieldCondition? When = null,
    IReadOnlyList<FieldOption>? Options = null,
    int MaxLength = 200,
    string? Hint = null)
{
    /// <summary>Etykieta w formularzu — z podpowiedzią, jak wypełnić. W PDF-ie drukuję samo <see cref="Label"/>.</summary>
    public string FormLabel => Hint is null ? Label : $"{Label} ({Hint})";
}

public sealed record SectionSpec(string Title, IReadOnlyList<FieldSpec> Fields);

public sealed record AttachmentSpec(string Text, string Basis, FieldCondition? When = null);

/// <param name="When">Uwaga trafia do PDF-u tylko wtedy, gdy warunek jest spełniony.</param>
public sealed record NoteSpec(string Text, string? Basis = null, FieldCondition? When = null);

public sealed record TemplateSpec(
    string Id,
    SubmissionType Type,
    SubmissionRecipient Recipient,
    string Title,
    string Short,
    IReadOnlyList<SectionSpec> Sections,
    IReadOnlyList<AttachmentSpec> Attachments,
    IReadOnlyList<NoteSpec> Notes,
    bool SignatureLine)
{
    public IEnumerable<FieldSpec> Fields => Sections.SelectMany(s => s.Fields);
}

public static class SubmissionNames
{
    public static string ToWire(SubmissionType type) => type switch
    {
        SubmissionType.WniosekUrlop => "wniosek_urlop",
        SubmissionType.DaneZam => "dane_zam",
        SubmissionType.ZapytanieEzus => "zapytanie_ezus",
        SubmissionType.OdpisAktu => "odpis_aktu",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static SubmissionType? ParseType(string? value) => value switch
    {
        "wniosek_urlop" => SubmissionType.WniosekUrlop,
        "dane_zam" => SubmissionType.DaneZam,
        "zapytanie_ezus" => SubmissionType.ZapytanieEzus,
        "odpis_aktu" => SubmissionType.OdpisAktu,
        _ => null,
    };

    public static string ToWire(SubmissionRecipient recipient) => recipient switch
    {
        SubmissionRecipient.Pracodawca => "pracodawca",
        SubmissionRecipient.Zus => "zus",
        SubmissionRecipient.Ezus => "ezus",
        SubmissionRecipient.UrzadStanuCywilnego => "urzad_stanu_cywilnego",
        _ => throw new ArgumentOutOfRangeException(nameof(recipient)),
    };

    public static SubmissionRecipient RecipientFor(SubmissionType type) => type switch
    {
        SubmissionType.WniosekUrlop => SubmissionRecipient.Pracodawca,
        SubmissionType.DaneZam => SubmissionRecipient.Zus,
        SubmissionType.ZapytanieEzus => SubmissionRecipient.Ezus,
        SubmissionType.OdpisAktu => SubmissionRecipient.UrzadStanuCywilnego,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
