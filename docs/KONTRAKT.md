# Kontrakt BabyRadnik — decyzje dla wszystkich gałęzi

Wersja kontraktu: `0.1.0` (Faza 0, `chore/bootstrap`). Źródło zakresu: `BABYRADNIK_INSTRUKCJA.md`.
Kontrakt zmienia **wyłącznie koordynator**, po zgodzie zatwierdzającego, i ogłasza zmianę wszystkim gałęziom.

## 1. Pliki kontraktu

| Plik | Co opisuje |
|---|---|
| `contracts/openapi.yaml` | wszystkie ścieżki z 12.1 (+ opcjonalny `POST /api/assistant/ask`) |
| `contracts/facts.schema.json` | zbiór faktów wejściowych silnika (sekcja 6) — **zamknięta lista kluczy** |
| `contracts/assessment.schema.json` | żądanie/wynik `POST /api/assessment`, scenariusze i wynik `compare` |
| `contracts/reference.schema.json` | `fixtures/reference/*.json` — przypadki referencyjne |
| `contracts/persona.schema.json` | `fixtures/personas/*.json` — fikcyjne osoby symulatora |
| `contracts/case.schema.json` | `fixtures/cases.json` — lista spraw ZUS |
| `contracts/parameters.schema.json` | `fixtures/parameters.seed.json` — parametry z datą obowiązywania |
| `contracts/examples/*.json` | atrapa wyniku i przykładowe żądanie |

Typy C#: `api/Domain/Contracts/`. Typy TypeScript: `web/src/app/api/types.ts`.
Test `ContractTests` pilnuje, że klucze faktów w C# = klucze w schemacie. Walidacja fixtures: `npm --prefix contracts run validate`.

## 2. Fakt i zbiór faktów

Fakt: `{ "value": …, "source": "mobywatel|zus|ezla|usc|user|assumption", "page": 3 }` (`page` opcjonalne — strona w dokumencie sprawy).
Brak klucza = fakt nieznany. Silnik nigdy nie zgaduje brakującego faktu; zgłasza go w `missingFacts`.

```json
{
  "sprawa":       { "zdarzenie.rodzaj": { "value": "urodzenie", "source": "user" } },
  "wnioskodawca": { "osoba.rola": { "value": "matka", "source": "user" } },
  "drugiRodzic":  { "ubezpieczenie.podlegaChorobowemu": { "value": true, "source": "user" } }
}
```

- `sprawa` — fakty o zdarzeniu i dziecku (wspólne dla rodziców).
- `wnioskodawca` — osoba zalogowana / dla której liczymy.
- `drugiRodzic` — opcjonalnie (gdy „dla mnie i drugiego rodzica” albo gdy prawo ojca zależy od sytuacji matki).
- `GET /api/sim/me/facts`: po urodzeniu rejestr stanu cywilnego daje też `zdarzenie.rodzaj = urodzenie` i `zdarzenie.data` (źródło `usc`, sterowane polem `usc/dziecko.dataUrodzenia` w panelu); osoba sprawy ZUS (`persona.sprawa`) niesie fakty sprawy przepisane z dokumentów (bez `zdarzenie.dlaKogo`). Gdy zdarzenia brak, ekran „Sprawdź dane” zadaje trzy pytania z ekranu 1 jako brakujące fakty.
- Brak PESEL, imion i nazwisk w faktach — silnik ich nie dostaje (zasada 2.5). Tożsamość tylko w odpowiedzi `POST /api/sim/login`.

### 2.1. Klucze faktów sprawy (`sprawa`)

