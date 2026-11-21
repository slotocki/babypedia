using System.Globalization;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Domain.Deadlines;

namespace BabyRadnik.Domain.Catalog;

public sealed record RiskContext(
    FactSet Facts,
    DateOnly AsOf,
    IParameterProvider Parameters,
    IReadOnlyList<BenefitResult> Benefits,
    CompletedDeadlines Completed)
{
    public IReadOnlyList<PersonRef> Persons => Facts.DrugiRodzic is null
        ? [PersonRef.Wnioskodawca]
        : [PersonRef.Wnioskodawca, PersonRef.DrugiRodzic];

    public DateOnly? EventDate => Facts.Sprawa.ZdarzenieData?.Value;

    public EventKind? Kind => Facts.Sprawa.ZdarzenieRodzaj?.Value;

    public IEnumerable<BenefitResult> Granted(params BenefitId[] ids) =>
        Benefits.Where(b => b.Status == BenefitStatus.Przysluguje && (ids.Length == 0 || ids.Contains(b.Id)));
}

/// <summary>Reguła ryzyka z 8.10: pokazuje się tylko, gdy pasuje do faktów. Aplikacja informuje, nie ocenia.</summary>
public interface IRiskRule
{
    string Id { get; }

    string PodstawaPrawna { get; }

    IReadOnlyList<Risk> Evaluate(RiskContext context);
}

public sealed class InsuranceEndsBeforeBirthRisk : IRiskRule
{
    public string Id => "koniec_ubezpieczenia_przed_porodem";

    public string PodstawaPrawna => "ustawa art. 29 ust. 1, art. 30";

    public IReadOnlyList<Risk> Evaluate(RiskContext context)
    {
        if (context.Kind is not (EventKind.Planuje or EventKind.Ciaza or EventKind.Przyjecie) || context.EventDate is not { } date)
        {
            return [];
        }

        foreach (var person in context.Persons)
        {
            var facts = context.Facts.For(person);
            var endedBefore = InsuranceFacts.Titles(facts).Any(t => InsuranceFacts.HasSickness(t) && t.Do < date);
            var uninsuredOnDate = InsuranceFacts.SicknessTitlesOn(facts, date).Count == 0;
            var contractEnds = facts?.UmowaNaCzasOkreslony?.Value == true && facts.UmowaDataKonca?.Value < date;
            var leaving = facts?.UmowaPlanowaneOdejscieData?.Value < date;
            if ((endedBefore && uninsuredOnDate) || contractEnds || leaving)
            {
                return
                [
                    new Risk(Id,
                        "Ubezpieczenie chorobowe kończy się przed dniem porodu lub przyjęcia dziecka (koniec umowy, odejście z pracy albo zamknięcie działalności). Od tego zależy prawo do zasiłku macierzyńskiego; wyjątki są w art. 30 ustawy.",
                        PodstawaPrawna, CatalogLinks.Zus),
                ];
            }
        }

        return [];
    }
}

public sealed class VoluntaryInsuranceRenewalRisk : IRiskRule
{
    public string Id => "ponowne_zgloszenie_do_chorobowego";

    public string PodstawaPrawna => "Poradnik ZUS 2026 s. 35";

    public IReadOnlyList<Risk> Evaluate(RiskContext context)
    {
        var date = context.EventDate ?? context.AsOf;
        foreach (var person in context.Persons)
        {
            var voluntary = InsuranceFacts.SicknessTitlesOn(context.Facts.For(person), date)
                .Any(t => t.Chorobowe == SicknessInsurance.Dobrowolne);
            var maternity = context.Granted(BenefitId.Macierzynski, BenefitId.Rodzicielski, BenefitId.Ojcowski, BenefitId.PrzyjecieDziecka)
                .Any(b => b.Person == person);
            if (voluntary && maternity)
            {
                return
                [
                    new Risk(Id,
                        "Dobrowolne ubezpieczenie chorobowe ustaje automatycznie, gdy zaczynasz pobierać zasiłek macierzyński. Po jego zakończeniu trzeba zgłosić się do tego ubezpieczenia ponownie, jeśli chcesz je mieć.",
                        PodstawaPrawna, CatalogLinks.Zus),
                ];
            }
        }

        return [];
    }
}

public sealed class AveragedVariantDeadlineRisk : IRiskRule
{
    public string Id => "termin_21_dni";

    public string PodstawaPrawna => "ustawa art. 30a";

    public IReadOnlyList<Risk> Evaluate(RiskContext context)
    {
        foreach (var benefit in context.Granted(BenefitId.Macierzynski, BenefitId.PrzyjecieDziecka))
        {
            var deadline = benefit.Deadlines.FirstOrDefault(d => d.Id == WniosekUsrednionaRule.RuleId);
            var filed = context.Facts.For(benefit.Person)?.PlanDataWnioskuUsrednionego?.Value is { } on && on <= deadline?.Date;
            if (deadline is null || deadline.Date < context.AsOf || filed || context.Completed.Contains(benefit.Person, deadline.Id))
            {
                continue;
            }

            return
            [
                new Risk(Id,
                    $"Wniosek o wypłatę zasiłku w wariancie 81,5% możesz złożyć najpóźniej {PolishDate.Words(deadline.Date)}. Po tym terminie zasiłek za urlop rodzicielski wynosi 70% podstawy.",
                    PodstawaPrawna, CatalogLinks.Zus),
            ];
        }

        return [];
    }
}

public sealed class UnusedPaternityLeaveRisk : IRiskRule
{
    public string Id => "niewykorzystany_ojcowski";

    public string PodstawaPrawna => "KP art. 182³ § 1";

