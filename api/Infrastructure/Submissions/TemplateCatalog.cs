using BabyRadnik.Domain.Contracts;

namespace BabyRadnik.Infrastructure.Submissions;

public static class LeaveTemplateIds
{
    public const string MacierzynskiIRodzicielski = "macierzynski_i_rodzicielski";
    public const string Rodzicielski = "rodzicielski";
    public const string Ojcowski = "ojcowski";
    public const string Uzupelniajacy = "uzupelniajacy";
}

/// <summary>
/// Treść szablonów wniosków. Podstawy: R25 = rozporządzenie MRPiPS z 11.03.2025 (docs/prawo/rozporzadzenie-2025-03-11-wnioski-pracownicze.md),
/// R26 = rozporządzenie MRPiPS z 27.09.2026 (docs/prawo/rozporzadzenie-2026-09-27-dokumenty-zasilki.md).
/// Pole bez przepisu ma w podstawie dopisek "pole pomocnicze" i nie jest wymogiem rozporządzenia.
/// </summary>
public static class TemplateCatalog
{
    private const string R25 = "rozp. 11.03.2025";
    private const string R26 = "rozp. 27.09.2026";
    private const string Aux = "pole pomocnicze (układ dokumentu, nie wymóg rozporządzenia)";

    private static readonly IReadOnlyList<FieldOption> YesNo = [new("tak", "Tak"), new("nie", "Nie")];

    private static readonly IReadOnlyList<FieldOption> OtherParent =
    [
        new("brak_zamiaru", "Drugi rodzic nie zamierza korzystać w tym okresie"),
        new("zamierza_w_okresie", "Drugi rodzic zamierza korzystać w okresie wskazanym poniżej"),
    ];

    private static FieldSpec Name(string basis) => new("imieNazwisko", "Imię i nazwisko pracownika", FieldKind.Text, basis, Required: true, MaxLength: 120);

    private static FieldSpec Employer() => new("pracodawca", "Pracodawca (adresat wniosku)", FieldKind.Text, Aux, MaxLength: 200);

    private static FieldSpec Place() => new("miejscowosc", "Miejscowość", FieldKind.Text, Aux, MaxLength: 80);

    private static FieldSpec BirthDate() => new("dataPorodu", "Przewidywana data porodu albo data urodzenia dziecka", FieldKind.Date, Aux);

    private static FieldSpec Adopted(string basis) =>
        new("dzieckoPrzyjete", "Dziecko przyjęte na wychowanie (przysposobienie / rodzina zastępcza)", FieldKind.Choice, basis, Options: YesNo);

    private static FieldSpec AdoptionDate(string basis) =>
        new("dataPrzyjeciaDziecka", "Data przyjęcia dziecka na wychowanie (oświadczenie pracownika)", FieldKind.Date, basis,
            Required: true, When: new("dzieckoPrzyjete", "tak"));

    private static FieldSpec SchoolDeferral(string basis) =>
        new("odroczenieObowiazkuSzkolnego", "Dziecko objęte decyzją o odroczeniu obowiązku szkolnego", FieldKind.Choice, basis,
            When: new("dzieckoPrzyjete", "tak"), Options: YesNo);

    private static IEnumerable<FieldSpec> ParentalCore() =>
    [
        new("dotychczasWykorzystano", "Okres dotychczas wykorzystanego urlopu rodzicielskiego lub zasiłku macierzyńskiego za okres odpowiadający urlopowi rodzicielskiemu (wpisz „brak”, jeśli nie korzystano)",
            FieldKind.LongText, $"§ 15 ust. 1 pkt 2 {R25}", Required: true, MaxLength: 500),
        new("liczbaWykorzystanychCzesci", "Liczba wykorzystanych części urlopu rodzicielskiego lub liczba wniosków o zasiłek macierzyński za część urlopu rodzicielskiego",
            FieldKind.Integer, $"§ 15 ust. 1 pkt 2 {R25}", Required: true),
        new("rodzicielskiOd", "Urlop rodzicielski lub jego część — od", FieldKind.Date, $"§ 15 ust. 1 pkt 3 {R25}", Required: true),
        new("rodzicielskiDo", "Urlop rodzicielski lub jego część — do", FieldKind.Date, $"§ 15 ust. 1 pkt 3 {R25}", Required: true),
        new("drugiRodzic", "Oświadczenie o drugim rodzicu dziecka", FieldKind.Choice, $"§ 15 ust. 2 pkt 2 {R25}", Required: true, Options: OtherParent),
        new("drugiRodzicOkres", "Okres, w którym drugi rodzic zamierza korzystać z urlopu rodzicielskiego albo z zasiłku za okres odpowiadający urlopowi rodzicielskiemu",
            FieldKind.Text, $"§ 15 ust. 2 pkt 2 {R25}", Required: true, When: new("drugiRodzic", "zamierza_w_okresie")),
        new("zaZyciem", "Wniosek dotyczy wymiaru urlopu rodzicielskiego z art. 182¹a § 2 KP (zaświadczenie „Za życiem”)", FieldKind.Choice,
            $"§ 15 ust. 2 pkt 3 {R25}", Options: YesNo),
    ];