| Klucz | Typ wartości | Pytanie / źródło (6.1–6.2) |
|---|---|---|
| `zdarzenie.rodzaj` | `planuje` \| `ciaza` \| `urodzenie` \| `przyjecie` | ekran 1 |
| `zdarzenie.data` | data | przewidywana data porodu / data urodzenia / data przyjęcia |
| `zdarzenie.dlaKogo` | `ja` \| `ja_i_drugi_rodzic` | ekran 1 |
| `dziecko.liczba` | int ≥ 1 | liczba dzieci przy jednym porodzie / przyjęciu; po porodzie z `usc` |
| `dziecko.dataUrodzenia` | data | `usc`; przy przyjęciu — data urodzenia przyjmowanego dziecka (wiek liczy silnik) |
| `dziecko.tydzienCiazyPrzyPorodzie` | int | wcześniactwo / hospitalizacja |
| `dziecko.masaUrodzeniowaG` | int (g) | jw.; wieloraczki: najniższa masa |
| `dziecko.pobytyWSzpitalu` | lista `{od, do}` | jw.; wieloraczki: dziecko z najdłuższym pobytem |
| `dziecko.zaZyciem` | bool | zaświadczenie „Za życiem” |
| `dziecko.martweUrodzenie` | bool | gdy użytkownik zaznaczy |
| `dziecko.liczbaZmarlych` | int | wieloraczki — zgon jednego z dzieci |
| `dziecko.dataZgonu` | data | gdy użytkownik zaznaczy |
| `dziecko.dataPochowku` | data | zasiłek pogrzebowy (termin Z-12) |
| `przyjecie.sciezka` | `przysposobienie` \| `rodzina_zastepcza_niezawodowa` \| `rodzina_zastepcza_zawodowa` | tylko przy przyjęciu |
| `przyjecie.wniosekDoSadu` | bool | przysposobienie: wniosek do sądu opiekuńczego |
| `przyjecie.dataUprawomocnieniaPostanowienia` | data | ojcowski przy przysposobieniu |
| `przyjecie.odroczenieObowiazkuSzkolnego` | bool | rodzina zastępcza, dziecko 7–10 lat |
| `matka.sytuacjaSzczegolna` | `{rodzaj, od, do?}`, rodzaj: `niezdolnosc_do_samodzielnej_egzystencji` \| `pobyt_w_szpitalu` \| `zgon` \| `porzucenie_dziecka` | gdy liczymy dla ojca i użytkownik zaznaczy |

### 2.2. Klucze faktów osoby (`wnioskodawca`, `drugiRodzic`)

