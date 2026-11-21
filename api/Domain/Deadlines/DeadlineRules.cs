using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Deadlines;

public sealed class WniosekMacierzynskiPrzedPorodemRule : DeadlineRule
{
    public const string RuleId = "wniosek_macierzynski_przed_porodem";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "Poradnik ZUS 2026 s. 27";
    public override DeadlineType Type => DeadlineType.NajpozniejPrzed;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.Macierzynski];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (c.Kind is not (EventKind.Planuje or EventKind.Ciaza) || c.Role != ParentRole.Matka || !c.IsEmployee
            || c.Benefit.Period is not { } period || c.EventDate is not { } birth || period.From >= birth)
        {
            yield break;
        }

        var days = c.Days(DeadlineParameterKeys.WniosekMacierzynskiPrzedPorodemDni);
        yield return NotLaterThanBefore(
            Id,
            period.From.AddDays(-days),
            "u pracodawcy wniosek o urlop macierzyński przed porodem",
            $" ({days} dni przed jego początkiem {PolishDate.Words(period.From)})",
            "urlop macierzyński zacznie się później");
    }
}

public sealed class WniosekUsrednionaRule : DeadlineRule
{
    public const string RuleId = "wniosek_usredniona";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "ustawa art. 30a";
    public override DeadlineType Type => DeadlineType.NajpozniejDo;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.Macierzynski, BenefitId.PrzyjecieDziecka];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        var birth = c.Benefit.Id == BenefitId.Macierzynski && c.BirthEvent && c.Role == ParentRole.Matka;
        var adoption = c.Benefit.Id == BenefitId.PrzyjecieDziecka && c.Kind == EventKind.Przyjecie;
        if ((!birth && !adoption) || c.ChildDeceased || c.EventDate is not { } eventDate)
        {
            yield break;
        }

        var days = c.Days(DeadlineParameterKeys.WniosekUsredniona);
        var anchor = !birth ? "po przyjęciu dziecka"
            : c.Kind == EventKind.Urodzenie ? "po porodzie"
            : "po porodzie; termin liczony od przewidywanej daty porodu, po porodzie wyliczymy go od faktycznej daty";
        yield return NotLaterThan(
            Id,
            eventDate.AddDays(days),
            "wniosek o wypłatę zasiłku w wariancie 81,5% (urlop macierzyński i rodzicielski w pełnym wymiarze)",
            $" ({days} dni {anchor})",
            "zasiłek za urlop rodzicielski wyniesie 70% podstawy");
    }
}

public sealed class RezygnacjaMatkiRule : DeadlineRule
{
    public const string RuleId = "rezygnacja_matki_z_macierzynskiego";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 180 § 8";
    public override DeadlineType Type => DeadlineType.NajpozniejPrzed;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.Macierzynski];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (c.Role != ParentRole.Matka || !c.IsEmployee || c.EventDate is not { } birth
            || c.Me?.PlanRezygnacjaZMacierzynskiegoPoTyg?.Value is not { } weeks || c.Benefit.Period is not { } period)
        {
            yield break;
        }

        var lastDayUsed = birth.AddDays(weeks * 7 - 1);
        if (period.To != lastDayUsed)
        {
            yield break;
        }

        var back = lastDayUsed.AddDays(1);
        var days = c.Days(DeadlineParameterKeys.RezygnacjaMatkiDni);
        yield return NotLaterThanBefore(
            Id,
            back.AddDays(-days),
            "u pracodawcy wniosek o rezygnację z pozostałej części urlopu macierzyńskiego",
            $" ({days} dni przed powrotem do pracy {PolishDate.Words(back)})",
            null);
    }
}

public sealed class WniosekOjcaOCzescMacierzynskiegoRule : DeadlineRule
{
    public const string RuleId = "wniosek_ojca_o_czesc_macierzynskiego";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 180 § 9";
    public override DeadlineType Type => DeadlineType.NajpozniejPrzed;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.PrzejeciePrzezOjca];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (c.Role != ParentRole.Ojciec || !c.IsEmployee || c.EventDate is not { } birth)
        {
            yield break;
        }

        var motherFacts = c.Facts.For(c.Person == PersonRef.Wnioskodawca ? PersonRef.DrugiRodzic : PersonRef.Wnioskodawca);
        if (motherFacts?.OsobaRola?.Value != ParentRole.Matka || motherFacts.PlanRezygnacjaZMacierzynskiegoPoTyg?.Value is not { } weeks)
        {
            yield break;
        }

        var start = birth.AddDays(weeks * 7);
        var days = c.Days(DeadlineParameterKeys.WniosekOjcaCzescMacierzynskiegoDni);
        yield return NotLaterThanBefore(
            Id,
            start.AddDays(-days),
            "u pracodawcy wniosek o część urlopu macierzyńskiego przejętą po matce",
            $" ({days} dni przed rozpoczęciem tej części {PolishDate.Words(start)})",
            null);
    }
}

