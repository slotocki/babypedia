using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Domain.Amounts.AmountMath;

namespace BabyRadnik.Domain.Amounts;

/// <summary>Podstawa wymiaru zasiłku (miesięczna, po odliczeniach) i jej pochodzenie. Wyznacza ją wywołujący (moduł podstawy) albo fakt <c>podstawa.ustalonaPrzezPlatnika</c>.</summary>
public sealed record BasisInput(decimal Amount, BasisSource Source);

public sealed record AmountsRequest(
    FactSet Facts,
    DateOnly AsOf,
    IReadOnlyList<BenefitResult> Benefits,
    IReadOnlyDictionary<PersonRef, BasisInput>? Bases = null);

public sealed record AmountsResult(IReadOnlyList<BenefitResult> Benefits);

public interface IAmountsService
{
    /// <summary>Uzupełnia karty (status „przysługuje”) o stawki, części okresu i kwoty. Pozostałe karty zwraca bez zmian.</summary>
    AmountsResult Calculate(AmountsRequest request);
}

public sealed class AmountsService(IParameterProvider parameters) : IAmountsService
{
    public AmountsResult Calculate(AmountsRequest request)
    {
        var processor = new CardProcessor(new AmountsContext(request, parameters));
        return new AmountsResult(request.Benefits.Select(processor.Process).ToList());
    }
}

public static class AmountRules
{
    public static IReadOnlyList<IAmountRule> All { get; } =
    [
        RateRules.Macierzynska,
        RateRules.Uzupelniajacego,
        RateRules.Ojcowska,
        RateRules.Rodzicielska,
        RateRules.ChorobowegoWCiazy,
        RateRules.Rehabilitacyjna,
        new WniosekUsrednionyRule(),
        new DziewiecTygodniDrugiegoRodzicaRule(),
        new RodzicielskiBezWykorzystaniaRule(),
        new PracaNaCzescEtatuRule(),
        new StawkaDziennaRule(),
        new KwotaZaOkresRule(),
        new BruttoRule(),
        new SwiadczenieRodzicielskieRule(),
        new ZasilekPogrzebowyRule(),
        new BrakPodstawyRule(),
        new OdliczenieSkladekRule(),
        new MinimalnaPodstawaRule(),
        new LimitPodstawyRule(),
    ];
}

internal sealed class BrakPodstawyRule : IAmountRule
{
    public string Id => "brak_podstawy";

    public string PodstawaPrawna => "instrukcja 8.6; ustawa art. 36 ust. 1, 3; art. 47";
}

internal sealed class StawkaDziennaRule : IAmountRule
{
    public string Id => "stawka_dzienna";

    public string PodstawaPrawna => "ustawa art. 36 ust. 3 w zw. z art. 47; instrukcja 8.6 (zaokrąglanie: parametr „zaokraglanie”, do weryfikacji)";
}

internal sealed class KwotaZaOkresRule : IAmountRule
{
    public string Id => "kwota_za_okres";

    public string PodstawaPrawna => "ustawa art. 31 ust. 1–4a; instrukcja 8.6";
}

internal sealed class CardProcessor(AmountsContext ctx)
{
    private static readonly StawkaDziennaRule StawkaDzienna = new();
    private static readonly KwotaZaOkresRule KwotaZaOkres = new();
    private static readonly BrakPodstawyRule BrakPodstawy = new();
    private static readonly PracaNaCzescEtatuRule Praca = new();
    private static readonly BruttoRule Brutto = new();
    private static readonly SwiadczenieRodzicielskieRule SwiadczenieRodzicielskie = new();
    private static readonly ZasilekPogrzebowyRule Pogrzebowy = new();

    public BenefitResult Process(BenefitResult card)
    {
        if (card.Status != BenefitStatus.Przysluguje)
        {
            return card;
        }

        try
        {
            return card.Id == BenefitId.Pogrzebowy ? Pogrzebowy.Apply(ctx, card) : Calculate(card);
        }
        catch (ParameterNotFoundException e)
        {
            return Undecided(card, $"Brakuje parametru prawnego „{e.Key}” na dzień {Date(e.Date)}. Nie rozstrzygam stawki ani kwoty — skontaktuj się z ZUS.");
        }
        catch (AmountsUndecidedException e)
        {
            return Undecided(card, e.Message);
        }
    }

    private static BenefitResult Undecided(BenefitResult card, string text) => card with
    {
        Notes = [.. card.Notes, text],
        Trace = [.. card.Trace, AmountTrace.Of(KwotaZaOkres, null, text)],
    };

    private static IReadOnlyList<Block> BlocksOf(BenefitResult card)
    {
        if (card.Parts.Count > 0)
        {
            return card.Parts.OrderBy(p => p.Period.From).Select(p => new Block(p.Label, p.Period.From, p.Period.To)).ToList();
        }

        return card.Period is { } period ? [new Block(null, period.From, period.From.AddDays(period.Days - 1))] : [];
    }

