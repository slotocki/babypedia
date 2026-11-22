# Kompas Rodzica

Aplikacja WWW dla przyszłych rodziców. Dla konkretnej osoby ustala, które zasiłki z ubezpieczenia chorobowego związane z rodzicielstwem przysługują, a które nie i dlaczego. Wylicza okres i wysokość, podaje terminy, dokumenty i miejsce złożenia wniosku. Pozwala sprawdzić skutki zmian („co jeśli”).

Projekt na HackNation 2026, zadanie ZUS „Zasiłkowy Przewodnik Przyszłego Rodzica”. Zespół 2Stosy: Sylwester Łotocki, Andrzej Laskowski.

- Demo: https://kompasrodzica.polandcentral.cloudapp.azure.com
- Repozytorium: https://github.com/slotocki/babypedia
- Panel `/admin`: hasło ustawia zmienna `ADMIN_PASSWORD`; jury dostaje je osobno.

Uprawnienia, okresy, kwoty i terminy liczy deterministyczny silnik reguł w C#. Model językowy nie bierze udziału w decyzji ani w kwocie. Integracje z mObywatelem, ZUS, e-ZLA, rejestrem stanu cywilnego i wysyłka wniosków są symulowane i oznaczone na ekranie. Osoby testowe są fikcyjne.

Nazwa techniczna: produkt nazywa się Kompas Rodzica. Wcześniejsza nazwa robocza BabyRadnik została w nazwach technicznych (przestrzenie nazw `BabyRadnik.*`, plik `BABYRADNIK_INSTRUKCJA.md`, projekt compose, baza, katalog na serwerze).

## Uruchomienie lokalne

### Sposób 1: bez Dockera i bez bazy danych (najprostszy)

Wymagania: .NET SDK 10 (wersja w [global.json](global.json), 10.0.100 lub nowsza) i Node.js 22.

```bash
git clone https://github.com/slotocki/babypedia.git && cd babypedia
ADMIN_PASSWORD=demo dotnet run --project api/Api     # API: http://localhost:5080
# w drugim terminalu:
cd web && npm ci && npm run dev                      # aplikacja: http://localhost:5173
```

W PowerShell pierwsza komenda to `$env:ADMIN_PASSWORD='demo'; dotnet run --project api/Api`.

- Bez zmiennej `ConnectionStrings__Default` API używa bazy w pamięci (EF Core InMemory). Parametry wczytują się z `fixtures/parameters.seed.json` przy każdym starcie. Zmiany z panelu `/admin` i zgłoszenia giną po restarcie API.
- `ADMIN_PASSWORD` jest opcjonalne. Bez niego panel `/admin` jest wyłączony.
- `CASES_PRIVATE_DIR=<katalog>` jest opcjonalne. Wskazuje katalog ze sprawami ZUS spoza repozytorium (patrz „Sprawy testowe ZUS”). Bez niego lista spraw jest pusta. Sprawy ZUS można sprawdzić na demo.
- Czat „Zapytaj o przepisy” działa bez klucza API: odpowiada z FAQ i pokazuje wyniki wyszukiwania.
- Frontend na :5173 przekazuje `/api` do API na :5080.

### Sposób 2: Docker Compose (z PostgreSQL)

Wymagany Docker z Docker Compose.

```bash
git clone https://github.com/slotocki/babypedia.git && cd babypedia
cp .env.example .env        # opcjonalnie; PowerShell: Copy-Item .env.example .env
docker compose up -d --build
```

Aplikacja: http://localhost:8080, API: http://localhost:5080, stan: http://localhost:8080/health. Pierwsze budowanie trwa kilka minut.

- Plik `.env` nie jest wymagany. Bez niego działają wartości domyślne, a `POSTGRES_PASSWORD` to `babyradnik`. Jeśli zmienisz hasło bazy po pierwszym starcie, usuń lokalny wolumen bazy (na własnym komputerze: `docker compose down -v`).
- W `.env` warto ustawić `ADMIN_PASSWORD` (panel `/admin`) i ewentualnie `CASES_PRIVATE_DIR`. Przy zajętym porcie zmień `WEB_PORT` lub `API_PORT`.
- `compose.yaml` w katalogu głównym dołącza `ops/docker-compose.yml` (postgres, api, web). Profil `proxy` (HTTPS przez Caddy, wymaga `DOMAIN`) służy do wdrożenia publicznego (`ops/deploy.sh`).

### Testy i walidacja

```bash
dotnet test --solution api/BabyRadnik.slnx                         # build + testy API
cd web && npx tsc -b && npm run lint && npm run build              # typy, lint, build frontendu
npm ci --prefix contracts && npm --prefix contracts run validate   # fixtures zgodne ze schematami
```

## Jak przetestować w 3 minuty

Na demo albo lokalnie.

