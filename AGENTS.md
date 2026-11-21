# Zasady pracy w tym repozytorium (dla agentów i ludzi)

Projekt: **Kompas Rodzica** (HackNation 2026, zadanie ZUS). Nazwy techniczne `BabyRadnik.*` zostają.
Najważniejsza jest zgodność z treścią zadania ZUS; `BABYRADNIK_INSTRUKCJA.md` to plan pomocniczy.

## Demo na Azure

- Adres: https://kompasrodzica.polandcentral.cloudapp.azure.com (zapasowy: https://20-215-34-39.sslip.io).
- Serwer: `ssh babyradnik-azure` (alias w `~/.ssh/config` na komputerze zespołu). Aplikacja leży w `~/babyradnik`.
- Wdrożenie: z katalogu repozytorium `bash ops/deploy.sh`. Skrypt wysyła na serwer **zapisany stan gałęzi `main`**
  (archiwum przez SSH — repozytorium jest prywatne), buduje kontenery i czeka na `/health`.
  Zmiany niezapisane w gicie nie trafiają na demo.
- `bash ops/deploy.sh babyradnik-azure main --reset-db` — dodatkowo czyści bazę. Potrzebne tylko po zmianie
  `fixtures/parameters.seed.json` albo schematu bazy (parametry wczytują się wyłącznie do pustej tabeli).
  Reset kasuje też to, co zapisano w panelu `/admin`.
- **Nigdy `docker compose down -v`** na serwerze. Kasuje wolumen z certyfikatami, a Let's Encrypt wydaje
  najwyżej 5 certyfikatów tygodniowo na adres — limit dla poprzedniego adresu już raz wyczerpaliśmy.
- Sekrety (hasło bazy, hasło panelu `/admin`, klucze API) są tylko w `~/babyradnik/.env` na serwerze.
  Nie kopiuj ich do repozytorium, logów ani rozmów. W repozytorium jest wyłącznie `.env.example`.
- Lokalny Docker Desktop na tym komputerze bywa niesprawny — nie polegaj na `docker compose` lokalnie.

## Przed wdrożeniem

1. `cd api && dotnet test` — wszystko zielone (1 test pominięty jest w porządku).
2. `cd web && npx tsc -b && npm run lint && npm run build`.
3. Nie ukrywaj kodów wyjścia (`| tail` bez `set -o pipefail` raz już wpuściło zepsuty build na demo).
4. `git push origin main`, potem `bash ops/deploy.sh`.
5. Po wdrożeniu sprawdź stronę i jedną ścieżkę przez API albo w przeglądarce.

## Praca równoległa

- W tym katalogu pracuje naraz kilka osób/agentów. Zapisuj (`git add`) tylko własne pliki, wskazane po nazwie —
  nigdy `git add -A` ani `git commit -a`.
- Nie cofaj i nie nadpisuj cudzych niezapisanych zmian. Jeśli cudzy kod nie kompiluje się, zgłoś to, nie „naprawiaj” po cichu.
- Wdrożenie wysyła cały `main`, więc razem z Twoim commitem pojadą wszystkie wcześniejsze — także cudze.
- Wszystkie polecenia gita wykonuj w `babypedia_repo`. Katalog nadrzędny należy do innego repozytorium.
- Commity bez dopisku `Co-Authored-By`. Użycie AI opisujemy w README i w zgłoszeniu.

## Sprawy testowe ZUS

- Dokumentów ZUS i danych z nich nie wolno rozpowszechniać. Nie trafiają do repozytorium ani logów.
- Leżą poza repozytorium: `../Sprawy` (PDF), `../analiza` (odczyty), `../cases-demo` (to, co jest na serwerze).
- Przeliczenie analiz na format aplikacji: `node tools/cases/convert.mjs --analiza ../analiza --pdf ../Sprawy --out ../cases-demo --tylko … --zgodnosc …`.
- Na serwerze sprawy są w `~/babyradnik-cases` (ustawione jako `CASES_PRIVATE_DIR` w `.env`). Wgranie:
  `tar -C ../cases-demo -cf - cases.json personas | ssh babyradnik-azure "rm -rf ~/babyradnik-cases/* && tar -x -C ~/babyradnik-cases && chmod -R a+rX ~/babyradnik-cases"`,
  potem `bash ops/deploy.sh` (API czyta sprawy przy starcie).
- Raport „silnik a decyzje ZUS”: `CASES_REPORT_DIR=<katalog ze sprawami> dotnet test --filter PrivateCasesReportTests`.

## Zasady merytoryczne

- Uprawnienia, okresy, kwoty i terminy liczy wyłącznie deterministyczny silnik w `api/Domain`. Model językowy nie bierze udziału w decyzji ani kwocie.
- Liczby z przepisów idą przez parametry (`fixtures/parameters.seed.json`), nie są wpisane w kod.
- Brak danych → pytanie do użytkownika; brak parametru lub sytuacja spoza reguł → „nie rozstrzygam”. Niczego nie zgadujemy.
- Osoby w repozytorium są wymyślone; PESEL fikcyjny i tylko w symulowanym logowaniu. Organizacji wsparcia nie wymyślamy.
- Zależności tylko na licencjach MIT/Apache-2.0/BSD (PDFsharp-MigraDoc zostaje w wersji 6.2.4 — nowsze są AGPL).
