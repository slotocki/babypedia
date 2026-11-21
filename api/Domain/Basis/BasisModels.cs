using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Basis;

public enum BasisOutcome
{
    Determined,
    MissingData,
    Undecided,
    NoTitle,
}

public sealed record BasisMonth(string Month, decimal Contribution, bool Included, string? Remark = null);

/// <summary>
/// Wynik ustalenia podstawy wymiaru zasiłku (instrukcja 8.7, poziom 1).
/// <c>Basis</c> i <c>Source</c> są puste, gdy <c>Outcome</c> ≠ <c>Determined</c>; wtedy <c>Message</c> mówi, czego nie liczymy,
/// a <c>MissingFacts</c> wskazuje fakty, które rozstrzygają sprawę (ostatnim jest zawsze „podstawa ustalona przez płatnika”).
/// </summary>
public sealed record BasisAssessment(
    BasisOutcome Outcome,
    decimal? Basis,
    BasisSource? Source,
    bool IsEstimate,
    DateOnly ReferenceDate,
    string Message,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> NotCounted,
    IReadOnlyList<BasisMonth> Months,
    IReadOnlyList<TraceStep> Trace,
    IReadOnlyList<MissingFact> MissingFacts)
{
    public bool HasBasis => Basis is not null;
}

public interface IBasisService
{
    /// <param name="facts">Fakty sprawy i osoby.</param>
    /// <param name="person">Osoba, której podstawę ustalamy.</param>
    /// <param name="dzienPowstaniaPrawa">Dzień powstania prawa do zasiłku; wyznacza miesiąc odniesienia i datę parametrów (przy art. 43 — dzień pierwszego zasiłku w ciągu).</param>
    BasisAssessment Assess(FactSet facts, PersonRef person, DateOnly dzienPowstaniaPrawa);
}
