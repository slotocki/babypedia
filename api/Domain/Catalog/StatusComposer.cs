using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Catalog;

/// <summary>
/// Składa jeden status (świadczenie × osoba) z wyniku uprawnień (prawo) i okresów (okres/części).
/// Pierwszeństwo: nie_dotyczy → pewna odmowa (prawo przed okresem) → nie_rozstrzygam → brakuje_danych → przysługuje.
/// </summary>
public static class StatusComposer
{
    public const string ContactNote = "Skontaktuj się z ZUS: infolinia 22 560 16 00, www.zus.pl/kontakt, albo placówka ZUS.";

    public static BenefitResult Compose(CatalogEntry entry, PersonRef person, BenefitResult? rights, BenefitResult? period, bool employee)
    {
        var modules = new[] { rights, period }.OfType<BenefitResult>().ToList();
        if (modules.Count == 0 || modules.Any(m => m.Status == BenefitStatus.NieDotyczy))
        {
            return Bare(entry, person, BenefitStatus.NieDotyczy);
        }

        if (modules.FirstOrDefault(m => m.Status == BenefitStatus.NiePrzysluguje) is { } denial)
        {
            return Denied(entry, person, denial, rights, period);
        }

        if (modules.FirstOrDefault(m => m.Status == BenefitStatus.NieRozstrzygam) is { } undecided)
        {
            return Undecided(entry, person, undecided, rights, period);
        }

        var trace = Trace(rights, period);
        if (modules.Any(m => m.Status == BenefitStatus.BrakujeDanych))
        {
            return Bare(entry, person, BenefitStatus.BrakujeDanych) with
            {
                Notes = Merge(rights?.Notes, period?.Notes),
                Trace = trace,
            };
        }

        return new BenefitResult
        {
            Id = entry.Id,
            Person = person,
            Status = BenefitStatus.Przysluguje,
            Period = period?.Period,
            Parts = period?.Parts ?? [],
            Payer = rights?.Payer,
            WhereToApply = rights?.WhereToApply,
            Documents = Merge(rights?.Documents, entry.Documents, employee ? entry.EmployerDocuments : null),
            Deadlines = MergeDeadlines(rights?.Deadlines, period?.Deadlines),
            Notes = Merge(rights?.Notes, period?.Notes),
            Links = Links(entry, rights, period),
            Trace = trace,
        };
    }

    private static BenefitResult Denied(CatalogEntry entry, PersonRef person, BenefitResult denial, BenefitResult? rights, BenefitResult? period) =>
        Bare(entry, person, BenefitStatus.NiePrzysluguje) with
        {
            Reason = denial.Reason,
            LegalBasis = denial.LegalBasis,
            WhatWouldChange = denial.WhatWouldChange,
            Deadlines = denial.Deadlines,
            Notes = denial.Notes,
            Links = Links(entry, denial),
            Trace = ReferenceEquals(denial, rights) ? Trace(rights, null) : Trace(rights, period),
        };

    private static BenefitResult Undecided(CatalogEntry entry, PersonRef person, BenefitResult undecided, BenefitResult? rights, BenefitResult? period)
    {
        var notes = Merge(undecided.Notes);
        if (!notes.Any(n => n.StartsWith("Skontaktuj się z ZUS", StringComparison.Ordinal)))
        {
            notes = [.. notes, ContactNote];
        }

        return Bare(entry, person, BenefitStatus.NieRozstrzygam) with
        {
            Reason = undecided.Reason,
            LegalBasis = undecided.LegalBasis,
            Notes = notes,
            Links = Links(entry, undecided).Append(CatalogLinks.ZusContact).DistinctBy(l => l.Url).ToList(),
            Trace = Trace(rights, period),
        };
    }

    private static BenefitResult Bare(CatalogEntry entry, PersonRef person, BenefitStatus status) =>
        new() { Id = entry.Id, Person = person, Status = status };

    private static IReadOnlyList<TraceStep> Trace(BenefitResult? rights, BenefitResult? period) =>
        [.. rights?.Trace ?? [], .. period?.Trace ?? []];

    private static IReadOnlyList<Link> Links(CatalogEntry entry, params BenefitResult?[] sources) =>
        [.. entry.Links.Concat(sources.OfType<BenefitResult>().SelectMany(s => s.Links)).DistinctBy(l => l.Url)];

    private static IReadOnlyList<string> Merge(params IEnumerable<string>?[] lists) =>
        [.. lists.OfType<IEnumerable<string>>().SelectMany(l => l).Distinct(StringComparer.OrdinalIgnoreCase)];

    private static IReadOnlyList<Deadline> MergeDeadlines(params IEnumerable<Deadline>?[] lists) =>
        [.. lists.OfType<IEnumerable<Deadline>>().SelectMany(l => l).DistinctBy(d => d.Id)];
}