    private BenefitResult Calculate(BenefitResult card)
    {
        var rule = RateRules.For(card.Id);
        if (rule is null)
        {
            return card;
        }

        var blocks = BlocksOf(card);
        var synthetic = blocks.Count == 0;
        if (synthetic)
        {
            var day = ctx.EventDate ?? ctx.AsOf;
            blocks = [new Block(null, day, day)];
        }

        // Bez własnego planu karta pokazuje łączny wymiar urlopu rodzicielskiego obojga rodziców. Kwotę liczę tylko
        // za część, z której ta osoba może skorzystać sama — tygodnie zastrzeżone dla drugiego rodzica nie są jej.
        string? reservedNote = null;
        if (!synthetic && card.Id == BenefitId.Rodzicielski && card.Parts.Count == 0 && blocks.Count == 1
            && ctx.OwnParentalMaxDays(card.Person) is { } ownDays && ownDays > 0 && blocks[0].Days > ownDays)
        {
            var whole = blocks[0];
            var own = whole with { To = whole.From.AddDays(ownDays - 1) };
            blocks = [own];
            reservedNote = $"Kwota obejmuje {Date(own.From)}–{Date(own.To)} ({ownDays} dni) — część urlopu rodzicielskiego, z której możesz skorzystać sam(a). " +
                $"Pozostałe {whole.Days - ownDays} dni ({Date(own.To.AddDays(1))}–{Date(whole.To)}) przysługują wyłącznie drugiemu rodzicowi (KP art. 182¹a § 4) i nie wliczam ich do Twojej kwoty.";
        }

        var plan = rule.Plan(ctx, card, blocks);
        var notes = card.Notes.Concat(plan.Notes).ToList();
        var trace = card.Trace.Concat(plan.Trace).ToList();
        if (reservedNote is not null)
        {
            notes.Add(reservedNote);
            trace.Add(AmountTrace.Of(KwotaZaOkres, true, reservedNote, "kwota tylko za część urlopu rodzicielskiego przysługującą tej osobie"));
        }
        if (plan.Undecided is { } why)
        {
            notes.Add(why);
            trace.Add(AmountTrace.Of(KwotaZaOkres, null, why));
            return card with { Notes = notes, Trace = trace };
        }

        var work = card.Id == BenefitId.Rodzicielski && !synthetic
            ? Praca.Resolve(ctx, card.Person, blocks)
            : new WorkResult([], [], []);
        notes.AddRange(work.Notes);
        trace.AddRange(work.Trace);

        var pieces = BuildPieces(plan.Spans, work.Ranges);
        var dominant = pieces.GroupBy(p => p.Rate)
            .Select(g => (Rate: g.Key, Days: g.Sum(p => p.Days), First: g.Min(p => p.From)))
            .OrderByDescending(g => g.Days).ThenBy(g => g.First).First();
        if (pieces.Select(p => p.Rate).Distinct().Count() > 1)
        {
            notes.Add("Stawka zmienia się w okresie: " + string.Join(", ",
                pieces.GroupBy(p => p.Rate).Select(g => $"{Percent(g.Key)} przez {g.Sum(p => p.Days)} dni")) + " — szczegóły w częściach okresu.");
        }

        if (synthetic)
        {
            notes.Add($"Okres zasiłku nie jest jeszcze ustalony — stawka {Percent(dominant.Rate)} dotyczy dnia {Date(blocks[0].From)}; kwoty nie liczę.");
            return card with { Rate = dominant.Rate, Notes = notes, Trace = trace };
        }

        var parts = card.Parts.Count > 0 || pieces.Count > 1 ? PartsOf(card, pieces) : card.Parts;
        var result = card with { Rate = dominant.Rate, Parts = parts, Notes = notes, Trace = trace };

        if (ctx.BasisFor(card.Person) is not { } basis)
        {
            notes.Add("Brak podstawy wymiaru: podaj podstawę, żeby zobaczyć kwotę.");
            trace.Add(AmountTrace.Of(BrakPodstawy, null, "Nie ma podstawy wymiaru zasiłku (ani ustalonej przez płatnika, ani szacunku) — pokazuję okresy i stawki procentowe bez kwoty.", "podstawa wymiaru znana"));
            return result with { Notes = notes, Trace = trace };
        }

        try
        {
            var amounts = new AmountCalculator(ctx, pieces[0].From, basis);
            var priced = pieces.Select(p => (Piece: p, Amount: amounts.For(p))).ToList();
            var dominantPiece = priced
                .OrderByDescending(x => pieces.Where(p => p.Rate == x.Piece.Rate && p.Factor == x.Piece.Factor).Sum(p => p.Days))
                .ThenBy(x => x.Piece.From).First();
            var total = priced.Sum(x => x.Amount.Total);
            var cardAmount = new Amount(dominantPiece.Amount.Daily, total, basis.Amount, basis.Source);

            if (parts.Count > 0)
            {
                var byPiece = priced.ToDictionary(x => x.Piece, x => x.Amount);
                parts = parts.Select((part, index) => part with { Amount = byPiece[pieces[index]] }).ToList();
            }

            trace.Add(AmountTrace.Of(StawkaDzienna, true, DailyText(amounts, basis, pieces[0]),
                "stawka dzienna = podstawa ÷ 30, zaokrąglona wg ustawienia"));
            trace.Add(AmountTrace.Of(KwotaZaOkres, true, TotalText(priced, total),
                "zasiłek za okres = stawka dzienna × stawka procentowa × dni kalendarzowe"));

            foreach (var rate in pieces.Where(p => p.Factor == 1).Select(p => p.Rate).Distinct().Take(3))
            {
                notes.Add($"Zasiłek miesięczny przy stawce {Percent(rate)}: {Money(Monthly(basis.Amount, rate))} (podstawa × stawka).");
            }

            var brutto = Brutto.Describe(ctx, pieces[0].From);
            notes.Add(brutto.Note);
            trace.Add(brutto.Step);

            if (card.Id is BenefitId.Macierzynski or BenefitId.MacierzynskiUzupelniajacy or BenefitId.Rodzicielski or BenefitId.Ojcowski
                or BenefitId.PrzyjecieDziecka or BenefitId.PrzejeciePrzezOjca)
            {
                var info = SwiadczenieRodzicielskie.Describe(ctx, pieces[0].From);
                notes.Add(info.Note);
                trace.Add(info.Step);
            }

            if (plan.EqualizeToFull)
            {
                var full = priced.Sum(x => amounts.For(x.Piece with { Rate = ctx.Value(AmountsParameterKeys.StawkaMacierzynski, x.Piece.From) }).Total);
                notes.Add($"Wyrównanie do 100% (art. 31 ust. 4, na wniosek) zwiększyłoby zasiłek za macierzyński o {Money(full - total)}.");
            }

            return result with { Amount = cardAmount, Parts = parts, Notes = notes, Trace = trace };
        }
        catch (Exception e) when (e is ParameterNotFoundException or AmountsUndecidedException)
        {
            var text = e is ParameterNotFoundException missing
                ? $"Brakuje parametru prawnego „{missing.Key}” na dzień {Date(missing.Date)}. Nie rozstrzygam kwoty — skontaktuj się z ZUS."
                : e.Message;
            notes.Add(text);
            trace.Add(AmountTrace.Of(KwotaZaOkres, null, text));
            return result with { Notes = notes, Trace = trace };
        }
    }