    private static AttachmentSpec[] ParentalAttachments() =>
    [
        new("Odpis skrócony aktu urodzenia dziecka (dzieci) albo zagraniczny dokument potwierdzający urodzenie, albo kopie tych dokumentów", $"§ 15 ust. 2 pkt 1 {R25}"),
        new("Kopia zaświadczenia, o którym mowa w art. 4 ust. 3 ustawy „Za życiem”", $"§ 15 ust. 2 pkt 3 {R25}", new("zaZyciem", "tak")),
        new("Kopia wniosku o wszczęcie postępowania w sprawie przysposobienia z poświadczeniem sądu opiekuńczego o dacie wystąpienia do sądu, albo zaświadczenie sądu, albo kopia prawomocnego orzeczenia o umieszczeniu dziecka w rodzinie zastępczej, albo kopia umowy zawartej między rodziną zastępczą a starostą",
            $"§ 15 ust. 4 pkt 1 {R25}", new("dzieckoPrzyjete", "tak")),
        new("Kopia prawomocnej decyzji o odroczeniu obowiązku szkolnego", $"§ 15 ust. 4 pkt 2 {R25}", new("odroczenieObowiazkuSzkolnego", "tak")),
    ];

    private static readonly NoteSpec[] GeneralLeaveNotes =
    [
        new("Dokumentów wymienionych w załącznikach nie dołącza się, jeśli znajdują się w aktach osobowych pracownika.", $"§ 22 {R25}"),
        new("Pracodawca może zażądać przedłożenia oryginału dokumentu, którego kopię dołączono do wniosku.", $"§ 23 {R25}"),
        new("Zagraniczny dokument potwierdzający urodzenie dziecka dołącza się wraz z tłumaczeniem na język polski (wyjątki: dokument z państw UE, EFTA — strony umowy o EOG, Szwajcarii oraz państw związanych umową o zabezpieczeniu społecznym — w języku urzędowym tego państwa).", $"§ 21 {R25}"),
    ];