public sealed class WniosekUzupelniajacyRule : DeadlineRule
{
    public const string RuleId = "wniosek_uzupelniajacy";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 180² § 4";
    public override DeadlineType Type => DeadlineType.NajpozniejPrzed;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.MacierzynskiUzupelniajacy];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (!c.IsEmployee || c.Benefit.Period is not { } period)
        {
            yield break;
        }

        var maternityEnd = period.From.AddDays(-1);
        var days = c.Days(DeadlineParameterKeys.WniosekUzupelniajacyDni);
        yield return NotLaterThanBefore(
            Id,
            maternityEnd.AddDays(-days),
            "u pracodawcy wniosek o uzupełniający urlop macierzyński",
            $" ({days} dni przed końcem urlopu macierzyńskiego {PolishDate.Words(maternityEnd)})",
            null);
    }
}

public sealed class WniosekRodzicielskiRule : DeadlineRule
{
    public const string RuleId = "wniosek_rodzicielski";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 182¹d § 1";
    public override DeadlineType Type => DeadlineType.NajpozniejPrzed;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.Rodzicielski];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (!c.IsEmployee)
        {
            yield break;
        }

        var starts = LeaveStarts(c.Benefit);
        if (starts.Count == 0)
        {
            yield break;
        }

        var days = c.Days(DeadlineParameterKeys.WniosekRodzicielskiDni);
        for (var index = 0; index < starts.Count; index++)
        {
            var part = starts.Count > 1 ? $"część {index + 1} urlopu rodzicielskiego" : "urlop rodzicielski";
            yield return NotLaterThanBefore(
                PartId(Id, index),
                starts[index].AddDays(-days),
                $"u pracodawcy wniosek o {part}",
                $" ({days} dni przed jego początkiem {PolishDate.Words(starts[index])})",
                null);
        }
    }
}

public sealed class WniosekPracaNaCzescEtatuRule : DeadlineRule
{
    public const string RuleId = "wniosek_praca_na_czesc_etatu";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 182¹e § 2";
    public override DeadlineType Type => DeadlineType.NajpozniejPrzed;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.Rodzicielski];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (!c.IsEmployee || c.Me?.PlanPracaNaCzescEtatu?.Value is not { Count: > 0 } work)
        {
            yield break;
        }

        var starts = work.Where(w => w.Od is not null).Select(w => w.Od!.Value).Order().ToList();
        var days = c.Days(DeadlineParameterKeys.WniosekPracaCzescEtatuDni);
        for (var index = 0; index < starts.Count; index++)
        {
            yield return NotLaterThanBefore(
                PartId(Id, index),
                starts[index].AddDays(-days),
                "u pracodawcy wniosek o łączenie urlopu rodzicielskiego z pracą",
                $" ({days} dni przed rozpoczęciem pracy {PolishDate.Words(starts[index])})",
                null);
        }
    }
}

public sealed class WniosekOjcowskiRule : DeadlineRule
{
    public const string RuleId = "wniosek_ojcowski";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 182³ § 2";
    public override DeadlineType Type => DeadlineType.NajpozniejPrzed;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.Ojcowski];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (!c.IsEmployee)
        {
            yield break;
        }

        var starts = LeaveStarts(c.Benefit);
        if (starts.Count == 0)
        {
            yield break;
        }

        var days = c.Days(DeadlineParameterKeys.WniosekOjcowskiDni);
        for (var index = 0; index < starts.Count; index++)
        {
            var part = starts.Count > 1 ? $"część {index + 1} urlopu ojcowskiego" : "urlop ojcowski";
            yield return NotLaterThanBefore(
                PartId(Id, index),
                starts[index].AddDays(-days),
                $"u pracodawcy wniosek o {part}",
                $" ({days} dni przed jego początkiem {PolishDate.Words(starts[index])})",
                null);
        }
    }
}

public sealed class WniosekUrlopPrzyjecieRule : DeadlineRule
{
    public const string RuleId = "wniosek_urlop_przyjecie";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 183 § 6";
    public override DeadlineType Type => DeadlineType.NajpozniejDo;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.PrzyjecieDziecka];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (!c.IsEmployee || c.Kind != EventKind.Przyjecie || c.EventDate is not { } taken)
        {
            yield break;
        }

        var days = c.Days(DeadlineParameterKeys.WniosekPrzyjecieDni);
        yield return NotLaterThan(
            Id,
            taken.AddDays(days),
            "u pracodawcy wniosek o urlop na warunkach urlopu macierzyńskiego",
            $" ({days} dni od przyjęcia dziecka)",
            null);
    }
}

