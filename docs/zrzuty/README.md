# Zrzuty ekranu

Zrzuty w tym katalogu generuje skrypt `zrzuty.mjs` (Playwright, Chromium) na lokalnej kopii aplikacji z fikcyjnymi osobami z `fixtures`. Szerokość 1280 px to widok komputera, 390 px to widok telefonu (skala ×2). Zrzuty `slajd-*` mają większą ostrość (skala ×1,25 albo ×2) i kadry dobrane do prezentacji.

## Jak wygenerować

1. API bez bazy (parametry z seeda) i front, np.:
   ```sh
   CASES_PRIVATE_DIR=../cases-demo ADMIN_PASSWORD=<hasło lokalne> dotnet run --no-launch-profile --urls http://localhost:5100 --project api/Api
   cd web && VITE_API_PROXY=http://localhost:5100 npx vite --port 5173
   ```
   Lista spraw ZUS (`/sprawy`) wymaga katalogu spraw (`CASES_PRIVATE_DIR`); bez niego zrzuty 17, 19, 33 i `slajd-05-sprawy-zus` się nie udadzą.
2. Z katalogu repozytorium:
   ```sh
   ADMIN_PASSWORD=<hasło lokalne> npx -y -p playwright@1.49.1 node docs/zrzuty/zrzuty.mjs http://localhost:5173 docs/zrzuty
   ```
   Za pierwszym razem: `npx -y playwright@1.49.1 install chromium`. Dodatkowe argumenty to fragmenty nazw plików, np. `… docs/zrzuty slajd-04 11-` robi tylko pasujące zrzuty. Bez `ADMIN_PASSWORD` zrzuty panelu `/admin` są pomijane; hasło nie pojawia się na żadnym zrzucie (pole jest maskowane, a zrzut powstaje po wejściu do panelu).
3. Skrypt kończy się kodem 1 i listą problemów, jeśli któryś ekran się nie wczytał. Każdy zrzut trzeba obejrzeć przed zapisaniem w repozytorium.

Skrypt chowa pływający przycisk czatu na zrzutach fragmentów (karty, ramki), żeby nie zasłaniał treści, i wyłącza animacje. Treści aplikacji nie zmienia.

**Sprawy ZUS.** Dokumentów ZUS i danych z nich nie wolno rozpowszechniać ani wpisywać do repozytorium. Skrypt nie wczytuje plików PDF spraw. Zrzut wyniku sprawy (19) jest przycięty do nagłówka ramki „Porównanie z decyzją ZUS (sprawa testowa)” i werdyktu, bez tabeli okresów, kwot i dat.

## Lista