    public static readonly TemplateSpec FullLeave = new(
        LeaveTemplateIds.MacierzynskiIRodzicielski,
        SubmissionType.WniosekUrlop,
        SubmissionRecipient.Pracodawca,
        "Wniosek o udzielenie urlopu macierzyńskiego i urlopu rodzicielskiego (pełny wymiar)",
        "Urlop macierzyński i rodzicielski",
        [
            new("Dane wnioskodawcy", [
                Employer(),
                Place(),
                Name($"§ 2 ust. 1 pkt 1 i § 15 ust. 1 pkt 1 {R25}"),
                BirthDate(),
            ]),
            new("Część urlopu macierzyńskiego przed przewidywaną datą porodu (art. 180 § 2 KP)", [
                new("macierzynskiPrzedPorodemOd", "Data, od której ma być udzielona część urlopu macierzyńskiego przed porodem (wypełnij tylko, jeśli korzystasz z tej części)",
                    FieldKind.Date, $"§ 2 ust. 1 pkt 2 {R25}"),
            ]),
            new("Urlop rodzicielski (art. 182¹d § 1 KP)", [
                .. ParentalCore(),
            ]),
            new("Dziecko przyjęte na wychowanie", [
                Adopted($"§ 15 ust. 3 i 4 {R25}"),
                AdoptionDate($"§ 15 ust. 4 pkt 1 {R25}"),
                SchoolDeferral($"§ 15 ust. 4 pkt 2 {R25}"),
            ]),
        ],
        [
            new("Kopia zaświadczenia lekarskiego wystawionego na zwykłym druku, określającego przewidywaną datę porodu", $"§ 2 ust. 2 {R25}", new("macierzynskiPrzedPorodemOd", "*")),
            .. ParentalAttachments(),
        ],
        [
            new("W rozporządzeniu z 11.03.2025 nie ma osobnego wniosku o „pełny wymiar”. Ten dokument łączy elementy wniosku o część urlopu macierzyńskiego przed porodem (§ 2) i wniosku o urlop rodzicielski (§ 15). Urlop macierzyński po porodzie nie wymaga osobnych danych we wniosku.", $"§ 2, § 15 {R25}"),
            new("Wniosek o wypłatę zasiłku macierzyńskiego składa się osobno (do płatnika zasiłku lub do ZUS) — zob. zestawienie „Dane do wniosku ZAM”."),
            .. GeneralLeaveNotes,
        ],
        true);

    public static readonly TemplateSpec Parental = new(
        LeaveTemplateIds.Rodzicielski,
        SubmissionType.WniosekUrlop,
        SubmissionRecipient.Pracodawca,
        "Wniosek o udzielenie urlopu rodzicielskiego lub jego części",
        "Urlop rodzicielski",
        [
            new("Dane wnioskodawcy", [
                Employer(),
                Place(),
                Name($"§ 15 ust. 1 pkt 1 {R25}"),
                BirthDate(),
            ]),
            new("Urlop rodzicielski (art. 182¹d § 1 KP)", [
                .. ParentalCore(),
            ]),
            new("Łączenie urlopu rodzicielskiego z pracą (art. 182¹e § 2 KP) — wypełnij tylko, jeśli składasz taki wniosek", [
                new("lacznieZPraca", "Łączę korzystanie z urlopu rodzicielskiego z pracą u tego pracodawcy w wymiarze nie wyższym niż połowa etatu", FieldKind.Choice,
                    $"§ 16 {R25}", Options: YesNo),
                new("wymiarCzasuPracy", "Wymiar czasu pracy", FieldKind.Text, $"§ 16 pkt 2 {R25}", Required: true, When: new("lacznieZPraca", "tak"), MaxLength: 80),
                new("laczenieOd", "Okres łączenia z pracą — od", FieldKind.Date, $"§ 16 pkt 3 {R25}", Required: true, When: new("lacznieZPraca", "tak")),
                new("laczenieDo", "Okres łączenia z pracą — do", FieldKind.Date, $"§ 16 pkt 3 {R25}", Required: true, When: new("lacznieZPraca", "tak")),
                new("laczenieCzescWydluzona", "Zamierzam łączyć część urlopu rodzicielskiego powstałą w wyniku proporcjonalnego wydłużenia tego urlopu z pracą w wymiarze nie wyższym niż połowa etatu",
                    FieldKind.Choice, $"§ 16 pkt 4 {R25}", Required: true, When: new("lacznieZPraca", "tak"), Options: YesNo),
                new("laczenieCzescWydluzonaWymiar", "Wymiar czasu pracy w części wydłużonej", FieldKind.Text, $"§ 16 pkt 4 {R25}",
                    Required: true, When: new("laczenieCzescWydluzona", "tak"), MaxLength: 80),
            ]),
            new("Dziecko przyjęte na wychowanie", [
                Adopted($"§ 15 ust. 3 i 4 {R25}"),
                AdoptionDate($"§ 15 ust. 4 pkt 1 {R25}"),
                SchoolDeferral($"§ 15 ust. 4 pkt 2 {R25}"),
            ]),
        ],
        [.. ParentalAttachments()],
        [
            new("Wniosek o łączenie urlopu rodzicielskiego z pracą (§ 16) jest w rozporządzeniu odrębnym wnioskiem; w tym dokumencie można go złożyć razem z wnioskiem o urlop.", $"§ 16 {R25}"),
            .. GeneralLeaveNotes,
        ],
        true);