| Klucz | Typ wartości | Pytanie / źródło |
|---|---|---|
| `osoba.rola` | `matka` \| `ojciec` | rola wobec dziecka (przy przyjęciu: kto korzysta jak matka) |
| `osoba.dataUrodzenia` | data | `mobywatel`; wiek ≥ 50 lat |
| `osoba.dataUkonczeniaSzkoly` | data | absolwent (art. 4 ust. 3) |
| `ubezpieczenie.tytuly` | lista tytułów* | `zus` — konto ubezpieczonego |
| `ubezpieczenie.podstawySkladek` | lista `{miesiac: "RRRR-MM", kwota, platnikId?}` | `zus` — 12 miesięcy, kwota brutto (przychód = podstawa składki chorobowej) |
| `ubezpieczenie.przerwy` | lista `{od, do, opis?}` | `zus` |
| `ubezpieczenie.pobraneSwiadczenia` | lista `{rodzaj, od, do, opis?}` | `zus`; rodzaj: `wynagrodzenie_chorobowe`, `chorobowy`, `rehabilitacyjne`, `macierzynski`, `macierzynski_uzupelniajacy`, `rodzicielski`, `ojcowski`, `opiekunczy`, `inne` |
| `ubezpieczenie.latWczesniejszegoObowiazkowego` | liczba lat | wyjątek od okresu wyczekiwania (10 lat) |
| `ubezpieczenie.podlegaChorobowemu` | bool | deklaracja, gdy brak danych z ZUS (np. drugi rodzic niezalogowany) |
| `ubezpieczenie.ustaniePrzyczyna` | `wypowiedzenie_przez_pracownika` \| `wypowiedzenie_przez_pracodawce` \| `porozumienie_stron` \| `koniec_umowy_na_czas_okreslony` \| `likwidacja_pracodawcy` \| `upadlosc_pracodawcy` \| `smierc_pracodawcy` \| `naruszenie_prawa_orzeczone_przez_sad` \| `zamkniecie_dzialalnosci` \| `inna` | ustanie ubezpieczenia w ciąży (art. 30) |
| `umowa.naCzasOkreslony` | bool | pracownik / zleceniobiorca przed porodem |
| `umowa.dataKonca` | data | jw. |
| `umowa.planowaneOdejscieData` | data | planowane odejście z pracy |
| `platnik.liczbaUbezpieczonych` | int | `zus` — konto płatnika, stan na 30 listopada poprzedniego roku |
| `skladki.zadluzenie` | `{naDzien, kwota}` | `zus` — saldo na dzień powstania prawa |
| `skladki.dataSplaty` | data | scenariusz „spłacam zadłużenie” |
| `ezla.zwolnienia` | lista `{od, do, kod?, szpital, dataWystawienia?}` | `ezla`; kod `B` = ciąża |
| `podstawa.ustalonaPrzezPlatnika` | kwota | opcjonalnie — ma pierwszeństwo przed szacunkiem z ZUS |
| `plan.wariantWyplaty` | `standardowy` \| `usredniony` | ekran wyniku |
| `plan.dataWnioskuUsrednionego` | data | termin 21 dni |
| `plan.macierzynskiPrzedPorodemTyg` | int | ile tygodni macierzyńskiego przed porodem |
| `plan.rezygnacjaZMacierzynskiegoPoTyg` | int | rezygnacja matki po N tygodniach (art. 29 ust. 3) |
| `plan.rodzicielskiCzesci` | lista `{tygodnie, od?}` | podział rodzicielskiego tego rodzica na części |
| `plan.pracaNaCzescEtatu` | lista `{wymiar, tygodnie, od?}` | łączenie rodzicielskiego z pracą |
| `plan.ojcowskiCzesci` | lista `{tygodnie, od?}` | ojcowski jednorazowo albo w 2 częściach |

\* Tytuł ubezpieczenia: `{rodzajTytulu, od, do?, chorobowe, kodTytulu?, opis?, platnikId?, platnikNazwa?, wymiarEtatu?, page?}`;
`rodzajTytulu`: `pracownik` | `zleceniobiorca` | `dzialalnosc` | `osoba_wspolpracujaca` | `pobierajacy_zasilek` | `inny`;
`chorobowe`: `obowiazkowe` | `dobrowolne` | `brak`. **Logika opiera się na `rodzajTytulu`, nie na kodzie** (6.1). Ulga na start = tytuł `dzialalnosc` z `chorobowe: brak`.

Dodanie klucza = zmiana kontraktu (schemat + `FactKeys.cs` + `FactSet.cs` + `types.ts` jednocześnie). Zgłoś potrzebę koordynatorowi.

## 3. Wynik oceny (`POST /api/assessment`)

Kształt jak w 12.1, z rozszerzeniami (oznaczone ★ — wymagają zatwierdzenia):

