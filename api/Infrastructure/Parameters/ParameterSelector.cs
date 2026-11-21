using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Parameters;

public static class ParameterSelector
{
    /// <summary>Wiersz z największym validFrom ≤ data; przy równych datach wygrywa późniejszy wpis (korekta).</summary>
    public static ParameterRow? Select(IEnumerable<ParameterRow> rows, string key, DateOnly date)
    {
        ParameterRow? best = null;
        foreach (var row in rows)
        {
            if (row.Key != key || row.ValidFrom > date)
            {
                continue;
            }

            if (best is null || row.ValidFrom > best.ValidFrom || (row.ValidFrom == best.ValidFrom && row.Id > best.Id))
            {
                best = row;
            }
        }

        return best;
    }

    public static ParameterRow? Latest(IEnumerable<ParameterRow> rows, string key) =>
        Select(rows, key, DateOnly.MaxValue);

    public static Parameter ToParameter(ParameterRow row) =>
        new(row.Key, row.ValueNumber, row.ValueText, row.ValidFrom, row.LegalBasis);
}
