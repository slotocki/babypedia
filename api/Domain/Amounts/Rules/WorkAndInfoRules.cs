using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Domain.Amounts.AmountMath;

namespace BabyRadnik.Domain.Amounts;

internal sealed record WorkResult(IReadOnlyList<WorkRange> Ranges, IReadOnlyList<string> Notes, IReadOnlyList<TraceStep> Trace);

internal sealed class PracaNaCzescEtatuRule : IAmountRule
{
    public string Id => "praca_na_czesc_etatu";

    public string PodstawaPrawna => "ustawa art. 31 ust. 4a; KP art. 182¹e § 1";

    public WorkResult Resolve(AmountsContext ctx, PersonRef person, IReadOnlyList<Block> blocks)
    {
        var entries = ctx.FactsOf(person)?.PlanPracaNaCzescEtatu?.Value ?? [];
        var ranges = new List<WorkRange>();
        var notes = new List<string>();
        var trace = new List<TraceStep>();
        if (entries.Count == 0)
        {
            return new WorkResult(ranges, notes, trace);
        }

        var maxShare = ctx.Value(AmountsParameterKeys.PracaNaRodzicielskimMaxEtat, ctx.ParamDate);
        var end = blocks[^1].To;
        foreach (var entry in entries)
        {
            if (entry.Wymiar <= 0 || entry.Wymiar > maxShare || entry.Tygodnie <= 0)
            {
                notes.Add($"Pominięto wpis o pracy na część etatu (wymiar {Number(entry.Wymiar)}, {entry.Tygodnie} tyg.): łączyć można pracę w wymiarze najwyżej {Number(maxShare)} etatu — kwota bez pomniejszenia.");
                trace.Add(AmountTrace.Of(this, false,
                    $"Praca w wymiarze {Number(entry.Wymiar)} etatu przez {entry.Tygodnie} tyg. nie spełnia warunków (najwyżej {Number(maxShare)} etatu) — nie pomniejszam zasiłku.",
                    $"praca w wymiarze nie wyższym niż {Number(maxShare)} etatu"));
                continue;
            }

            var days = entry.Tygodnie * DaysPerWeek;
            DateOnly from;
            if (entry.Od is { } start)
            {
                from = start;
            }
            else
            {
                from = end.AddDays(-days + 1);
                end = from.AddDays(-1);
                notes.Add("Nie podano daty początku pracy na część etatu — przyjmuję, że przypada na końcowe dni urlopu rodzicielskiego (kwota nie zależy od położenia, o ile stawka się nie zmienia).");
            }

            var range = new WorkRange(from, from.AddDays(days - 1), entry.Wymiar);
            ranges.Add(range);
            trace.Add(AmountTrace.Of(this, true,
                $"Praca na {Number(entry.Wymiar)} etatu przez {entry.Tygodnie} tyg. ({Date(range.From)}–{Date(range.To)}): zasiłek za te dni × (1 − {Number(entry.Wymiar)}) = {Percent(1 - entry.Wymiar)} zasiłku.",
                "zasiłek pomniejszony proporcjonalnie do wymiaru czasu pracy łączonej z urlopem rodzicielskim"));
        }

        return new WorkResult(ranges, notes.Distinct().ToList(), trace);
    }
}

internal sealed class BruttoRule : IAmountRule
{
    public string Id => "kwoty_brutto";

    public string PodstawaPrawna => "instrukcja 8.1 (parametr „kwoty”); Poradnik ZUS s. 19, 24–25 (rozbieżność C3 w ROZBIEZNOSCI.md)";

    public (string Note, TraceStep Step) Describe(AmountsContext ctx, DateOnly day)
    {
        var text = ctx.Get(AmountsParameterKeys.Kwoty, day).Text;
        return text switch
        {
            "brutto" => ("Kwoty brutto — przed pomniejszeniem o zaliczkę na podatek dochodowy.",
                AmountTrace.Of(this, true, "Kwoty podaję brutto, przed zaliczką na podatek dochodowy (ustawienie „kwoty” = brutto; do weryfikacji).")),
            "netto" => ("Silnik nie odlicza zaliczki na podatek dochodowy — pokazane kwoty są brutto (ustawienie „netto” nie jest obsługiwane).",
                AmountTrace.Of(this, null, "Ustawienie „kwoty” = netto nie jest obsługiwane: silnik nie odlicza zaliczki na podatek, więc kwoty są brutto — nie rozstrzygam wartości netto.")),
            _ => ("Nie rozstrzygam, czy kwoty są brutto czy netto — skontaktuj się z ZUS.",
                AmountTrace.Of(this, null, "Ustawienie „kwoty” nie jest rozpoznane — nie rozstrzygam, czy kwoty są brutto czy netto.")),
        };
    }
}

internal sealed class SwiadczenieRodzicielskieRule : IAmountRule
{
    public string Id => "podwyzszenie_do_swiadczenia_rodzicielskiego";

    public string PodstawaPrawna => "ustawa art. 31 ust. 3a–3c (kwota: ustawa o świadczeniach rodzinnych art. 17c — pobrane, do potwierdzenia)";

    public (string Note, TraceStep Step) Describe(AmountsContext ctx, DateOnly day)
    {
        var configured = ctx.Get(AmountsParameterKeys.KwotaSwiadczeniaRodzicielskiego, day).Value;
        if (configured is null)
        {
            const string note = "Nie stosuję podwyższenia z art. 31 ust. 3a: jeżeli miesięczna kwota zasiłku jest niższa niż kwota świadczenia rodzicielskiego, zasiłek podwyższa się do tej kwoty (za część miesiąca proporcjonalnie — ust. 3c). Kwoty świadczenia rodzicielskiego nie ma w parametrach — sprawdź w ZUS.";
            return (note, AmountTrace.Of(this, null, "Parametr „kwota_swiadczenia_rodzicielskiego” jest pusty — nie podwyższam zasiłku do kwoty świadczenia rodzicielskiego; pokazuję tylko informację."));
        }

        var text = $"Kwota świadczenia rodzicielskiego w parametrach: {Money(configured.Value)}. Podwyższenia z art. 31 ust. 3a nie wyliczam automatycznie — jeśli miesięczna kwota zasiłku jest niższa, zasiłek podwyższa się do tej kwoty (sprawdź w ZUS).";
        return (text, AmountTrace.Of(this, null, text));
    }
}

internal sealed class ZasilekPogrzebowyRule : IAmountRule
{
    public string Id => "zasilek_pogrzebowy";

    public string PodstawaPrawna => "Poradnik ZUS s. 35–36 (w wyciągu ustawy brak przepisu — rozbieżność B w ROZBIEZNOSCI.md)";

    public BenefitResult Apply(AmountsContext ctx, BenefitResult card)
    {
        var day = ctx.Case.DzieckoDataPochowku?.Value ?? ctx.EventDate ?? ctx.AsOf;
        var amount = ctx.Value(AmountsParameterKeys.ZasilekPogrzebowy, day);
        var brutto = new BruttoRule().Describe(ctx, day);
        return card with
        {
            Notes = [.. card.Notes, $"Zasiłek pogrzebowy: {Money(amount)} (kwota ryczałtowa, nie zależy od podstawy wymiaru ani od kosztów pogrzebu).", brutto.Note],
            Trace = [.. card.Trace,
                AmountTrace.Of(this, true, $"Zasiłek pogrzebowy w kwocie {Money(amount)} (parametr „{AmountsParameterKeys.ZasilekPogrzebowy}” na dzień {Date(day)}).")],
        };
    }
}