    public static readonly TemplateSpec Paternity = new(
        LeaveTemplateIds.Ojcowski,
        SubmissionType.WniosekUrlop,
        SubmissionRecipient.Pracodawca,
        "Wniosek o udzielenie urlopu ojcowskiego lub jego części",
        "Urlop ojcowski",
        [
            new("Dane wnioskodawcy", [
                Employer(),
                Place(),
                Name($"§ 17 ust. 1 pkt 1 {R25}"),
                BirthDate(),
            ]),
            new("Urlop ojcowski (art. 182³ § 2 KP)", [
                new("ojcowskiOd", "Urlop ojcowski lub jego część — od", FieldKind.Date, $"§ 17 ust. 1 pkt 2 {R25}", Required: true),
                new("ojcowskiDo", "Urlop ojcowski lub jego część — do", FieldKind.Date, $"§ 17 ust. 1 pkt 2 {R25}", Required: true),
                new("dzieckoPrzysposobione", "Wniosek dotyczy dziecka przysposobionego", FieldKind.Choice, $"§ 17 ust. 2 pkt 2 {R25}", Options: YesNo),
                new("korzystalZUrlopuOjcowskiego", "Oświadczenie: czy korzystałem już z urlopu ojcowskiego albo jego części", FieldKind.Choice,
                    $"§ 17 ust. 2 pkt 3 {R25}", Required: true, Options: YesNo),
                new("urlopOjcowskiOpis", "Z jakiej części / w jakim okresie korzystałem", FieldKind.Text, $"§ 17 ust. 2 pkt 3 {R25}",
                    Required: true, When: new("korzystalZUrlopuOjcowskiego", "tak"), MaxLength: 300),
            ]),
        ],
        [
            new("Odpis skrócony aktu urodzenia dziecka (dzieci) albo zagraniczny dokument potwierdzający urodzenie, albo kopie tych dokumentów", $"§ 17 ust. 2 pkt 1 {R25}"),
            new("Kopia prawomocnego postanowienia sądu o przysposobieniu dziecka", $"§ 17 ust. 2 pkt 2 {R25}", new("dzieckoPrzysposobione", "tak")),
        ],
        [.. GeneralLeaveNotes],
        true);

    public static readonly TemplateSpec Supplementary = new(
        LeaveTemplateIds.Uzupelniajacy,
        SubmissionType.WniosekUrlop,
        SubmissionRecipient.Pracodawca,
        "Wniosek o udzielenie uzupełniającego urlopu macierzyńskiego",
        "Uzupełniający urlop macierzyński",
        [
            new("Dane wnioskodawcy", [
                Employer(),
                Place(),
                new("imieNazwisko", "Imię i nazwisko pracownicy albo pracownika — ojca wychowującego dziecko", FieldKind.Text,
                    $"§ 14 ust. 1 pkt 1 {R25}", Required: true, MaxLength: 120),
                BirthDate(),
            ]),
            new("Uzupełniający urlop macierzyński (art. 180² § 4 KP)", [
                new("terminZakonczeniaMacierzynskiego", "Termin zakończenia urlopu macierzyńskiego", FieldKind.Date, $"§ 14 ust. 1 pkt 2 {R25}", Required: true),
                new("uzupelniajacyOd", "Uzupełniający urlop macierzyński — od", FieldKind.Date, $"§ 14 ust. 1 pkt 3 {R25}", Required: true),
                new("uzupelniajacyDo", "Uzupełniający urlop macierzyński — do", FieldKind.Date, $"§ 14 ust. 1 pkt 3 {R25}", Required: true),
                new("drugiRodzicBrakZamiaru", "Oświadczam, że drugi z rodziców dziecka nie zamierza korzystać z uzupełniającego urlopu macierzyńskiego ani z zasiłku macierzyńskiego za okres odpowiadający temu urlopowi",
                    FieldKind.Choice, $"§ 14 ust. 2 pkt 2 {R25}", Required: true, Options: [new("tak", "Tak, składam takie oświadczenie")]),
            ]),
            new("Dziecko przyjęte na wychowanie (art. 183 § 3¹ KP)", [
                Adopted($"§ 14 ust. 3 i 4 {R25}"),
                AdoptionDate($"§ 14 ust. 4 {R25}"),
            ]),
        ],
        [
            new("Zaświadczenie, o którym mowa w art. 180² § 5 Kodeksu pracy", $"§ 14 ust. 2 pkt 1 {R25}"),
            new("Kopia wniosku o wszczęcie postępowania w sprawie przysposobienia z poświadczeniem sądu opiekuńczego o dacie wystąpienia do sądu, albo zaświadczenie sądu, albo kopia prawomocnego orzeczenia o umieszczeniu dziecka w rodzinie zastępczej, albo kopia umowy zawartej między rodziną zastępczą a starostą",
                $"§ 14 ust. 4 {R25}", new("dzieckoPrzyjete", "tak")),
        ],
        [.. GeneralLeaveNotes],
        true);

