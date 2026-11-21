# Kompas Rodzica

Responsywna aplikacja WWW, która dla konkretnej osoby ustala, jakie zasiłki z ubezpieczenia chorobowego przysługują w związku z rodzicielstwem (a jakie nie i dlaczego), wylicza okres i wysokość, pokazuje terminy, dokumenty i miejsce złożenia wniosku oraz pozwala sprawdzić skutki zmian („co jeśli"). Projekt na HackNation 2026, zadanie ZUS „Zasiłkowy Przewodnik Przyszłego Rodzica". Zespół: 2Stosy (Sylwester Łotocki, Andrzej Laskowski).

- Demo: https://kompasrodzica.polandcentral.cloudapp.azure.com
- Panel `/admin`: hasło jest ustawiane zmienną środowiskową `ADMIN_PASSWORD` i przekazywane jury osobno.
- Repozytorium: https://github.com/slotocki/babypedia

Uwaga o nazwach: produkt nazywa się Kompas Rodzica. Wcześniejsza nazwa robocza BabyRadnik została w nazwach technicznych (przestrzenie nazw `BabyRadnik.*`, plik `BABYRADNIK_INSTRUKCJA.md`, nazwa projektu compose i bazy, katalog serwera).

Uprawnienia, okresy, kwoty i terminy liczy wyłącznie deterministyczny silnik reguł w C#. Wszystkie integracje z systemami zewnętrznymi (mObywatel, ZUS, e-ZLA, rejestr stanu cywilnego, wysyłka wniosków) są symulowane i oznaczone na ekranie, a dane osób są fikcyjne.

## Trzy komendy do uruchomienia

Wymagany Docker z Docker Compose.

```bash
git clone https://github.com/slotocki/babypedia.git && cd babypedia
cp .env.example .env        # ustaw w nim ADMIN_PASSWORD i POSTGRES_PASSWORD (PowerShell: Copy-Item .env.example .env)
docker compose up -d --build
```

Aplikacja: http://localhost:8080 (WWW), API: http://localhost:5080, stan: http://localhost:8080/health. `compose.yaml` w katalogu głównym dołącza `ops/docker-compose.yml` (postgres, api, web). Bez ustawienia `ADMIN_PASSWORD` panel `/admin` jest wyłączony. Profil `proxy` (HTTPS przez Caddy, wymaga `DOMAIN`) służy do wdrożenia publicznego (`ops/deploy.sh`).

Praca bez Dockera dla API i frontendu:

```bash
dotnet test --solution api/BabyRadnik.slnx       # build + testy (.NET 10, wersja w global.json)
dotnet run --project api/Api                     # API: http://localhost:5080, baza z ConnectionStrings__Default
cd web && npm ci && npm run dev                  # http://localhost:5173, proxy /api na :5080
npm ci --prefix contracts && npm --prefix contracts run validate   # walidacja fixtures schematami
```

## Jak przetestować w 3 minuty

1. **Start** (`/`): odpowiedz na trzy pytania: co się dzieje (planuję dziecko, jestem w ciąży, dziecko się urodziło, przyjmuję dziecko na wychowanie), kiedy i dla kogo liczymy (dla mnie albo dla mnie i drugiego rodzica).
2. **Logowanie (symulacja)** (`/logowanie`): wybierz fikcyjną osobę z listy albo „Wprowadzę dane sam".
3. **Sprawdź dane** (`/sprawdz-dane`): fakty z symulowanych systemów, każdy ze źródłem i przyciskiem „Popraw". Niżej tylko te pytania, które silnik zgłosił jako brakujące.
4. **Wynik** (`/wynik`): „Co Ci przysługuje" (karta świadczenia ze statusem, okresem, kwotą i rozwijaną ścieżką decyzji), „Co zrobić i do kiedy" (z jednym najbliższym krokiem), „Na co uważać", „Sprawdź inny wariant". Przy odmowie: powód, podstawa prawna i „Co musiałoby się zmienić".
5. **Porównaj warianty** (`/porownaj-warianty`): plan bazowy kontra wariant, różnica w tygodniach i złotych oraz zmiana statusów.
6. **Wnioski** (`/wnioski`), **Moje dane w ZUS** (`/moje-dane-zus`), **Wsparcie** (`/wsparcie`), **Sprawy ZUS** (`/sprawy`), **panel** (`/admin`).

### Fikcyjne osoby demo

Pliki w `fixtures/personas/`. Opisy w kolumnie „Co pokazuje" wynikają z danych osoby i z reguł; „oczekiwane" oznacza wynik zgodny z przypadkami referencyjnymi, do obejrzenia w demo.

| Osoba | Co pokazuje |
|---|---|
| `anna-przyklad` | Pracownica na umowie o pracę od 2021 r., ciąża z terminem 15.03.2027. Płatnik zgłasza 150 osób, więc zasiłek wypłaca płatnik. Ścieżka standardowa: macierzyński, rodzicielski, terminy. |
| `klara-demo` | Pracownica u dużego płatnika (240 osób), zasiłek wypłaca płatnik. |
| `halina-demo` | Pracownica małego płatnika (12 osób): zasiłek wypłaca ZUS, wniosek przez pracodawcę. Ma zwolnienie e-ZLA z kodem B (ciąża). |
| `monika-demo` | Po porodzie (8.10.2026), dziecko widoczne w rejestrze stanu cywilnego, płatnik 8 osób. Terminy liczone od daty porodu. |
| `edyta-demo` | Działalność z dobrowolnym chorobowym i zadłużeniem składkowym 120,00 zł. Oczekiwane: odmowa z powodem, kwotą do spłaty i datą graniczną; scenariusz „Spłacam zadłużenie". Zasiłek wypłaca ZUS. Ma też zaświadczenie o saldzie (PDF, symulacja). |
| `weronika-demo` | Zleceniobiorczyni z dobrowolnym chorobowym od 22.09.2026 (poniżej 90 dni wyczekiwania) i e-ZLA z kodem B. Oczekiwane: zasiłek chorobowy w ciąży odmowny z powodu okresu wyczekiwania. |
| `tadeusz-demo` | Ojciec-pracownik, liczymy „dla mnie i drugiego rodzica". Prawo ojca, gdy matka nie ma ubezpieczenia chorobowego. |
| `olga-demo` | Partnerka Tadeusza, bez ubezpieczenia chorobowego (rejestr ZUS zwraca pustą listę tytułów). Pokazuje odmowę i „co musiałoby się zmienić". |

### Sprawy ZUS (ekran `/sprawy`)

Ekran ma listę spraw testowych oraz „Dodaj PDF sprawy". Wybranie sprawy ładuje odpowiadającą jej osobę z symulatora i przechodzi do „Sprawdź dane". Dodany PDF jest rozpoznawany po sumie SHA-256 liczonej w przeglądarce (zapas: nazwa pliku i liczba stron), pokazuje się jego podgląd (pdf.js), a plik nie opuszcza przeglądarki: do API trafia tylko suma. Nieznany plik daje podgląd i komunikat „nie znam tej sprawy". Aplikacja nie czyta treści PDF (bez OCR). Na wyniku jest porównanie z decyzją ZUS.

Stan faktyczny (gałąź `main`, na dzień pisania): dane osób dla spraw przepisują ludzie z dokumentów i nie są częścią repozytorium. Są ładowane z katalogu wskazanego zmienną `CASES_PRIVATE_DIR` (w kontenerze `/cases`, tylko do odczytu). W repozytorium:

- `fixtures/cases.json` zawiera 14 spraw (tytuł, oczekiwana decyzja);
- wszystkie 14 plików `fixtures/reference/case-*.json` ma status `do_uzupelnienia`, a odpowiadające im testy są pomijane, nie zielone;
- w `fixtures/personas/` nie ma żadnej osoby dla tych spraw, więc bez katalogu prywatnego ekran pokazuje listę spraw z informacją, że ich dane nie zostały dostarczone;
- rozpoznawanie pliku po sumie działa tylko dla spraw, które w prywatnym `cases.json` mają pole `plik.sha256`.

W publicznym demo dane spraw nie są obecnie załadowane (stan na 21.11.2026). Do przejścia przez cały przebieg służą osoby demo z tabeli wyżej.

## Mapa wymagań

Oznaczenia z zadania ZUS (pkt 3) i instrukcji projektu (sekcja 4). Status wg kodu na gałęzi `main`; wiersze „w toku" mogą się zmienić po scaleniu gałęzi `fix/integration`.

| ID | Wymaganie | Ekran | Endpoint | Moduł kodu | Status |
|---|---|---|---|---|---|
| a | Świadczenia w ciąży, także brak uprawnień (chorobowy, rehabilitacyjne, zasiłek do dnia porodu) | Wynik (`/wynik`), karty świadczeń | `POST /api/assessment` | `api/Domain/Eligibility`, `api/Domain/Catalog` | Działa częściowo: świadczenie rehabilitacyjne po wyczerpaniu zasiłku zawsze kończy się „nie rozstrzygam" (zależy od orzeczenia lekarza orzecznika ZUS) |
| b | Kalkulator zasiłku macierzyńskiego dla obojga rodziców: okres, wysokość, liczba dzieci, wiek, „Za życiem" | Wynik, Porównaj warianty | `POST /api/assessment` | `api/Domain/Periods`, `api/Domain/Amounts` | Działa. Kwota tylko przy znanej podstawie (z ZUS albo wpisanej); bez podstawy są okresy i stawki |
| c | Łączenie pracy z urlopem rodzicielskim | Porównaj warianty (scenariusz „Praca na część etatu"), wniosek o urlop rodzicielski | `POST /api/assessment/compare` | `api/Domain/Scenarios`, reguły `rodzicielski_wydluzenie_praca`, `praca_na_czesc_etatu` | Działa |
| d | Symulacja korzystania przez oboje rodziców | Porównaj warianty (scenariusz „Drugi rodzic bierze część") | `POST /api/assessment/compare` | `api/Domain/Scenarios` | Działa (walidacja 9 tygodni nieprzenoszalnych i 5 części) |
| e, j | Rodzice adopcyjni i rodziny zastępcze | Start (zdarzenie „przyjmuję dziecko"), Wynik | `POST /api/assessment` | `api/Domain/Periods/Rules/PrzyjecieRules.cs` | Działa (przysposobienie, rodzina zastępcza niezawodowa; zawodowa: odmowa) |
| f | Indywidualna podstawa wymiaru | Wynik (pole „Znasz podstawę ustaloną przez płatnika?"), Sprawdź dane | `POST /api/assessment` | `api/Domain/Basis`, `api/Domain/Amounts` | Działa na poziomie 1 (średnia z 12 miesięcy dla pracownika i niepracownika, art. 48a/49) lub przez wpisanie podstawy. Poza poziomem 1 (np. zmiana wymiaru etatu, zbieg tytułów) silnik odsyła do podstawy ustalonej przez płatnika |
| g | Dokumenty, terminy, sposób i miejsce złożenia wniosku | Wynik („Co zrobić i do kiedy", „Gdzie złożyć wniosek", „Dokumenty") | `POST /api/assessment` | `api/Domain/Deadlines`, `api/Domain/Eligibility` (`platnik_zasilku_i_miejsce_zlozenia`) | Działa. Nazwy druków Z-3, Z-3a, ZAS-53 pochodzą z instrukcji projektu i nie występują w wyciągach przepisów (patrz ograniczenia) |
| h | Zagrożenia: utrata uprawnień, zmiana wymiaru lub wysokości | Wynik („Na co uważać"), Porównaj warianty | `POST /api/assessment`, `/compare` | `api/Domain/Catalog/Risks.cs`, `api/Domain/Scenarios` | Działa (ryzyka z listy w instrukcji 8.10, pokazywane tylko gdy pasują do faktów; informacja, nie ocena) |
| i | Podmioty wyłącznie charytatywne w sytuacjach szczególnych | Wsparcie (`/wsparcie`) | `GET /api/organizations?situation=` | `api/Infrastructure/Simulation/Catalogs.cs`, `web/src/features/support` | Działa częściowo: ekran i filtrowanie są, ale lista jest pusta, bo `fixtures/organizations.json` nie istnieje w repozytorium, a API pokazuje tylko wpisy z `zweryfikowano: true`. Wpisy z panelu `/admin` (zakładka „Organizacje") są zapisywane w bazie, lecz `/api/organizations` na `main` czyta wyłącznie plik. W toku |
| S1 | Logowanie przez mObywatel (symulacja) | Logowanie | `POST /api/sim/login`, `GET /api/sim/personas` | `api/Infrastructure/Simulation`, `web/src/features/intake` | Działa |
| S2 | Dane z ZUS o ubezpieczeniu lub jego braku (symulacja) + wpis ręczny | Sprawdź dane | `GET /api/sim/me/facts` | `api/Infrastructure/Simulation`, `web/src/features/review` | Działa częściowo: dane z symulatora i wpis ręczny działają. Przełączniki pól integracji w panelu `/admin` są zapisywane, ale na `main` nie zamieniają pola w pytanie (używany jest `AllFieldsEnabledSettings`). W toku |
| S3 | Zadłużenie składkowe na dzień powstania prawa (symulacja zaświadczenia i kwoty) | Moje dane w ZUS (saldo), Wnioski („Zaświadczenie o stanie składek") | `GET /api/sim/me/zus`, `GET /api/sim/me/debt-certificate` | `api/Infrastructure/Submissions/DebtCertificate.cs`, reguła `zadluzenie_skladkowe` | Działa (PDF z napisem „SYMULACJA") |
| S4 | Podgląd e-ZLA (symulacja) | Moje dane w ZUS („Zwolnienia lekarskie (e-ZLA)") | `GET /api/sim/me/zus` | `api/Infrastructure/Simulation` | Działa |
| S5 | PUE/eZUS: zapytanie, podgląd danych konta (symulacja) | Wnioski („Zapytanie do ZUS"), Moje dane w ZUS | `POST /api/submissions`, `GET /api/sim/me/zus` | `api/Infrastructure/Submissions`, `web/src/features/zus-data` | Działa |
| S6 | Wypełnienie wniosków o urlopy i świadczenia, wysyłka do pracodawcy lub ZUS (symulacja) | Wnioski | `POST /api/submissions`, `GET /api/submissions`, `GET /api/submissions/{id}/pdf` | `api/Infrastructure/Submissions`, `web/src/features/submissions` | Działa: cztery wzory wniosków o urlop (PDF) i „dane do wniosku ZAM" (zestawienie, nie druk ZUS). Wysyłka jest symulowana: potwierdzenie z numerem i wpis w „Moje zgłoszenia" |
| S7 | Wniosek o odpis aktu urodzenia (symulacja) | Wnioski | `POST /api/submissions` | `api/Infrastructure/Submissions` | Działa |
| S8 | Informacja o podmiotach wspierających | Wsparcie | `GET /api/organizations` | jak w wierszu `i` | Działa częściowo: zasiłek pogrzebowy (7 000 zł wg Poradnika ZUS), kontakt do ZUS i linki są; lista organizacji pusta (patrz `i`) |
| S9 | Przekierowania do zus.pl, MRPiPS, PIP | Wsparcie, Wnioski, Wynik | brak (linki) | `web/src/features/support/links.tsx` | Działa |
| S10 | Panel administracyjny do zmiany parametrów | `/admin` | `GET/PUT /api/admin/parameters`, `GET /api/admin/parameters/history`, `/api/admin/catalog` | `api/Infrastructure/Parameters`, `web/src/features/admin` | Działa dla parametrów (z datą obowiązywania i historią zmian) i włączania świadczeń w katalogu. Zakładki „Integracje" i „Organizacje": patrz S2 i `i` |
| T1 | Pełna responsywność | cały interfejs | brak | `web/src/ui`, `web/src/app/Layout.tsx` | Układ projektowany od 360 px (Tailwind, nawigacja przewijana poziomo). Do potwierdzenia testem na telefonach |
| T2 | Demo: upload lub wybór przykładowych spraw; weryfikacja ubezpieczenia i kalkulator | Sprawy ZUS | `GET /api/cases`, `GET /api/cases/by-hash/{sha256}` | `web/src/features/cases`, `api/Infrastructure/Simulation/Catalogs.cs` | Działa częściowo: lista, dodawanie PDF, suma SHA-256 i podgląd działają, ale dane spraw ZUS nie są w repozytorium (patrz wyżej) |

Dodatkowo: tryb trudnej sytuacji (martwe urodzenie, zgon dziecka, wcześniactwo z hospitalizacją, ciężka choroba) zmienia ton interfejsu i stawia organizacje wsparcia na górze (`web/src/ui/ToneScope.tsx`, `web/src/features/support`).

## Architektura w skrócie

Modularny monolit: React + TypeScript (Vite) i ASP.NET Core Web API (.NET 10), PostgreSQL przez EF Core, Docker Compose. REST/JSON.

```
contracts/   kontrakt: openapi.yaml, schematy JSON, przykłady, walidacja fixtures
fixtures/    parameters.seed.json, cases.json, personas/, reference/
api/Domain/  czysty C# (bez bazy i HTTP): Periods, Amounts, Eligibility, Basis, Catalog, Deadlines, Scenarios, Contracts
api/Infrastructure/  Parameters, Simulation, Submissions, Persistence, Assistant
api/Api/     Program.cs i endpointy
api/Tests/   testy modułów + testy referencyjne
web/src/     app (router, klient API), ui (komponenty), features (intake, review, result, scenarios, submissions, zus-data, cases, support, admin)
ops/         docker-compose.yml, proxy (Caddy), deploy.sh
docs/        zgłoszenie, zrzuty, wyciągi przepisów (docs/prawo)
```

Zasady:

- Decyzje i kwoty liczy wyłącznie deterministyczny silnik reguł. Każda reguła to mała klasa z `Id`, `PodstawaPrawna` i metodą oceny (w `api/Domain` jest ich ponad 80). Kwoty w `decimal`, daty w `DateOnly`.
- Parametry prawne (wymiary, stawki, progi, płaca minimalna) są w bazie z datą obowiązywania. Dostawca `IParameterProvider.Get(klucz, data)` zwraca wartość z największą datą `validFrom` nie późniejszą niż dzień, którego dotyczy ocena. Dane startowe: `fixtures/parameters.seed.json` (99 parametrów). Brak parametru daje „nie rozstrzygam", nie zgadywanie.
- Silnik dostaje fakty z pochodzeniem (`mobywatel`, `zus`, `ezla`, `usc`, `user`, `assumption`). Brakujący fakt wraca jako gotowe pytanie.
- Pięć interfejsów integracji, każdy z implementacją `Simulated…` nad jednym zbiorem `fixtures/personas`, gotowych do podmiany na prawdziwe usługi: `IIdentityProvider`, `IInsuranceRegistry`, `IContributionAccount`, `IMedicalCertificates`, `ISubmissionGateway`. Dodatkowo rejestr stanu cywilnego (`ICivilRegistry`) w `api/Infrastructure/Simulation`.
- Kontrakt (OpenAPI, schematy, typy C# i TypeScript) opisuje [docs/KONTRAKT.md](docs/KONTRAKT.md).

## Testy

Wynik `dotnet test --solution api/BabyRadnik.slnx` na gałęzi `main` (21.11.2026, .NET 10):

```
Podsumowanie przebiegu testu: Powodzenie!
  suma: 807
  zakończone niepowodzeniem: 0
  zakończone powodzeniem: 793
  pominięto: 14
```

14 pominiętych testów to przypadki spraw ZUS (`fixtures/reference/case-*.json`, status `do_uzupelnienia`): czekają na dane z dokumentów, więc są pomijane, a nie zielone.

Przypadki referencyjne (`fixtures/reference/`): 68 plików, w tym 54 aktywne: okresy 30, uprawnienia 14, kwoty 8, parametry 2; do tego 14 spraw ZUS oczekujących na dane. Pozostałe moduły (podstawa wymiaru, katalog, terminy, scenariusze, wnioski, symulacja, administracja) mają testy w `api/Tests/Domain/**` i `api/Tests/Infrastructure/**`.

Zasada: testy referencyjne powstają przed regułami, a reguła jest gotowa, gdy jej przypadki są zielone. Przypadek to plik `{id, opis, fakty, oczekiwane}`; testy silnika czytają je z `fixtures/reference/` (ewaluator wybierany polem `modul`, porównanie częściowe). Format opisuje [docs/KONTRAKT.md](docs/KONTRAKT.md), sekcja 6. CI (`.github/workflows`) buduje API, uruchamia testy, waliduje fixtures schematami i buduje frontend.

## Wykorzystanie AI

**(a) W działającej aplikacji.** Uprawnienia, okresy, kwoty i terminy liczy wyłącznie deterministyczny silnik reguł w C# (`api/Domain`). Żaden model językowy nie bierze udziału w decyzji ani w kwocie. Aplikacja nie używa OCR ani nie czyta treści PDF.

Jedyny element z modelem językowym to opcjonalny pomocnik tekstowy, endpoint `POST /api/assistant/ask` (`api/Infrastructure/Assistant`):

- jest domyślnie wyłączony (`ASSISTANT_ENABLED=false` w `.env.example`); wyłączony zwraca `503` „Pomocnik tekstowy jest wyłączony";
- włączony, wysyła do endpointu zgodnego z OpenAI (`ASSISTANT_BASE_URL`, `ASSISTANT_MODEL`; profil Compose `assistant` uruchamia lokalnego Ollama) wyłącznie treść pytania wpisanego przez użytkownika (do 1000 znaków) i stałe polecenie systemowe zakazujące podawania kwot i oceniania uprawnień. Nie dostaje faktów sprawy, danych osoby ani kwot; odpowiedź ma zawsze dopisek, że uprawnienia i kwoty wylicza kalkulator;
- interfejs WWW go nie wywołuje (w `web/src` istnieje tylko definicja klienta w `web/src/app/api/client.ts`), a `Domain` nie zależy od tego modułu (pilnuje tego `ContractTests`).

**(b) Wytworzenie kodu.** Kod, testy i dokumentację wytworzyły modele językowe Claude (Anthropic) w narzędziu Claude Code, pracujące jako zespół agentów pod nadzorem ludzi: modele pisały kod gałęzi, a osobny model przeglądał i zatwierdzał zmiany silnika (instrukcja, sekcja 13). Ludzie dostarczyli materiały prawne, decyzje o zakresie i weryfikację. Reguły pisano z wyciągów przepisów w `docs/prawo/`, nie z pamięci modelu. Spójność wyciągów z instrukcją opisuje [docs/prawo/ROZBIEZNOSCI.md](docs/prawo/ROZBIEZNOSCI.md).

## Komponenty zewnętrzne i licencje

Pełna lista z wersjami: [docs/ZALEZNOSCI.md](docs/ZALEZNOSCI.md). Zasada projektu: tylko licencje MIT, Apache-2.0, BSD. Wyjątki i uwagi wymieniono niżej i w tamtym pliku.

| Komponent | Licencja |
|---|---|
| ASP.NET Core / .NET 10, Entity Framework Core | MIT |
| Npgsql i Npgsql.EntityFrameworkCore.PostgreSQL | PostgreSQL License (permisywna; wymaga decyzji zatwierdzającego, brak alternatywnego sterownika) |
| PDFsharp-MigraDoc 6.2.4 (generowanie PDF) | MIT (nie podnosić do 6.5.0 i 7.x: licencja AGPL-3.0) |
| Font Roboto (osadzony w PDF) | Apache-2.0 |
| xunit.v3, xunit.runner.visualstudio | Apache-2.0 |
| React, React Router, TanStack Query, React Hook Form, Zod, Radix UI, clsx, tailwind-merge | MIT |
| class-variance-authority, pdfjs-dist (podgląd PDF), TypeScript | Apache-2.0 |
| Vite, Tailwind CSS 4, ESLint | MIT (Tailwind 4 używa przy budowaniu `lightningcss` na MPL-2.0: narzędzie budowania, nie trafia do aplikacji; do decyzji zatwierdzającego) |
| ajv, ajv-formats, js-yaml (walidacja kontraktu, tylko dev) | MIT |
| Obrazy: postgres 17 (PostgreSQL License), nginx 1.27 (BSD-2-Clause), caddy 2.10 (Apache-2.0), ollama 0.9.6 (MIT; profil opcjonalny, wagi modelu mają własne licencje) | jak w nawiasie |

## Bezpieczeństwo i dane

- Wszystkie osoby są fikcyjne. PESEL występuje tylko w odpowiedzi symulowanego logowania, jest fikcyjny (nie przechodzi walidacji sumy kontrolnej), służy do wyliczenia wieku i nie trafia do faktów silnika. Użytkownik go nie wpisuje, a formularze wniosków odrzucają 11-cyfrowe numery.
- Dokumenty ZUS (PDF spraw) i dane osób dla spraw nie są w repozytorium. `.gitignore` wyklucza `/Sprawy/`, `/sprawy/`, `/cases-private/`, `/fixtures/personas-private/` i `*.sprawa.pdf`. Dane spraw wchodzą z katalogu `CASES_PRIVATE_DIR` (montowanego tylko do odczytu). Plik PDF dodany na ekranie „Sprawy ZUS" nie opuszcza przeglądarki.
- Sekrety (`ADMIN_PASSWORD`, `POSTGRES_PASSWORD`) są tylko w `.env`, który jest w `.gitignore`; w repozytorium jest `.env.example`. Panel `/admin` jest wyłączony, gdy `ADMIN_PASSWORD` nie jest ustawione; hasło idzie w nagłówku `X-Admin-Password`.
- Wynik jest informacją, nie decyzją: aplikacja nie orzeka i nie ocenia użytkownika.

## Ograniczenia

Czego aplikacja nie robi (zakres, instrukcja sekcja 3): nie ma OCR ani parsera PDF, wyszukiwania w przepisach, bazy wektorowej, hostowania modelu, kont z hasłami, powiadomień e-mail i SMS, kwot netto, świadczeń spoza ubezpieczenia chorobowego (800+, żłobkowe), etapów rozwoju dziecka, wersji językowych. Aplikacja informuje i nie wydaje decyzji.

Co jest symulacją: logowanie mObywatel, konto ubezpieczonego i płatnika w ZUS, e-ZLA, rejestr stanu cywilnego, wysyłka wniosków do pracodawcy, ZUS, PUE i urzędu stanu cywilnego, zaświadczenie o stanie składek. Dane to fikcyjne osoby, a wnioski PDF i zestawienie „dane do wniosku ZAM" nie są drukami urzędowymi.

Otwarte kwestie do potwierdzenia z ZUS (źródło: `docs/prawo/ROZBIEZNOSCI.md`, instrukcja sekcja 14). Do czasu odpowiedzi działają ustawienia domyślne:

- **Zaokrąglanie.** Przepisy w wyciągach nie podają reguły zaokrąglania stawki dziennej ani kwoty zasiłku. Domyślnie: stawka dzienna do 0,01 zł, kwota za okres = stawka dzienna × liczba dni (parametry `zaokraglanie.*`, status `do_weryfikacji`).
- **Brutto czy netto.** Kwoty są brutto, z dopiskiem o zaliczce na podatek (parametr `kwoty`). Poradnik ZUS odejmuje od podstawy także zaliczkę na PIT, ustawa i Załącznik tego nie robią; stosujemy akt (tylko 13,71%).
- **Minimalna podstawa przy niepełnym etacie.** Art. 45 ust. 1 mówi o pełnym wymiarze czasu pracy; proporcjonalnego przeliczania w materiałach nie ma. Wartość 4 147,10 zł (4 806 × 0,8629) jest wyliczeniem ze wzoru z ustawy, tabela w Załączniku jest pusta.
- **Druki Z-3, Z-3a, ZAS-53 i data 24.10.2026.** Nie występują w materiałach; Poradnik opisuje Z-3b. Nazwy druków w aplikacji pochodzą z instrukcji projektu i są opatrzone zastrzeżeniem, by sprawdzić aktualny druk w ZUS.
- **Kwota świadczenia rodzicielskiego (art. 31 ust. 3a).** Parametr jest pusty, więc aplikacja nie stosuje podwyższenia i pokazuje informację. W `docs/prawo/pobrane/` jest odnaleziony przepis (1000,00 zł miesięcznie), ale czeka na potwierdzenie przez człowieka.
- **Parametry kwotowe od 2026 r.** Płaca minimalna i pochodne (podstawa minimalna, limit podstawy dobrowolnej, próg zadłużenia) mają datę obowiązywania 1.01.2026; wymiary i stawki mają datę techniczną „od zawsze" (2000-01-01), bo instrukcja nie podaje dat. Dla dni sprzed 2026 r. parametrów kwotowych brak, więc zgodnie z zasadą silnika (brak parametru = „nie rozstrzygam") kwota nie jest podawana.
- Wyciągi przepisów są ze stanu z 2026 r.; część zmian wchodzi później (np. od 1.01.2027), opisane w ROZBIEZNOSCI, A7 i E7.

Przypadki, w których silnik świadomie odpowiada „nie rozstrzygam" (zamiast zgadywać):

- zbieg tytułów ubezpieczenia w dniu zdarzenia albo działalność obok zatrudnienia (uprawnienia, płatnik, podstawa);
- tytuł „inny" (czy obejmuje go art. 2a), wyjątki z art. 30 dla tytułów niebędących zatrudnieniem pracowniczym, trwające inne zatrudnienie;
- zasiłek chorobowy po ustaniu ubezpieczenia (art. 6–7, 13): choroby o długim okresie wylęgania, wyłączenia z art. 13;
- wcześniejsza przerwa w ubezpieczeniu dłuższa niż 30 dni, z możliwym wliczeniem (urlop wychowawczy, wojsko) — okresu wyczekiwania;
- świadczenie rehabilitacyjne po wyczerpaniu zasiłku (orzeka lekarz orzecznik ZUS);
- pobyt matki nieubezpieczonej w szpitalu (nie wymieniony w art. 29 ust. 9);
- stawka przejęcia zasiłku przez ojca, gdy matka złożyła wniosek o wariant uśredniony;
- urlop na przyjęcie dziecka skrócony do granicy wieku, ale dłuższy niż minimum (wymiar urlopu rodzicielskiego niewskazany w przepisie);
- podstawa wymiaru poza poziomem 1: zatrudnienie krótsze niż pełny miesiąc, zleceniobiorca krócej niż miesiąc, zmiana wymiaru czasu pracy w okresie obliczeniowym, przerwa do 30 dni lub zmiana tytułu;
- brak parametru prawnego obowiązującego w danym dniu, nieprawidłowe dane (np. liczba dzieci < 1, zgon przed porodem).

## Materiały zgłoszeniowe

- [docs/OPIS_PROJEKTU.md](docs/OPIS_PROJEKTU.md): tytuł i opis do formularza
- [docs/PREZENTACJA.md](docs/PREZENTACJA.md): treść prezentacji (do 10 slajdów)
- [docs/SCENARIUSZ_FILMU.md](docs/SCENARIUSZ_FILMU.md): scenariusz filmu (do 3 minut)
- [docs/zrzuty/README.md](docs/zrzuty/README.md): lista zrzutów ekranu
- [BABYRADNIK_INSTRUKCJA.md](BABYRADNIK_INSTRUKCJA.md): instrukcja zakresu projektu
- [docs/prawo/INDEKS.md](docs/prawo/INDEKS.md): wyciągi przepisów, z których powstały reguły