public sealed class StartUrlopuPrzyjecieRule : DeadlineRule
{
    public const string RuleId = "start_urlopu_przyjecie";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "KP art. 183 § 6";
    public override DeadlineType Type => DeadlineType.NajpozniejDo;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.PrzyjecieDziecka];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (!c.IsEmployee || c.Kind != EventKind.Przyjecie || c.EventDate is not { } taken)
        {
            yield break;
        }

        var days = c.Days(DeadlineParameterKeys.StartUrlopuPrzyjecieDni);
        var date = taken.AddDays(days);
        yield return new DeadlineCandidate(
            Id,
            date,
            $"Urlop na warunkach urlopu macierzyńskiego musi się rozpocząć najpóźniej {PolishDate.Words(date)} ({days} dni od przyjęcia dziecka).",
            $"Termin rozpoczęcia urlopu na warunkach urlopu macierzyńskiego minął {PolishDate.Words(date)}.",
            null);
    }
}

public sealed class PrzedawnienieRoszczeniaRule(BenefitId benefit) : DeadlineRule
{
    public static readonly IReadOnlyList<BenefitId> Covered =
    [
        BenefitId.Macierzynski,
        BenefitId.MacierzynskiUzupelniajacy,
        BenefitId.Rodzicielski,
        BenefitId.Ojcowski,
        BenefitId.PrzyjecieDziecka,
    ];

    private const string Consequence = "roszczenie o wypłatę zasiłku przepada";

    public static string IdFor(BenefitId id) => $"przedawnienie_{BenefitNames.Of(id)}";

    public override string Id => IdFor(benefit);
    public override string PodstawaPrawna => "ustawa art. 67 ust. 1";
    public override DeadlineType Type => DeadlineType.Przedawnienie;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [benefit];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (c.Benefit.Period is not { } period)
        {
            yield break;
        }

        var date = period.To.AddMonths(c.Months(DeadlineParameterKeys.PrzedawnienieMies));
        yield return new DeadlineCandidate(
            Id,
            date,
            $"Zgłoś roszczenie o wypłatę zasiłku za okres do {PolishDate.Words(period.To)} najpóźniej {PolishDate.Words(date)} — potem się przedawnia.",
            $"Roszczenie o wypłatę zasiłku za okres do {PolishDate.Words(period.To)} przedawniło się {PolishDate.Words(date)}. Skutek: {Consequence}.",
            Consequence);
    }
}

public sealed class WniosekZasilekPogrzebowyRule : DeadlineRule
{
    public const string RuleId = "wniosek_zasilek_pogrzebowy";

    public override string Id => RuleId;
    public override string PodstawaPrawna => "Poradnik ZUS 2026 s. 35–36";
    public override DeadlineType Type => DeadlineType.NajpozniejDo;
    public override IReadOnlyList<BenefitId> Benefits { get; } = [BenefitId.Pogrzebowy];

    protected override IEnumerable<DeadlineCandidate> Compute(DeadlineContext c)
    {
        if (c.Facts.Sprawa.DzieckoDataPochowku?.Value is not { } burial)
        {
            yield break;
        }

        var months = c.Months(DeadlineParameterKeys.ZasilekPogrzebowyTerminMies);
        yield return NotLaterThan(
            Id,
            burial.AddMonths(months),
            "do ZUS wniosek Z-12 o zasiłek pogrzebowy z rachunkami",
            $" ({months} miesięcy od dnia pochówku {PolishDate.Words(burial)})",
            null);
    }
}

public interface IDeadlineService
{
    DeadlineOutcome For(DeadlineContext context);
}

public sealed class DeadlineService : IDeadlineService
{
    public static IReadOnlyList<DeadlineRule> Rules { get; } =
    [
        new WniosekMacierzynskiPrzedPorodemRule(),
        new WniosekUsrednionaRule(),
        new RezygnacjaMatkiRule(),
        new WniosekOjcaOCzescMacierzynskiegoRule(),
        new WniosekUzupelniajacyRule(),
        new WniosekRodzicielskiRule(),
        new WniosekPracaNaCzescEtatuRule(),
        new WniosekOjcowskiRule(),
        new WniosekUrlopPrzyjecieRule(),
        new StartUrlopuPrzyjecieRule(),
        .. PrzedawnienieRoszczeniaRule.Covered.Select(b => new PrzedawnienieRoszczeniaRule(b)),
        new WniosekZasilekPogrzebowyRule(),
    ];

    public DeadlineOutcome For(DeadlineContext context)
    {
        if (context.Benefit.Status != BenefitStatus.Przysluguje)
        {
            return DeadlineOutcome.Empty;
        }

        var deadlines = new List<Deadline>();
        var notes = new List<string>();
        foreach (var rule in Rules.Where(r => r.Benefits.Contains(context.Benefit.Id)))
        {
            var outcome = rule.Evaluate(context);
            deadlines.AddRange(outcome.Deadlines);
            notes.AddRange(outcome.Notes);
        }

        return new DeadlineOutcome(deadlines, notes);
    }
}
