using System.Text.Json.Serialization;

namespace BabyRadnik.Domain.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<BenefitId>))]
public enum BenefitId
{
    [JsonStringEnumMemberName("chorobowy_w_ciazy")] ChorobowyWCiazy,
    [JsonStringEnumMemberName("rehabilitacyjne")] Rehabilitacyjne,
    [JsonStringEnumMemberName("zasilek_do_dnia_porodu")] ZasilekDoDniaPorodu,
    [JsonStringEnumMemberName("macierzynski")] Macierzynski,
    [JsonStringEnumMemberName("macierzynski_uzupelniajacy")] MacierzynskiUzupelniajacy,
    [JsonStringEnumMemberName("rodzicielski")] Rodzicielski,
    [JsonStringEnumMemberName("ojcowski")] Ojcowski,
    [JsonStringEnumMemberName("przyjecie_dziecka")] PrzyjecieDziecka,
    [JsonStringEnumMemberName("przejecie_przez_ojca")] PrzejeciePrzezOjca,
    [JsonStringEnumMemberName("pogrzebowy")] Pogrzebowy,
}

[JsonConverter(typeof(JsonStringEnumConverter<BenefitStatus>))]
public enum BenefitStatus
{
    [JsonStringEnumMemberName("przysługuje")] Przysluguje,
    [JsonStringEnumMemberName("nie_przysługuje")] NiePrzysluguje,
    [JsonStringEnumMemberName("brakuje_danych")] BrakujeDanych,
    [JsonStringEnumMemberName("nie_dotyczy")] NieDotyczy,
    [JsonStringEnumMemberName("nie_rozstrzygam")] NieRozstrzygam,
}

[JsonConverter(typeof(JsonStringEnumConverter<Payer>))]
public enum Payer
{
    [JsonStringEnumMemberName("płatnik_składek")] PlatnikSkladek,
    [JsonStringEnumMemberName("zus")] Zus,
}

[JsonConverter(typeof(JsonStringEnumConverter<ApplicationPlace>))]
public enum ApplicationPlace
{
    [JsonStringEnumMemberName("pracodawca")] Pracodawca,
    [JsonStringEnumMemberName("płatnik_składek")] PlatnikSkladek,
    [JsonStringEnumMemberName("zus")] Zus,
}

[JsonConverter(typeof(JsonStringEnumConverter<BasisSource>))]
public enum BasisSource
{
    [JsonStringEnumMemberName("szacunek_zus")] SzacunekZus,
    [JsonStringEnumMemberName("ustalona_przez_płatnika")] UstalonaPrzezPlatnika,
    [JsonStringEnumMemberName("minimalna")] Minimalna,
}

[JsonConverter(typeof(JsonStringEnumConverter<DeadlineType>))]
public enum DeadlineType
{
    [JsonStringEnumMemberName("najpóźniej_do")] NajpozniejDo,
    [JsonStringEnumMemberName("najpóźniej_przed")] NajpozniejPrzed,
    [JsonStringEnumMemberName("okno")] Okno,
    [JsonStringEnumMemberName("przedawnienie")] Przedawnienie,
}

[JsonConverter(typeof(JsonStringEnumConverter<MissingFactType>))]
public enum MissingFactType
{
    [JsonStringEnumMemberName("date")] Date,
    [JsonStringEnumMemberName("integer")] Integer,
    [JsonStringEnumMemberName("number")] Number,
    [JsonStringEnumMemberName("money")] Money,
    [JsonStringEnumMemberName("boolean")] Boolean,
    [JsonStringEnumMemberName("enum")] Enum,
    [JsonStringEnumMemberName("list")] List,
}

public sealed record AssessmentRequest(
    [property: JsonPropertyName("facts")] FactSet Facts,
    [property: JsonPropertyName("asOf")] DateOnly? AsOf = null,
    [property: JsonPropertyName("completedDeadlines")] IReadOnlyList<string>? CompletedDeadlines = null);

public sealed record Period(
    [property: JsonPropertyName("from")] DateOnly From,
    [property: JsonPropertyName("to")] DateOnly To,
    [property: JsonPropertyName("days")] int Days,
    [property: JsonPropertyName("weeks")] decimal Weeks);

public sealed record Amount(
    [property: JsonPropertyName("daily")] decimal Daily,
    [property: JsonPropertyName("total")] decimal Total,
    [property: JsonPropertyName("basis")] decimal Basis,
    [property: JsonPropertyName("basisSource")] BasisSource BasisSource);

