using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Scenarios;

internal static class FactOverlay
{
    public static Fact<T> Assume<T>(T value) => new(value, FactSource.Assumption);

    public static FactSet WithPerson(this FactSet facts, PersonRef person, Func<PersonFacts, PersonFacts> change) =>
        person == PersonRef.Wnioskodawca
            ? facts with { Wnioskodawca = change(facts.Wnioskodawca) }
            : facts with { DrugiRodzic = change(facts.DrugiRodzic ?? new PersonFacts()) };

    public static PersonRef Other(this PersonRef person) =>
        person == PersonRef.Wnioskodawca ? PersonRef.DrugiRodzic : PersonRef.Wnioskodawca;

    public static ValidationIssue NotApplicable(string message, PersonRef person, string? fact = null) =>
        new("scenariusz_nie_dotyczy", message, fact, person);

    public static ValidationIssue MissingFact(string message, PersonRef person, string fact) =>
        new("scenariusz_brak_faktu", message, fact, person);

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy");

    public static string PersonName(PersonRef person) =>
        person == PersonRef.Wnioskodawca ? "wnioskodawcy" : "drugiego rodzica";

    public static bool ActiveOn(InsuranceTitle title, DateOnly date) =>
        title.Od <= date && (title.Do is null || title.Do >= date);

    public static IReadOnlyList<InsuranceTitle> Replace(IReadOnlyList<InsuranceTitle> titles, InsuranceTitle old, InsuranceTitle changed) =>
        titles.Select(t => ReferenceEquals(t, old) ? changed : t).ToList();
}