| Plik | Ekran (adres) | Osoba lub sprawa | Szerokość | Co widać |
|---|---|---|---|---|
| `01-start.png` | Start (`/`) | brak | 1280 | „Sprawdź, co Ci przysługuje”, kafelki „Wprowadź dane ręcznie” (4–5 krótkich pytań), „Połącz się przez mObywatela i ZUS”, „Dla oceniających: Sprawdź na rzeczywistych przykładach”; nawigacja Start, Wnioski, Wsparcie, Baza wiedzy; w stopce „Sprawy ZUS (dla oceniających)” i „Porównaj warianty” |
| `02-wpis-reczny.png` | Wpis ręczny (`/wprowadz-dane`) | brak | 1280 | „Krok 1 z 3: Dane”, „Twoje dane: pytanie 1 z 4”, „Co się u Ciebie dzieje?” z czterema odpowiedziami |
| `03-logowanie.png` | Logowanie (`/logowanie`) | lista osób | 1280 | „Zaloguj się przez mObywatela”, baner „SYMULACJA”, kafelki ośmiu fikcyjnych osób |
| `04-zgoda.png` | Logowanie, krok zgody | `Anna` | 1280 | „Zgoda na pobranie danych”, „Udostępnij dane z:” (ZUS, e-ZLA, rejestr stanu cywilnego), „Połącz i pobierz dane (symulacja)” |
| `05-potwierdz-dane.png` | Sprawdź dane (`/sprawdz-dane`) | `Anna` | 1280 | „Sprawdź swoje dane”, dane z plakietkami źródeł i przyciskami „Popraw”, „Dane się zgadzają — dalej” niżej |
| `06-uzupelnij-dane.png` | Sprawdź dane, po „Dane się zgadzają — dalej” | `Anna` | 1280 | „Kilka pytań, których ZUS nie zna”: pytania w jednej turze (tu „Czy Twoja umowa jest zawarta na czas określony?” → „Nie”), „Pokaż wynik” |
| `07-wynik-przysluguje.png` | Wynik (`/wynik`) | `Anna` | 1280 | „Twój plan”, „W skrócie”, „Twoje świadczenia” z kartą zasiłku (kwota, okres), panel „Co dalej?”, przycisk czatu w prawym dolnym rogu |
| `08-szczegoly-sciezka-decyzji.png` | Wynik, karta zasiłku macierzyńskiego z rozwiniętymi „Szczegóły świadczenia” i „Ścieżka decyzji” | `Anna` | 1280 | Stawka, podstawa, „Kto wypłaca”, „Gdzie złożyć wniosek”, „Dokumenty”, słowniczek, najważniejsze kroki ścieżki i „Pokaż wszystkie kroki (19)” |
| `09-terminy.png` | Wynik, „Co zrobić i do kiedy” | `Anna` | 1280 | Lista terminów z „To jest Twój najbliższy krok.” i polami „Zrobione” (przycięte do ok. 1000 px) |
| `10-ryzyka-wariant.png` | Wynik, rozwinięte „Ryzyka i wariant wypłaty” | `Anna` | 1280 | „Na co uważać” i „Sprawdź inny wariant” (standardowy / uśredniony 81,5%) |
| `11-odmowa.png` | Wynik, karta zasiłku macierzyńskiego | `Edyta` | 1280 | „Nie przysługuje”, powód (zaległość 120,00 zł), „To da się naprawić” z terminem spłaty, przepis, „Ścieżka decyzji” |
| `12-porownaj-warianty.png` | Porównaj warianty (`/porownaj-warianty`) | `Anna`, „Wariant wypłaty” → „Uśredniony” | 1280 | „Różnica: plan bazowy a wariant …” z kwotami i „Co się zmienia” (plan bazowy obok wariantu) |
| `13-porownanie-zmiana-statusu.png` | Porównaj warianty | `Edyta`, „Spłacam zadłużenie” (spłata 01.04.2027) | 1280 | Zmiana statusu świadczeń z „nie przysługuje” na „przysługuje” |
| `14-porownanie-drugi-rodzic.png` | Porównaj warianty | wpis ręczny „Dla mnie i drugiego rodzica”, oboje na umowie o pracę | 1280 | „Drugi rodzic bierze część urlopu rodzicielskiego”: wybrana osoba („Drugi rodzic”), 16 tygodni, różnica i zmiana okresów obojga rodziców |
| `15-wnioski.png` | Wnioski (`/wnioski`), cała strona | `Anna` | 1280 | „Wnioski o urlop dla Ciebie teraz” (2 wzory + „Pozostałe wnioski o urlop (2)”), „Zasiłek, zapytania i dokumenty”, „Zaświadczenie o stanie składek” |
| `16-wniosek-pdf.png` | Wnioski, po „Przygotuj PDF” (urlop macierzyński i rodzicielski) | `Anna` | 1280 | „PDF jest gotowy”, numer zgłoszenia, „Pobierz PDF”, „Otwórz PDF” (bez samego pliku PDF — patrz niżej) |
| `17-sprawy-zus.png` | Sprawy ZUS (`/sprawy`), cała strona | brak | 1280 | Kafelki spraw z oznaczeniami („Okres zgodny z ZUS co do dnia”, „Okres i kwota zgodne z wydrukiem ZUS”, „Odmowa zgodna z decyzją ZUS”) i rozwinięte „Masz plik PDF sprawy ZUS? Rozpoznaj go” (bez wybranego pliku) |
| `19-sprawa-porownanie-z-zus.png` | Wynik po wczytaniu sprawy (pierwsza na liście) | sprawa testowa | 1280 | Tylko nagłówek ramki „Porównanie z decyzją ZUS (sprawa testowa)” i werdykt „Porównanie: zgodne” |
| `21-moje-dane-zus.png` | Moje dane w ZUS (`/moje-dane-zus`), cała strona | `Halina` | 1280 | „Ubezpieczenia i płatnicy”, „Podstawy składek”, „Zwolnienia lekarskie (e-ZLA)” z kodem B, „Saldo składek” |
| `22-moje-konto.png` | Moje konto (`/moje-konto`) | `Anna` | 1280 | Profil testowy, stan połączenia i źródła danych, „Przejdź do planu”, „Moje dane w ZUS”, „Moje wnioski” |
| `23-wsparcie.png` | Wsparcie (`/wsparcie`) | brak | 1280 | „Twoja sytuacja”, „Masz pytanie do ZUS lub pracodawcy?”, początek „Organizacje wsparcia” |
| `24-trudna-sytuacja.png` | Wsparcie, „Twoja sytuacja”: „Martwe urodzenie”, cała strona | brak | 1280 | Zmieniony ton, „Co możesz zrobić teraz”, organizacje, „Zasiłek pogrzebowy” |
| `25-baza-wiedzy.png` | Baza wiedzy (`/baza-wiedzy`) | brak | 1280 | „Poradnik ZUS” ze spisem rozdziałów, „Oficjalne źródła” |
| `26-czat-przepisy.png` | Baza wiedzy z otwartym czatem „Zapytaj o przepisy” | brak | 1280 | Odpowiedź na pytanie z FAQ „Czy można pracować na urlopie rodzicielskim?”, źródła jako etykiety, rozwinięty cytat KP art. 182¹e (rozmowa przewinięta do końca) |
| `27-admin-parametry.png` | Panel `/admin`, „Parametry” (lokalna kopia) | brak | 1280 | „Parametry prawne”, instrukcja, wyszukiwarka, pierwszy parametr z polską nazwą i „Obowiązuje od”; bez hasła |
| `28-admin-historia.png` | Panel `/admin`, „Historia zmian” (lokalna kopia) | brak | 1280 | Na świeżej kopii: „Brak zmian” (żeby pokazać wpis, trzeba najpierw dodać wartość parametru) |
| `29-admin-organizacje.png` | Panel `/admin`, „Organizacje” (lokalna kopia) | brak | 1280 | Lista organizacji ze statusem „Zweryfikowana — widoczna dla użytkowników” |
| `30-telefon-start.png` | Start | brak | 390 | Układ na telefonie; „Start” to logo, w menu Wnioski, Wsparcie, Baza wiedzy |
| `31-telefon-wynik.png` | Wynik, przewinięty do kart | `Anna` | 390 | Karty zasiłku macierzyńskiego i rodzicielskiego, czat jako ikona w prawym dolnym rogu |
| `32-telefon-odmowa.png` | Wynik, przewinięty do karty | `Edyta` | 390 | Odmowa z powodem i „To da się naprawić” na wąskim ekranie |
| `33-telefon-sprawy.png` | Sprawy ZUS | brak | 390 | Lista spraw na wąskim ekranie |

