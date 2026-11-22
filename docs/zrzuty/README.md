# Zrzuty ekranu do wykonania

Zrzutów nie robi automat. Wykonują je ludzie na działającym demo (https://kompasrodzica.polandcentral.cloudapp.azure.com) i zapisują w tym katalogu pod podanymi nazwami (PNG). Szerokość 1280 px to widok komputera, 390 px to widok telefonu (okno przeglądarki w trybie urządzenia albo prawdziwy telefon). Przed zrzutem sprawdź, że na ekranie nie ma haseł, tokenów ani danych spoza fikcyjnych osób. Panel `/admin` rób na lokalnej kopii.

**Sprawy ZUS.** Dokumentów ZUS i danych z nich nie wolno rozpowszechniać ani wpisywać do repozytorium. Na zrzutach 17–19 nie może być podglądu PDF (zwiń go albo wytnij z kadru). Zrzut wyniku sprawy (19) zapisuj poza repozytorium i dołączaj tylko do zgłoszenia; w repozytorium może zostać wersja przycięta do ramki „Porównanie z decyzją ZUS (sprawa testowa)”, bez kwot i dat.

| Plik | Ekran (adres) | Osoba lub sprawa | Szerokość | Co ma być widać |
|---|---|---|---|---|
| `01-start.png` | Start (`/`) | brak | 1280 | Nagłówek „Sprawdź, co Ci przysługuje”, kafelki „Wprowadź dane ręcznie”, „Połącz się przez mObywatela i ZUS”, „Sprawdź na rzeczywistych przykładach”; nawigacja Start, Wnioski, Wsparcie, Baza wiedzy |
| `02-wpis-reczny.png` | Wpis ręczny (`/wprowadz-dane`) | brak | 1280 | „Pytanie 1 z …”, pasek postępu, pytanie „Co się u Ciebie dzieje?” z czterema odpowiedziami |
| `03-logowanie.png` | Logowanie (`/logowanie`) | lista osób | 1280 | „Zaloguj się przez mObywatela”, baner symulacji, kafelki fikcyjnych osób, przycisk „Zaloguj przez mObywatela” |
| `04-zgoda.png` | Logowanie, krok zgody | `anna-przyklad` | 1280 | „Zgoda na pobranie danych”, „Udostępnij dane z:” (ZUS, e-ZLA, rejestr stanu cywilnego), „Połącz i pobierz dane (symulacja)” |
| `05-potwierdz-dane.png` | Sprawdź dane (`/sprawdz-dane`) | `anna-przyklad` | 1280 | „Potwierdź pobrane dane”, informacje z plakietkami źródeł i przyciskami „Popraw” |
| `06-uzupelnij-dane.png` | Sprawdź dane, po „Potwierdź dane” | `anna-przyklad` | 1280 | „Uzupełnij brakujące dane”, pytania uzupełniające albo informacja, że są wszystkie dane; „Pokaż wynik” |
| `07-wynik-przysluguje.png` | Wynik (`/wynik`) | `anna-przyklad` | 1280 | „Twój plan”, „Twoje świadczenia”, karta zasiłku z okresem i kwotą, panel „Co dalej?” z najbliższym krokiem |
| `08-szczegoly-sciezka-decyzji.png` | Wynik, rozwinięte „Szczegóły świadczenia” i „Ścieżka decyzji” | `anna-przyklad` | 1280 | Stawka, podstawa, „Kto wypłaca”, „Gdzie złożyć wniosek”, „Dokumenty”, kroki ścieżki decyzji |
| `09-terminy.png` | Wynik, „Co zrobić i do kiedy” | `anna-przyklad` | 1280 | Lista terminów z oznaczeniem „To jest Twój najbliższy krok.” |
| `10-ryzyka-wariant.png` | Wynik, rozwinięte „Ryzyka i wariant wypłaty” | `anna-przyklad` | 1280 | „Na co uważać” i „Sprawdź inny wariant” |
| `11-odmowa.png` | Wynik | `edyta-demo` | 1280 | Status „nie przysługuje”, powód, „Podstawa prawna”, „Co musiałoby się zmienić” (kwota do spłaty i data) |
| `12-porownaj-warianty.png` | Porównaj warianty (`/porownaj-warianty`) | `anna-przyklad`, „Wariant wypłaty” → „Uśredniony” | 1280 | Plan bazowy kontra wariant, różnica w tygodniach i złotych |
| `13-porownanie-zmiana-statusu.png` | Porównaj warianty | `edyta-demo`, „Spłacam zadłużenie” | 1280 | Zmiana statusu świadczenia po spłacie |
| `14-porownanie-drugi-rodzic.png` | Porównaj warianty | wpis ręczny „Dla mnie i drugiego rodzica”, oboje na umowie o pracę | 1280 | „Drugi rodzic bierze część urlopu rodzicielskiego”: wybrana osoba i liczba tygodni, zmiana okresów obojga rodziców |
| `15-wnioski.png` | Wnioski (`/wnioski`) | `anna-przyklad` | 1280 | „Wnioski o urlop do pracodawcy” (cztery wzory), „Zasiłek, zapytania i dokumenty”, „Zaświadczenie o stanie składek” |
| `16-wniosek-pdf.png` | Wnioski, po „Przygotuj PDF”, oraz pobrany PDF | `anna-przyklad` | 1280 | „PDF jest gotowy”, „Pobierz PDF”; pierwsza strona wniosku o urlop rodzicielski z napisem o symulacji |
| `17-sprawy-zus.png` | Sprawy ZUS (`/sprawy`) | brak | 1280 | Lista spraw z oznaczeniami („Okres i kwota zgodne z wydrukiem ZUS”, „Okres zgodny z ZUS co do dnia”, „Odmowa zgodna z decyzją ZUS”) i rozwijane „Masz plik PDF sprawy ZUS? Rozpoznaj go” |
| `18-sprawa-rozpoznana.png` | Sprawy ZUS, po wybraniu pliku | plik sprawy `zus-008` | 1280 | „Dodaj PDF sprawy”, „Plik nie opuszcza Twojego urządzenia”, komunikat „To sprawa: …”, „Załaduj dane sprawy”; bez podglądu dokumentu |
| `19-sprawa-porownanie-z-zus.png` | Wynik po wczytaniu sprawy | `zus-008` | 1280 | Ramka „Porównanie z decyzją ZUS (sprawa testowa)” z „Porównanie: zgodne” (zasady zapisu — patrz wyżej) |
| `20-zaswiadczenie-skladki.png` | PDF zaświadczenia o stanie składek | `edyta-demo` | 1280 | Napis „SYMULACJA — dokument nie jest zaświadczeniem ZUS”, saldo 120,00 zł |
| `21-moje-dane-zus.png` | Moje dane w ZUS (`/moje-dane-zus`) | `halina-demo` | 1280 | „Ubezpieczenia i płatnicy”, „Podstawy składek”, „Zwolnienia lekarskie (e-ZLA)” z kodem B, „Saldo składek” |
| `22-moje-konto.png` | Moje konto (`/moje-konto`) | `anna-przyklad` | 1280 | Profil testowy, stan połączenia, „Zobacz mój wynik”, „Moje dane w ZUS”, „Moje wnioski” |
| `23-wsparcie.png` | Wsparcie (`/wsparcie`) | brak | 1280 | „Twoja sytuacja”, „Organizacje wsparcia” z listą organizacji |
| `24-trudna-sytuacja.png` | Wsparcie, „Twoja sytuacja”: „Martwe urodzenie” | brak | 1280 | Zmieniony ton, „Co możesz zrobić teraz”, organizacje i „Zasiłek pogrzebowy” |
| `25-baza-wiedzy.png` | Baza wiedzy (`/baza-wiedzy`) | brak | 1280 | „Poradnik ZUS” ze spisem rozdziałów, „Oficjalne źródła” |
| `26-czat-przepisy.png` | Czat „Zapytaj o przepisy” (Wynik, Wsparcie albo Baza wiedzy) | brak | 1280 | Pytanie o przepis, krótkie wyjaśnienie z dopiskiem, że napisał je model językowy, rozwinięty cytat przepisu |
| `27-admin-parametry.png` | Panel `/admin`, zakładka „Parametry” (lokalna kopia) | brak | 1280 | „Parametry prawne” z datami „Obowiązuje od”; bez widocznego hasła |
| `28-admin-historia.png` | Panel `/admin`, „Historia zmian” (lokalna kopia) | brak | 1280 | Wpis o zmianie parametru z datą obowiązywania |
| `29-admin-organizacje.png` | Panel `/admin`, zakładka „Organizacje” (lokalna kopia) | brak | 1280 | Lista organizacji z oznaczeniem weryfikacji |
| `30-telefon-start.png` | Start | brak | 390 | Układ na telefonie, nawigacja |
| `31-telefon-wynik.png` | Wynik | `anna-przyklad` | 390 | Karta świadczenia i „Co dalej?” na wąskim ekranie |
| `32-telefon-odmowa.png` | Wynik | `edyta-demo` | 390 | Odmowa z powodem na wąskim ekranie |
| `33-telefon-sprawy.png` | Sprawy ZUS | brak | 390 | Lista spraw na wąskim ekranie |

Materiały graficzne (logo, plansze do filmu i prezentacji) dodaj w tym samym katalogu z prefiksem `gfx-`. Nie używaj godła, znaków ZUS, Gov.pl ani mObywatela jako własnych.
