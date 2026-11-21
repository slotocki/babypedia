using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Submissions;

namespace BabyRadnik.Tests.Infrastructure.Submissions;

internal static class Samples
{
    public const string Name = "Żaneta Ćwikła-Łęcka";

    public static IReadOnlyDictionary<string, string> Fields(string templateId) => templateId switch
    {
        LeaveTemplateIds.MacierzynskiIRodzicielski => new Dictionary<string, string>
        {
            ["imieNazwisko"] = Name,
            ["pracodawca"] = "Zażółć Gęślą Jaźń sp. z o.o.",
            ["miejscowosc"] = "Łódź",
            ["dataPorodu"] = "2027-02-10",
            ["macierzynskiPrzedPorodemOd"] = "2027-01-04",
            ["dotychczasWykorzystano"] = "brak",
            ["liczbaWykorzystanychCzesci"] = "0",
            ["rodzicielskiOd"] = "2027-08-02",
            ["rodzicielskiDo"] = "2028-03-01",
            ["drugiRodzic"] = "zamierza_w_okresie",
            ["drugiRodzicOkres"] = "od 2028-03-02 do 2028-04-30",
            ["zaZyciem"] = "tak",
            ["dzieckoPrzyjete"] = "nie",
        },
        LeaveTemplateIds.Rodzicielski => new Dictionary<string, string>
        {
            ["imieNazwisko"] = Name,
            ["pracodawca"] = "Hurtownia Świętokrzyska",
            ["dotychczasWykorzystano"] = "od 2027-02-11 do 2027-07-31 (urlop macierzyński)",
            ["liczbaWykorzystanychCzesci"] = "1",
            ["rodzicielskiOd"] = "2027-08-02",
            ["rodzicielskiDo"] = "2027-12-31",
            ["drugiRodzic"] = "brak_zamiaru",
            ["lacznieZPraca"] = "tak",
            ["wymiarCzasuPracy"] = "1/2 etatu",
            ["laczenieOd"] = "2027-09-01",
            ["laczenieDo"] = "2027-12-31",
            ["laczenieCzescWydluzona"] = "nie",
            ["dzieckoPrzyjete"] = "tak",
            ["dataPrzyjeciaDziecka"] = "2027-06-15",
            ["odroczenieObowiazkuSzkolnego"] = "tak",
        },
        LeaveTemplateIds.Ojcowski => new Dictionary<string, string>
        {
            ["imieNazwisko"] = "Tadeusz Wąsowski-Śliwa",
            ["pracodawca"] = "Żwirownia Ząbkowice",
            ["ojcowskiOd"] = "2027-02-15",
            ["ojcowskiDo"] = "2027-02-28",
            ["dzieckoPrzysposobione"] = "tak",
            ["korzystalZUrlopuOjcowskiego"] = "nie",
        },
        LeaveTemplateIds.Uzupelniajacy => new Dictionary<string, string>
        {
            ["imieNazwisko"] = Name,
            ["terminZakonczeniaMacierzynskiego"] = "2027-07-31",
            ["uzupelniajacyOd"] = "2027-08-01",
            ["uzupelniajacyDo"] = "2027-09-26",
            ["drugiRodzicBrakZamiaru"] = "tak",
        },
        "dane_zam" => new Dictionary<string, string>
        {
            ["imieNazwisko"] = Name,
            ["platnikNipRegon"] = "5252525252 / 123456789",
            ["platnikNazwaAdres"] = "Zażółć Gęślą Jaźń sp. z o.o., ul. Łąkowa 5, 90-001 Łódź",
            ["rodzajSwiadczenia"] = "macierzynski",
            ["okresOd"] = "2027-02-11",
            ["okresDo"] = "2027-07-31",
            ["podstawaWniosku"] = "urodzenie",
            ["zaZyciem"] = "nie",
            ["dzieckoDataUrodzenia"] = "2027-02-10",
            ["pelnyWymiar"] = "tak",
        },
        "zapytanie_ezus" => new Dictionary<string, string>
        {
            ["imieNazwisko"] = Name,
            ["temat"] = "okresy_ubezpieczenia",
            ["tresc"] = "Proszę o potwierdzenie okresów ubezpieczenia chorobowego w ostatnich 12 miesiącach.",
        },
        "odpis_aktu" => new Dictionary<string, string>
        {
            ["imieNazwisko"] = Name,
            ["dzieckoImieNazwisko"] = "Zdzisław Ćwikła",
            ["dzieckoDataUrodzenia"] = "2027-02-10",
            ["miejsceUrodzenia"] = "Łódź",
            ["rodzajOdpisu"] = "skrocony",
            ["liczbaOdpisow"] = "2",
        },
        _ => throw new ArgumentOutOfRangeException(nameof(templateId)),
    };

    public static TheoryData<string> TemplateIds => new(TemplateCatalog.All.Select(t => t.Id));

    public static object Request(string templateId)
    {
        var spec = TemplateCatalog.All.Single(t => t.Id == templateId);
        return new
        {
            type = SubmissionNames.ToWire(spec.Type),
            template = spec.Type == SubmissionType.WniosekUrlop ? spec.Id : null,
            fields = Fields(templateId),
        };
    }
}
