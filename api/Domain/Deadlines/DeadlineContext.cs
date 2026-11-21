using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Deadlines;

public static class InsuranceFacts
{
    public static IReadOnlyList<InsuranceTitle> Titles(PersonFacts? person) => person?.UbezpieczenieTytuly?.Value ?? [];

    public static bool ActiveOn(InsuranceTitle title, DateOnly date) => title.Od <= date && (title.Do is null || title.Do >= date);

    public static bool HasSickness(InsuranceTitle title) => title.Chorobowe != SicknessInsurance.Brak;

    public static IReadOnlyList<InsuranceTitle> SicknessTitlesOn(PersonFacts? person, DateOnly date) =>
        [.. Titles(person).Where(t => HasSickness(t) && ActiveOn(t, date))];

    public static bool IsEmployee(PersonFacts? person, DateOnly? eventDate, DateOnly asOf)
    {
        var active = SicknessTitlesOn(person, eventDate ?? asOf);
        if (active.Count == 0 && eventDate is not null)
        {
            active = SicknessTitlesOn(person, asOf);
        }

        return active is [{ RodzajTytulu: TitleKind.Pracownik }];
    }
}

public sealed record DeadlineContext(FactSet Facts, PersonRef Person, DateOnly AsOf, IParameterProvider Parameters, BenefitResult Benefit)
{
    public PersonFacts? Me => Facts.For(Person);

    public EventKind? Kind => Facts.Sprawa.ZdarzenieRodzaj?.Value;

    public DateOnly? EventDate => Facts.Sprawa.ZdarzenieData?.Value;

    public ParentRole? Role => Me?.OsobaRola?.Value;

    public bool IsEmployee => InsuranceFacts.IsEmployee(Me, EventDate, AsOf);

    public bool BirthEvent => Kind is EventKind.Planuje or EventKind.Ciaza or EventKind.Urodzenie;

    public bool ChildDeceased => Facts.Sprawa.DzieckoMartweUrodzenie?.Value == true || Facts.Sprawa.DzieckoDataZgonu is not null;

    public int Days(string key) => (int)Parameters.GetDecimal(key, EventDate ?? AsOf);

    public int Months(string key) => (int)Parameters.GetDecimal(key, EventDate ?? AsOf);
}

public sealed record DeadlineOutcome(IReadOnlyList<Deadline> Deadlines, IReadOnlyList<string> Notes)
{
    public static readonly DeadlineOutcome Empty = new([], []);
}