Nie ma w repozytorium:

- `18-sprawa-rozpoznana.png` (Sprawy ZUS po wybraniu pliku PDF sprawy) — wymaga prawdziwego dokumentu ZUS, a aplikacja pokazuje obok jego podgląd. Robi się ręcznie, z podglądem zwiniętym albo wyciętym, i zapisuje poza repozytorium.
- `20-zaswiadczenie-skladki.png` (PDF zaświadczenia o stanie składek, `Edyta`) i strona pobranego PDF wniosku z zrzutu 16 — to pliki PDF, nie ekran aplikacji; przeglądarka w trybie automatycznym ich nie wyświetla. Zrób ręcznie: Wnioski → „Zaświadczenie o stanie składek” → „Wybierz dzień i pobierz”, otwórz PDF i zrób zrzut pierwszej strony (napis „SYMULACJA — dokument nie jest zaświadczeniem ZUS”, saldo 120,00 zł).

## Zrzuty do slajdów

| Plik | Co widać | Kadr |
|---|---|---|
| `slajd-01-start.png` | Start | 1440×900, ×1,25 |
| `slajd-02-odmowa.png` | Karta odmowy (`Edyta`): powód, „To da się naprawić”, przepis | karta, ×2 |
| `slajd-03-logowanie.png` | Logowanie z kafelkami osób | 1440×900, ×1,25 |
| `slajd-03-pytania-169.png` | „Kilka pytań, których ZUS nie zna” z zaznaczoną odpowiedzią (`Anna`) | 16:9 (800×450, ×2) |
| `slajd-03-wynik.png` | Górna część wyniku (`Anna`): „Twój plan”, „W skrócie”, karta, „Co dalej?” | 1440×900, ×1,25 |
| `slajd-04-karta.png` | „W skrócie” i karta zasiłku macierzyńskiego z „Co dalej?” obok | fragment, ×2 |
| `slajd-04-sciezka.png` | Rozwinięta „Ścieżka decyzji”: najważniejsze kroki i „Pokaż wszystkie kroki (19)” | fragment, ×2 |
| `slajd-05-scenariusze.png` | Lista wariantów „co jeśli” (niedostępne wyszarzone na końcu) | fragment, ×2 |
| `slajd-05-roznica.png` | „Różnica: plan bazowy a wariant” (wariant wypłaty uśredniony) | fragment, ×2 |
| `slajd-05-sprawy-zus.png` | Lista „Sprawy ZUS” z oznaczeniami zgodności, bez podglądu dokumentu i bez kwot/dat ze spraw | 1440×900, ×1,25 |
| `slajd-06-telefon-wynik.png` | Wynik na telefonie (`Anna`) | 390×844, ×2 |
| `slajd-06-wnioski-top.png` | Górna część „Wnioski i zgłoszenia” | 720×800, ×2 |
| `slajd-07-faq-telefon.png` | Czat na telefonie: pytanie z FAQ, źródła, rozwinięty cytat | 390×844, ×2 |
| `slajd-08-czat.png` | Czat: odpowiedź z FAQ, źródła jako etykiety, jeden rozwinięty cytat. Na komputerze okno czatu ma stałą wysokość i cała odpowiedź się w nim nie mieści, więc zrzut jest w układzie pełnoekranowym (szerokość 480 px) | 480×800, ×2 |
| `slajd-09-admin.png` | Panel parametrów: zakładki i parametry z polskimi nazwami | 1440×900, ×1,25 |
| `slajd-10-telefon-start.png` | Start na telefonie | 390×844, ×2 |

Materiały graficzne (logo, plansze do filmu i prezentacji) dodawaj w tym katalogu z prefiksem `gfx-`; skrypt ich nie tworzy. Nie używaj godła, znaków ZUS, Gov.pl ani mObywatela jako własnych.
