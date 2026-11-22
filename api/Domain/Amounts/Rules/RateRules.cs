using BabyRadnik.Domain.Contracts;
using static BabyRadnik.Domain.Amounts.AmountMath;

namespace BabyRadnik.Domain.Amounts;

internal abstract class RateRule : IAmountRule
{
    public abstract string Id { get; }

    public abstract string PodstawaPrawna { get; }

    public abstract RatePlan Plan(AmountsContext ctx, BenefitResult card, IReadOnlyList<Block> blocks);

    protected TraceStep Rate(AmountsContext ctx, string key, DateOnly day, string what)
    {
        var rate = ctx.Value(key, day);
        return AmountTrace.Of(this, true, $"{what}: {Percent(rate)} podstawy wymiaru zasiłku (parametr „{key}”, wg dnia, za który przysługuje zasiłek).");
    }
}

internal sealed class StawkaMacierzynskaRule : RateRule
{
    private static readonly WniosekUsrednionyRule Wniosek = new();
    private static readonly RodzicielskiBezWykorzystaniaRule BezWykorzystania = new();

    public override string Id => "stawka_macierzynski";

    public override string PodstawaPrawna => "ustawa art. 31 ust. 1 i 3; art. 30 ust. 3";

    public override RatePlan Plan(AmountsContext ctx, BenefitResult card, IReadOnlyList<Block> blocks)
    {
        var plan = new RatePlan();
        var start = blocks[0].From;
        var what = card.Id switch
        {
            BenefitId.ZasilekDoDniaPorodu => "Zasiłek w wysokości zasiłku macierzyńskiego do dnia porodu",
            BenefitId.PrzyjecieDziecka => "Zasiłek za okres urlopu na warunkach urlopu macierzyńskiego",
            BenefitId.PrzejeciePrzezOjca => "Zasiłek za okres urlopu macierzyńskiego przejęty przez ojca",
            _ => "Zasiłek za okres urlopu macierzyńskiego",
        };

        var usesVariant = card.Id is BenefitId.Macierzynski or BenefitId.PrzyjecieDziecka or BenefitId.PrzejeciePrzezOjca;
        var variant = usesVariant ? Wniosek.Resolve(ctx, card.Person) : null;
        if (variant is not null)
        {
            plan.Trace.AddRange(variant.Trace);
            plan.Notes.AddRange(variant.Notes);
        }

        if (card.Id == BenefitId.PrzejeciePrzezOjca && variant?.Source != VariantSource.None)
        {
            plan.Undecided = "Matka złożyła wniosek o wariant uśredniony, a przepisy w wyciągu nie rozstrzygają, jaką stawką ojciec przejmuje jej zasiłek za macierzyński — nie rozstrzygam kwoty, skontaktuj się z ZUS.";
            plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaMacierzynski));
            return plan;
        }

        if (variant?.Source == VariantSource.Own)
        {
            plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaUsredniona));
            plan.Trace.Add(Rate(ctx, AmountsParameterKeys.StawkaUsredniona, start, what + " w wariancie uśrednionym"));
            if (card.Id == BenefitId.Macierzynski)
            {
                var usage = BezWykorzystania.Assess(ctx);
                plan.Trace.Add(usage.Step);
                plan.EqualizeToFull = usage.Usage == FirstYearUsage.NotUsed;
                plan.Notes.Add("Art. 31 ust. 3g i 4: jeśli w pierwszym roku życia dziecka nie wykorzystasz ani jednego dnia zasiłku za urlop rodzicielski, zasiłek rodzicielski wypłacany jest po 70%, a zasiłek za macierzyński pobrany po 81,5% wyrównuje się jednorazowo do 100% na wniosek.");
            }

            return plan;
        }

        plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaMacierzynski));
        plan.Trace.Add(Rate(ctx, AmountsParameterKeys.StawkaMacierzynski, start, what));
        return plan;
    }
}

internal sealed class StawkaUzupelniajacegoRule : RateRule
{
    public override string Id => "stawka_uzupelniajacy";

    public override string PodstawaPrawna => "ustawa art. 31 ust. 1";

    public override RatePlan Plan(AmountsContext ctx, BenefitResult card, IReadOnlyList<Block> blocks)
    {
        var plan = new RatePlan();
        plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaUzupelniajacy));
        plan.Trace.Add(Rate(ctx, AmountsParameterKeys.StawkaUzupelniajacy, blocks[0].From, "Zasiłek za okres uzupełniającego urlopu macierzyńskiego (bez względu na wariant wypłaty)"));
        return plan;
    }
}

