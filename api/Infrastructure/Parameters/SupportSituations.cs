namespace BabyRadnik.Infrastructure.Parameters;

/// <summary>
/// Słownik sytuacji dla katalogu wsparcia (kontrakt: `SupportSituation` w openapi.yaml; frontend: `web/src/app/situations.ts`).
/// </summary>
public static class SupportSituations
{
    public static readonly IReadOnlyList<string> All =
    [
        "martwe_urodzenie",
        "zgon_dziecka",
        "wczesniak",
        "ciezka_choroba_dziecka",
        "samotny_rodzic",
        "trudna_sytuacja_materialna",
        "przyjecie_dziecka",
    ];

    public static bool IsKnown(string? id) => id is not null && All.Contains(id, StringComparer.Ordinal);
}