public sealed record BenefitPart(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("period")] Period Period,
    [property: JsonPropertyName("rate")] decimal Rate,
    [property: JsonPropertyName("amount")] Amount? Amount = null);

public sealed record Deadline(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] DeadlineType Type,
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("consequence")] string? Consequence = null,
    [property: JsonPropertyName("basis")] string? Basis = null);

/// <summary>Krok ścieżki decyzji: {reguła, podstawa prawna, warunek, spełniony, wyjaśnienie} (sekcja 7).</summary>
public sealed record TraceStep(
    [property: JsonPropertyName("rule")] string Rule,
    [property: JsonPropertyName("basis")] string Basis,
    [property: JsonPropertyName("met")] bool? Met,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("condition")] string? Condition = null);

public sealed record Link(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("url")] string Url);

public sealed record BenefitResult
{
    [JsonPropertyName("id")] public required BenefitId Id { get; init; }
    [JsonPropertyName("person")] public PersonRef Person { get; init; } = PersonRef.Wnioskodawca;
    [JsonPropertyName("status")] public required BenefitStatus Status { get; init; }
    [JsonPropertyName("period")] public Period? Period { get; init; }
    [JsonPropertyName("rate")] public decimal? Rate { get; init; }
    [JsonPropertyName("amount")] public Amount? Amount { get; init; }

    /// <summary>Kwota ryczałtowa świadczenia (np. zasiłek pogrzebowy) — stała kwota z parametru, bez okresu i podstawy.</summary>
    [JsonPropertyName("lumpSum")] public decimal? LumpSum { get; init; }
    [JsonPropertyName("parts")] public IReadOnlyList<BenefitPart> Parts { get; init; } = [];
    [JsonPropertyName("payer")] public Payer? Payer { get; init; }
    [JsonPropertyName("whereToApply")] public ApplicationPlace? WhereToApply { get; init; }
    [JsonPropertyName("documents")] public IReadOnlyList<string> Documents { get; init; } = [];
    [JsonPropertyName("deadlines")] public IReadOnlyList<Deadline> Deadlines { get; init; } = [];
    [JsonPropertyName("reason")] public string? Reason { get; init; }
    [JsonPropertyName("legalBasis")] public string? LegalBasis { get; init; }
    [JsonPropertyName("whatWouldChange")] public string? WhatWouldChange { get; init; }
    [JsonPropertyName("notes")] public IReadOnlyList<string> Notes { get; init; } = [];
    [JsonPropertyName("links")] public IReadOnlyList<Link> Links { get; init; } = [];
    [JsonPropertyName("trace")] public IReadOnlyList<TraceStep> Trace { get; init; } = [];
}

public sealed record MissingFact(
    [property: JsonPropertyName("fact")] string Fact,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("type")] MissingFactType Type,
    [property: JsonPropertyName("person")] PersonRef? Person = null,
    [property: JsonPropertyName("options")] IReadOnlyList<string>? Options = null,
    [property: JsonPropertyName("benefitIds")] IReadOnlyList<BenefitId>? BenefitIds = null);

public sealed record ValidationIssue(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("fact")] string? Fact = null,
    [property: JsonPropertyName("person")] PersonRef? Person = null);

public sealed record NextStep(
    [property: JsonPropertyName("deadlineId")] string DeadlineId,
    [property: JsonPropertyName("benefitId")] BenefitId BenefitId,
    [property: JsonPropertyName("person")] PersonRef Person = PersonRef.Wnioskodawca);

public sealed record Risk(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("basis")] string? Basis = null,
    [property: JsonPropertyName("link")] Link? Link = null);

public sealed record AssessmentResult
{
    [JsonPropertyName("rulesetVersion")] public required string RulesetVersion { get; init; }
    [JsonPropertyName("benefits")] public IReadOnlyList<BenefitResult> Benefits { get; init; } = [];
    [JsonPropertyName("missingFacts")] public IReadOnlyList<MissingFact> MissingFacts { get; init; } = [];
    [JsonPropertyName("validationErrors")] public IReadOnlyList<ValidationIssue> ValidationErrors { get; init; } = [];
    [JsonPropertyName("nextStep")] public NextStep? NextStep { get; init; }
    [JsonPropertyName("risks")] public IReadOnlyList<Risk> Risks { get; init; } = [];
    [JsonPropertyName("difficultSituation")] public bool DifficultSituation { get; init; }
}

public interface IAssessmentEngine
{
    AssessmentResult Assess(AssessmentRequest request);
}
