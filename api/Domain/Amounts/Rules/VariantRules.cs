using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Domain.Amounts.AmountMath;

namespace BabyRadnik.Domain.Amounts;

internal enum VariantSource
{
    None,
    Own,
    Other,
}

internal sealed record VariantResolution(VariantSource Source, IReadOnlyList<TraceStep> Trace, IReadOnlyList<string> Notes);

internal sealed class WniosekUsrednionyRule : IAmountRule
{
    public string Id => "wniosek_usredniony";

    public string PodstawaPrawna => "ustawa art. 30a ust. 1–2; art. 31 ust. 3";

    private sealed record Check(bool Declared, bool Effective, TraceStep? Step, string? Note);

    public VariantResolution Resolve(AmountsContext ctx, PersonRef person)
    {
        var own = Evaluate(ctx, person, isOwn: true);
        var trace = new List<TraceStep>();
        var notes = new List<string>();
        if (own.Step is not null)
        {
            trace.Add(own.Step);
        }

        if (own.Note is not null)
        {
            notes.Add(own.Note);
        }

        if (own.Effective)
        {
            return new VariantResolution(VariantSource.Own, trace, notes);
        }

        var other = Evaluate(ctx, AmountsContext.Other(person), isOwn: false);
        if (other.Step is not null)
        {
            trace.Add(other.Step);
        }

        if (other.Note is not null)
        {
            notes.Add(other.Note);
        }

        return new VariantResolution(other.Effective ? VariantSource.Other : VariantSource.None, trace, notes);
    }

    private Check Evaluate(AmountsContext ctx, PersonRef person, bool isOwn)
    {
        var facts = ctx.FactsOf(person);
        if (facts is null || facts.PlanWariantWyplaty?.Value != PaymentVariant.Usredniony)
        {
            return new Check(false, false, null, null);
        }

        var who = isOwn ? "Wniosek" : "Wniosek drugiego rodzica";
        if (ctx.EventDate is not { } eventDate)
        {
            return new Check(true, false,
                AmountTrace.Of(this, null, $"{who} o wypłatę w wariancie uśrednionym: brak daty porodu / przyjęcia dziecka, więc nie ustalam terminu 21 dni. Nie rozstrzygam wariantu."),
                "Wariant uśredniony: bez daty porodu (przyjęcia) nie ustalam terminu wniosku — liczę stawki standardowe.");
        }

        if (ctx.IsBirth && facts.OsobaRola?.Value != ParentRole.Matka)
        {
            return new Check(true, false,
                AmountTrace.Of(this, false, $"{who} o wypłatę w wariancie uśrednionym przy urodzeniu dziecka może złożyć tylko matka dziecka (art. 30a ust. 1) — stawka 81,5% nie dotyczy tej osoby.",
                    "wniosek składa matka dziecka (przy urodzeniu)"),
                isOwn ? "Wniosek o wariant uśredniony przy urodzeniu dziecka składa matka (art. 30a ust. 1) — dla ojca liczę stawki standardowe." : null);
        }

        var days = ctx.Int(AmountsParameterKeys.TerminWnioskuUsredniona, eventDate);
        var deadline = eventDate.AddDays(days);
        var condition = $"wniosek najpóźniej {days} dni po {(ctx.IsAdoption ? "przyjęciu dziecka" : "porodzie")}";
        if (facts.PlanDataWnioskuUsrednionego?.Value is { } filed)
        {
            if (filed <= deadline)
            {
                return new Check(true, true,
                    AmountTrace.Of(this, true, $"{who} z {Date(filed)} złożony w terminie (najpóźniej {Date(deadline)}): zasiłek za macierzyński i rodzicielski w pełnym wymiarze po 81,5% podstawy.", condition),
                    null);
            }

            return new Check(true, false,
                AmountTrace.Of(this, false, $"{who} z {Date(filed)} złożony po terminie (najpóźniej {Date(deadline)}): wariant uśredniony nie przysługuje — macierzyński 100%, rodzicielski 70%.", condition),
                $"Wniosek o wariant uśredniony z {Date(filed)} jest po terminie (do {Date(deadline)}) — liczę stawki standardowe: macierzyński 100%, rodzicielski 70%.");
        }

        if (ctx.AsOf <= deadline)
        {
            return new Check(true, true,
                AmountTrace.Of(this, null, $"{who} o wariant uśredniony: brak daty złożenia; zakładam złożenie w terminie (najpóźniej {Date(deadline)}).", condition),
                $"Stawka 81,5% obowiązuje pod warunkiem złożenia wniosku najpóźniej {Date(deadline)} ({days} dni po {(ctx.IsAdoption ? "przyjęciu dziecka" : "porodzie")}).");
        }

        return new Check(true, false,
            AmountTrace.Of(this, false, $"{who} o wariant uśredniony: termin ({Date(deadline)}) minął, a nie ma informacji o wniosku złożonym w terminie — wariant nie przysługuje.", condition),
            $"Termin na wniosek o wariant uśredniony minął {Date(deadline)}, a brak daty wniosku złożonego w terminie — liczę stawki standardowe.");
    }
}

