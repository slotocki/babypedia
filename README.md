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

1. **Start** (`/`): wybierz wpis ręczny, połączenie z mObywatelem i ZUS albo przejdź do przykładowych spraw ZUS.
2. **Połączenie (symulacja)** (`/logowanie`): wybierz kafelek z fikcyjnym profilem, a następnie pobierz jego dane. Z poziomu „Moje konto” wrócisz do wyniku, danych i wniosków.
3. **Sprawdź dane** (`/sprawdz-dane`): sprawdź źródła, popraw informacje w razie potrzeby i potwierdź pobrane dane. Potem zobaczysz pytania o brakujące informacje.
4. **Wynik** (`/wynik`): karta pokazuje świadczenie, status, okres, kwotę i szczegóły. Sekcja „Co dalej?” prowadzi do terminów i wniosków. Przy odmowie aplikacja podaje powód, podstawę prawną i okoliczności, które mogłyby zmienić wynik.
5. **Porównaj warianty** (`/porownaj-warianty`): plan bazowy kontra wariant, różnica w tygodniach i złotych oraz zmiana statusów.
6. **Wnioski** (`/wnioski`), **Moje dane w ZUS** (`/moje-dane-zus`), **Wsparcie** (`/wsparcie`), **Baza wiedzy** (`/baza-wiedzy`), **Sprawy ZUS** (`/sprawy`), **panel** (`/admin`).

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

### Sprawy testowe ZUS

Sprawę można wybrać na ekranie `/sprawy`, który umożliwia też dodanie pliku PDF. Dodany PDF jest rozpoznawany po sumie SHA-256 liczonej w przeglądarce (zapas: nazwa pliku i liczba stron), pokazuje się jego podgląd (pdf.js), a plik nie opuszcza przeglądarki: do API trafia tylko suma. Nieznany plik daje podgląd i komunikat „nie znam tej sprawy". Aplikacja nie czyta treści PDF (bez OCR). Po wybraniu sprawy wczytują się dane odczytane z jej dokumentów, a na wyniku jest porównanie z decyzją ZUS.

Każda sprawa na liście ma oznaczenie, co w niej sprawdzono z dokumentami ZUS:

| Oznaczenie | Znaczenie | Sprawy na demo (22.11.2026) |
|---|---|---|
| Okres i kwota zgodne z wydrukiem ZUS | podstawa wymiaru, stawka dzienna i okres zgadzają się z wydrukiem ZUS w aktach | 1 |
| Okres zgodny z ZUS co do dnia | okresy z silnika równe okresom z dokumentów; kwoty ZUS w aktach nie ma | 8 |
| Odmowa zgodna z decyzją ZUS | silnik odmawia tak jak ZUS | 2 |

Skąd są dane i czego nie ma:

- Dokumentów ZUS nie wolno rozpowszechniać, więc ani PDF-y, ani dane z nich nie są w repozytorium. `fixtures/cases.json` jest pusty; sprawy ładują się z katalogu `CASES_PRIVATE_DIR` (w kontenerze `/cases`, tylko do odczytu). Bez niego lista spraw się nie pokazuje.
- Dokumenty odczytano z pomocą modeli językowych, a wynik przeliczono na format aplikacji skryptem `tools/cases/convert.mjs`. Skrypt niczego nie zgaduje: brakujące pole zostaje pytaniem do użytkownika. Raport „silnik a decyzje ZUS” tworzy test `PrivateCasesReportTests` (uruchamiany ze zmienną `CASES_REPORT_DIR`).
- W większości akt kwota zasiłku i dane płatnika są zaczernione albo ich nie ma. Dlatego kwotę da się porównać z ZUS tylko w jednej sprawie; w dwóch kolejnych silnik liczy ją z wynagrodzeń z zaświadczenia płatnika, a w pozostałych aplikacja pyta o podstawę.
- W sprawach z poziomu „okres” aplikacja może dopytać o dane, których w aktach nie ma (liczba ubezpieczonych u płatnika, data pochówku).
- Na 43 przeanalizowane sprawy (z 87) silnik rozstrzyga inaczej niż ZUS w jednej: umowa o pracę uznana przez ZUS za zawartą dla pozoru. To ustalenie z kontroli, którego nie da się wyprowadzić z danych ubezpieczenia.