- `benefits[]` — jedna karta na **(świadczenie × osoba)**; ★ pole `person` (`wnioskodawca` | `drugiRodzic`).
- `status`: `przysługuje` | `nie_przysługuje` | `brakuje_danych` | `nie_dotyczy` | `nie_rozstrzygam` (dokładnie jak w sekcji 7, z polskimi znakami).
- `period {from, to, days, weeks}` — okres włącznie z dniem początkowym i końcowym.
- `rate` — ułamek (0.815 = 81,5%). ★ `parts[]` — części okresu z różnymi stawkami (np. 9 tygodni drugiego rodzica 70%, praca na część etatu).
- `amount {daily, total, basis, basisSource}` albo `null`, gdy brak podstawy (wtedy w `notes`: „podaj podstawę, żeby zobaczyć kwotę”). Kwoty brutto. `basisSource`: `szacunek_zus` | `ustalona_przez_płatnika` | ★`minimalna`.
- `payer`: `płatnik_składek` | `zus`; `whereToApply`: `pracodawca` | `płatnik_składek` | `zus`.
- `deadlines[] {id, type, date, text, consequence, basis★}`; `type`: `najpóźniej_do` | `najpóźniej_przed` | `okno` | `przedawnienie`.
- `reason` + ★`legalBasis` + `whatWouldChange` — dla `nie_przysługuje` / `nie_rozstrzygam`.
- `trace[] {rule, basis, met, text, condition}` — ścieżka decyzji; `met: null` = brak danych / nie rozstrzygam.
- ★ `notes[]`, `links[]` (S9).
- `missingFacts[] {fact, question, type, person, options, benefitIds}` — `fact` to klucz z sekcji 2; ★ `person` (null = fakt sprawy); `benefitIds[]` — świadczenia, których dotyczy pytanie (karta „brakuje danych” pokazuje tylko swoje pytania; puste = pytanie ogólne).
- ★ `validationErrors[] {code, message, fact, person}` — błędy planu (np. 33 tyg. z 41 dla jednego rodzica, szósta część) pokazywane przy polu; to nie jest błąd HTTP.
- `nextStep {deadlineId, benefitId, person★}` — najwcześniejszy niewykonany termin typu „najpóźniej”.
- `risks[] {id, text, basis★, link★}`; ★ `difficultSituation` — tryb trudnej sytuacji.
- ★ Żądanie: `{facts, asOf?, completedDeadlines?}` — `asOf` = „dzisiaj” (testy deterministyczne), `completedDeadlines` = terminy oznaczone jako wykonane.