    public static readonly TemplateSpec Zam = new(
        "dane_zam",
        SubmissionType.DaneZam,
        SubmissionRecipient.Zus,
        "Dane do wniosku o zasiłek macierzyński (ZAM) — zestawienie",
        "Dane do wniosku ZAM",
        [
            new("Płatnik składek", [
                new("platnikNipRegon", "NIP i REGON płatnika składek", FieldKind.Text, $"Zał. 6 pkt 1 {R26}", MaxLength: 80),
                new("platnikNazwaAdres", "Nazwa i adres płatnika składek", FieldKind.LongText, $"Zał. 6 pkt 2 {R26}", MaxLength: 300),
            ]),
            new("Ubezpieczony", [
                new("imieNazwisko", "Pierwsze imię i nazwisko ubezpieczonego", FieldKind.Text, $"Zał. 6 pkt 3 {R26}", Required: true, MaxLength: 120),
                new("adresZamieszkania", "Adres zamieszkania", FieldKind.Text, $"Zał. 6 pkt 3 {R26}", MaxLength: 300),
                new("objetyUbezpieczeniem", "Czy objęty ubezpieczeniem w razie choroby i macierzyństwa lub wypadkowym jako osoba z art. 6 ust. 1 pkt 5, 10 i 24 ustawy o systemie ubezpieczeń społecznych",
                    FieldKind.Choice, $"Zał. 6 pkt 4 {R26}", Options: YesNo),
            ]),
            new("Dziecko i drugi rodzic", [
                new("dzieckoImieNazwisko", "Imię i nazwisko dziecka", FieldKind.Text, $"Zał. 6 pkt 5 {R26}", MaxLength: 120),
                new("dzieckoDataUrodzenia", "Data urodzenia dziecka", FieldKind.Date, $"Zał. 6 pkt 5 {R26}"),
                new("dzieckoMiejsceUrodzenia", "Miejsce urodzenia dziecka", FieldKind.Text, $"Zał. 6 pkt 5 {R26}", MaxLength: 120),
                new("drugiRodzicDane", "Dane drugiego rodzica dziecka albo członka rodziny (imię, nazwisko, adres) — jeśli niezbędne do rozpatrzenia wniosku", FieldKind.LongText,
                    $"Zał. 6 pkt 6 {R26}", MaxLength: 300),
            ]),
            new("Świadczenie i okoliczności", [
                new("rodzajSwiadczenia", "Rodzaj świadczenia, o które ubiega się wnioskujący", FieldKind.Choice, $"Zał. 6 pkt 7 {R26}", Required: true, Options:
                [
                    new("macierzynski", "Zasiłek macierzyński za okres urlopu macierzyńskiego"),
                    new("na_warunkach_macierzynskiego", "Zasiłek macierzyński za okres urlopu na warunkach urlopu macierzyńskiego"),
                    new("uzupelniajacy", "Zasiłek macierzyński za okres uzupełniającego urlopu macierzyńskiego"),
                    new("rodzicielski", "Zasiłek macierzyński za okres urlopu rodzicielskiego"),
                    new("ojcowski", "Zasiłek macierzyński za okres urlopu ojcowskiego"),
                ]),
                new("okresOd", "Okres świadczenia — od", FieldKind.Date, $"Zał. 6 pkt 7 {R26}", Required: true),
                new("okresDo", "Okres świadczenia — do", FieldKind.Date, $"Zał. 6 pkt 7 {R26}", Required: true),
                new("podstawaWniosku", "Czego dotyczy wniosek", FieldKind.Choice, $"Zał. 6 pkt 8 {R26}", Required: true, Options:
                [
                    new("urodzenie", "Urodzenia dziecka"),
                    new("przyjecie_przysposobienie", "Przyjęcia dziecka na wychowanie i wystąpienia do sądu opiekuńczego w sprawie przysposobienia"),
                    new("rodzina_zastepcza", "Przyjęcia dziecka na wychowanie w ramach rodziny zastępczej (z wyjątkiem zawodowej)"),
                ]),
                new("zaZyciem", "Czy dziecko posiada zaświadczenie z art. 4 ust. 3 ustawy „Za życiem”", FieldKind.Choice, $"Zał. 6 pkt 9 {R26}", Options: YesNo),
                new("przewidywanaDataPorodu", "Przewidywana data porodu", FieldKind.Date, $"Zał. 6 pkt 10 {R26}"),
                new("pracodawcaUpadlosc", "Czy wniosek dotyczy sytuacji, gdy pracodawca ogłosił upadłość, likwidację, rozwiązał umowę z naruszeniem prawa lub zmarł",
                    FieldKind.Choice, $"Zał. 6 pkt 11 {R26}", Options: YesNo),
                new("szpitalDziecka", "Dane o urodzeniu i okresie pobytu dziecka w szpitalu (dotyczy zasiłku za okres uzupełniającego urlopu macierzyńskiego)", FieldKind.LongText,
                    $"Zał. 6 pkt 12 {R26}", MaxLength: 500),
                new("sytuacjaMatki", "Informacje o sytuacji matki dziecka mające wpływ na ustalenie prawa do zasiłku", FieldKind.LongText, $"Zał. 6 pkt 13 {R26}", MaxLength: 500),
                new("drugiRodzicZasilek", "Korzystanie przez drugiego rodzica z zasiłku za okres urlopu rodzicielskiego lub uzupełniającego urlopu macierzyńskiego", FieldKind.LongText,
                    $"Zał. 6 pkt 14 {R26}", MaxLength: 500),
                new("pelnyWymiar", "Czy zamierzasz korzystać z pełnego wymiaru urlopu macierzyńskiego / na warunkach urlopu macierzyńskiego i urlopu rodzicielskiego, z co najmniej jednym dniem urlopu rodzicielskiego w pierwszym roku życia dziecka",
                    FieldKind.Choice, $"Zał. 6 pkt 15 {R26}", Options: YesNo),
                new("czesciRodzicielskiegoOjcowskiego", "Informacja dotycząca części urlopu rodzicielskiego lub urlopu ojcowskiego", FieldKind.LongText,
                    $"Zał. 6 pkt 16 {R26}", MaxLength: 500),
            ]),
        ],
        [],
        [
            new("Numerów PESEL (ubezpieczonego, dziecka, drugiego rodzica) i numeru rachunku bankowego (pkt 3, 5, 6 i 17) Kompas Rodzica nie zbiera i nie drukuje — wpisz je samodzielnie we wniosku.", $"Zał. 6 pkt 3, 5, 6, 17 {R26}"),
            new("Miejscowość, data i podpis oraz pouczenie o zasiłku macierzyńskim (pkt 18 i 19) znajdują się we wniosku, który składasz w ZUS.", $"Zał. 6 pkt 18, 19 {R26}"),
            new("To nie jest druk ZUS ZAM ani jego odpowiednik. Zestawienie zawiera dane, które trzeba podać we wniosku o zasiłek macierzyński. Wniosek złożysz na stronie https://www.zus.pl (PUE ZUS) lub na druku udostępnionym przez ZUS."),
            new("Płatnik składek zgłaszający do ubezpieczeń ponad 20 osób wypłaca zasiłek sam; ZUS wypłaca zasiłek w pozostałych przypadkach. Sprawdź w wyniku oceny, gdzie złożyć wniosek."),
        ],
        false);