    private static string DailyText(AmountCalculator amounts, BasisInput basis, Piece piece) =>
        $"Stawka dzienna = podstawa × stopa ÷ {amounts.Divisor}: {Money(basis.Amount)} × {Percent(piece.Rate)} ÷ {amounts.Divisor} = {Money(amounts.DailyAt(basis.Amount, piece.Rate))} (zaokrąglenie do {amounts.Places} miejsc po przecinku, ustawienie do weryfikacji).";

    private static string TotalText(IReadOnlyList<(Piece Piece, Amount Amount)> priced, decimal total) =>
        string.Join("; ", priced.Select(x =>
            $"{Date(x.Piece.From)}–{Date(x.Piece.To)}: {Money(x.Amount.Daily)} dziennie ({Percent(x.Piece.Rate)}{(x.Piece.Factor == 1 ? "" : $", × {Percent(x.Piece.Factor)} za pracę na część etatu")}) × {x.Piece.Days} dni = {Money(x.Amount.Total)}"))
        + $". Razem {Money(total)}.";

    private IReadOnlyList<Piece> BuildPieces(IReadOnlyList<RateSpan> spans, IReadOnlyList<WorkRange> work)
    {
        var pieces = new List<Piece>();
        foreach (var span in spans.OrderBy(s => s.From))
        {
            foreach (var segment in ctx.Timeline(span.Key, span.From, span.To))
            {
                var rate = segment.Parameter.Value ?? throw new ParameterNotFoundException(span.Key, segment.From);
                var cuts = new SortedSet<DateOnly> { segment.From };
                foreach (var range in work)
                {
                    foreach (var cut in new[] { range.From, range.To.AddDays(1) })
                    {
                        if (cut > segment.From && cut <= segment.To)
                        {
                            cuts.Add(cut);
                        }
                    }
                }

                var starts = cuts.ToList();
                for (var i = 0; i < starts.Count; i++)
                {
                    var from = starts[i];
                    var to = i + 1 < starts.Count ? starts[i + 1].AddDays(-1) : segment.To;
                    var covering = work.FirstOrDefault(r => r.From <= from && from <= r.To);
                    var piece = new Piece(span.Block, from, to, rate, covering is null ? 1m : 1m - covering.Wymiar, span.Label,
                        covering is null ? null : $"praca na {Number(covering.Wymiar)} etatu");
                    if (pieces.Count > 0 && Mergeable(pieces[^1], piece))
                    {
                        pieces[^1] = pieces[^1] with { To = piece.To };
                    }
                    else
                    {
                        pieces.Add(piece);
                    }
                }
            }
        }

        return pieces;
    }