1. **Start** (`/`): „Sprawdź, co Ci przysługuje” i trzy drogi: „Wprowadź dane ręcznie”, „Połącz się przez mObywatela i ZUS”, „Sprawdź na rzeczywistych przykładach”.
2. **Logowanie (symulacja)**: „Połącz się przez mObywatela i ZUS”, wybierz osobę testową (np. Anna), „Zaloguj przez mObywatela”. W kroku „Zgoda na pobranie danych” wybierz źródła i kliknij „Połącz i pobierz dane (symulacja)”.
3. **Sprawdź dane** (`/sprawdz-dane`): każda informacja ma źródło i przycisk „Popraw”. Kliknij „Potwierdź dane”. Jeśli pojawią się pytania o brakujące dane, odpowiedz na nie. Potem „Pokaż wynik”.
4. **Twój plan** (`/wynik`):
   - „W skrócie” na górze: co przysługuje, w jakiej kwocie, czego brakuje.
   - Karty świadczeń: status, okres, kwota. Przy urlopie rodzicielskim karta pokazuje „Twoja część” i wymiar łączny obojga rodziców.
   - „Szczegóły świadczenia”: kto wypłaca, gdzie złożyć wniosek, dokumenty, „Co znaczą te pojęcia” (ZAM, płatnik składek, e-ZLA, Z-3…) i „Ścieżka decyzji” (6 najważniejszych kroków, reszta pod „Pokaż wszystkie kroki”).
   - „Co dalej?”: najbliższy krok, terminy, wnioski. Wariant wypłaty 81,5% wybiera się w „Ryzyka i wariant wypłaty”.
5. **Odmowa**: zaloguj osobę Edyta („Zmień profil lub sprawę”). Karta „nie przysługuje” podaje powód, przepis i następny krok („To da się naprawić”: kwota do spłaty i termin).
6. **Porównaj warianty** (`/porownaj-warianty`): np. „Wariant wypłaty” → „Uśredniony” albo „Spłacam zadłużenie”. Na górze jedno zdanie podsumowania, niżej różnica w tygodniach, złotych i statusach.
7. **Sprawy ZUS** (`/sprawy`, link „Sprawy ZUS (dla oceniających)” w stopce albo trzeci kafelek na starcie): wybierz sprawę z listy albo dodaj jej PDF. Wynik ma ramkę „Porównanie z decyzją ZUS (sprawa testowa)” z okresami z akt obok okresów z silnika.
8. Dalej: **Wnioski** (`/wnioski`), **Moje dane w ZUS** (`/moje-dane-zus`), **Wsparcie** (`/wsparcie`), **Baza wiedzy** (`/baza-wiedzy`), czat „Zapytaj o przepisy” i panel **`/admin`**.

### Fikcyjne osoby demo

Pliki w `fixtures/personas/`. Opis wynika z danych osoby i z reguł; „oczekiwane” oznacza wynik zgodny z przypadkami referencyjnymi.

| Osoba | Co pokazuje |
|---|---|
| `anna-przyklad` | Pracownica na umowie o pracę od 2021 r., ciąża z terminem 15.03.2027. Płatnik zgłasza 150 osób, więc zasiłek wypłaca płatnik. Ścieżka standardowa: macierzyński, rodzicielski, terminy. |
| `klara-demo` | Pracownica u dużego płatnika (240 osób), zasiłek wypłaca płatnik. |
| `halina-demo` | Pracownica małego płatnika (12 osób): zasiłek wypłaca ZUS, wniosek przez pracodawcę. Ma zwolnienie e-ZLA z kodem B (ciąża). |
| `monika-demo` | Po porodzie (8.10.2026), dziecko widoczne w rejestrze stanu cywilnego, płatnik 8 osób. Terminy liczone od daty porodu. |
| `edyta-demo` | Działalność z dobrowolnym chorobowym i zadłużeniem składkowym 120,00 zł. Oczekiwane: odmowa z powodem, kwotą do spłaty i datą graniczną; scenariusz „Spłacam zadłużenie”. Zasiłek wypłaca ZUS. Ma zaświadczenie o stanie składek (PDF, symulacja). |
| `weronika-demo` | Zleceniobiorczyni z dobrowolnym chorobowym od 22.09.2026 (poniżej 90 dni wyczekiwania) i e-ZLA z kodem B. Oczekiwane: zasiłek chorobowy w ciąży odmowny z powodu okresu wyczekiwania. |
| `tadeusz-demo` | Ojciec-pracownik, liczymy „dla mnie i drugiego rodzica”. Prawo ojca, gdy matka nie ma ubezpieczenia chorobowego. |
| `olga-demo` | Partnerka Tadeusza, bez ubezpieczenia chorobowego (rejestr ZUS zwraca pustą listę tytułów). Pokazuje odmowę i „Co musiałoby się zmienić”. |

## Sprawy testowe ZUS

Ekran `/sprawy` pokazuje listę spraw z zestawu ZUS i pozwala dodać plik PDF sprawy. Plik jest rozpoznawany po sumie SHA-256 liczonej w przeglądarce (zapasowo: nazwa pliku i liczba stron). Do API trafia tylko suma, plik nie opuszcza przeglądarki. Podgląd pokazuje pdf.js. Aplikacja nie czyta treści PDF (bez OCR). Nieznany plik daje podgląd i komunikat „Nie znam tej sprawy, wprowadź dane”. Po wybraniu sprawy wczytują się dane odczytane z jej dokumentów, a wynik ma ramkę „Porównanie z decyzją ZUS (sprawa testowa)”.

Każda sprawa ma oznaczenie, co sprawdzono z dokumentami ZUS:

| Oznaczenie | Znaczenie | Sprawy na demo (22.11.2026) |
|---|---|---|
| Okres i kwota zgodne z wydrukiem ZUS | podstawa wymiaru, stawka dzienna i okres zgadzają się z wydrukiem ZUS w aktach | 1 |
| Okres zgodny z ZUS co do dnia | okresy z silnika równe okresom z dokumentów; kwoty ZUS w aktach nie ma | 8 |
| Odmowa zgodna z decyzją ZUS | silnik odmawia tak jak ZUS | 2 |

Skąd są dane:

- Dokumentów ZUS nie wolno rozpowszechniać. Dlatego w repozytorium nie ma ani PDF-ów, ani odczytów z nich. Przypadki referencyjne i testy nie zawierają imion ani nazw plików spraw. `fixtures/cases.json` jest pusty.
- Sprawy ładują się z katalogu `CASES_PRIVATE_DIR` poza repozytorium (w kontenerze `/cases`, tylko do odczytu). Bez niego lista spraw jest pusta. Na demo katalog jest ustawiony.
- Dokumenty odczytano z pomocą modeli językowych, poza aplikacją. Wynik przeliczono na format aplikacji skryptem `tools/cases/convert.mjs`. Skrypt niczego nie zgaduje: brakujące pole zostaje pytaniem do użytkownika. Raport „silnik a decyzje ZUS” tworzy test `PrivateCasesReportTests` (ze zmienną `CASES_REPORT_DIR`).
- W większości akt kwota zasiłku i dane płatnika są zaczernione albo ich nie ma. Kwotę da się porównać z ZUS w jednej sprawie. W dwóch kolejnych silnik liczy ją z wynagrodzeń z zaświadczenia płatnika, a w pozostałych pyta o podstawę.
- Aplikacja może dopytać o dane, których w aktach nie ma (np. liczba ubezpieczonych u płatnika, data pochówku).
- Na 43 przeanalizowane sprawy (z 87) silnik rozstrzyga inaczej niż ZUS w jednej: umowę o pracę ZUS uznał za zawartą dla pozoru. To ustalenie z kontroli, którego nie da się wyprowadzić z danych ubezpieczenia.

## Mapa wymagań

Oznaczenia z zadania ZUS (pkt 3) i instrukcji projektu (sekcja 4). Status według kodu na gałęzi `main` (22.11.2026).

