namespace BabyRadnik.Domain.Deadlines;

public static class DeadlineParameterKeys
{
    public const string WniosekUsredniona = "termin_wniosku_usredniona_dni";
    public const string PrzedawnienieMies = "przedawnienie_mies";
    public const string ZasilekPogrzebowy = "zasilek_pogrzebowy";
    public const string ZasilekPogrzebowyTerminMies = "zasilek_pogrzebowy.termin_wniosku_mies";

    public const string WniosekMacierzynskiPrzedPorodemDni = "termin_wniosku_macierzynski_przed_porodem_dni";
    public const string RezygnacjaMatkiDni = "termin_rezygnacji_matki_dni";
    public const string WniosekOjcaCzescMacierzynskiegoDni = "termin_wniosku_ojca_czesc_macierzynskiego_dni";
    public const string WniosekUzupelniajacyDni = "termin_wniosku_uzupelniajacy_dni";
    public const string WniosekRodzicielskiDni = "termin_wniosku_rodzicielski_dni";
    public const string WniosekPracaCzescEtatuDni = "termin_wniosku_praca_czesc_etatu_dni";
    public const string WniosekOjcowskiDni = "termin_wniosku_ojcowski_dni";
    public const string WniosekPrzyjecieDni = "termin_wniosku_przyjecie_dni";
    public const string StartUrlopuPrzyjecieDni = "przyjecie_start_urlopu_max_dni";

    public sealed record SupplementRow(string Key, decimal Value, string LegalBasis);

    /// <summary>
    /// Liczby dni z przepisów (8.9), których nie ma w fixtures/parameters.seed.json. Reguły NIE mają wartości domyślnych:
    /// bez wiersza w dostawcy parametrów termin nie jest wyliczany (notatka przy świadczeniu). Lista do dodania do seed przez be/params.
    /// </summary>
    public static IReadOnlyList<SupplementRow> Supplement { get; } =
    [
        new(WniosekMacierzynskiPrzedPorodemDni, 14, "Poradnik ZUS s. 27"),
        new(RezygnacjaMatkiDni, 7, "KP art. 180 § 8"),
        new(WniosekOjcaCzescMacierzynskiegoDni, 14, "KP art. 180 § 9"),
        new(WniosekUzupelniajacyDni, 21, "KP art. 180² § 4"),
        new(WniosekRodzicielskiDni, 21, "KP art. 182¹d § 1"),
        new(WniosekPracaCzescEtatuDni, 21, "KP art. 182¹e § 2"),
        new(WniosekOjcowskiDni, 7, "KP art. 182³ § 2"),
        new(WniosekPrzyjecieDni, 7, "KP art. 183 § 6"),
        new(StartUrlopuPrzyjecieDni, 21, "KP art. 183 § 6"),
    ];
}