## Mapa wymagań

Oznaczenia z zadania ZUS (pkt 3) i instrukcji projektu (sekcja 4). Status wg kodu na gałęzi `main` (22.11.2026).

| ID | Wymaganie | Ekran | Endpoint | Moduł kodu | Status |
|---|---|---|---|---|---|
| a | Świadczenia w ciąży, także brak uprawnień (chorobowy, rehabilitacyjne, zasiłek do dnia porodu) | Wynik (`/wynik`), karty świadczeń | `POST /api/assessment` | `api/Domain/Eligibility`, `api/Domain/Catalog` | Działa częściowo: świadczenie rehabilitacyjne po wyczerpaniu zasiłku zawsze kończy się „nie rozstrzygam" (zależy od orzeczenia lekarza orzecznika ZUS) |
| b | Kalkulator zasiłku macierzyńskiego dla obojga rodziców: okres, wysokość, liczba dzieci, wiek, „Za życiem" | Wynik, Porównaj warianty | `POST /api/assessment` | `api/Domain/Periods`, `api/Domain/Amounts` | Działa. Kwota tylko przy znanej podstawie (z ZUS albo wpisanej); bez podstawy są okresy i stawki. Urlop rodzicielski bez planu jest pokazywany jako wymiar łączny obojga rodziców, z dopiskiem na karcie. Podwyższenia do kwoty świadczenia rodzicielskiego (art. 31 ust. 3a) aplikacja nie wylicza, tylko o nim informuje |
| c | Łączenie pracy z urlopem rodzicielskim | Porównaj warianty (scenariusz „Praca na część etatu"), wniosek o urlop rodzicielski | `POST /api/assessment/compare` | `api/Domain/Scenarios`, reguły `rodzicielski_wydluzenie_praca`, `praca_na_czesc_etatu` | Działa |
| d | Symulacja korzystania przez oboje rodziców | Porównaj warianty (scenariusz „Drugi rodzic bierze część") | `POST /api/assessment/compare` | `api/Domain/Scenarios` | Działa (walidacja 9 tygodni nieprzenoszalnych i 5 części) |
| e, j | Rodzice adopcyjni i rodziny zastępcze | Start (zdarzenie „przyjmuję dziecko"), Wynik | `POST /api/assessment` | `api/Domain/Periods/Rules/PrzyjecieRules.cs` | Działa (przysposobienie, rodzina zastępcza niezawodowa; zawodowa: odmowa) |
| f | Indywidualna podstawa wymiaru | Wynik (pole „Znasz podstawę ustaloną przez płatnika?"), Sprawdź dane | `POST /api/assessment` | `api/Domain/Basis`, `api/Domain/Amounts` | Działa na poziomie 1 (średnia z 12 miesięcy dla pracownika i niepracownika, art. 48a/49) lub przez wpisanie podstawy. Poza poziomem 1 (np. zmiana wymiaru etatu, zbieg tytułów) silnik odsyła do podstawy ustalonej przez płatnika |
| g | Dokumenty, terminy, sposób i miejsce złożenia wniosku | Wynik („Co zrobić i do kiedy", „Gdzie złożyć wniosek", „Dokumenty") | `POST /api/assessment` | `api/Domain/Deadlines`, `api/Domain/Eligibility` (`platnik_zasilku_i_miejsce_zlozenia`) | Działa. Nazwy druków (ZAM, Z-3, Z-3a, ZAS-53, ZAS-12) są zgodne z treścią zadania ZUS. ZAS-12 (zaświadczenie płatnika za dalszy okres niezdolności do pracy) pojawia się przy zasiłku chorobowym wypłacanym przez ZUS |
| h | Zagrożenia: utrata uprawnień, zmiana wymiaru lub wysokości | Wynik („Na co uważać"), Porównaj warianty | `POST /api/assessment`, `/compare` | `api/Domain/Catalog/Risks.cs`, `api/Domain/Scenarios` | Działa (ryzyka z listy w instrukcji 8.10, pokazywane tylko gdy pasują do faktów; informacja, nie ocena) |
| i | Podmioty wyłącznie charytatywne w sytuacjach szczególnych | Wsparcie (`/wsparcie`) | `GET /api/organizations?situation=` | `api/Infrastructure/Parameters/AdminSettingsService.cs`, `web/src/features/support` | Działa. Katalog startowy zawiera zweryfikowane organizacje oferujące bezpłatną pomoc z `fixtures/organizations.json`. Po zapisaniu listy w zakładce „Organizacje” panelu `/admin` obowiązuje zapisana lista. Publiczne API pokazuje wpisy oznaczone jako zweryfikowane. |
| S1 | Logowanie przez mObywatel (symulacja) | Logowanie | `POST /api/sim/login`, `GET /api/sim/personas` | `api/Infrastructure/Simulation`, `web/src/features/intake` | Działa |
| S2 | Dane z ZUS o ubezpieczeniu lub jego braku (symulacja) + wpis ręczny | Sprawdź dane | `GET /api/sim/me/facts` | `api/Infrastructure/Simulation`, `web/src/features/review` | Działa: dane z symulatora, wpis ręczny oraz przełączniki pól integracji w panelu `/admin` (wyłączone pole wraca jako pytanie do użytkownika) |
| S3 | Zadłużenie składkowe na dzień powstania prawa (symulacja zaświadczenia i kwoty) | Moje dane w ZUS (saldo), Wnioski („Zaświadczenie o stanie składek") | `GET /api/sim/me/zus`, `GET /api/sim/me/debt-certificate` | `api/Infrastructure/Submissions/DebtCertificate.cs`, reguła `zadluzenie_skladkowe` | Działa (PDF z napisem „SYMULACJA") |
| S4 | Podgląd e-ZLA (symulacja) | Moje dane w ZUS („Zwolnienia lekarskie (e-ZLA)") | `GET /api/sim/me/zus` | `api/Infrastructure/Simulation` | Działa |
| S5 | PUE/eZUS: zapytanie, podgląd danych konta (symulacja) | Wnioski („Zapytanie do ZUS"), Moje dane w ZUS | `POST /api/submissions`, `GET /api/sim/me/zus` | `api/Infrastructure/Submissions`, `web/src/features/zus-data` | Działa |
| S6 | Wypełnienie wniosków o urlopy i świadczenia, wysyłka do pracodawcy lub ZUS (symulacja) | Wnioski | `POST /api/submissions`, `GET /api/submissions`, `GET /api/submissions/{id}/pdf` | `api/Infrastructure/Submissions`, `web/src/features/submissions` | Działa: cztery wzory wniosków o urlop (PDF) i „dane do wniosku ZAM" (zestawienie, nie druk ZUS). Wysyłka jest symulowana: potwierdzenie z numerem i wpis w „Moje zgłoszenia" |
| S7 | Wniosek o odpis aktu urodzenia (symulacja) | Wnioski | `POST /api/submissions` | `api/Infrastructure/Submissions` | Działa |
| S8 | Informacja o podmiotach wspierających | Wsparcie | `GET /api/organizations` | jak w wierszu `i` | Działa. Ekran pokazuje katalog organizacji z filtrami sytuacji i kontakt do ZUS; informację o zasiłku pogrzebowym tylko przy martwym urodzeniu lub zgonie dziecka. |
| S9 | Przekierowania do zus.pl, MRPiPS, PIP | Baza wiedzy („Oficjalne źródła”; tam też Poradnik ZUS dla rodziców w PDF ze spisem rozdziałów) | brak (linki) | `web/src/features/support/links.tsx`, `web/src/features/support/KnowledgePage.tsx` | Działa |
| S10 | Panel administracyjny do zmiany parametrów | `/admin` | `GET/PUT /api/admin/parameters`, `GET /api/admin/parameters/history`, `/api/admin/catalog` | `api/Infrastructure/Parameters`, `web/src/features/admin` | Działa dla parametrów (z datą obowiązywania i historią zmian) i włączania świadczeń w katalogu. Zakładki „Integracje" i „Organizacje" także działają (patrz S2 i `i`) |
| T1 | Pełna responsywność | cały interfejs | brak | `web/src/ui`, `web/src/app/Layout.tsx` | Układ projektowany od 360 px (Tailwind, nawigacja przewijana poziomo). Do potwierdzenia testem na telefonach |
| T2 | Demo: upload lub wybór przykładowych spraw; weryfikacja ubezpieczenia i kalkulator | Start (lista spraw), Sprawy ZUS (dodanie PDF) | `GET /api/cases`, `GET /api/cases/by-hash/{sha256}` | `web/src/features/cases`, `api/Infrastructure/Simulation/Catalogs.cs`, `tools/cases` | Działa dla 11 spraw z zestawu ZUS (patrz „Sprawy testowe ZUS”). Pozostałe pliki z zestawu dają podgląd i komunikat „nie znam tej sprawy” |

Dodatkowo: tryb trudnej sytuacji (martwe urodzenie, zgon dziecka, wcześniactwo z hospitalizacją, ciężka choroba) zmienia ton interfejsu i stawia organizacje wsparcia na górze (`web/src/ui/ToneScope.tsx`, `web/src/features/support`).

## Architektura w skrócie

Modularny monolit: React + TypeScript (Vite) i ASP.NET Core Web API (.NET 10), PostgreSQL przez EF Core, Docker Compose. REST/JSON.

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
docs/        zgłoszenie, zrzuty, wyciągi przepisów (docs/prawo)
```

Zasady:

- Decyzje i kwoty liczy wyłącznie deterministyczny silnik reguł. Każda reguła to mała klasa z `Id`, `PodstawaPrawna` i metodą oceny (w `api/Domain` jest ich ponad 80). Kwoty w `decimal`, daty w `DateOnly`.
- Parametry prawne (wymiary, stawki, progi, płaca minimalna) są w bazie z datą obowiązywania. Dostawca `IParameterProvider.Get(klucz, data)` zwraca wartość z największą datą `validFrom` nie późniejszą niż dzień, którego dotyczy ocena. Dane startowe: `fixtures/parameters.seed.json` (102 wpisy, 8 ze statusem „do weryfikacji”). Brak parametru daje „nie rozstrzygam", nie zgadywanie.
- Silnik dostaje fakty z pochodzeniem (`mobywatel`, `zus`, `ezla`, `usc`, `user`, `assumption`). Brakujący fakt wraca jako gotowe pytanie.
- Pięć interfejsów integracji, każdy z implementacją `Simulated…` nad jednym zbiorem `fixtures/personas`, gotowych do podmiany na prawdziwe usługi: `IIdentityProvider`, `IInsuranceRegistry`, `IContributionAccount`, `IMedicalCertificates`, `ISubmissionGateway`. Dodatkowo rejestr stanu cywilnego (`ICivilRegistry`) w `api/Infrastructure/Simulation`.
- Kontrakt (OpenAPI, schematy, typy C# i TypeScript) opisuje [docs/KONTRAKT.md](docs/KONTRAKT.md).

## Testy

Wynik `dotnet test --solution api/BabyRadnik.slnx` na gałęzi `main` (22.11.2026, .NET 10):

```
Podsumowanie przebiegu testu: Powodzenie!
  suma: 946
  zakończone niepowodzeniem: 0
  zakończone powodzeniem: 945
  pominięto: 1
```

Pominięty test to raport dla spraw ZUS z katalogu prywatnego (`PrivateCasesReportTests`), uruchamiany tylko ze zmienną `CASES_REPORT_DIR`.

Przypadki referencyjne (`fixtures/reference/`): 55 plików — okresy 30, uprawnienia 15, kwoty 8, parametry 2. Pozostałe moduły (podstawa wymiaru, katalog, terminy, scenariusze, wnioski, symulacja, administracja) mają testy w `api/Tests/Domain/**` i `api/Tests/Infrastructure/**`.

Zasada: testy referencyjne powstają przed regułami, a reguła jest gotowa, gdy jej przypadki są zielone. Przypadek to plik `{id, opis, fakty, oczekiwane}`; testy silnika czytają je z `fixtures/reference/` (ewaluator wybierany polem `modul`, porównanie częściowe). Format opisuje [docs/KONTRAKT.md](docs/KONTRAKT.md), sekcja 6. CI (`.github/workflows`) buduje API, uruchamia testy, waliduje fixtures schematami i buduje frontend.

## Wykorzystanie AI

**(a) W działającej aplikacji.** Uprawnienia, okresy, kwoty i terminy liczy wyłącznie deterministyczny silnik reguł w C# (`api/Domain`). Żaden model językowy nie bierze udziału w decyzji ani w kwocie. Aplikacja nie używa OCR ani nie czyta treści PDF.

„Zapytaj o przepisy” otwiera czat na ekranie Wsparcie i pod wynikiem. Wyszukuje przepisy w wyciągach z `docs/prawo`, obsługuje dopytania i pokazuje pasujące artykuły. Pytania o własną sprawę kieruje do kalkulatora.

Opcjonalny model (na demo Claude Haiku firmy Anthropic; obsługiwany też np. DeepSeek przez OpenCode Go) pisze 2–3 zdania wyjaśnienia prostym językiem i wybiera cytaty spośród wysłanych przepisów. Aplikacja pokazuje tylko cytaty zweryfikowane z tekstem w repozytorium; wyjaśnienie pokazuje wyłącznie obok takich cytatów, z dopiskiem, że napisał je model, a wiążące są przepisy. Wyjaśnienie z kwotą, linkiem, danymi osobowymi albo zbyt długie jest zastępowane zdaniem ogólnym. Przy błędzie dostawcy, odrzuconym cytacie lub wyczerpaniu limitu czat wyświetla wyniki wyszukiwania. Instrukcja konfiguracji i odbioru jest w [docs/PLAN_POMOCNIK.md](docs/PLAN_POMOCNIK.md).

**(a2) Przygotowanie danych testowych.** Dokumenty spraw ZUS (skany) odczytano z pomocą modeli językowych, poza aplikacją. Odczyty są w katalogu prywatnym, a w aplikacji występują tylko jako dane wejściowe silnika; sam silnik liczy deterministycznie.

**(a3) Grafika.** Ilustracja na ekranie startowym (`web/public/kompas-start.png`) została wygenerowana przez model AI. Nie pochodzi z zewnętrznego banku zdjęć ani z cudzej pracy.

**(b) Wytworzenie kodu.** Kod, testy i dokumentację wytworzyły modele językowe (głównie Claude firmy Anthropic w narzędziu Claude Code), pracujące jako zespół agentów pod nadzorem ludzi: modele pisały kod gałęzi, a osobny model przeglądał i zatwierdzał zmiany silnika (instrukcja, sekcja 13). Ludzie dostarczyli materiały prawne, decyzje o zakresie i weryfikację. Reguły pisano z wyciągów przepisów w `docs/prawo/`, nie z pamięci modelu. Spójność wyciągów z instrukcją opisuje [docs/prawo/ROZBIEZNOSCI.md](docs/prawo/ROZBIEZNOSCI.md).

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
- Dokumenty ZUS (PDF spraw) i dane odczytane z nich nie są w repozytorium. Dane osobowe w tych dokumentach są zaczernione; osoby w sprawach testowych mają w aplikacji tylko imię z nazwy pliku i umowny PESEL. `.gitignore` wyklucza `/Sprawy/`, `/sprawy/`, `/cases-private/`, `/fixtures/personas-private/` i `*.sprawa.pdf`. Dane spraw wchodzą z katalogu `CASES_PRIVATE_DIR` (montowanego tylko do odczytu). Plik PDF dodany na ekranie „Sprawy ZUS" nie opuszcza przeglądarki.
- Sekrety (`ADMIN_PASSWORD`, `POSTGRES_PASSWORD`) są tylko w `.env`, który jest w `.gitignore`; w repozytorium jest `.env.example`. Panel `/admin` jest wyłączony, gdy `ADMIN_PASSWORD` nie jest ustawione; hasło idzie w nagłówku `X-Admin-Password`.
- Wynik jest informacją, nie decyzją: aplikacja nie orzeka i nie ocenia użytkownika.

## Ograniczenia

Czego aplikacja nie robi (zakres, instrukcja sekcja 3): nie ma OCR ani parsera PDF, bazy wektorowej, zarządzania kontami z hasłami, powiadomień e-mail i SMS, kwot netto, świadczeń spoza ubezpieczenia chorobowego (800+, żłobkowe), etapów rozwoju dziecka, wersji językowych. Aplikacja informuje i nie wydaje decyzji.

Co jest symulacją: logowanie mObywatel, konto ubezpieczonego i płatnika w ZUS, e-ZLA, rejestr stanu cywilnego, wysyłka wniosków do pracodawcy, ZUS, PUE i urzędu stanu cywilnego, zaświadczenie o stanie składek. Dane to fikcyjne osoby, a wnioski PDF i zestawienie „dane do wniosku ZAM" nie są drukami urzędowymi.

Otwarte kwestie do potwierdzenia z ZUS (źródło: `docs/prawo/ROZBIEZNOSCI.md`, instrukcja sekcja 14). Do czasu odpowiedzi działają ustawienia domyślne:

- **Zaokrąglanie.** Przepisy w wyciągach nie podają reguły zaokrąglania stawki dziennej ani kwoty zasiłku. Domyślnie tak jak w wyliczeniach ZUS w sprawach testowych: stawka dzienna = podstawa × stopa ÷ 30, zaokrąglona raz do 0,01 zł; kwota za okres = stawka dzienna × liczba dni (parametry `zaokraglanie.*`, status `do_weryfikacji`).
- **Brutto czy netto.** Kwoty są brutto, z dopiskiem o zaliczce na podatek (parametr `kwoty`). Poradnik ZUS odejmuje od podstawy także zaliczkę na PIT, ustawa i Załącznik tego nie robią; stosujemy akt (tylko 13,71%).
- **Minimalna podstawa przy niepełnym etacie.** Art. 45 ust. 1 mówi o pełnym wymiarze czasu pracy; proporcjonalnego przeliczania w materiałach nie ma. Wartość 4 147,10 zł (4 806 × 0,8629) jest wyliczeniem ze wzoru z ustawy, tabela w Załączniku jest pusta.
- **Druki.** Z-3, Z-3a, ZAS-53 i połączenie wniosków w ZAM od 24.10.2026 potwierdza treść zadania ZUS; Poradnik opisuje jeszcze Z-3b. ZAS-12 (zaświadczenie płatnika za dalszy okres zasiłku chorobowego) opisano wg treści zadania i druku w aktach spraw; wyciągi przepisów go nie wymieniają.
- **Zasiłek do dnia porodu przy naruszeniu prawa.** Treść zadania wymienia też rozwiązanie umowy z naruszeniem prawa stwierdzone wyrokiem; wyciąg ustawy (art. 30 ust. 3) wymienia śmierć, upadłość i likwidację pracodawcy. Silnik stosuje ustawę.
- **Kwota świadczenia rodzicielskiego (art. 31 ust. 3a).** Parametr jest pusty, więc aplikacja nie stosuje podwyższenia i pokazuje informację. W `docs/prawo/pobrane/` jest odnaleziony przepis (1000,00 zł miesięcznie), a pisma ZUS w sprawach testowych potwierdzają tę kwotę; samo wyliczenie wymaga kwoty po zaliczce na podatek, której aplikacja nie liczy.
- **Parametry kwotowe od 2026 r.** Płaca minimalna i pochodne (podstawa minimalna, limit podstawy dobrowolnej) mają datę obowiązywania 1.01.2026; próg zadłużenia ma też wartości za 2024 i 2025 r. (1% płacy minimalnej, status „do weryfikacji”, kwoty spoza materiałów konkursowych), żeby dało się rozstrzygać sprawy testowe ZUS z tych lat; wymiary i stawki mają datę techniczną „od zawsze" (2000-01-01), bo instrukcja nie podaje dat. Dla dni sprzed 2026 r. silnik podaje kwotę tylko przy podstawie ustalonej przez płatnika albo policzonej z wynagrodzeń pracownika; tam, gdzie potrzebny jest brakujący parametr (np. podstawa przy działalności), odpowiada „nie rozstrzygam".
- Wyciągi przepisów są ze stanu z 2026 r.; część zmian wchodzi później (np. od 1.01.2027), opisane w ROZBIEZNOSCI, A7 i E7.

Przypadki, w których silnik świadomie odpowiada „nie rozstrzygam" (zamiast zgadywać):

- zbieg tytułów ubezpieczenia chorobowego w dniu zdarzenia: prawo do zasiłku silnik uznaje (przysługuje z każdego tytułu, art. 57 ust. 1c, art. 61 ust. 4), ale podstawy, kwoty i płatnika dla każdego tytułu osobno nie ustala; przy zbiegu z działalnością nie rozstrzyga też kontroli zadłużenia (art. 2a). Tytuł bez ubezpieczenia chorobowego (np. działalność tylko ze składką zdrowotną obok etatu) nie tworzy zbiegu;
- zmiana pracodawcy (znany, inny płatnik) bez przerwy w okresie obliczeniowym — podstawa tylko z wynagrodzenia u obecnego płatnika (art. 36 ust. 4), czego poziom 1 nie liczy;
- tytuł „inny" (czy obejmuje go art. 2a), wyjątki z art. 30 dla tytułów niebędących zatrudnieniem pracowniczym, trwające inne zatrudnienie;
- zasiłek chorobowy po ustaniu ubezpieczenia (art. 6–7, 13): choroby o długim okresie wylęgania, wyłączenia z art. 13;
- wcześniejsza przerwa w ubezpieczeniu dłuższa niż 30 dni, z możliwym wliczeniem (urlop wychowawczy, wojsko) — okresu wyczekiwania;
- świadczenie rehabilitacyjne po wyczerpaniu zasiłku (orzeka lekarz orzecznik ZUS);
- pobyt matki nieubezpieczonej w szpitalu (nie wymieniony w art. 29 ust. 9);
- urlop ojcowski po zgonie dziecka (art. 182³ KP nie odsyła do art. 180¹). Urlop rodzicielski i uzupełniający po zgonie dziecka silnik liczy: 7 dni od zgonu, gdy dziecko zmarło w trakcie urlopu (art. 180¹ § 2 w zw. z art. 182¹g i 180² § 7), a „nie przysługuje”, gdy zmarło przed jego rozpoczęciem lub urodziło się martwe. Przy porodzie mnogim, gdy jedno dziecko żyje, wymiar rodzicielskiego to 41 tyg. z notą, że ZUS może przyznać 43 (przepis nie rozstrzyga);
- stawka przejęcia zasiłku przez ojca, gdy matka złożyła wniosek o wariant uśredniony;
- urlop na przyjęcie dziecka skrócony do granicy wieku, ale dłuższy niż minimum (wymiar urlopu rodzicielskiego niewskazany w przepisie);
- podstawa wymiaru poza poziomem 1: zatrudnienie krótsze niż pełny miesiąc, zleceniobiorca krócej niż miesiąc, zmiana wymiaru czasu pracy w okresie obliczeniowym, przerwa do 30 dni lub zmiana tytułu;
- brak parametru prawnego obowiązującego w danym dniu, nieprawidłowe dane (np. liczba dzieci < 1, zgon przed porodem).

## Materiały zgłoszeniowe

- [docs/OPIS_PROJEKTU.md](docs/OPIS_PROJEKTU.md): tytuł i opis do formularza
- [docs/PREZENTACJA.md](docs/PREZENTACJA.md): treść prezentacji (do 10 slajdów)
- [docs/SCENARIUSZ_FILMU.md](docs/SCENARIUSZ_FILMU.md): scenariusz filmu (do 3 minut)
- [docs/zrzuty/README.md](docs/zrzuty/README.md): lista zrzutów ekranu
- [BABYRADNIK_INSTRUKCJA.md](BABYRADNIK_INSTRUKCJA.md): pierwotna instrukcja zakresu projektu (nadrzędna jest treść zadania ZUS)
- [AGENTS.md](AGENTS.md): zasady pracy w repozytorium i wdrożenie demo
- [docs/PLAN_POMOCNIK.md](docs/PLAN_POMOCNIK.md): plan pomocnika „Zapytaj o przepisy”
- [fixtures/organizations-verification.md](fixtures/organizations-verification.md): jak zweryfikowano organizacje wsparcia
- [docs/prawo/INDEKS.md](docs/prawo/INDEKS.md): wyciągi przepisów, z których powstały reguły