internal sealed class StawkaOjcowskaRule : RateRule
{
    public override string Id => "stawka_ojcowski";

    public override string PodstawaPrawna => "ustawa art. 31 ust. 1";

    public override RatePlan Plan(AmountsContext ctx, BenefitResult card, IReadOnlyList<Block> blocks)
    {
        var plan = new RatePlan();
        plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaOjcowski));
        plan.Trace.Add(Rate(ctx, AmountsParameterKeys.StawkaOjcowski, blocks[0].From, "Zasiłek za okres urlopu ojcowskiego (bez względu na wariant wypłaty)"));
        return plan;
    }
}

internal sealed class StawkaRodzicielskaRule : RateRule
{
    private static readonly WniosekUsrednionyRule Wniosek = new();
    private static readonly DziewiecTygodniDrugiegoRodzicaRule DziewiecTygodni = new();
    private static readonly RodzicielskiBezWykorzystaniaRule BezWykorzystania = new();

    public override string Id => "stawka_rodzicielski";

    public override string PodstawaPrawna => "ustawa art. 31 ust. 2, 3, 3f, 3g";

    public override RatePlan Plan(AmountsContext ctx, BenefitResult card, IReadOnlyList<Block> blocks)
    {
        var plan = new RatePlan();
        var start = blocks[0].From;
        var variant = Wniosek.Resolve(ctx, card.Person);
        plan.Trace.AddRange(variant.Trace);
        plan.Notes.AddRange(variant.Notes);

        if (variant.Source == VariantSource.None)
        {
            plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaRodzicielski));
            plan.Trace.Add(Rate(ctx, AmountsParameterKeys.StawkaRodzicielski, start, "Zasiłek za okres urlopu rodzicielskiego"));
            return plan;
        }

        var usage = BezWykorzystania.Assess(ctx);
        plan.Trace.Add(usage.Step);
        if (usage.Usage == FirstYearUsage.NotUsed)
        {
            plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaRodzicielski));
            plan.Trace.Add(Rate(ctx, AmountsParameterKeys.StawkaRodzicielski, start, "Zasiłek za okres urlopu rodzicielskiego niewykorzystanego w pierwszym roku życia dziecka"));
            plan.Notes.Add("Art. 31 ust. 3g: żaden dzień urlopu rodzicielskiego nie przypada w pierwszym roku życia dziecka, więc zasiłek rodzicielski wynosi 70% (nie 81,5%).");
            return plan;
        }

        var nine = DziewiecTygodni.Days(ctx);
        if (variant.Source == VariantSource.Other)
        {
            plan.Spans.AddRange(SpanOps.Carve(blocks, nine, fromStart: true,
                AmountsParameterKeys.Stawka9TygDrugiegoRodzica, DziewiecTygodni.Label, AmountsParameterKeys.StawkaUsredniona));
            plan.Trace.Add(AmountTrace.Of(DziewiecTygodni, true,
                $"Wniosek złożył drugi rodzic: pierwsze {nine / DaysPerWeek} tyg. Twojego urlopu rodzicielskiego (część wyłącznie dla Ciebie) to zasiłek 70% podstawy; reszta po 81,5%. Przyjmuję, że 70% dotyczy najwcześniejszych dni — przepis nie wskazuje, które to dni (suma nie zależy od kolejności, o ile stawka się nie zmienia)."));
        }
        else
        {
            plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.StawkaUsredniona));
        }

        plan.Trace.Add(Rate(ctx, AmountsParameterKeys.StawkaUsredniona, start, "Zasiłek za okres urlopu rodzicielskiego w wariancie uśrednionym"));
        return plan;
    }
}

internal sealed class StawkaChorobowegoWCiazyRule : RateRule
{
    public override string Id => "stawka_chorobowy_w_ciazy";

    public override string PodstawaPrawna => "ustawa art. 11 ust. 2 pkt 1";

    public override RatePlan Plan(AmountsContext ctx, BenefitResult card, IReadOnlyList<Block> blocks)
    {
        var plan = new RatePlan();
        plan.Spans.AddRange(SpanOps.Whole(blocks, AmountsParameterKeys.ChorobowyCiazaStawka));
        plan.Trace.Add(Rate(ctx, AmountsParameterKeys.ChorobowyCiazaStawka, blocks[0].From, "Zasiłek chorobowy za niezdolność do pracy przypadającą w okresie ciąży"));
        return plan;
    }
}