| ID | Wymaganie | Ekran | Endpoint | Moduł kodu | Status |
|---|---|---|---|---|---|
| a | Świadczenia w ciąży, także brak uprawnień (chorobowy, rehabilitacyjne, zasiłek do dnia porodu) | Twój plan (`/wynik`), karty świadczeń | `POST /api/assessment` | `api/Domain/Eligibility`, `api/Domain/Catalog` | Częściowo: świadczenie rehabilitacyjne po wyczerpaniu zasiłku zawsze kończy się „nie rozstrzygam” (zależy od orzeczenia lekarza orzecznika ZUS). Pozostałe działają |
| b | Kalkulator zasiłku macierzyńskiego dla obojga rodziców: okres, wysokość, liczba dzieci, wiek, „Za życiem” | Twój plan, Porównaj warianty | `POST /api/assessment` | `api/Domain/Periods`, `api/Domain/Amounts` | Działa. Kwota przy znanej podstawie (z ZUS albo wpisanej); bez podstawy są okresy i stawki. Karta urlopu rodzicielskiego pokazuje „Twoja część” (kwota i tygodnie tej osoby) i wymiar łączny obojga rodziców. Wariant 81,5% wybiera się na wyniku. Podwyższenia do kwoty świadczenia rodzicielskiego (art. 31 ust. 3a) aplikacja nie wylicza, tylko o nim informuje |
| c | Łączenie pracy z urlopem rodzicielskim | Porównaj warianty („Praca na część etatu w czasie urlopu rodzicielskiego”), wniosek o urlop rodzicielski | `POST /api/assessment/compare` | `api/Domain/Scenarios`, reguły `rodzicielski_wydluzenie_praca`, `praca_na_czesc_etatu` | Działa |
| d | Symulacja korzystania przez oboje rodziców | Porównaj warianty („Drugi rodzic bierze część urlopu rodzicielskiego”) | `POST /api/assessment/compare` | `api/Domain/Scenarios` | Działa (9 tygodni nieprzenoszalnych, najwyżej 5 części) |
| e, j | Rodzice adopcyjni i rodziny zastępcze | Start (wpis ręczny: „przyjmuję dziecko”), Twój plan | `POST /api/assessment` | `api/Domain/Periods/Rules/PrzyjecieRules.cs` | Działa (przysposobienie, rodzina zastępcza niezawodowa; zawodowa: odmowa) |
| f | Indywidualna podstawa wymiaru | Twój plan (pole „Podstawa ustalona przez płatnika”), Sprawdź dane | `POST /api/assessment` | `api/Domain/Basis`, `api/Domain/Amounts` | Częściowo: silnik liczy podstawę na poziomie 1 (średnia z 12 miesięcy dla pracownika i niepracownika, art. 48a/49) albo przyjmuje podstawę wpisaną przez użytkownika. Przy zmianie wymiaru etatu, zbiegu tytułów, zmianie pracodawcy lub zatrudnieniu krótszym niż miesiąc prosi o podstawę ustaloną przez płatnika. Kwotę z decyzją ZUS porównano w 1 sprawie testowej (w pozostałych akta nie mają kwoty) |
| g | Dokumenty, terminy, sposób i miejsce złożenia wniosku | Twój plan („Co zrobić i do kiedy”, „Gdzie złożyć wniosek”, „Dokumenty”), Wnioski (daty z planu) | `POST /api/assessment` | `api/Domain/Deadlines`, `api/Domain/Eligibility` (`platnik_zasilku_i_miejsce_zlozenia`) | Działa. Nazwy druków (ZAM, Z-3, Z-3a, ZAS-53, ZAS-12) według treści zadania ZUS. ZAS-12 (zaświadczenie płatnika za dalszy okres niezdolności do pracy) pojawia się przy zasiłku chorobowym wypłacanym przez ZUS |
| h | Zagrożenia: utrata uprawnień, zmiana wymiaru lub wysokości | Twój plan („Na co uważać”), Porównaj warianty | `POST /api/assessment`, `/compare` | `api/Domain/Catalog/Risks.cs`, `api/Domain/Scenarios` | Działa (ryzyka pokazywane tylko, gdy pasują do faktów; informacja, nie ocena) |
| i | Podmioty wyłącznie charytatywne w sytuacjach szczególnych | Wsparcie (`/wsparcie`) | `GET /api/organizations?situation=` | `api/Infrastructure/Parameters/AdminSettingsService.cs`, `web/src/features/support` | Działa. Publiczne API pokazuje tylko organizacje zweryfikowane (lista startowa w `fixtures/organizations.json`, opis weryfikacji w [fixtures/organizations-verification.md](fixtures/organizations-verification.md)). Listę można zmienić w zakładce „Organizacje” panelu `/admin` |
| S1 | Logowanie przez mObywatel (symulacja) | Logowanie | `POST /api/sim/login`, `GET /api/sim/personas` | `api/Infrastructure/Simulation`, `web/src/features/intake` | Działa |
| S2 | Dane z ZUS o ubezpieczeniu lub jego braku (symulacja) + wpis ręczny | Sprawdź dane | `GET /api/sim/me/facts` | `api/Infrastructure/Simulation`, `web/src/features/review` | Działa: dane z symulatora, wpis ręczny i przełączniki pól integracji w panelu `/admin` (wyłączone pole wraca jako pytanie do użytkownika) |
| S3 | Zadłużenie składkowe na dzień powstania prawa (symulacja zaświadczenia i kwoty) | Moje dane w ZUS (saldo), Wnioski („Zaświadczenie o stanie składek”) | `GET /api/sim/me/zus`, `GET /api/sim/me/debt-certificate` | `api/Infrastructure/Submissions/DebtCertificate.cs`, reguła `zadluzenie_skladkowe` | Działa (PDF z napisem „SYMULACJA” i datą stanu salda) |
| S4 | Podgląd e-ZLA (symulacja) | Moje dane w ZUS („Zwolnienia lekarskie (e-ZLA)”) | `GET /api/sim/me/zus` | `api/Infrastructure/Simulation` | Działa |
| S5 | PUE/eZUS: zapytanie, podgląd danych konta (symulacja) | Wnioski („Zapytanie do ZUS”), Moje dane w ZUS | `POST /api/submissions`, `GET /api/sim/me/zus` | `api/Infrastructure/Submissions`, `web/src/features/zus-data` | Działa |
| S6 | Wypełnienie wniosków o urlopy i świadczenia, wysyłka do pracodawcy lub ZUS (symulacja) | Wnioski | `POST /api/submissions`, `GET /api/submissions`, `GET /api/submissions/{id}/pdf` | `api/Infrastructure/Submissions`, `web/src/features/submissions` | Działa: cztery wzory wniosków o urlop (PDF) i „Dane do wniosku o zasiłek macierzyński (ZAM)” (zestawienie, nie druk ZUS). Daty podpowiada plan, a pola bez danych zostają puste do uzupełnienia odręcznie. Wysyłka jest symulowana: potwierdzenie z numerem i wpis w „Moje zgłoszenia” |
| S7 | Wniosek o odpis aktu urodzenia (symulacja) | Wnioski | `POST /api/submissions` | `api/Infrastructure/Submissions` | Działa |
| S8 | Informacja o podmiotach wspierających | Wsparcie | `GET /api/organizations` | jak w wierszu `i` | Działa. Katalog organizacji z filtrem „Twoja sytuacja” i kontakt do ZUS. W trudnej sytuacji (np. martwe urodzenie) na górze jest „Co możesz zrobić teraz” i wsparcie, a informacja o zasiłku pogrzebowym pojawia się tylko przy martwym urodzeniu lub zgonie dziecka |
| S9 | Przekierowania do zus.pl, MRPiPS, PIP | Baza wiedzy („Oficjalne źródła”; tam też Poradnik ZUS dla rodziców w PDF ze spisem rozdziałów) | brak (linki) | `web/src/features/support/links.tsx`, `web/src/features/support/KnowledgePage.tsx` | Działa |
| S10 | Panel administracyjny do zmiany parametrów | `/admin` | `GET/PUT /api/admin/parameters`, `GET /api/admin/parameters/history`, `/api/admin/catalog` | `api/Infrastructure/Parameters`, `web/src/features/admin` | Działa: parametry z polskimi nazwami w grupach, z datą obowiązywania i historią zmian; włączanie świadczeń; zakładki „Integracje” i „Organizacje” (patrz S2 i `i`) |
| T1 | Pełna responsywność | cały interfejs | brak | `web/src/ui`, `web/src/app/Layout.tsx` | Działa: sprawdzone automatycznie przy 320, 390 i 768 px bez przewijania w poziomie |
| T2 | Demo: upload lub wybór przykładowych spraw; weryfikacja ubezpieczenia i kalkulator | Sprawy ZUS (lista i dodanie PDF), Start (trzeci kafelek) | `GET /api/cases`, `GET /api/cases/by-hash/{sha256}` | `web/src/features/cases`, `api/Infrastructure/Simulation/Catalogs.cs`, `tools/cases` | Częściowo: rozpoznawanych jest 11 z 87 plików z zestawu ZUS (patrz „Sprawy testowe ZUS”). Pozostałe pliki dają podgląd i komunikat „Nie znam tej sprawy, wprowadź dane”; dane można wtedy wpisać ręcznie |