    public static readonly TemplateSpec Ezus = new(
        "zapytanie_ezus",
        SubmissionType.ZapytanieEzus,
        SubmissionRecipient.Ezus,
        "Zapytanie do ZUS w sprawie danych na koncie ubezpieczonego (eZUS/PUE)",
        "Zapytanie do ZUS (eZUS)",
        [
            new("Zapytanie", [
                new("imieNazwisko", "Imię i nazwisko zadającego pytanie", FieldKind.Text, Aux, Required: true, MaxLength: 120),
                new("temat", "Czego dotyczy zapytanie", FieldKind.Choice, Aux, Required: true, Options:
                [
                    new("okresy_ubezpieczenia", "Okresy i tytuły ubezpieczenia"),
                    new("podstawa_skladek", "Podstawy wymiaru składek"),
                    new("zadluzenie_skladkowe", "Zadłużenie składkowe"),
                    new("zasilek_macierzynski", "Zasiłek macierzyński i świadczenia rodzicielskie"),
                    new("e_zla", "Zwolnienia lekarskie (e-ZLA)"),
                    new("inne", "Inna sprawa"),
                ]),
                new("tresc", "Treść zapytania (bez numeru PESEL)", FieldKind.LongText, Aux, Required: true, MaxLength: 2000),
            ]),
        ],
        [],
        [
            new("Zapytanie jest symulowane — nie zostało przesłane do ZUS ani do platformy PUE. Prawdziwe zapytanie złożysz na https://www.zus.pl po zalogowaniu do PUE ZUS."),
        ],
        false);

