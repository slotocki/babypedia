using System.Text.Json.Serialization;

namespace BabyRadnik.Domain.Contracts;

/// <summary>Zbiór faktów wejściowych silnika: fakty sprawy + fakty wnioskodawcy + opcjonalnie drugiego rodzica.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FactSet
{
    [JsonPropertyName("sprawa")] public CaseFacts Sprawa { get; init; } = new();
    [JsonPropertyName("wnioskodawca")] public PersonFacts Wnioskodawca { get; init; } = new();
    [JsonPropertyName("drugiRodzic")] public PersonFacts? DrugiRodzic { get; init; }

    public PersonFacts? For(PersonRef person) => person == PersonRef.Wnioskodawca ? Wnioskodawca : DrugiRodzic;
}

[JsonConverter(typeof(JsonStringEnumConverter<PersonRef>))]
public enum PersonRef
{
    [JsonStringEnumMemberName("wnioskodawca")] Wnioskodawca,
    [JsonStringEnumMemberName("drugiRodzic")] DrugiRodzic,
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CaseFacts
{
    [JsonPropertyName(FactKeys.ZdarzenieRodzaj)] public Fact<EventKind>? ZdarzenieRodzaj { get; init; }
    [JsonPropertyName(FactKeys.ZdarzenieData)] public Fact<DateOnly>? ZdarzenieData { get; init; }
    [JsonPropertyName(FactKeys.ZdarzenieDlaKogo)] public Fact<CalculationScope>? ZdarzenieDlaKogo { get; init; }

    [JsonPropertyName(FactKeys.DzieckoLiczba)] public Fact<int>? DzieckoLiczba { get; init; }
    [JsonPropertyName(FactKeys.DzieckoDataUrodzenia)] public Fact<DateOnly>? DzieckoDataUrodzenia { get; init; }
    [JsonPropertyName(FactKeys.DzieckoTydzienCiazyPrzyPorodzie)] public Fact<int>? DzieckoTydzienCiazyPrzyPorodzie { get; init; }
    [JsonPropertyName(FactKeys.DzieckoMasaUrodzeniowaG)] public Fact<int>? DzieckoMasaUrodzeniowaG { get; init; }
    [JsonPropertyName(FactKeys.DzieckoPobytyWSzpitalu)] public Fact<IReadOnlyList<DateRange>>? DzieckoPobytyWSzpitalu { get; init; }
    [JsonPropertyName(FactKeys.DzieckoZaZyciem)] public Fact<bool>? DzieckoZaZyciem { get; init; }
    [JsonPropertyName(FactKeys.DzieckoMartweUrodzenie)] public Fact<bool>? DzieckoMartweUrodzenie { get; init; }
    [JsonPropertyName(FactKeys.DzieckoLiczbaZmarlych)] public Fact<int>? DzieckoLiczbaZmarlych { get; init; }
    [JsonPropertyName(FactKeys.DzieckoDataZgonu)] public Fact<DateOnly>? DzieckoDataZgonu { get; init; }
    [JsonPropertyName(FactKeys.DzieckoDataPochowku)] public Fact<DateOnly>? DzieckoDataPochowku { get; init; }

    [JsonPropertyName(FactKeys.PrzyjecieSciezka)] public Fact<AdoptionPath>? PrzyjecieSciezka { get; init; }
    [JsonPropertyName(FactKeys.PrzyjecieWniosekDoSadu)] public Fact<bool>? PrzyjecieWniosekDoSadu { get; init; }
    [JsonPropertyName(FactKeys.PrzyjecieDataUprawomocnieniaPostanowienia)] public Fact<DateOnly>? PrzyjecieDataUprawomocnieniaPostanowienia { get; init; }
    [JsonPropertyName(FactKeys.PrzyjecieOdroczenieObowiazkuSzkolnego)] public Fact<bool>? PrzyjecieOdroczenieObowiazkuSzkolnego { get; init; }

    [JsonPropertyName(FactKeys.MatkaSytuacjaSzczegolna)] public Fact<MotherSituation>? MatkaSytuacjaSzczegolna { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PersonFacts
{
    [JsonPropertyName(FactKeys.OsobaRola)] public Fact<ParentRole>? OsobaRola { get; init; }
    [JsonPropertyName(FactKeys.OsobaDataUrodzenia)] public Fact<DateOnly>? OsobaDataUrodzenia { get; init; }
    [JsonPropertyName(FactKeys.OsobaDataUkonczeniaSzkoly)] public Fact<DateOnly>? OsobaDataUkonczeniaSzkoly { get; init; }

    [JsonPropertyName(FactKeys.UbezpieczenieTytuly)] public Fact<IReadOnlyList<InsuranceTitle>>? UbezpieczenieTytuly { get; init; }
    [JsonPropertyName(FactKeys.UbezpieczeniePodstawySkladek)] public Fact<IReadOnlyList<ContributionBase>>? UbezpieczeniePodstawySkladek { get; init; }
    [JsonPropertyName(FactKeys.UbezpieczeniePrzerwy)] public Fact<IReadOnlyList<InsuranceBreak>>? UbezpieczeniePrzerwy { get; init; }
    [JsonPropertyName(FactKeys.UbezpieczeniePobraneSwiadczenia)] public Fact<IReadOnlyList<ReceivedBenefit>>? UbezpieczeniePobraneSwiadczenia { get; init; }
    [JsonPropertyName(FactKeys.UbezpieczenieLatWczesniejszegoObowiazkowego)] public Fact<decimal>? UbezpieczenieLatWczesniejszegoObowiazkowego { get; init; }
    [JsonPropertyName(FactKeys.UbezpieczeniePodlegaChorobowemu)] public Fact<bool>? UbezpieczeniePodlegaChorobowemu { get; init; }
    [JsonPropertyName(FactKeys.UbezpieczenieUstaniePrzyczyna)] public Fact<InsuranceEndReason>? UbezpieczenieUstaniePrzyczyna { get; init; }

    [JsonPropertyName(FactKeys.UmowaNaCzasOkreslony)] public Fact<bool>? UmowaNaCzasOkreslony { get; init; }
    [JsonPropertyName(FactKeys.UmowaDataKonca)] public Fact<DateOnly>? UmowaDataKonca { get; init; }
    [JsonPropertyName(FactKeys.UmowaPlanowaneOdejscieData)] public Fact<DateOnly>? UmowaPlanowaneOdejscieData { get; init; }

    [JsonPropertyName(FactKeys.PlatnikLiczbaUbezpieczonych)] public Fact<int>? PlatnikLiczbaUbezpieczonych { get; init; }

    [JsonPropertyName(FactKeys.SkladkiZadluzenie)] public Fact<ContributionDebt>? SkladkiZadluzenie { get; init; }
    [JsonPropertyName(FactKeys.SkladkiDataSplaty)] public Fact<DateOnly>? SkladkiDataSplaty { get; init; }

    [JsonPropertyName(FactKeys.EzlaZwolnienia)] public Fact<IReadOnlyList<SickLeave>>? EzlaZwolnienia { get; init; }

    [JsonPropertyName(FactKeys.PodstawaUstalonaPrzezPlatnika)] public Fact<decimal>? PodstawaUstalonaPrzezPlatnika { get; init; }

    [JsonPropertyName(FactKeys.PlanWariantWyplaty)] public Fact<PaymentVariant>? PlanWariantWyplaty { get; init; }
    [JsonPropertyName(FactKeys.PlanDataWnioskuUsrednionego)] public Fact<DateOnly>? PlanDataWnioskuUsrednionego { get; init; }
    [JsonPropertyName(FactKeys.PlanMacierzynskiPrzedPorodemTyg)] public Fact<int>? PlanMacierzynskiPrzedPorodemTyg { get; init; }
    [JsonPropertyName(FactKeys.PlanRezygnacjaZMacierzynskiegoPoTyg)] public Fact<int>? PlanRezygnacjaZMacierzynskiegoPoTyg { get; init; }
    [JsonPropertyName(FactKeys.PlanRodzicielskiCzesci)] public Fact<IReadOnlyList<LeavePart>>? PlanRodzicielskiCzesci { get; init; }
    [JsonPropertyName(FactKeys.PlanPracaNaCzescEtatu)] public Fact<IReadOnlyList<PartTimeWork>>? PlanPracaNaCzescEtatu { get; init; }
    [JsonPropertyName(FactKeys.PlanOjcowskiCzesci)] public Fact<IReadOnlyList<LeavePart>>? PlanOjcowskiCzesci { get; init; }
}
