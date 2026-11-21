# Zrzuty ekranu do wykonania

Zrzutów nie robi automat. Wykonują je ludzie na działającym demo (https://20-215-34-39.sslip.io) i zapisują w tym katalogu pod podanymi nazwami (PNG). Szerokość 1280 px to widok komputera, 390 px to widok telefonu (okno przeglądarki w trybie urządzenia albo prawdziwy telefon). Przed zrzutem sprawdź, że na ekranie nie ma haseł, tokenów ani danych spoza fikcyjnych osób. Panel `/admin` rób na lokalnej kopii.

| Plik | Ekran (adres) | Osoba | Szerokość | Co ma być widać |
|---|---|---|---|---|
| `01-start.png` | Start (`/`) | brak | 1280 | Trzy pytania, pasek kroków |
| `02-logowanie.png` | Logowanie (`/logowanie`) | lista osób | 1280 | Baner „symulacja", lista fikcyjnych osób z przyciskami „Zaloguj" |
| `03-sprawdz-dane.png` | Sprawdź dane (`/sprawdz-dane`) | `anna-przyklad` | 1280 | Lista faktów z plakietkami źródeł i przyciskami „Popraw" |
| `04-wynik-przysluguje.png` | Wynik (`/wynik`) | `anna-przyklad` | 1280 | Karta zasiłku z okresem, stawką i kwotą, „Kto wypłaca", „Gdzie złożyć wniosek" |
| `05-sciezka-decyzji.png` | Wynik, rozwinięta ścieżka decyzji | `anna-przyklad` | 1280 | Kroki: reguła, artykuł, warunek, spełniony lub nie |
| `06-terminy-najblizszy-krok.png` | Wynik, „Co zrobić i do kiedy" | `anna-przyklad` | 1280 | Lista terminów z jednym najbliższym krokiem |
| `07-na-co-uwazac.png` | Wynik, „Na co uważać" | `anna-przyklad` | 1280 | Ryzyka dopasowane do sytuacji |
| `08-odmowa-co-musialoby-sie-zmienic.png` | Wynik | `edyta-demo` | 1280 | Status „nie przysługuje", powód, podstawa prawna, „Co musiałoby się zmienić" (kwota do spłaty i data) |
| `09-porownaj-warianty.png` | Porównaj warianty (`/porownaj-warianty`) | `anna-przyklad`, wariant „Uśredniony" | 1280 | Plan bazowy kontra wariant, różnica w tygodniach i złotych |
| `10-porownanie-zmiana-statusu.png` | Porównaj warianty | `edyta-demo`, „Spłacam zadłużenie" | 1280 | Zmiana statusu świadczenia po spłacie |
| `11-wnioski.png` | Wnioski (`/wnioski`) | `anna-przyklad` | 1280 | Cztery wzory wniosków o urlop, zasiłek i zapytania, „Zaświadczenie o stanie składek" |
| `12-wniosek-potwierdzenie.png` | Wnioski, po „Wyślij (symulacja)" | `anna-przyklad` | 1280 | Potwierdzenie z numerem, „Pobierz PDF", napis „symulacja" |
| `13-wniosek-pdf.png` | Pobrany PDF wniosku | `anna-przyklad` | 1280 | Pierwsza strona wniosku o urlop rodzicielski |
| `14-zaswiadczenie-skladki.png` | PDF zaświadczenia o stanie składek | `edyta-demo` | 1280 | Napis „SYMULACJA — dokument nie jest zaświadczeniem ZUS", saldo 120,00 zł |
| `15-moje-dane-zus.png` | Moje dane w ZUS (`/moje-dane-zus`) | `halina-demo` | 1280 | Ubezpieczenia i płatnicy, podstawy składek, e-ZLA z kodem B |
| `16-sprawy-zus.png` | Sprawy ZUS (`/sprawy`) | brak | 1280 | Lista spraw testowych i „Dodaj PDF sprawy" (stan faktyczny, także komunikat o danych spraw, jeśli jest) |
| `17-wsparcie.png` | Wsparcie (`/wsparcie`) | brak | 1280 | Organizacje (o ile lista nie jest pusta), zasiłek pogrzebowy, linki do instytucji |
| `18-admin-parametry.png` | Panel `/admin`, zakładka „Parametry" (lokalna kopia) | brak | 1280 | Lista parametrów z datami obowiązywania; bez widocznego hasła |
| `19-admin-historia.png` | Panel `/admin`, „Historia zmian" (lokalna kopia) | brak | 1280 | Wpis o zmianie parametru z datą i powodem |
| `20-telefon-start.png` | Start | brak | 390 | Układ na telefonie, nawigacja |
| `21-telefon-wynik.png` | Wynik | `anna-przyklad` | 390 | Karta świadczenia na wąskim ekranie |
| `22-telefon-odmowa.png` | Wynik | `edyta-demo` | 390 | Odmowa z powodem na wąskim ekranie |
| `23-telefon-porownanie.png` | Porównaj warianty | `anna-przyklad` | 390 | Porównanie na wąskim ekranie |
| `24-trudna-sytuacja.png` | Wsparcie lub Wynik w trybie trudnej sytuacji | do ustalenia (osoba z martwym urodzeniem, zgonem dziecka lub wcześniactwem, jeśli demo ją udostępnia) | 1280 | Zmieniony ton, organizacje wsparcia na górze. Do potwierdzenia, czy któraś osoba demo uruchamia ten tryb; jeśli nie, pomiń lub wejdź przez `Wsparcie` z wybraną sytuacją |

Materiały graficzne (logo, plansze do filmu i prezentacji) dodaj w tym samym katalogu z prefiksem `gfx-`. Nie używaj godła, znaków ZUS, Gov.pl ani mObywatela jako własnych.