    public IReadOnlyList<Risk> Evaluate(RiskContext context)
    {
        foreach (var benefit in context.Granted(BenefitId.Ojcowski))
        {
            var window = benefit.Deadlines.FirstOrDefault(d => d.Id == "wykorzystanie_ojcowskiego");
            if (window is null || benefit.Period is not null || context.Completed.Contains(benefit.Person, window.Id))
            {
                continue;
            }

            return
            [
                new Risk(Id,
                    $"Urlop ojcowski trzeba wykorzystać najpóźniej {PolishDate.Words(window.Date)}. Jeśli ojciec z niego nie skorzysta, prawo wygasa i nie można go wykorzystać później.",
                    PodstawaPrawna, CatalogLinks.Mrpips),
            ];
        }

        return [];
    }
}

public sealed class ClaimLimitationRisk : IRiskRule
{
    public string Id => "przedawnienie_zasilku";

    public string PodstawaPrawna => "ustawa art. 67";

    public IReadOnlyList<Risk> Evaluate(RiskContext context)
    {
        var due = context.Granted()
            .Where(b => b.Period is { } p && p.To <= context.AsOf)
            .Select(b => (Benefit: b, Deadline: b.Deadlines.FirstOrDefault(d => d.Type == DeadlineType.Przedawnienie)))
            .Where(x => x.Deadline is not null && !context.Completed.Contains(x.Benefit.Person, x.Deadline.Id))
            .OrderBy(x => x.Deadline!.Date)
            .FirstOrDefault();
        if (due.Deadline is null)
        {
            return [];
        }

        var end = PolishDate.Words(due.Benefit.Period!.To);
        var date = PolishDate.Words(due.Deadline.Date);
        var text = due.Deadline.Date >= context.AsOf
            ? $"Roszczenie o wypłatę zasiłku za okres do {end} przedawnia się {date} (6 miesięcy od ostatniego dnia okresu). Złóż wniosek przed tym terminem."
            : $"Roszczenie o wypłatę zasiłku za okres do {end} mogło się przedawnić {date} (6 miesięcy od ostatniego dnia okresu). Sprawdź w ZUS, czy nie obowiązuje dłuższy termin (art. 67 ust. 3–4).";
        return [new Risk(Id, text, PodstawaPrawna, CatalogLinks.Zus)];
    }
}

public sealed class ContributionOrContractChangeInPregnancyRisk : IRiskRule
{
    private const string PregnancyWeeksKey = "ciaza_trwanie_tyg";

    public string Id => "zmiana_podstawy_lub_umowa_w_ciazy";

    public string PodstawaPrawna => "BabyRadnik, sekcja 8.10 (informacja neutralna)";

    public IReadOnlyList<Risk> Evaluate(RiskContext context)
    {
        if (context.EventDate is not { } date)
        {
            return [];
        }

        DateOnly pregnancyStart;
        try
        {
            pregnancyStart = date.AddDays(-7 * (int)context.Parameters.GetDecimal(PregnancyWeeksKey, date));
        }
        catch (ParameterNotFoundException)
        {
            return [];
        }

        foreach (var person in context.Persons)
        {
            var facts = context.Facts.For(person);
            var newTitle = InsuranceFacts.Titles(facts).Any(t => InsuranceFacts.HasSickness(t) && t.Od > pregnancyStart && t.Od <= date);
            if (newTitle || BaseRoseDuringPregnancy(facts, pregnancyStart))
            {
                return
                [
                    new Risk(Id,
                        "Umowa lub ubezpieczenie rozpoczęte w czasie ciąży albo podwyższenie podstawy składek w ciąży mogą być wyjaśniane przez ZUS. To informacja ogólna, a nie ocena Twojej sytuacji; w razie pytań z ZUS przygotuj dokumenty umowy i składek.",
                        PodstawaPrawna, CatalogLinks.Zus),
                ];
            }
        }

        return [];
    }

    private static bool BaseRoseDuringPregnancy(PersonFacts? facts, DateOnly pregnancyStart)
    {
        var months = facts?.UbezpieczeniePodstawySkladek?.Value
            .Select(b => (Month: DateOnly.TryParseExact(b.Miesiac + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateOnly?)null, b.Kwota))
            .Where(m => m.Month is not null)
            .ToList() ?? [];
        var startMonth = new DateOnly(pregnancyStart.Year, pregnancyStart.Month, 1);
        var before = months.Where(m => m.Month < startMonth).Select(m => m.Kwota).ToList();
        return before.Count > 0 && months.Any(m => m.Month >= startMonth && m.Kwota > before.Max());
    }
}

public static class RiskRules
{
    public static IReadOnlyList<IRiskRule> Rules { get; } =
    [
        new InsuranceEndsBeforeBirthRisk(),
        new VoluntaryInsuranceRenewalRisk(),
        new AveragedVariantDeadlineRisk(),
        new UnusedPaternityLeaveRisk(),
        new ClaimLimitationRisk(),
        new ContributionOrContractChangeInPregnancyRisk(),
    ];

    /// <summary>Ryzyka z uprawnień (brak dobrowolnego chorobowego, zadłużenie, praca na zwolnieniu) plus reguły katalogu; scalone po identyfikatorze.</summary>
    public static IReadOnlyList<Risk> Merge(IEnumerable<Risk> fromEligibility, RiskContext context)
    {
        var own = Rules.SelectMany(r => r.Evaluate(context)).ToList();
        var ownIds = Rules.Select(r => r.Id).ToHashSet();
        return [.. own.Concat(fromEligibility.Where(r => !ownIds.Contains(r.Id))).DistinctBy(r => r.Id)];
    }
}
