using System.Text.Json.Serialization;

namespace BabyRadnik.Domain.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<EventKind>))]
public enum EventKind
{
    [JsonStringEnumMemberName("planuje")] Planuje,
    [JsonStringEnumMemberName("ciaza")] Ciaza,
    [JsonStringEnumMemberName("urodzenie")] Urodzenie,
    [JsonStringEnumMemberName("przyjecie")] Przyjecie,
}

[JsonConverter(typeof(JsonStringEnumConverter<CalculationScope>))]
public enum CalculationScope
{
    [JsonStringEnumMemberName("ja")] Ja,
    [JsonStringEnumMemberName("ja_i_drugi_rodzic")] JaIDrugiRodzic,
}

[JsonConverter(typeof(JsonStringEnumConverter<AdoptionPath>))]
public enum AdoptionPath
{
    [JsonStringEnumMemberName("przysposobienie")] Przysposobienie,
    [JsonStringEnumMemberName("rodzina_zastepcza_niezawodowa")] RodzinaZastepczaNiezawodowa,
    [JsonStringEnumMemberName("rodzina_zastepcza_zawodowa")] RodzinaZastepczaZawodowa,
}

[JsonConverter(typeof(JsonStringEnumConverter<MotherSituationKind>))]
public enum MotherSituationKind
{
    [JsonStringEnumMemberName("niezdolnosc_do_samodzielnej_egzystencji")] NiezdolnoscDoSamodzielnejEgzystencji,
    [JsonStringEnumMemberName("pobyt_w_szpitalu")] PobytWSzpitalu,
    [JsonStringEnumMemberName("zgon")] Zgon,
    [JsonStringEnumMemberName("porzucenie_dziecka")] PorzucenieDziecka,
}

[JsonConverter(typeof(JsonStringEnumConverter<ParentRole>))]
public enum ParentRole
{
    [JsonStringEnumMemberName("matka")] Matka,
    [JsonStringEnumMemberName("ojciec")] Ojciec,
}

[JsonConverter(typeof(JsonStringEnumConverter<TitleKind>))]
public enum TitleKind
{
    [JsonStringEnumMemberName("pracownik")] Pracownik,
    [JsonStringEnumMemberName("zleceniobiorca")] Zleceniobiorca,
    [JsonStringEnumMemberName("dzialalnosc")] Dzialalnosc,
    [JsonStringEnumMemberName("osoba_wspolpracujaca")] OsobaWspolpracujaca,
    [JsonStringEnumMemberName("pobierajacy_zasilek")] PobierajacyZasilek,
    [JsonStringEnumMemberName("inny")] Inny,
}

[JsonConverter(typeof(JsonStringEnumConverter<SicknessInsurance>))]
public enum SicknessInsurance
{
    [JsonStringEnumMemberName("obowiazkowe")] Obowiazkowe,
    [JsonStringEnumMemberName("dobrowolne")] Dobrowolne,
    [JsonStringEnumMemberName("brak")] Brak,
}

[JsonConverter(typeof(JsonStringEnumConverter<ReceivedBenefitKind>))]
public enum ReceivedBenefitKind
{
    [JsonStringEnumMemberName("wynagrodzenie_chorobowe")] WynagrodzenieChorobowe,
    [JsonStringEnumMemberName("chorobowy")] Chorobowy,
    [JsonStringEnumMemberName("rehabilitacyjne")] Rehabilitacyjne,
    [JsonStringEnumMemberName("macierzynski")] Macierzynski,
    [JsonStringEnumMemberName("macierzynski_uzupelniajacy")] MacierzynskiUzupelniajacy,
    [JsonStringEnumMemberName("rodzicielski")] Rodzicielski,
    [JsonStringEnumMemberName("ojcowski")] Ojcowski,
    [JsonStringEnumMemberName("opiekunczy")] Opiekunczy,
    [JsonStringEnumMemberName("inne")] Inne,
}

