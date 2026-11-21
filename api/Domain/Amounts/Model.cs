using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Amounts;

/// <summary>Reguła kwot: stała nazwa (Id) i podstawa prawna; każdy krok liczenia trafia do ścieżki decyzji pod tym Id.</summary>
public interface IAmountRule
{
    string Id { get; }

    string PodstawaPrawna { get; }
}

internal static class AmountTrace
{
    public static TraceStep Of(IAmountRule rule, bool? met, string text, string? condition = null) =>
        new(rule.Id, rule.PodstawaPrawna, met, text, condition);
}

internal sealed record Block(string? Label, DateOnly From, DateOnly To)
{
    public int Days => AmountMath.DaysInclusive(From, To);
}

internal sealed record RateSpan(Block Block, DateOnly From, DateOnly To, string Key, string? Label);

internal sealed record WorkRange(DateOnly From, DateOnly To, decimal Wymiar);

internal sealed record Piece(Block Block, DateOnly From, DateOnly To, decimal Rate, decimal Factor, string? SpanLabel, string? WorkLabel)
{
    public int Days => AmountMath.DaysInclusive(From, To);
}

internal sealed class RatePlan
{
    public List<RateSpan> Spans { get; } = [];

    public List<TraceStep> Trace { get; } = [];

    public List<string> Notes { get; } = [];

    public string? Undecided { get; set; }

    public bool EqualizeToFull { get; set; }

    public RatePlan Add(IEnumerable<RateSpan> spans)
    {
        Spans.AddRange(spans.OrderBy(s => s.From));
        return this;
    }
}

internal static class SpanOps
{
    public static List<RateSpan> Whole(IEnumerable<Block> blocks, string key, string? label = null) =>
        blocks.Select(b => new RateSpan(b, b.From, b.To, key, label)).ToList();

    /// <summary>Wydziela <paramref name="days"/> dni od początku (albo od końca) okresu; reszta dostaje <paramref name="restKey"/>.</summary>
    public static List<RateSpan> Carve(IReadOnlyList<Block> blocks, int days, bool fromStart, string carvedKey, string? carvedLabel, string restKey)
    {
        var result = new List<RateSpan>();
        var left = days;
        IEnumerable<Block> ordered = fromStart ? blocks : blocks.Reverse();
        foreach (var block in ordered)
        {
            var take = Math.Min(left, block.Days);
            left -= take;
            var rest = block.Days - take;
            if (fromStart)
            {
                if (take > 0)
                {
                    result.Add(new RateSpan(block, block.From, block.From.AddDays(take - 1), carvedKey, carvedLabel));
                }

                if (rest > 0)
                {
                    result.Add(new RateSpan(block, block.From.AddDays(take), block.To, restKey, null));
                }
            }
            else
            {
                if (rest > 0)
                {
                    result.Add(new RateSpan(block, block.From, block.To.AddDays(-take), restKey, null));
                }

                if (take > 0)
                {
                    result.Add(new RateSpan(block, block.To.AddDays(-take + 1), block.To, carvedKey, carvedLabel));
                }
            }
        }

        return result.OrderBy(s => s.From).ToList();
    }
}
