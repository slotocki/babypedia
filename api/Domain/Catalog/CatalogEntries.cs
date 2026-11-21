using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Domain.Catalog;

public sealed record CatalogEntry(
    BenefitId Id,
    string Name,
    IReadOnlyList<string> RequiredFacts,
    IReadOnlyList<string> Documents,
    IReadOnlyList<string> EmployerDocuments,
    IReadOnlyList<Link> Links);

public static class CatalogLinks
{
    public static readonly Link Zus = new("ZUS — informacje o zasiłkach", "https://www.zus.pl");
    public static readonly Link ZusContact = new("Kontakt z ZUS", "https://www.zus.pl/kontakt");
    public static readonly Link Mrpips = new("MRPiPS — urlop rodzicielski", "https://www.gov.pl/web/rodzina/urlop-rodzicielski");
    public static readonly Link Pip = new("PIP — rodzicielstwo, porady prawne", "https://www.pip.gov.pl/dla-pracodawcow/porady-prawne/rodzicielstwo");

    public static readonly IReadOnlyList<Link> ZusOnly = [Zus];
    public static readonly IReadOnlyList<Link> Leaves = [Zus, Mrpips, Pip];
}

/// <summary>Katalog P0 (sekcja 7 instrukcji, KONTRAKT §4). Każda pozycja ma wymagane fakty, dokumenty i linki S9.</summary>
public static class BenefitCatalog
{
    private const string BirthCertificate = "Skrócony odpis aktu urodzenia dziecka (do wglądu albo ZUS pozyska go z urzędu stanu cywilnego)";

    public static IReadOnlyList<CatalogEntry> Entries { get; } =
    [
        new(BenefitId.ChorobowyWCiazy, "Zasiłek chorobowy w okresie ciąży",
            [FactKeys.ZdarzenieRodzaj, FactKeys.OsobaRola, FactKeys.UbezpieczenieTytuly, FactKeys.EzlaZwolnienia, FactKeys.PlatnikLiczbaUbezpieczonych, FactKeys.SkladkiZadluzenie],
            [], [], CatalogLinks.ZusOnly),
        new(BenefitId.Rehabilitacyjne, "Świadczenie rehabilitacyjne",
            [FactKeys.ZdarzenieRodzaj, FactKeys.OsobaRola, FactKeys.UbezpieczenieTytuly, FactKeys.EzlaZwolnienia],
            [], [], CatalogLinks.ZusOnly),
        new(BenefitId.ZasilekDoDniaPorodu, "Zasiłek w wysokości zasiłku macierzyńskiego (art. 30 ust. 3)",
            [FactKeys.ZdarzenieData, FactKeys.OsobaRola, FactKeys.UbezpieczenieTytuly, FactKeys.UbezpieczenieUstaniePrzyczyna],
            [], [], CatalogLinks.ZusOnly),
        new(BenefitId.Macierzynski, "Zasiłek macierzyński za okres urlopu macierzyńskiego",
            [FactKeys.ZdarzenieRodzaj, FactKeys.ZdarzenieData, FactKeys.DzieckoLiczba, FactKeys.OsobaRola, FactKeys.UbezpieczenieTytuly, FactKeys.PlatnikLiczbaUbezpieczonych],
            [BirthCertificate], ["Wniosek do pracodawcy w sprawie urlopu macierzyńskiego (rozporządzenie z 11.03.2025)"], CatalogLinks.Leaves),
        new(BenefitId.MacierzynskiUzupelniajacy, "Zasiłek macierzyński za okres uzupełniającego urlopu macierzyńskiego",
            [FactKeys.ZdarzenieData, FactKeys.DzieckoTydzienCiazyPrzyPorodzie, FactKeys.DzieckoMasaUrodzeniowaG, FactKeys.DzieckoPobytyWSzpitalu, FactKeys.OsobaRola, FactKeys.UbezpieczenieTytuly],
            ["Zaświadczenie ze szpitala o pobycie dziecka i o jego urodzeniu (KP art. 180² § 5)"], ["Wniosek do pracodawcy o uzupełniający urlop macierzyński"], CatalogLinks.Leaves),
        new(BenefitId.Rodzicielski, "Zasiłek macierzyński za okres urlopu rodzicielskiego",
            [FactKeys.ZdarzenieData, FactKeys.DzieckoLiczba, FactKeys.OsobaRola, FactKeys.UbezpieczenieTytuly, FactKeys.PlanRodzicielskiCzesci],
            [], ["Wniosek do pracodawcy o urlop rodzicielski"], CatalogLinks.Leaves),
        new(BenefitId.Ojcowski, "Zasiłek macierzyński za okres urlopu ojcowskiego",
            [FactKeys.ZdarzenieData, FactKeys.OsobaRola, FactKeys.UbezpieczenieTytuly, FactKeys.PlanOjcowskiCzesci],
            [BirthCertificate], ["Wniosek do pracodawcy o urlop ojcowski"], CatalogLinks.Leaves),
        new(BenefitId.PrzyjecieDziecka, "Zasiłek za okres urlopu na warunkach urlopu macierzyńskiego",
            [FactKeys.ZdarzenieData, FactKeys.PrzyjecieSciezka, FactKeys.DzieckoDataUrodzenia, FactKeys.PrzyjecieWniosekDoSadu, FactKeys.PrzyjecieOdroczenieObowiazkuSzkolnego, FactKeys.UbezpieczenieTytuly],
            ["Dokumenty potwierdzające przyjęcie dziecka na wychowanie (zakres: rozporządzenie z 27.09.2026)"], ["Wniosek do pracodawcy o urlop na warunkach urlopu macierzyńskiego"], CatalogLinks.Leaves),
        new(BenefitId.PrzejeciePrzezOjca, "Przejęcie zasiłku w sytuacjach szczególnych matki",
            [FactKeys.ZdarzenieData, FactKeys.OsobaRola, FactKeys.MatkaSytuacjaSzczegolna, FactKeys.PlanRezygnacjaZMacierzynskiegoPoTyg, FactKeys.UbezpieczenieTytuly],
            ["Dokumenty potwierdzające rezygnację matki albo jej sytuację szczególną"], ["Wniosek do pracodawcy o część urlopu macierzyńskiego przejętą po matce"], CatalogLinks.Leaves),
        new(BenefitId.Pogrzebowy, "Zasiłek pogrzebowy (informacja)",
            [FactKeys.DzieckoMartweUrodzenie, FactKeys.DzieckoDataZgonu, FactKeys.DzieckoDataPochowku],
            [
                "Wniosek o zasiłek pogrzebowy (druk Z-12)",
                "Rachunki dotyczące kosztów pogrzebu",
                "Akt urodzenia z adnotacją o martwym urodzeniu albo karta zgonu, albo zaświadczenie o martwym urodzeniu wydane przez uprawnionego lekarza lub położną",
            ], [], CatalogLinks.ZusOnly),
    ];

    public static CatalogEntry Get(BenefitId id) => Entries.Single(e => e.Id == id);
}

public interface ICatalogSettings
{
    bool IsEnabled(BenefitId id);
}

public sealed class AllEnabledCatalogSettings : ICatalogSettings
{
    public bool IsEnabled(BenefitId id) => true;
}

public sealed class FixedCatalogSettings(IEnumerable<BenefitId> disabled) : ICatalogSettings
{
    private readonly HashSet<BenefitId> disabledIds = [.. disabled];

    public bool IsEnabled(BenefitId id) => !disabledIds.Contains(id);
}