[JsonConverter(typeof(JsonStringEnumConverter<InsuranceEndReason>))]
public enum InsuranceEndReason
{
    [JsonStringEnumMemberName("wypowiedzenie_przez_pracownika")] WypowiedzeniePrzezPracownika,
    [JsonStringEnumMemberName("wypowiedzenie_przez_pracodawce")] WypowiedzeniePrzezPracodawce,
    [JsonStringEnumMemberName("porozumienie_stron")] PorozumienieStron,
    [JsonStringEnumMemberName("koniec_umowy_na_czas_okreslony")] KoniecUmowyNaCzasOkreslony,
    [JsonStringEnumMemberName("likwidacja_pracodawcy")] LikwidacjaPracodawcy,
    [JsonStringEnumMemberName("upadlosc_pracodawcy")] UpadloscPracodawcy,
    [JsonStringEnumMemberName("smierc_pracodawcy")] SmiercPracodawcy,
    [JsonStringEnumMemberName("naruszenie_prawa_orzeczone_przez_sad")] NaruszeniePrawaOrzeczonePrzezSad,
    [JsonStringEnumMemberName("zamkniecie_dzialalnosci")] ZamkniecieDzialalnosci,
    [JsonStringEnumMemberName("inna")] Inna,
}

[JsonConverter(typeof(JsonStringEnumConverter<PaymentVariant>))]
public enum PaymentVariant
{
    [JsonStringEnumMemberName("standardowy")] Standardowy,
    [JsonStringEnumMemberName("usredniony")] Usredniony,
}

public sealed record DateRange(
    [property: JsonPropertyName("od")] DateOnly Od,
    [property: JsonPropertyName("do")] DateOnly Do,
    [property: JsonPropertyName("page")] int? Page = null);

public sealed record InsuranceTitle(
    [property: JsonPropertyName("rodzajTytulu")] TitleKind RodzajTytulu,
    [property: JsonPropertyName("od")] DateOnly Od,
    [property: JsonPropertyName("do")] DateOnly? Do,
    [property: JsonPropertyName("chorobowe")] SicknessInsurance Chorobowe,
    [property: JsonPropertyName("kodTytulu")] string? KodTytulu = null,
    [property: JsonPropertyName("opis")] string? Opis = null,
    [property: JsonPropertyName("platnikId")] string? PlatnikId = null,
    [property: JsonPropertyName("platnikNazwa")] string? PlatnikNazwa = null,
    [property: JsonPropertyName("wymiarEtatu")] decimal? WymiarEtatu = null,
    [property: JsonPropertyName("page")] int? Page = null);

public sealed record ContributionBase(
    [property: JsonPropertyName("miesiac")] string Miesiac,
    [property: JsonPropertyName("kwota")] decimal Kwota,
    [property: JsonPropertyName("platnikId")] string? PlatnikId = null,
    [property: JsonPropertyName("page")] int? Page = null);

public sealed record InsuranceBreak(
    [property: JsonPropertyName("od")] DateOnly Od,
    [property: JsonPropertyName("do")] DateOnly Do,
    [property: JsonPropertyName("opis")] string? Opis = null,
    [property: JsonPropertyName("page")] int? Page = null);

public sealed record ReceivedBenefit(
    [property: JsonPropertyName("rodzaj")] ReceivedBenefitKind Rodzaj,
    [property: JsonPropertyName("od")] DateOnly Od,
    [property: JsonPropertyName("do")] DateOnly Do,
    [property: JsonPropertyName("opis")] string? Opis = null,
    [property: JsonPropertyName("page")] int? Page = null);

public sealed record SickLeave(
    [property: JsonPropertyName("od")] DateOnly Od,
    [property: JsonPropertyName("do")] DateOnly Do,
    [property: JsonPropertyName("kod")] string? Kod,
    [property: JsonPropertyName("szpital")] bool Szpital,
    [property: JsonPropertyName("dataWystawienia")] DateOnly? DataWystawienia = null,
    [property: JsonPropertyName("page")] int? Page = null);

public sealed record ContributionDebt(
    [property: JsonPropertyName("naDzien")] DateOnly NaDzien,
    [property: JsonPropertyName("kwota")] decimal Kwota);

public sealed record MotherSituation(
    [property: JsonPropertyName("rodzaj")] MotherSituationKind Rodzaj,
    [property: JsonPropertyName("od")] DateOnly Od,
    [property: JsonPropertyName("do")] DateOnly? Do = null);

public sealed record LeavePart(
    [property: JsonPropertyName("tygodnie")] int Tygodnie,
    [property: JsonPropertyName("od")] DateOnly? Od = null);

public sealed record PartTimeWork(
    [property: JsonPropertyName("wymiar")] decimal Wymiar,
    [property: JsonPropertyName("tygodnie")] int Tygodnie,
    [property: JsonPropertyName("od")] DateOnly? Od = null);