    private static bool Mergeable(Piece previous, Piece next) =>
        previous.Block == next.Block && previous.Rate == next.Rate && previous.Factor == next.Factor
        && previous.SpanLabel == next.SpanLabel && previous.WorkLabel == next.WorkLabel && previous.To.AddDays(1) == next.From;

    private static IReadOnlyList<BenefitPart> PartsOf(BenefitResult card, IReadOnlyList<Piece> pieces)
    {
        var parts = new List<BenefitPart>();
        foreach (var group in pieces.GroupBy(p => p.Block))
        {
            var inBlock = group.ToList();
            var baseLabel = group.Key.Label ?? DefaultLabel(card.Id);
            foreach (var piece in inBlock)
            {
                var label = baseLabel;
                if (inBlock.Count > 1)
                {
                    var extras = new List<string> { $"{Date(piece.From)}–{Date(piece.To)}" };
                    if (piece.SpanLabel is not null)
                    {
                        extras.Add(piece.SpanLabel);
                    }

                    if (piece.WorkLabel is not null)
                    {
                        extras.Add(piece.WorkLabel);
                    }

                    label = $"{baseLabel}: {string.Join(", ", extras)}";
                }

                parts.Add(new BenefitPart(label, Span(piece.From, piece.To), piece.Rate));
            }
        }

        return parts;
    }

    private static string DefaultLabel(BenefitId id) => id switch
    {
        BenefitId.Macierzynski => "Urlop macierzyński",
        BenefitId.MacierzynskiUzupelniajacy => "Uzupełniający urlop macierzyński",
        BenefitId.Rodzicielski => "Urlop rodzicielski",
        BenefitId.Ojcowski => "Urlop ojcowski",
        BenefitId.PrzyjecieDziecka => "Urlop na warunkach urlopu macierzyńskiego",
        BenefitId.PrzejeciePrzezOjca => "Przejęty zasiłek macierzyński",
        BenefitId.ZasilekDoDniaPorodu => "Zasiłek do dnia porodu",
        BenefitId.ChorobowyWCiazy => "Zasiłek chorobowy w ciąży",
        BenefitId.Rehabilitacyjne => "Świadczenie rehabilitacyjne",
        _ => "Okres zasiłku",
    };
}

internal sealed class AmountCalculator
{
    private readonly string mode;
    private readonly AmountsContext ctx;

    public AmountCalculator(AmountsContext context, DateOnly day, BasisInput basis)
    {
        ctx = context;
        Places = ctx.Int(AmountsParameterKeys.ZaokraglanieMiejsca, day);
        Divisor = ctx.Int(AmountsParameterKeys.PodstawaDzielnikDzienny, day);
        mode = ctx.Get(AmountsParameterKeys.ZaokraglanieKwotaZaOkres, day).Text
            ?? throw new AmountsUndecidedException("Ustawienie zaokrąglania kwoty za okres jest puste — nie rozstrzygam kwoty.");
        if (mode is not (AmountsParameterKeys.KwotaZaOkresStawkaDziennaXDni or AmountsParameterKeys.KwotaZaOkresIloczynNaKoncu))
        {
            throw new AmountsUndecidedException($"Nieznane ustawienie zaokrąglania kwoty za okres („{mode}”) — nie rozstrzygam kwoty.");
        }

        Basis = basis;
    }

    public int Places { get; }

    public int Divisor { get; }

    public BasisInput Basis { get; }

    public decimal DailyAt(decimal basis, decimal rate) => Round(basis * rate / Divisor, Places);

    public Amount For(Piece piece)
    {
        if (mode == AmountsParameterKeys.KwotaZaOkresIloczynNaKoncu)
        {
            var exactDaily = Basis.Amount / Divisor * piece.Rate * piece.Factor;
            return new Amount(Round(exactDaily, Places), Round(exactDaily * piece.Days, 2), Basis.Amount, Basis.Source);
        }

        // Jak ZUS: stawka dzienna = podstawa × stopa ÷ 30, zaokrąglana raz (a nie stopa × zaokrąglona podstawa ÷ 30).
        var daily = Round(DailyAt(Basis.Amount, piece.Rate) * piece.Factor, Places);
        return new Amount(daily, Round(daily * piece.Days, 2), Basis.Amount, Basis.Source);
    }
}