internal sealed class StawkaRehabilitacyjnaRule : RateRule
{
    public override string Id => "stawka_rehabilitacyjne";

    public override string PodstawaPrawna => "ustawa art. 19 ust. 1; art. 22 w zw. z art. 11 ust. 5";

    public override RatePlan Plan(AmountsContext ctx, BenefitResult card, IReadOnlyList<Block> blocks)
    {
        var plan = new RatePlan();
        var firstDay = card.Period?.From ?? blocks[0].From;
        var monthDays = ctx.Int(AmountsParameterKeys.MiesiacDni, firstDay);
        var firstMonths = ctx.Int(AmountsParameterKeys.RehabPierwszeMiesiaceLiczba, firstDay);
        var firstPeriodEnd = firstDay.AddDays(monthDays * firstMonths - 1);
        DateOnly? pregnancyEnd = ctx.Kind is EventKind.Ciaza or EventKind.Urodzenie && ctx.EventDate is { } birth ? birth.AddDays(-1) : null;

        foreach (var block in blocks)
        {
            var cuts = new SortedSet<DateOnly> { block.From };
            foreach (var cut in new[] { firstPeriodEnd.AddDays(1), pregnancyEnd?.AddDays(1) ?? DateOnly.MinValue })
            {
                if (cut > block.From && cut <= block.To)
                {
                    cuts.Add(cut);
                }
            }

            var starts = cuts.ToList();
            for (var i = 0; i < starts.Count; i++)
            {
                var from = starts[i];
                var to = i + 1 < starts.Count ? starts[i + 1].AddDays(-1) : block.To;
                var (key, label) = pregnancyEnd is { } end && from <= end
                    ? (AmountsParameterKeys.RehabWCiazy, "niezdolność do pracy w okresie ciąży")
                    : from <= firstPeriodEnd
                        ? (AmountsParameterKeys.RehabPierwszeMiesiace, "pierwsze trzy miesiące świadczenia")
                        : (AmountsParameterKeys.RehabKolejneMiesiace, "pozostały okres świadczenia");
                plan.Spans.Add(new RateSpan(block, from, to, key, label));
            }
        }

        foreach (var key in plan.Spans.Select(s => s.Key).Distinct())
        {
            plan.Trace.Add(Rate(ctx, key, plan.Spans.First(s => s.Key == key).From,
                key == AmountsParameterKeys.RehabWCiazy ? "Świadczenie rehabilitacyjne za okres ciąży"
                : key == AmountsParameterKeys.RehabPierwszeMiesiace ? "Świadczenie rehabilitacyjne za pierwsze trzy miesiące (miesiąc = 30 dni)"
                : "Świadczenie rehabilitacyjne za pozostały okres"));
        }

        if (pregnancyEnd is not null)
        {
            plan.Notes.Add($"Dzień porodu przyjęty z faktu o zdarzeniu ({Date(pregnancyEnd.Value.AddDays(1))}): do {Date(pregnancyEnd.Value)} niezdolność przypada w okresie ciąży (stawka 100%).");
        }

        return plan;
    }
}

internal static class RateRules
{
    public static readonly StawkaMacierzynskaRule Macierzynska = new();
    public static readonly StawkaUzupelniajacegoRule Uzupelniajacego = new();
    public static readonly StawkaOjcowskaRule Ojcowska = new();
    public static readonly StawkaRodzicielskaRule Rodzicielska = new();
    public static readonly StawkaChorobowegoWCiazyRule ChorobowegoWCiazy = new();
    public static readonly StawkaRehabilitacyjnaRule Rehabilitacyjna = new();

    public static RateRule? For(BenefitId id) => id switch
    {
        BenefitId.Macierzynski or BenefitId.PrzyjecieDziecka or BenefitId.PrzejeciePrzezOjca or BenefitId.ZasilekDoDniaPorodu => Macierzynska,
        BenefitId.MacierzynskiUzupelniajacy => Uzupelniajacego,
        BenefitId.Ojcowski => Ojcowska,
        BenefitId.Rodzicielski => Rodzicielska,
        BenefitId.ChorobowyWCiazy => ChorobowegoWCiazy,
        BenefitId.Rehabilitacyjne => Rehabilitacyjna,
        _ => null,
    };
}
