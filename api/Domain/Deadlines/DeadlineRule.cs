using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Deadlines;

public sealed record DeadlineCandidate(string Id, DateOnly Date, string Upcoming, string Overdue, string? Consequence);

public static class BenefitNames
{
    public static string Of(BenefitId id) => id switch
    {
        BenefitId.ChorobowyWCiazy => "chorobowy_w_ciazy",
        BenefitId.Rehabilitacyjne => "rehabilitacyjne",
        BenefitId.ZasilekDoDniaPorodu => "zasilek_do_dnia_porodu",
        BenefitId.Macierzynski => "macierzynski",
        BenefitId.MacierzynskiUzupelniajacy => "macierzynski_uzupelniajacy",
        BenefitId.Rodzicielski => "rodzicielski",
        BenefitId.Ojcowski => "ojcowski",
        BenefitId.PrzyjecieDziecka => "przyjecie_dziecka",
        BenefitId.PrzejeciePrzezOjca => "przejecie_przez_ojca",
        BenefitId.Pogrzebowy => "pogrzebowy",
        _ => throw new ArgumentOutOfRangeException(nameof(id)),
    };
}

/// <summary>Reguła terminu z tabeli 8.9: stałe <see cref="Id"/>, podstawa prawna, typ i data wyliczona z faktów albo z okresu świadczenia.</summary>
public abstract class DeadlineRule
{
    public abstract string Id { get; }

    public abstract string PodstawaPrawna { get; }

    public abstract DeadlineType Type { get; }

    public abstract IReadOnlyList<BenefitId> Benefits { get; }

    protected abstract IEnumerable<DeadlineCandidate> Compute(DeadlineContext context);

    public DeadlineOutcome Evaluate(DeadlineContext context)
    {
        try
        {
            var deadlines = Compute(context)
                .Select(c => new Deadline(
                    c.Id,
                    Type,
                    c.Date,
                    context.AsOf > c.Date ? c.Overdue : c.Upcoming,
                    c.Consequence,
                    PodstawaPrawna))
                .ToList();
            return new DeadlineOutcome(deadlines, []);
        }
        catch (ParameterNotFoundException e)
        {
            return new DeadlineOutcome([], [$"Nie wyliczono terminu „{Id}”: brakuje parametru „{e.Key}” na dzień {e.Date:yyyy-MM-dd}."]);
        }
    }

    protected static string PartId(string id, int index) => index == 0 ? id : $"{id}_czesc_{index + 1}";

    protected static DeadlineCandidate NotLaterThan(string id, DateOnly date, string action, string detail, string? consequence) =>
        new(id, date,
            $"Złóż {action} do {PolishDate.Words(date)}{detail}.",
            Overdue(action, date, consequence),
            consequence);

    protected static DeadlineCandidate NotLaterThanBefore(string id, DateOnly date, string action, string detail, string? consequence) =>
        new(id, date,
            $"Złóż {action} najpóźniej {PolishDate.Words(date)}{detail}.",
            Overdue(action, date, consequence),
            consequence);

    protected static string Overdue(string action, DateOnly date, string? consequence) =>
        $"Termin na {action} minął {PolishDate.Words(date)}." + (consequence is null ? "" : $" Skutek: {consequence}.");

    protected static IReadOnlyList<DateOnly> LeaveStarts(BenefitResult benefit) =>
        benefit.Parts.Count > 0
            ? [.. benefit.Parts.Select(p => p.Period.From).Order()]
            : benefit.Period is { } period ? [period.From] : [];
}