Dodatkowo: przy martwym urodzeniu, zgonie dziecka, wcześniactwie z hospitalizacją i ciężkiej chorobie interfejs zmienia ton i stawia wsparcie na górze (`web/src/ui/ToneScope.tsx`, `web/src/features/support`).

## Architektura w skrócie

Modularny monolit: React + TypeScript (Vite) i ASP.NET Core Web API (.NET 10), PostgreSQL przez EF Core (lokalnie także baza w pamięci), Docker Compose. REST/JSON.

```
contracts/   kontrakt: openapi.yaml, schematy JSON, przykłady, walidacja fixtures
fixtures/    parameters.seed.json, organizations.json, personas/, reference/, cases.json (pusty — sprawy ZUS są poza repozytorium)
tools/       cases/convert.mjs — przeliczenie odczytów spraw ZUS na format aplikacji
api/Domain/  czysty C# (bez bazy i HTTP): Periods, Amounts, Eligibility, Basis, Catalog, Deadlines, Scenarios, Contracts
api/Infrastructure/  Parameters, Simulation, Submissions, Persistence, Assistant
api/Api/     Program.cs i endpointy
api/Tests/   testy modułów + testy referencyjne
web/src/     app (router, klient API), ui (komponenty), features (intake, review, result, scenarios, submissions, zus-data, cases, support, admin)
ops/         docker-compose.yml, proxy (Caddy), deploy.sh
docs/        zgłoszenie, zrzuty, wyciągi przepisów i FAQ czatu (docs/prawo)
```

Zasady:

- Decyzje i kwoty liczy wyłącznie deterministyczny silnik reguł. Każda reguła to mała klasa z `Id`, `PodstawaPrawna` i metodą oceny (w `api/Domain` jest ich ponad 80). Kwoty w `decimal`, daty w `DateOnly`.
- Parametry prawne (wymiary, stawki, progi, płaca minimalna) są w bazie z datą obowiązywania. `IParameterProvider.Get(klucz, data)` zwraca wartość z największą datą `validFrom` nie późniejszą niż dzień, którego dotyczy ocena. Dane startowe: `fixtures/parameters.seed.json` (102 wpisy). Brak parametru daje „nie rozstrzygam”.
- Silnik dostaje fakty z pochodzeniem (`mobywatel`, `zus`, `ezla`, `usc`, `user`, `assumption`). Brakujący fakt wraca jako gotowe pytanie.
- Pięć interfejsów integracji, każdy z implementacją `Simulated…` nad jednym zbiorem `fixtures/personas`, gotowych do podmiany na prawdziwe usługi: `IIdentityProvider`, `IInsuranceRegistry`, `IContributionAccount`, `IMedicalCertificates`, `ISubmissionGateway`. Do tego rejestr stanu cywilnego (`ICivilRegistry`) w `api/Infrastructure/Simulation`.
- Kontrakt (OpenAPI, schematy, typy C# i TypeScript) opisuje [docs/KONTRAKT.md](docs/KONTRAKT.md).

## Testy

Wynik `dotnet test --solution api/BabyRadnik.slnx` na gałęzi `main` (.NET 10):

```
Podsumowanie przebiegu testu: Powodzenie!
  suma: TESTY_SUMA
  zakończone niepowodzeniem: 0
  zakończone powodzeniem: TESTY_ZIELONE
  pominięto: 1
```

Pominięty test to raport dla spraw ZUS z katalogu prywatnego (`PrivateCasesReportTests`), uruchamiany tylko ze zmienną `CASES_REPORT_DIR`.

Przypadki referencyjne (`fixtures/reference/`): 55 plików — okresy 30, uprawnienia 15, kwoty 8, parametry 2. Pozostałe moduły (podstawa wymiaru, katalog, terminy, scenariusze, wnioski, symulacja, administracja, czat i FAQ) mają testy w `api/Tests/Domain/**`, `api/Tests/Infrastructure/**` i `api/Tests/Api/**`.

Testy referencyjne powstają przed regułami, a reguła jest gotowa, gdy jej przypadki są zielone. Przypadek to plik `{id, opis, fakty, oczekiwane}`; testy silnika czytają je z `fixtures/reference/` (ewaluator wybierany polem `modul`, porównanie częściowe). Format opisuje [docs/KONTRAKT.md](docs/KONTRAKT.md), sekcja 6. CI (`.github/workflows/ci.yml`) buduje API, uruchamia testy, waliduje fixtures schematami i buduje frontend.

## Wykorzystanie AI

**W działającej aplikacji: tylko czat „Zapytaj o przepisy”.** Uprawnienia, okresy, kwoty i terminy liczy deterministyczny silnik reguł w C# (`api/Domain`). Żaden model językowy nie bierze udziału w decyzji ani w kwocie. Aplikacja nie używa OCR i nie czyta treści PDF.

Czat jest na ekranach Twój plan, Wsparcie i Baza wiedzy. Odpowiada w tej kolejności:

1. **Gotowe odpowiedzi (FAQ), bez modelu.** 71 typowych pytań w [docs/prawo/faq.json](docs/prawo/faq.json). Każda odpowiedź ma dosłowne cytaty z materiałów ZUS i przepisów. Test `FaqTests` sprawdza, że każdy cytat występuje słowo w słowo we wskazanym wyciągu. Dopasowanie pytania do FAQ jest deterministyczne.
2. **Wyszukiwarka, bez modelu.** Gdy pytanie nie pasuje do FAQ, wyszukiwarka BM25 przeszukuje wyciągi przepisów i materiały ZUS w `docs/prawo` i pokazuje pasujące fragmenty.
3. **Model językowy, opcjonalnie.** Gdy ustawiony jest klucz API (domyślnie Claude Haiku przez Anthropic API), model pisze krótką odpowiedź prostym językiem i wybiera do 5 cytatów spośród znalezionych fragmentów. Aplikacja pokazuje tylko cytaty zgodne słowo w słowo z tekstem w repozytorium, a wyjaśnienie tylko obok nich, z dopiskiem „Wyjaśnienie napisał model — wiążące są przepisy”. Odpowiedź z kwotą, linkiem, danymi osobowymi albo zbyt długą aplikacja zastępuje zdaniem ogólnym. Przy błędzie dostawcy, odrzuconym cytacie lub wyczerpaniu limitu czat pokazuje wyniki wyszukiwania.

Bez modelu czat odpowiada z FAQ i pokazuje wyniki wyszukiwania. Źródła odpowiedzi są zwinięte do krótkich etykiet; po rozwinięciu widać cytat i cały fragment aktu. Pytania o własną sprawę czat kieruje do kalkulatora. Konfiguracja: [docs/PLAN_POMOCNIK.md](docs/PLAN_POMOCNIK.md).

**Przygotowanie danych testowych.** Dokumenty spraw ZUS (skany) odczytano z pomocą modeli językowych, poza aplikacją. Odczyty są w katalogu prywatnym, a w aplikacji służą tylko jako dane wejściowe silnika.

**Grafika.** Ilustrację na ekranie startowym (`web/public/kompas-start.png`) wygenerował model AI. Nie pochodzi z banku zdjęć ani z cudzej pracy.

**Wytworzenie projektu.** Kod, testy, treść FAQ i dokumentację wytworzyły modele językowe (głównie Claude firmy Anthropic w narzędziu Claude Code), pracujące jako zespół agentów pod nadzorem ludzi. Osobny model przeglądał zmiany silnika. Ludzie dostarczyli materiały prawne, zdecydowali o zakresie i weryfikowali wyniki. Reguły pisano z wyciągów przepisów w `docs/prawo/`, nie z pamięci modelu. Spójność wyciągów z instrukcją opisuje [docs/prawo/ROZBIEZNOSCI.md](docs/prawo/ROZBIEZNOSCI.md).

## Komponenty zewnętrzne i licencje

Pełna lista z wersjami: [docs/ZALEZNOSCI.md](docs/ZALEZNOSCI.md). Zasada projektu: licencje permisywne (MIT, Apache-2.0, BSD i równoważne).

| Komponent | Licencja |
|---|---|
| ASP.NET Core / .NET 10, Entity Framework Core (w tym InMemory) | MIT |
| Npgsql i Npgsql.EntityFrameworkCore.PostgreSQL (sterownik PostgreSQL) | PostgreSQL License — permisywna, zatwierdzona przez OSI, w treści równoważna MIT/BSD |
| PDFsharp-MigraDoc 6.2.4 (generowanie PDF) | MIT (wersje 6.5.0 i 7.x mają AGPL-3.0, dlatego zostajemy przy 6.2.4) |
| Font Roboto (osadzony w PDF) | Apache-2.0 |
| xunit.v3, xunit.runner.visualstudio | Apache-2.0 |
| React, React Router, TanStack Query, React Hook Form, Zod, Radix UI, clsx, tailwind-merge | MIT |
| class-variance-authority, pdfjs-dist (podgląd PDF), TypeScript | Apache-2.0 |
| Vite, Tailwind CSS 4, ESLint | MIT. Tailwind 4 używa przy budowaniu `lightningcss` (MPL-2.0); to narzędzie budowania, nie trafia do aplikacji, a MPL-2.0 nie nakłada obowiązków na nasz kod |
| ajv, ajv-formats, js-yaml (walidacja kontraktu, tylko dev) | MIT |
| Obrazy kontenerów: postgres 17 (PostgreSQL License), nginx 1.27 (BSD-2-Clause), caddy 2.10 (Apache-2.0), ollama 0.9.6 (MIT; profil opcjonalny, nieużywany na demo) | jak w nawiasie |
| Usługi: Claude Haiku przez Anthropic API (opcjonalnie, czat), Microsoft Azure (maszyna wirtualna demo), Let's Encrypt (certyfikat HTTPS) | warunki dostawców |
| Ilustracja startowa wygenerowana przez AI, Poradnik ZUS dla rodziców (PDF z materiałów zadania, w Bazie wiedzy) | — |

## Bezpieczeństwo i dane

- Wszystkie osoby demo są fikcyjne. PESEL występuje tylko w odpowiedzi symulowanego logowania, jest fikcyjny (nie przechodzi walidacji sumy kontrolnej), służy do wyliczenia wieku i nie trafia do faktów silnika. Użytkownik go nie wpisuje, a formularze wniosków odrzucają 11-cyfrowe numery.
- Dokumenty ZUS (PDF spraw) i odczyty z nich nie są w repozytorium. Dane osobowe w tych dokumentach są zaczernione. W aplikacji sprawa testowa ma tylko tytuł z nazwy pliku z zestawu ZUS i umowny PESEL. `.gitignore` wyklucza `/Sprawy/`, `/sprawy/`, `/cases-private/`, `/fixtures/personas-private/` i `*.sprawa.pdf`. Dane spraw wchodzą z katalogu `CASES_PRIVATE_DIR` (montowanego tylko do odczytu). Plik PDF dodany na ekranie „Sprawy ZUS” nie opuszcza przeglądarki.
- Sekrety (`ADMIN_PASSWORD`, `POSTGRES_PASSWORD`, klucze API) są tylko w `.env`, który jest w `.gitignore`. W repozytorium jest `.env.example`. Panel `/admin` jest wyłączony, gdy `ADMIN_PASSWORD` nie jest ustawione; hasło idzie w nagłówku `X-Admin-Password`.
- Czat odrzuca pytania z ciągami przypominającymi PESEL, telefon, numer rachunku lub e-mail.
- Wynik jest informacją, nie decyzją: aplikacja nie orzeka i nie ocenia użytkownika.

## Ograniczenia i założenia

### Zakres

Aplikacja nie ma OCR ani parsera PDF, bazy wektorowej, kont z hasłami, powiadomień e-mail i SMS, kwot netto, świadczeń spoza ubezpieczenia chorobowego (800+, żłobkowe), etapów rozwoju dziecka ani wersji językowych. Informuje i nie wydaje decyzji.

Symulowane są: logowanie mObywatel, konto ubezpieczonego i płatnika w ZUS, e-ZLA, rejestr stanu cywilnego, wysyłka wniosków do pracodawcy, ZUS, PUE i urzędu stanu cywilnego, zaświadczenie o stanie składek. Wnioski PDF i zestawienie „Dane do wniosku o zasiłek macierzyński (ZAM)” nie są drukami urzędowymi.

### Przyjęte założenia

Tam, gdzie materiały zadania nie rozstrzygają kwestii, przyjęliśmy jawne założenia. Każde jest parametrem w panelu `/admin`, więc zmienia się je bez zmiany kodu. Źródła: [docs/prawo/ROZBIEZNOSCI.md](docs/prawo/ROZBIEZNOSCI.md).

- **Zaokrąglanie.** Stawka dzienna = podstawa × stopa ÷ 30, zaokrąglona raz do 0,01 zł. Kwota za okres = stawka dzienna × liczba dni. Tak liczą wyliczenia ZUS w sprawach testowych; wyciągi przepisów nie podają reguły (parametry `zaokraglanie.*`).
- **Kwoty brutto.** Kwoty są brutto, z dopiskiem o zaliczce na podatek (parametr `kwoty`). Podstawę liczymy według ustawy: wynagrodzenie po odliczeniu 13,71% składek. Poradnik ZUS odejmuje także zaliczkę na podatek, ale stosujemy akt prawny.
- **Minimalna podstawa.** Dla pełnego etatu 4 147,10 zł (płaca minimalna 4 806 zł po odliczeniu 13,71%, art. 45 ust. 1). Przy niepełnym etacie stosujemy ją proporcjonalnie do wymiaru czasu pracy, bo przepis mówi tylko o pełnym etacie.
- **Druki.** Z-3, Z-3a, ZAS-53 i połączenie wniosków w ZAM od 24.10.2026 stosujemy według treści zadania ZUS (Poradnik opisuje jeszcze Z-3b). ZAS-12 opisujemy według treści zadania i druku w aktach spraw.
- **Zasiłek do dnia porodu.** Stosujemy art. 30 ust. 3 ustawy: śmierć, upadłość i likwidacja pracodawcy. Treść zadania wymienia też rozwiązanie umowy z naruszeniem prawa stwierdzone wyrokiem; wyciąg ustawy go nie obejmuje.
- **Świadczenie rodzicielskie (art. 31 ust. 3a).** Aplikacja informuje o podwyższeniu zasiłku do kwoty świadczenia rodzicielskiego, ale go nie wylicza, bo wymaga to kwoty po zaliczce na podatek. Parametr `kwota_swiadczenia_rodzicielskiego` jest pusty; przepis z kwotą 1000 zł miesięcznie jest w `docs/prawo/pobrane/`.
- **Daty obowiązywania.** Płaca minimalna i pochodne (podstawa minimalna, limit podstawy dobrowolnej) obowiązują od 1.01.2026. Próg zadłużenia ma też wartości za 2024 i 2025 r. (1% płacy minimalnej), żeby rozstrzygać sprawy testowe z tych lat. Parametry oparte na założeniach albo na wartościach spoza materiałów zadania (8 ze 102, m.in. zaokrąglanie i te progi) mają w panelu status „do weryfikacji”. Wymiary i stawki mają datę techniczną 1.01.2000, bo materiały nie podają dat. Dla dni sprzed 2026 r. silnik podaje kwotę tylko przy podstawie ustalonej przez płatnika albo policzonej z wynagrodzeń pracownika.
- Wyciągi przepisów są ze stanu z 2026 r. Zmiany wchodzące później (np. od 1.01.2027) opisuje `docs/prawo/ROZBIEZNOSCI.md`.

### Kiedy silnik odpowiada „nie rozstrzygam”

Zamiast zgadywać, silnik mówi „nie rozstrzygam” i odsyła do ZUS, gdy:

- jest zbieg tytułów ubezpieczenia chorobowego w dniu zdarzenia: prawo do zasiłku silnik uznaje (art. 57 ust. 1c, art. 61 ust. 4), ale podstawy, kwoty i płatnika dla każdego tytułu osobno nie ustala; przy zbiegu z działalnością nie rozstrzyga kontroli zadłużenia (art. 2a). Tytuł bez ubezpieczenia chorobowego (np. działalność tylko ze składką zdrowotną obok etatu) nie tworzy zbiegu;
- była zmiana pracodawcy bez przerwy w okresie obliczeniowym (podstawa tylko z wynagrodzenia u obecnego płatnika, art. 36 ust. 4);
- tytuł ubezpieczenia to „inny” (czy obejmuje go art. 2a), są wyjątki z art. 30 dla tytułów niebędących zatrudnieniem pracowniczym albo trwa inne zatrudnienie;
- chodzi o zasiłek chorobowy po ustaniu ubezpieczenia (art. 6–7, 13): choroby o długim okresie wylęgania, wyłączenia z art. 13;
- wcześniejsza przerwa w ubezpieczeniu była dłuższa niż 30 dni i mogą się do niej wliczać okresy (urlop wychowawczy, wojsko) — dotyczy okresu wyczekiwania;
- chodzi o świadczenie rehabilitacyjne po wyczerpaniu zasiłku (orzeka lekarz orzecznik ZUS);
- matka nieubezpieczona przebywa w szpitalu (sytuacja niewymieniona w art. 29 ust. 9);
- chodzi o urlop ojcowski po zgonie dziecka (art. 182³ KP nie odsyła do art. 180¹). Urlop rodzicielski i uzupełniający po zgonie dziecka silnik liczy: 7 dni od zgonu, gdy dziecko zmarło w trakcie urlopu (art. 180¹ § 2 w zw. z art. 182¹g i 180² § 7), a „nie przysługuje”, gdy zmarło przed jego rozpoczęciem lub urodziło się martwe. Przy porodzie mnogim, gdy jedno dziecko żyje, wymiar rodzicielskiego to 41 tyg. z informacją, że ZUS może przyznać 43;
- chodzi o stawkę przejęcia zasiłku przez ojca, gdy matka złożyła wniosek o wariant uśredniony;
- urlop na przyjęcie dziecka jest skrócony do granicy wieku, ale dłuższy niż minimum (przepis nie wskazuje wymiaru urlopu rodzicielskiego);
- podstawa wymiaru wychodzi poza poziom 1: zatrudnienie krótsze niż pełny miesiąc, zleceniobiorca krócej niż miesiąc, zmiana wymiaru czasu pracy w okresie obliczeniowym, przerwa do 30 dni lub zmiana tytułu;
- brakuje parametru prawnego obowiązującego w danym dniu albo dane są nieprawidłowe (np. liczba dzieci < 1, zgon przed porodem).

## Materiały zgłoszeniowe

- [docs/OPIS_PROJEKTU.md](docs/OPIS_PROJEKTU.md): tytuł i opis do formularza
- [docs/PREZENTACJA.md](docs/PREZENTACJA.md): treść prezentacji (10 slajdów); wersje [HTML](docs/PREZENTACJA.html) i [PDF](docs/PREZENTACJA.pdf)
- [docs/SCENARIUSZ_FILMU.md](docs/SCENARIUSZ_FILMU.md): scenariusz filmu (do 3 minut)
- [docs/zrzuty/README.md](docs/zrzuty/README.md): lista zrzutów ekranu
- [docs/ZALEZNOSCI.md](docs/ZALEZNOSCI.md): zależności i licencje
- [docs/PLAN_POMOCNIK.md](docs/PLAN_POMOCNIK.md): czat „Zapytaj o przepisy” (FAQ, wyszukiwarka, model)
- [docs/prawo/INDEKS.md](docs/prawo/INDEKS.md): wyciągi przepisów, z których powstały reguły
- [fixtures/organizations-verification.md](fixtures/organizations-verification.md): jak zweryfikowano organizacje wsparcia
- [BABYRADNIK_INSTRUKCJA.md](BABYRADNIK_INSTRUKCJA.md): pierwotna instrukcja zakresu projektu (nadrzędna jest treść zadania ZUS)
- [AGENTS.md](AGENTS.md): zasady pracy w repozytorium i wdrożenie demo