internal sealed class DziewiecTygodniDrugiegoRodzicaRule : IAmountRule
{
    public string Id => "dziewiec_tygodni_drugiego_rodzica";

    public string PodstawaPrawna => "ustawa art. 31 ust. 3f; KP art. 182¹a § 4";

    public int Days(AmountsContext ctx) => ctx.Int(AmountsParameterKeys.RodzicielskiNieprzenoszalneTyg, ctx.ParamDate) * DaysPerWeek;

    public string Label => "9 tygodni urlopu rodzicielskiego przysługujących drugiemu rodzicowi (art. 31 ust. 3f)";
}

internal enum FirstYearUsage
{
    Used,
    NotUsed,
    Unknown,
}

internal sealed record FirstYearAssessment(FirstYearUsage Usage, TraceStep Step);

internal sealed class RodzicielskiBezWykorzystaniaRule : IAmountRule
{
    public string Id => "rodzicielski_niewykorzystany_w_pierwszym_roku";

    public string PodstawaPrawna => "ustawa art. 31 ust. 3g i 4";

    public FirstYearAssessment Assess(AmountsContext ctx)
    {
        const string condition = "ani jeden dzień zasiłku za urlop rodzicielski w pierwszym roku życia dziecka";
        if (ctx.ChildBirth is not { } birth)
        {
            return new FirstYearAssessment(FirstYearUsage.Unknown,
                AmountTrace.Of(this, null, "Brak daty urodzenia dziecka — nie rozstrzygam, czy zasiłek rodzicielski wypada w pierwszym roku życia.", condition));
        }

        var yearEnd = birth.AddMonths(ctx.Int(AmountsParameterKeys.PierwszyRokZyciaMiesiecy, ctx.ParamDate)).AddDays(-1);
        if (ctx.EventDate is { } taken && ctx.IsAdoption && taken > yearEnd)
        {
            return new FirstYearAssessment(FirstYearUsage.Unknown,
                AmountTrace.Of(this, null, "Dziecko ma już więcej niż rok w dniu przyjęcia — przepis o „pierwszym roku życia” (art. 31 ust. 3g i 4) nie rozstrzyga takiej sytuacji; nie rozstrzygam.", condition));
        }

        var starts = new List<DateOnly?>();
        foreach (var card in ctx.Cards.Where(c => c.Id == BenefitId.Rodzicielski && c.Status == BenefitStatus.Przysluguje))
        {
            if (card.Parts.Count > 0)
            {
                starts.AddRange(card.Parts.Select(p => (DateOnly?)p.Period.From));
            }
            else
            {
                starts.Add(card.Period?.From);
            }
        }

        foreach (var person in new[] { PersonRef.Wnioskodawca, PersonRef.DrugiRodzic })
        {
            var facts = ctx.FactsOf(person);
            starts.AddRange((facts?.PlanRodzicielskiCzesci?.Value ?? []).Select(p => p.Od));
            starts.AddRange((facts?.UbezpieczeniePobraneSwiadczenia?.Value ?? [])
                .Where(b => b.Rodzaj == ReceivedBenefitKind.Rodzicielski).Select(b => (DateOnly?)b.Od));
        }

        if (starts.Any(s => s is { } d && d <= yearEnd))
        {
            return new FirstYearAssessment(FirstYearUsage.Used,
                AmountTrace.Of(this, false, $"Urlop rodzicielski zaczyna się najpóźniej {Date(yearEnd)} (koniec pierwszego roku życia dziecka) — warunek z art. 31 ust. 3g nie jest spełniony.", condition));
        }

        if (starts.Count == 0 || starts.Any(s => s is null))
        {
            return new FirstYearAssessment(FirstYearUsage.Unknown,
                AmountTrace.Of(this, null, "Nie ma pełnych dat urlopu rodzicielskiego — nie rozstrzygam, czy choć jeden dzień zasiłku wypadnie w pierwszym roku życia dziecka.", condition));
        }

        return new FirstYearAssessment(FirstYearUsage.NotUsed,
            AmountTrace.Of(this, true, $"Żaden dzień zasiłku za urlop rodzicielski nie wypada do {Date(yearEnd)} (koniec pierwszego roku życia dziecka): zasiłek rodzicielski 70%, a zasiłek za macierzyński pobrany po 81,5% wyrównuje się do 100% na wniosek.", condition));
    }
}