Nieznany klucz faktu albo zła wartość → `400` ProblemDetails (walidacja na granicy API; C# odrzuca nieznane pola).

### 3.1. Wynik porównania (`POST /api/assessment/compare`)

`{base, variant, diff[], validationErrors[], variantFacts, changes[]}`:

- `variantFacts` — pełny zbiór faktów po nakładce scenariuszy (silnik `Domain/Scenarios`); ocena tego zbioru daje dokładnie `variant`. „Zastosuj wariant” na ekranie zapisuje **ten** zbiór — frontend nie tłumaczy scenariuszy na fakty sam. `null`, gdy scenariusza nie dało się zastosować (wtedy `validationErrors` niepuste).
- `changes[]` — co zmieniono w faktach, zdania prostym językiem (`ScenarioOutcome.Changes`).

## 4. Identyfikatory i konwencje nazw

- Świadczenia (katalog P0): `chorobowy_w_ciazy`, `rehabilitacyjne`, `zasilek_do_dnia_porodu`, `macierzynski`, `macierzynski_uzupelniajacy`, `rodzicielski`, `ojcowski`, `przyjecie_dziecka`, `przejecie_przez_ojca`, `pogrzebowy`.
- Scenariusze (sekcja 9): `odchodze_z_pracy{data}`, `umowa_nie_zostanie_przedluzona{}`, `przechodze_na_dzialalnosc{data, dobrowolneChorobowe}`, `drugi_rodzic_bierze{tygodnie}`, `praca_na_czesc_etatu{wymiar, tygodnie}`, `wariant_wyplaty{wariant}`, `splacam_zadluzenie{data}`; każdy z opcjonalnym `person`.
- Klucze faktów: `grupa.nazwaCamelCase` (ASCII). Pola JSON odpowiedzi: angielskie camelCase jak w 12.1. Wartości wyliczeń: polskie `snake_case`; bez polskich znaków — **poza** statusami, `payer`, `whereToApply`, `basisSource`, `type` terminu, które zachowują pisownię z przykładu 12.1 (`przysługuje`, `płatnik_składek`, `najpóźniej_do`).
- Id reguł (`trace.rule`) i terminów (`deadlines.id`): `snake_case`, stałe (testy referencyjne się do nich odwołują). Id reguły = `IRule.Id`.
- Daty `RRRR-MM-DD`; kwoty `decimal` w C#, `number` w JSON, 2 miejsca po przecinku.
- Parametry: klucze z tabeli 8.1; wiersze z kilkoma wartościami rozbite podkluczem po kropce (np. `macierzynski_tyg.dzieci_2`, `rodzicielski_tyg.wiecej_dzieci`); pole `wiersz` wskazuje oryginalny wiersz 8.1. Ułamki zamiast procentów (`0.815`).

## 5. Parametry

`fixtures/parameters.seed.json` — 67 parametrów z 43 wierszy tabeli 8.1. `IParameterProvider.Get(klucz, data)` zwraca wiersz z największym `validFrom ≤ data`; brak → `ParameterNotFoundException` (silnik ma wtedy zwrócić `nie_rozstrzygam`, nie zgadywać).

- `validFrom = 2000-01-01` = data techniczna „od zawsze”: instrukcja nie podaje dat obowiązywania wymiarów i stawek.
- `placa_minimalna` od `2026-01-01` (z instrukcji). Pochodne kwoty (`podstawa_minimalna_pelny_etat`, `max_podstawa_dobrowolnej_skladki*`, `prog_zadluzenia`) też od `2026-01-01` — **przyjęte**, opisane w `note`.
- `[DO WERYFIKACJI]` (`status: do_weryfikacji`): `kwota_swiadczenia_rodzicielskiego = null`, `zaokraglanie.*`, `kwoty = brutto`.
- `PUT /api/admin/parameters` (`ParameterChange`): opcjonalne `status` (`ok` | `do_weryfikacji`); bez pola nowa wartość dziedziczy status poprzedniej wartości tego klucza.
- Testy silnika używają `SeedParameterProvider` (`api/Tests/Reference/`) — bez bazy i bez gałęzi `be/params`.

## 6. Przypadki referencyjne

Plik `fixtures/reference/<id>.json`, `id` = nazwa pliku:

```json
{
  "$schema": "../../contracts/reference.schema.json",
  "id": "okresy-blizneta-za-zyciem",
  "opis": "Bliźnięta + „Za życiem”: macierzyński 31 tyg.",
  "status": "gotowe",
  "modul": "okresy",
  "test": 3,
  "asOf": "2026-11-21",
  "fakty": { "sprawa": { "…": {} }, "wnioskodawca": { "…": {} } },
  "oczekiwane": {
    "benefits": [ { "id": "macierzynski", "status": "przysługuje", "period": { "weeks": 31, "days": 217 } } ]
  }
}
```

- `modul`: `okresy` | `kwoty` | `uprawnienia` | `podstawa` | `katalog` | `terminy` | `scenariusze` | `parametry` — wybiera ewaluator.
- `oczekiwane` porównywane częściowo (tylko podane pola). Pola: `benefits[]` (m.in. `status`, `period`, `parts`, `rate`, `amount{daily,total,basis,monthly}`, `payer`, `whereToApply`, `deadlines`, `reasonContains`, `whatWouldChangeContains`, `traceRules`), `missingFacts[]` (klucze), `validationErrors[]`, `risks[]`, `nextStepDeadlineId`, `decyzjaZus`.
- `parametry[]` — dodatkowe wiersze parametrów na czas testu (test 24). `scenariusze[]` — dla `compare`.
- `"status": "do_uzupelnienia"` → test **pominięty** (nie zielony). 14 plików `case-<id>.json` (sekcja 10) już istnieje w tym stanie.
- Ewaluator modułu: klasa implementująca `IReferenceEvaluator` w lustrzanym katalogu testów gałęzi (np. `api/Tests/Domain/Periods/PeriodsReferenceEvaluator.cs`, `Modul => "okresy"`). Wykrywana automatycznie; bez ewaluatora test jest pomijany z powodem „brak silnika”. Do porównań: `ReferenceAssert.Matches(case, result)`.

## 7. Kto co edytuje

| Ścieżka | Właściciel |
|---|---|
| `contracts/**`, `api/Domain/Contracts/**`, `api/Api/Program.cs`, `api/Api/ApiJson.cs`, `api/Api/Endpoints/HealthEndpoints.cs`, `api/Infrastructure/Common/**`, `api/Tests/Reference/**`, `api/Tests/Domain/ContractTests.cs`, `api/Tests/Api/**`, `web/src/app/**`, `ops/docker-compose.yml`, `compose.yaml`, `.github/**`, `docs/KONTRAKT.md`, `docs/ZALEZNOSCI.md` | koordynator |
| `api/Domain/<Moduł>/**` + `api/Tests/Domain/<Moduł>/**` | gałąź modułu (`be/engine-periods` → `Periods`, `be/engine-eligibility` → `Eligibility`, `be/engine-amounts` → `Amounts`, `be/basis` → `Basis`, `be/catalog` → `Catalog` + `Deadlines`, `be/scenarios` → `Scenarios`) |
| `api/Api/Endpoints/AssessmentEndpoints.cs` | `be/catalog` (rejestruje `IAssessmentEngine` w `AddCatalog()`, wtedy atrapa znika sama) |
| `api/Api/Endpoints/CompareEndpoints.cs` | `be/scenarios` (rejestruje `IScenarioComparer` w `AddScenarios()`) |
| `api/Infrastructure/Parameters/**` (+ `/api/admin/*`, `/api/organizations` — zweryfikowane organizacje z panelu, sytuacje wg enum `SupportSituation`) | `be/params` |
| `api/Infrastructure/Simulation/**` (+ `/api/sim/*`, `/api/cases*`) | `be/sim` |
| `api/Infrastructure/Submissions/**` (+ `/api/submissions`, `/api/sim/me/debt-certificate`) | `be/submissions` |
| `api/Infrastructure/Persistence/**` | koordynator; moduły dodają encje przez `IEntityTypeConfiguration<T>` we **własnym** katalogu (wykrywane automatycznie) |
| `api/Infrastructure/Assistant/**` | gałąź pomocnika (P2) |
| `web/src/ui/**` | `fe/shell` |
| `web/src/features/<nazwa>/**` | gałąź ekranu (`fe/intake` → `intake` + `review`, `fe/result`, `fe/scenarios`, `fe/admin`, `fe/submissions` → `submissions` + `zus-data`, `fe/cases`, `fe/support`) |
| `fixtures/reference/**` | `data/reference`; `case-*.json` uzupełnia `data/cases` |
| `fixtures/personas/**`, `fixtures/cases.json` | `data/cases` (`be/sim` może dodać wymyślone osoby demo) |
| `fixtures/parameters.seed.json` | `be/params` (wartości tylko z instrukcji) |
| `ops/**` poza `docker-compose.yml` | `ops/deploy` |
| `docs/prawo/**` | ludzie |

Każdy moduł rejestruje się metodą z własnego pliku (`AddPeriods()`, `MapSimulationEndpoints()`, …), już wywołaną w `Program.cs`. Endpoint-zaślepka zwraca `501` ProblemDetails — gałąź podmienia go w swoim pliku.
Frontend: router (`web/src/app/router.tsx`) ma już trasy 10 ekranów; ekran eksportuje komponent z `web/src/features/<nazwa>/index.tsx`. Stan przebiegu (fakty, id sesji, id sprawy, terminy „Zrobione” — `completedDeadlines`) — `useCaseState()` z `web/src/app/case-context.ts`. Klient API — `api` z `web/src/app/api/client.ts`.

## 8. Zasady przypominające (sekcja 2)

- `Domain` nie zależy od bazy, HTTP ani pomocnika tekstowego (sprawdza `ContractTests`).
- Reguły tylko z `docs/prawo/`; nierozpoznane = `nie_rozstrzygam`.
- PESEL: tylko w odpowiedzi logowania; nigdy w faktach, sesji, logach, żądaniu do silnika. Walidator odrzuca personę z PESEL-em przechodzącym sumę kontrolną.
- Dokumenty i dane spraw ZUS: poza repo (`CASES_PRIVATE_DIR`).
- Nowa zależność: tylko MIT / Apache-2.0 / BSD, za zgodą koordynatora, wpis w `docs/ZALEZNOSCI.md`.