    public static readonly TemplateSpec BirthCertificate = new(
        "odpis_aktu",
        SubmissionType.OdpisAktu,
        SubmissionRecipient.UrzadStanuCywilnego,
        "Wniosek o wydanie odpisu aktu urodzenia",
        "Odpis aktu urodzenia",
        [
            new("Wnioskodawca i dziecko", [
                new("imieNazwisko", "Imię i nazwisko wnioskodawcy", FieldKind.Text, Aux, Required: true, MaxLength: 120),
                new("dzieckoImieNazwisko", "Imię i nazwisko dziecka", FieldKind.Text, Aux, Required: true, MaxLength: 120),
                new("dzieckoDataUrodzenia", "Data urodzenia dziecka", FieldKind.Date, Aux, Required: true),
                new("miejsceUrodzenia", "Miejsce urodzenia dziecka", FieldKind.Text, Aux, MaxLength: 120),
            ]),
            new("Odpis", [
                new("rodzajOdpisu", "Rodzaj odpisu", FieldKind.Choice, $"odpis skrócony wymieniają m.in. § 5, § 6, § 15 ust. 2 pkt 1 i § 17 ust. 2 pkt 1 {R25}", Required: true,
                    Options: [new("skrocony", "Odpis skrócony"), new("zupelny", "Odpis zupełny")]),
                new("liczbaOdpisow", "Liczba odpisów", FieldKind.Integer, Aux, Required: true),
                new("urzad", "Urząd stanu cywilnego (miejsce rejestracji urodzenia)", FieldKind.Text, Aux, MaxLength: 150),
            ]),
        ],
        [],
        [
            new("Wniosek jest symulowany — nie został przesłany do urzędu stanu cywilnego. Informacje o rejestrze stanu cywilnego: https://www.gov.pl/web/cyfryzacja/rejestr-stanu-cywilnego"),
            new("Rozporządzenie z 11.03.2025 wymienia odpis skrócony aktu urodzenia jako dokument dołączany do wniosków o urlopy; treści samego wniosku o odpis nie określa."),
        ],
        false);

    public static readonly IReadOnlyList<TemplateSpec> Leave = [FullLeave, Parental, Paternity, Supplementary];

    public static readonly IReadOnlyList<TemplateSpec> All = [.. Leave, Zam, Ezus, BirthCertificate];

    public static TemplateSpec? Resolve(SubmissionType type, string? template) => type switch
    {
        SubmissionType.WniosekUrlop => Leave.FirstOrDefault(t => t.Id == template),
        SubmissionType.DaneZam => Zam,
        SubmissionType.ZapytanieEzus => Ezus,
        SubmissionType.OdpisAktu => BirthCertificate,
        _ => null,
    };
}
