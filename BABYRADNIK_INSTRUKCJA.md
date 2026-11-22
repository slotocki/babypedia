# BabyRadnik — instrukcja implementacji dla orkiestratora

Wersja: sobota 21.11.2026, 15:45. Ten plik jest jedynym źródłem zakresu. Zastępuje wszystkie wcześniejsze plany.
Koniec kodowania: niedziela 22.11.2026, 11:00. Zamrożenie kodu: 9:30. Zgłoszenie: do 10:30.

Znaczniki: **[TWARDE]** = nie wolno złamać. **[PARAM]** = wartość konfigurowalna, nie stała w kodzie. **[DO WERYFIKACJI]** = niepotwierdzone; zaimplementuj jako ustawienie z podaną wartością domyślną i nie zgaduj dalej.

---

## 1. Co budujemy

BabyRadnik to responsywna aplikacja WWW na hackathon HackNation 2026, zadanie ZUS „Zasiłkowy Przewodnik Przyszłego Rodzica". Dla konkretnej osoby aplikacja:

1. ustala, które świadczenia z ubezpieczenia chorobowego przysługują, a które nie i dlaczego,
2. wylicza okres i wysokość,
3. pokazuje terminy, dokumenty i miejsce złożenia wniosku,
4. ostrzega przed ryzykiem utraty uprawnień,
5. pozwala sprawdzić skutki zmian („co jeśli").

Główny przebieg (pierwszy, który ma działać od początku do końca):

**trzy pytania → logowanie i pobranie danych (symulacja) → sprawdź dane → świadczenia, terminy i najbliższy krok**

Kryteria oceny jury: związek z wyzwaniem 25%, kompletność i UX 25%, pomysł 20%, oryginalność 15%, potencjał wdrożeniowy 15%. Jury testuje demo praktycznie na przykładowych sprawach ZUS i przegląda strukturę kodu.

## 2. Zasady nadrzędne [TWARDE]

1. **Uprawnienia, okresy, kwoty i terminy liczy wyłącznie deterministyczny silnik reguł w C#.** Żaden model językowy nie bierze udziału w decyzji ani w kwocie.
2. **Bez OCR i bez odczytu treści PDF.** Plik PDF sprawy służy tylko do rozpoznania sprawy i do podglądu.
3. **Wszystkie integracje są symulowane** i każda jest na ekranie oznaczona słowem „symulacja". Nie łącz się z prawdziwymi systemami.
4. **Dokumenty ZUS (PDF spraw) nigdy nie trafiają do repozytorium, logów ani zewnętrznych usług.** Katalog z nimi jest w `.gitignore`.
5. **Wyłącznie fikcyjne dane osób.** PESEL występuje tylko w odpowiedzi symulowanego logowania, jest fikcyjny (nie przechodzi walidacji sumy kontrolnej), służy do wyliczenia wieku. Nie trafia do sprawy, sesji, logów ani do żądania do silnika. Użytkownik nigdy go nie wpisuje.
6. **Parametry prawne są w bazie z datą obowiązywania**, nie w kodzie. Logika reguł jest w kodzie.
7. **Reguły pisz z wyciągów przepisów w `docs/prawo/`, nie z pamięci.** Jeśli przepis nie rozstrzyga sytuacji albo jej nie rozpoznajesz, silnik zwraca „nie rozstrzygam — skontaktuj się z ZUS", nie zgaduje.
8. **Testy referencyjne powstają przed regułami.** Reguła jest gotowa, gdy jej przypadki z `fixtures/reference/` są zielone.
9. **Aplikacja informuje, nie orzeka.** Przy ryzykach wymagających postępowania ZUS (pozorność umowy, zawyżona podstawa) pokazuje informację i odesłanie, bez oceniania użytkownika.
10. **Bez sekretów w repozytorium.** Tylko `.env.example`.
11. Zależności tylko na licencjach pozwalających przenieść prawa do kodu (MIT, Apache-2.0, BSD). Każdą nową zależność zatwierdza koordynator.

## 3. Poza zakresem

Nie buduj: OCR, parsera PDF, RAG, bazy wektorowej, hostowania modelu, kont z hasłami, powiadomień e-mail/SMS, kwot netto, świadczeń spoza ubezpieczenia chorobowego (800+, żłobkowe), etapów rozwoju dziecka, wersji językowych, mikroserwisów, kolejek.

Pomocnik tekstowy jest opcjonalny (P2): prosta wyszukiwarka po fragmentach przepisów albo mały model do pytań opisowych. Nie podaje kwot ani decyzji i nie dostaje danych osoby.

---

## 4. Wymagania ZUS i gdzie je spełniamy

Wszystkie pozycje są w P0. Wersja może być cienka, ale musi działać od kliknięcia do potwierdzenia.

| ID | Wymaganie z zadania (pkt 3) | Realizacja |
|---|---|---|
| a | Świadczenia w ciąży, także brak uprawnień: zasiłek chorobowy, świadczenie rehabilitacyjne, zasiłek w wysokości zasiłku macierzyńskiego | Katalog świadczeń, statusy z powodem |
| b | Kalkulator zasiłku macierzyńskiego dla obojga rodziców: okres i wysokość, liczba dzieci, wiek, „Za życiem" | Silnik: okresy + kwoty |
| c | Łączenie pracy z urlopem rodzicielskim | Scenariusz „praca na część etatu" |
| d | Symulacja korzystania przez oboje rodziców | Ekran „Porównaj warianty" |
| e, j | Rodzice adopcyjni i rodziny zastępcze (jeden moduł) | Silnik: przyjęcie dziecka, dwie ścieżki z art. 183 KP |
| f | Indywidualna podstawa wymiaru na konkretnej sprawie | Podstawa wymiaru poziom 1 + ręczne wpisanie |
| g | Dokumenty, terminy, sposób i miejsce złożenia wniosku (płatnik czy ZUS) | Terminy i „najbliższy krok" |
| h | Zagrożenia: utrata uprawnień, zmiana wymiaru lub wysokości | Blok „Na co uważać" + „co jeśli" |
| i | Podmioty wyłącznie charytatywne w sytuacjach szczególnych | Katalog organizacji |
| S1 | Logowanie przez mObywatel (symulacja) | Ekran logowania symulowanego |
| S2 | Dane z ZUS o ubezpieczeniu lub jego braku (symulacja) + wpis ręczny | Symulator ZUS, ekran „Sprawdź dane" |
| S3 | Zadłużenie składkowe na dzień powstania prawa (symulacja zaświadczenia i kwoty) | Symulator: saldo + zaświadczenie PDF |
| S4 | Podgląd e-ZLA (symulacja) | Symulator: lista zwolnień |
| S5 | PUE/eZUS: zapytanie w razie wątpliwości, podgląd danych konta (symulacja) | Symulowane zgłoszenie + „Moje dane w ZUS" |
| S6 | Wypełnienie wniosków o urlopy i świadczenia, przesłanie do pracodawcy lub ZUS (symulacja) | Generator wniosków + symulowana wysyłka |
| S7 | Wniosek o odpis aktu urodzenia (symulacja) | Symulowane zgłoszenie |
| S8 | Informacja o podmiotach wspierających | Ekran „Wsparcie" |
| S9 | Przekierowania do zus.pl, MRPiPS, PIP | Linki przy wynikach |
| S10 | Panel administracyjny do zmiany parametrów | `/admin` |
| T1 | Pełna responsywność | Układ od 360 px |
| T2 | Demo: upload lub wybór przykładowych spraw; weryfikacja ubezpieczenia i kalkulator | Ekran „Sprawy ZUS" |

Linki do S9: `https://www.zus.pl`, `https://www.gov.pl/web/rodzina/urlop-rodzicielski`, `https://www.pip.gov.pl/dla-pracodawcow/porady-prawne/rodzicielstwo`, do S7: `https://www.gov.pl/web/cyfryzacja/rejestr-stanu-cywilnego`.

---

## 5. Przebieg i ekrany

Każdy ekran prowadzi do jednej głównej akcji. Statusy opisuj tekstem, nie samym kolorem. Błąd pokazuj przy polu, z informacją, co zrobić dalej. Własna identyfikacja BabyRadnik; nie używaj godła ani znaków Gov.pl, mObywatel i ZUS jako własnych.

1. **Start — trzy pytania.**
   - Co się dzieje? `planuję dziecko` / `jestem w ciąży` / `dziecko się urodziło` / `przyjmuję dziecko na wychowanie`.
   - Kiedy? przewidywana data porodu / data urodzenia / data przyjęcia (i wiek dziecka).
   - Dla kogo liczymy? `dla mnie` / `dla mnie i drugiego rodzica`.
2. **Logowanie przez mObywatel (symulacja).** Wybór fikcyjnej osoby z listy. Alternatywa: „Wprowadzę dane sam".
3. **Sprawdź dane.** Lista faktów; przy każdym źródło (mObywatel, ZUS, e-ZLA, rejestr stanu cywilnego, Ty, założenie) i przycisk „Popraw". Pod spodem tylko te pytania, które silnik zgłosił jako brakujące.
4. **Wynik.** Cztery bloki:
   - „Co Ci przysługuje": karta na świadczenie, status, okres, kwota, rozwijana ścieżka decyzji.
   - „Co zrobić i do kiedy": terminy posortowane, na górze jeden „najbliższy krok".
   - „Na co uważać": ryzyka dopasowane do sytuacji.
   - „Sprawdź inny wariant": wejście do „Porównaj warianty".
5. **Porównaj warianty („co jeśli").** Plan bazowy kontra wariant; wynik jako różnica w tygodniach i złotych oraz zmiana statusów.
6. **Wnioski i zgłoszenia.** Wniosek o urlop (PDF), dane do wniosku ZAM, zapytanie do ZUS, wniosek o odpis aktu urodzenia, zaświadczenie o stanie składek; każde kończy się potwierdzeniem z numerem i wpisem w „Moje zgłoszenia".
7. **Moje dane w ZUS (symulacja).** Ubezpieczenia i płatnicy, podstawy składek, e-ZLA, saldo.
8. **Sprawy ZUS.** Lista spraw testowych do wyboru oraz „Dodaj PDF sprawy". Wybranie ładuje odpowiadającą osobę z symulatora i przechodzi do ekranu 3; obok podgląd dokumentu, jeśli plik dodano. Na wyniku porównanie z decyzją ZUS.
9. **Wsparcie.** Organizacje filtrowane po sytuacji; zasiłek pogrzebowy; linki.
10. **Panel administratora** (`/admin`, hasło ze zmiennej środowiskowej).

**Tryb trudnej sytuacji:** gdy fakty wskazują martwe urodzenie, zgon dziecka, wcześniactwo z hospitalizacją albo ciężką chorobę, interfejs zmienia ton: bez gratulacji, krótkie kroki, organizacje wsparcia na górze.

---

## 6. Model danych wejściowych: fakty z pochodzeniem

Silnik dostaje zbiór faktów. Każdy fakt: `wartość`, `źródło` (`mobywatel` | `zus` | `ezla` | `usc` | `user` | `assumption`), opcjonalnie `strona` (numer strony w dokumencie sprawy).

### 6.1. Co zwraca każda symulowana integracja

Zakres pól odpowiada temu, co te systemy faktycznie udostępniają.

| Źródło | Pola | Użycie |
|---|---|---|
| mObywatel (login) | imię, nazwisko, nazwisko rodowe, PESEL (fikcyjny), data urodzenia | tożsamość; wiek ≥ 50 lat |
| ZUS — konto ubezpieczonego | lista tytułów: kod tytułu, opis, płatnik, data od, data do, rodzaje ubezpieczeń (chorobowe: obowiązkowe/dobrowolne/brak); podstawy wymiaru składek za 12 miesięcy; przerwy; pobierane świadczenia (rodzaj, okres); wyrejestrowanie | prawo do zasiłku, okres wyczekiwania, szacunek podstawy, wykorzystane części urlopu i dni zasiłku |
| ZUS — konto płatnika | liczba osób zgłoszonych do ubezpieczeń na 30 listopada poprzedniego roku; saldo składek na wskazany dzień | kto wypłaca; zadłużenie |
| e-ZLA | okres od–do, kod literowy (B = ciąża), pobyt w szpitalu, data wystawienia | zasiłek chorobowy, licznik 270 dni |
| Rejestr stanu cywilnego | po urodzeniu: dziecko (data urodzenia), rodzice | potwierdzenie porodu, liczba dzieci |

Kody tytułów potwierdzone w dokumentach ZUS: `0110` pracownik, `1240` osoba pobierająca zasiłek macierzyński. Pozostałe kody traktuj jako dane opisowe; logikę opieraj na polu `rodzajTytułu`, nie na kodzie.

Podstawy składek z ZUS dają **szacunek** podstawy zasiłku. Pokazuj jako „szacunek z danych ZUS" i pozwól wpisać „podstawę ustaloną przez płatnika".

### 6.2. O co pytamy użytkownika (tylko gdy potrzebne)

| Fakt | Kiedy pytać |
|---|---|
| zdarzenie, data, dla kogo liczymy | zawsze (ekran 1) |
| liczba dzieci przy jednym porodzie / przyjęciu | przed porodem lub przy przyjęciu |
| ścieżka przyjęcia: przysposobienie (wniosek do sądu opiekuńczego) / rodzina zastępcza niezawodowa / rodzina zastępcza zawodowa; wiek dziecka; odroczenie obowiązku szkolnego | tylko przy przyjęciu dziecka |
| umowa na czas określony i data jej końca; planowane odejście z pracy | pracownik lub zleceniobiorca przed porodem |
| tydzień ciąży przy porodzie, masa urodzeniowa, okresy pobytu dziecka w szpitalu | po porodzie, gdy zaznaczono wcześniactwo lub hospitalizację |
| zaświadczenie „Za życiem" | gdy zaznaczono ciężką chorobę dziecka |
| martwe urodzenie / zgon dziecka i data | gdy użytkownik to zaznaczy |
| sytuacja matki: orzeczenie o niezdolności do samodzielnej egzystencji, pobyt w szpitalu, zgon, porzucenie dziecka | gdy liczymy dla ojca i użytkownik to zaznaczy |
| plan: wariant wypłaty, podział tygodni między rodziców, praca na część etatu, liczba części | ekran wyniku, wartości domyślne |
| podstawa ustalona przez płatnika | opcjonalnie |

Wyłączenie pola integracji w panelu administratora zamienia je automatycznie w pytanie do użytkownika.

---

## 7. Katalog świadczeń i statusy

Każde świadczenie to wpis katalogu: identyfikator, nazwa, wymagane fakty, reguły uprawnień, reguła okresu, reguła kwoty, terminy, dokumenty, linki, `włączone`.

Silnik ocenia cały katalog i dla każdego wpisu zwraca jeden status:

| Status | Zawartość odpowiedzi |
|---|---|
| `przysługuje` | okres (od–do, dni, tygodnie), stawka, kwota (jeśli znana podstawa), terminy, gdzie złożyć, ścieżka decyzji |
| `nie_przysługuje` | powód jednym zdaniem + podstawa prawna + „co musiałoby się zmienić" (jeśli cokolwiek) |
| `brakuje_danych` | lista brakujących faktów jako gotowe pytania |
| `nie_dotyczy` | nic; interfejs ukrywa wpis |
| `nie_rozstrzygam` | komunikat i kanały kontaktu z ZUS |

Katalog P0:

1. `chorobowy_w_ciazy` — zasiłek chorobowy w okresie ciąży
2. `rehabilitacyjne` — świadczenie rehabilitacyjne
3. `zasilek_do_dnia_porodu` — zasiłek w wysokości zasiłku macierzyńskiego (art. 30 ust. 3)
4. `macierzynski` — za okres urlopu macierzyńskiego
5. `macierzynski_uzupelniajacy` — za okres uzupełniającego urlopu macierzyńskiego
6. `rodzicielski` — za okres urlopu rodzicielskiego
7. `ojcowski` — za okres urlopu ojcowskiego
8. `przyjecie_dziecka` — za okres urlopu na warunkach urlopu macierzyńskiego
9. `przejecie_przez_ojca` — przejęcie zasiłku w sytuacjach szczególnych matki
10. `pogrzebowy` — informacja o zasiłku pogrzebowym (bez wyliczeń poza kwotą)

Ścieżka decyzji: lista kroków `{reguła, podstawa prawna, warunek, spełniony: tak/nie, wyjaśnienie prostym językiem}`.

---

## 8. Reguły prawne

Źródła: `docs/prawo/` — wyciąg z ustawy zasiłkowej („ustawa"), wyciąg z Kodeksu pracy („KP"), Załącznik ZUS, rozporządzenie z 27.09.2026 (dokumenty do zasiłków), rozporządzenie z 11.03.2025 (wnioski pracownicze). Przed implementacją reguły przeczytaj wskazany artykuł.

### 8.1. Parametry [PARAM] — dane startowe

| Klucz | Wartość | Podstawa |
|---|---|---|
| macierzynski_tyg (1/2/3/4/5+ dzieci) | 20 / 31 / 33 / 35 / 37 | KP art. 180 § 1 |
| macierzynski_przed_porodem_max_tyg | 6 | KP art. 180 § 2 |
| matka_min_po_porodzie_tyg | 14 | KP art. 180 § 4; ustawa art. 29 ust. 3 |
| matka_min_po_porodzie_szczegolne_tyg | 8 | ustawa art. 29 ust. 5–7 |
| rodzicielski_tyg (1 / więcej dzieci) | 41 / 43 | KP art. 182¹a § 1 |
| rodzicielski_za_zyciem_tyg | 65 / 67 | KP art. 182¹a § 2 |
| rodzicielski_nieprzenoszalne_tyg | 9 | KP art. 182¹a § 4 |
| rodzicielski_max_czesci | 5 | KP art. 182¹c |
| rodzicielski_do_konca_roku_wieku | 6 | KP art. 182¹c |
| praca_na_rodzicielskim_max_etat | 0,5 | KP art. 182¹e |
| rodzicielski_z_praca_max_tyg | 82 / 86; z „Za życiem" 130 / 134 | KP art. 182¹f |
| ojcowski_tyg | 2; maks. 2 części, każda ≥ 1 tydzień | KP art. 182³ |
| ojcowski_do_miesiaca_zycia | 12 | KP art. 182³ § 1 |
| przyjecie_min_tyg | 9 | KP art. 183 § 3 |
| przyjecie_rodzicielski_przy_minimum_tyg | 38; z „Za życiem" 62 | KP art. 183 § 4, 4², 4³ |
| wiek_przysposobienie_max | 14 | KP art. 183 § 1¹; ustawa art. 29 ust. 1 pkt 2 |
| wiek_rodzina_zastepcza_max | 7; przy odroczeniu obowiązku szkolnego 10 | KP art. 183 § 1; ustawa art. 29 ust. 1 pkt 3 |
| martwe_urodzenie_tyg_po_porodzie | 8; nie krócej niż 7 dni od zgonu | KP art. 180¹ § 1 |
| zgon_po_8_tyg_dni | 7 dni od zgonu | KP art. 180¹ § 2 |
| stawka_macierzynski / uzupelniajacy / ojcowski | 100% | ustawa art. 31 |
| stawka_rodzicielski | 70% | ustawa art. 31 |
| stawka_usredniona | 81,5% | ustawa art. 31 ust. 3 |
| stawka_9_tyg_drugiego_rodzica | 70% | ustawa art. 31 ust. 3f |
| termin_wniosku_usredniona_dni | 21 dni po porodzie / przyjęciu | ustawa art. 30a |
| chorobowy_ciaza_stawka | 100% | ustawa art. 11 ust. 2 |
| chorobowy_ciaza_max_dni | 270 | ustawa art. 8 ust. 1 |
| wyczekiwanie_obowiazkowe_dni / dobrowolne_dni | 30 / 90 | ustawa art. 4 ust. 1 |
| wyczekiwanie_max_przerwa_dni | 30 | ustawa art. 4 ust. 2 |
| wynagrodzenie_chorobowe_dni (do 50 lat / od 50 lat) | 33 / 14 w roku | Załącznik |
| rehabilitacyjne_stawki | 90% pierwsze 3 miesiące, potem 75%; 100% w okresie ciąży | ustawa art. 19 ust. 1 |
| rehabilitacyjne_max_miesiecy | 12 | ustawa art. 18 |
| odliczenie_skladek | 13,71% | ustawa art. 3 pkt 4; Załącznik |
| placa_minimalna | 4 806,00 zł od 1.01.2026 | Załącznik |
| podstawa_minimalna_pelny_etat | 4 147,10 zł (4 806 × 0,8629) | ustawa art. 45 |
| max_podstawa_dobrowolnej_skladki | 23 550,00 zł → limit podstawy zasiłku 20 321,30 zł | Załącznik |
| prog_zadluzenia | 1% płacy minimalnej = 48,06 zł | ustawa art. 2a ust. 1 |
| termin_splaty_zadluzenia_mies | 6 | ustawa art. 2a ust. 2 |
| prog_platnika_ubezpieczonych | 20 | ustawa art. 61 ust. 1 |
| przedawnienie_mies | 6 | ustawa art. 67 |
| zasilek_pogrzebowy | 7 000 zł; wniosek Z-12 do 12 miesięcy od pochówku | Poradnik ZUS s. 35–36 |
| kwota_swiadczenia_rodzicielskiego | **[DO WERYFIKACJI]** brak w materiałach; domyślnie puste = nie stosuj podwyższenia z art. 31 ust. 3a, pokaż informację | ustawa art. 31 ust. 3a |
| zaokraglanie | **[DO WERYFIKACJI]** domyślnie: stawka dzienna do 0,01 zł, kwota za okres = stawka dzienna × dni | — |
| kwoty | **[DO WERYFIKACJI]** domyślnie brutto, z dopiskiem „przed zaliczką na podatek" | — |

Który dzień wybiera parametr: wymiary — dzień porodu lub przyjęcia dziecka; stawki, progi i płaca minimalna — dzień, za który przysługuje zasiłek (dla progu zadłużenia: dzień powstania prawa).

### 8.2. Tytuły ubezpieczenia

| Cecha | Pracownik | Zleceniobiorca | Działalność / osoba współpracująca | Brak ubezpieczenia chorobowego |
|---|---|---|---|---|
| Ubezpieczenie chorobowe | obowiązkowe | dobrowolne | dobrowolne | — |
| Okres wyczekiwania na zasiłek chorobowy | 30 dni | 90 dni | 90 dni | — |
| Wynagrodzenie chorobowe (33/14 dni) przed zasiłkiem | tak | nie | nie | — |
| Zasiłek macierzyński | za okres udzielonego urlopu | za okres odpowiadający urlopowi | za okres odpowiadający urlopowi | nie |
| Okres wyczekiwania na zasiłek macierzyński | brak | brak | brak | — |
| Wniosek o urlop do pracodawcy | tak | nie | nie | — |
| Minimalna podstawa z płacy minimalnej | tak (proporcjonalnie do etatu) | nie | nie | — |
| Kontrola zadłużenia (art. 2a) | nie | nie | **tak** | — |
| Ochrona stosunku pracy (KP art. 177) | tak | nie | nie | — |

W kodzie rozdziel `okres zasiłku` (każdy ubezpieczony) od `wniosek o urlop` (tylko pracownik). Zbieg tytułów z chorobowym → prawo `przysługuje` z każdego tytułu (art. 57 ust. 1c, art. 61 ust. 4), a podstawa, kwota i płatnik każdego tytułu → `nie_rozstrzygam`. Tytuł bez chorobowego (np. działalność tylko ze składką zdrowotną obok etatu) nie tworzy zbiegu.

### 8.3. Prawo do zasiłku macierzyńskiego (ustawa art. 29–30)

1. Podstawowy warunek: poród albo przyjęcie dziecka **w okresie ubezpieczenia chorobowego** (albo urlopu wychowawczego). Brak ubezpieczenia w tym dniu → `nie_przysługuje`.
2. Wyjątki po ustaniu ubezpieczenia w okresie ciąży (art. 30):
   - ustanie wskutek śmierci, upadłości lub likwidacji pracodawcy → zasiłek macierzyński przysługuje; dodatkowo pracownicy do dnia porodu przysługuje `zasilek_do_dnia_porodu` (ust. 3);
   - ustanie z naruszeniem prawa stwierdzonym prawomocnym orzeczeniem sądu → zasiłek macierzyński po porodzie;
   - umowa na czas określony przedłużona do dnia porodu (KP art. 177 § 3) → zasiłek macierzyński po ustaniu ubezpieczenia (ust. 4).
   - **Własne wypowiedzenie pracownicy nie jest wyjątkiem** → `nie_przysługuje`.
3. Matka nieubezpieczona, ojciec ubezpieczony → ojcu przysługuje zasiłek za okres urlopu rodzicielskiego oraz za okres urlopu ojcowskiego.
4. Rezygnacja matki po co najmniej 14 tygodniach po porodzie → resztę może przejąć ubezpieczony ojciec (art. 29 ust. 3).
5. Sytuacje szczególne matki (art. 29 ust. 5–7), próg 8 tygodni po porodzie: orzeczenie o niezdolności do samodzielnej egzystencji; pobyt matki w szpitalu (zasiłek matki przerywa się na ten okres); zgon matki; porzucenie dziecka.
6. Przyjęcie dziecka — **dwie różne ścieżki**:
   - przysposobienie (przyjęcie + wniosek do sądu opiekuńczego): dziecko do 14. roku życia;
   - rodzina zastępcza niezawodowa: dziecko do 7. roku życia, a do 10. tylko przy odroczeniu obowiązku szkolnego;
   - rodzina zastępcza zawodowa: `nie_przysługuje`.
   - Okres 20–37 tygodni, ale nie dłużej niż do granicy wieku; jeśli do granicy wieku zostaje mniej, przysługuje minimum 9 tygodni i wtedy urlop rodzicielski 38 tygodni (KP art. 183 § 3, 4, 4²).
7. Zadłużenie (art. 2a) — dotyczy osób prowadzących działalność i współpracujących: zadłużenie z tytułu składek powyżej progu w dniu powstania prawa → `nie_przysługuje` do czasu spłaty całości; po 6 miesiącach bez spłaty prawo się przedawnia. „Co musiałoby się zmienić": kwota do spłaty i data graniczna. Dotyczy także zasiłku chorobowego i zasiłku za okres urlopu ojcowskiego.

### 8.4. Świadczenia w okresie ciąży

- Zasiłek chorobowy: wymaga upływu okresu wyczekiwania (30/90 dni nieprzerwanego ubezpieczenia; wliczaj wcześniejsze okresy, jeśli przerwa ≤ 30 dni). Bez wyczekiwania m.in.: co najmniej 10 lat wcześniejszego obowiązkowego ubezpieczenia; absolwent objęty ubezpieczeniem w ciągu 90 dni od ukończenia szkoły (art. 4 ust. 3). Pracownik: najpierw wynagrodzenie chorobowe od pracodawcy (33/14 dni w roku), potem zasiłek.
- Niezdolność w trakcie ciąży: 100%, okres zasiłkowy do 270 dni.
- Po wyczerpaniu okresu zasiłkowego: świadczenie rehabilitacyjne do 12 miesięcy, stawki wg parametru.
- Po ustaniu tytułu ubezpieczenia: wg art. 6–7 ustawy; przeczytaj przepis i zakoduj dokładnie, w razie wątpliwości `nie_rozstrzygam`.
- Utrata prawa: praca zarobkowa lub wykorzystywanie zwolnienia niezgodnie z celem (art. 17) — pokazuj jako ryzyko, nie rozstrzygaj.

### 8.5. Okresy — arytmetyka

- Tydzień = 7 dni kalendarzowych. Okres liczony włącznie z dniem początkowym i końcowym.
- Macierzyński: od dnia porodu (albo do 6 tygodni wcześniej) przez `macierzynski_tyg × 7` dni.
- Uzupełniający (KP art. 180²), tydzień za tydzień pobytu dziecka w szpitalu; suma dni pobytu ÷ 7, zaokrąglona w górę do pełnego tygodnia:
  - poród przed ukończeniem 28. tygodnia ciąży **lub** masa ≤ 1000 g → maks. 15 tygodni;
  - poród po ukończeniu 28. i przed ukończeniem 37. tygodnia, masa > 1000 g → maks. 8 tygodni;
  - poród po ukończeniu 37. tygodnia, pobyt co najmniej 2 kolejne dni, pierwszy z nich między 5. a 28. dniem po porodzie → maks. 8 tygodni, liczone od 5. dnia;
  - wieloraczki: najniższa masa, najdłuższy pobyt;
  - przypada bezpośrednio po macierzyńskim, przed rodzicielskim; stawka 100%.
- Rodzicielski: bezpośrednio po macierzyńskim (i uzupełniającym) albo później, do 5 części, do końca roku kalendarzowego, w którym dziecko kończy 6 lat. Wniosek o szóstą część → `nie_przysługuje`.
- Część nieprzenoszalna: z łącznego wymiaru każdy rodzic ma 9 tygodni wyłącznie dla siebie. Jeden rodzic może wykorzystać najwyżej `wymiar − 9` tygodni (32 lub 34; z „Za życiem" 56 lub 58).
- Ojcowski: 2 tygodnie, jednorazowo albo w 2 częściach po tygodniu, do ukończenia przez dziecko 12 miesięcy (przysposobienie: 12 miesięcy od uprawomocnienia postanowienia, nie dłużej niż do 14. roku życia). Po terminie → `nie_przysługuje`.
- Martwe urodzenie lub zgon dziecka przed upływem 8 tygodni życia: 8 tygodni po porodzie, nie krócej niż 7 dni od zgonu. Zgon po 8 tygodniach życia: 7 dni od zgonu. Jedno z wieloraczków: wymiar jak dla pozostałych przy życiu.
- Praca na część etatu w czasie urlopu rodzicielskiego (najwyżej pół etatu): urlop wydłuża się o `tygodnie łączenia × wymiar etatu`, do limitu z parametru.

### 8.6. Kwoty

- Stawka dzienna = podstawa wymiaru ÷ 30. Zasiłek za okres = stawka dzienna × stawka procentowa × liczba dni kalendarzowych.
- Wariant standardowy: macierzyński 100%, rodzicielski 70%.
- Wariant uśredniony (wniosek do 21 dni po porodzie lub przyjęciu, „pełny wymiar"): macierzyński i część rodzicielskiego przypadająca temu rodzicowi po 81,5%. Dziewięć tygodni drugiego rodzica zawsze 70%.
- Po terminie 21 dni: rodzicielski 70%.
- Jeśli żaden dzień rodzicielskiego nie zostanie wykorzystany w 1. roku życia dziecka: rodzicielski 70%, a macierzyński pobrany po 81,5% wyrównuje się do 100% na wniosek (art. 31 ust. 3g i 4).
- Uzupełniający i ojcowski: 100%.
- Praca na część etatu: zasiłek za te dni × (1 − wymiar etatu) (art. 31 ust. 4a).
- Brak podstawy wymiaru → zwróć okresy i stawki procentowe, pole kwoty puste, komunikat „podaj podstawę, żeby zobaczyć kwotę".

### 8.7. Podstawa wymiaru — poziom 1

Pracownik (ustawa art. 36–42, 45):
1. Średnia wynagrodzeń z 12 miesięcy kalendarzowych przed miesiącem powstania prawa; przy krótszym zatrudnieniu — z pełnych miesięcy.
2. Wynagrodzenie = przychód stanowiący podstawę składki chorobowej pomniejszony o 13,71%.
3. Miesiąc, w którym pracownik z przyczyn usprawiedliwionych przepracował mniej niż połowę obowiązującego czasu pracy — pomiń. Przepracował co najmniej połowę — uzupełnij składniki stałe do pełnego miesiąca.
4. Składniki kwartalne: 1/12 sumy z 4 kwartałów. Składniki roczne: 1/12 kwoty za poprzedni rok.
5. Wynik nie niższy niż minimalna podstawa dla wymiaru etatu.
6. Przerwa między okresami pobierania zasiłków krótsza niż miesiąc kalendarzowy → podstawy nie przeliczaj (art. 43).

Osoba niebędąca pracownikiem (art. 48–50):
1. Średni miesięczny przychód (zadeklarowana podstawa składki chorobowej pomniejszona o 13,71%) z 12 miesięcy.
2. Ubezpieczenie krótsze niż 12 miesięcy: wzór z art. 48a — najniższa podstawa + (nadwyżka ponad najniższą podstawę × liczba pełnych miesięcy ubezpieczenia ÷ 12). Przeczytaj przepis przed implementacją.
3. Limit podstawy wg parametru.

Wszystko poza powyższym → pole „podstawa ustalona przez płatnika" i komunikat wprost wymieniający, czego nie liczymy.

### 8.8. Kto wypłaca i gdzie złożyć wniosek (ustawa art. 61)

- Płatnik zgłaszający ponad 20 ubezpieczonych (stan na 30 listopada poprzedniego roku) → zasiłek wypłaca płatnik; wniosek do płatnika.
- Płatnik do 20 ubezpieczonych → wypłaca ZUS; pracownik składa wniosek pracodawcy, który przekazuje do ZUS zaświadczenie Z-3 (zleceniobiorca: Z-3a).
- Działalność, osoba współpracująca, po ustaniu ubezpieczenia → zawsze ZUS; wniosek ZAM do ZUS.
- Formularze: ZAM (wniosek o zasiłek macierzyński; od 24.10.2026 zastąpił też ZUR i ZAO), Z-3, Z-3a, ZAS-53 (chorobowy dla prowadzących działalność), Z-12 (pogrzebowy). Z-3b formalnie zlikwidowano 24.10.2026, ale występuje w starszych sprawach — rozpoznawaj nazwę, nie proponuj go użytkownikowi.
- ZUS wypłaca w ciągu 30 dni od wyjaśnienia ostatniej okoliczności (art. 64) — pokaż jako informację.

### 8.9. Terminy

Każdy termin: typ, data wyliczona od zdarzenia, komunikat tekstowy, skutek przekroczenia.

| Czynność | Typ | Reguła | Skutek | Podstawa |
|---|---|---|---|---|
| Wniosek o macierzyński przed porodem | najpóźniej przed | 14 dni przed początkiem | późniejszy start | Poradnik s. 27 |
| Wniosek o wariant 81,5% | najpóźniej do | 21 dni po porodzie / przyjęciu | rodzicielski 70% | ustawa art. 30a |
| Rezygnacja matki z części macierzyńskiego | najpóźniej przed | 7 dni przed powrotem do pracy | — | KP art. 180 § 8 |
| Wniosek ojca o część macierzyńskiego | najpóźniej przed | 14 dni przed | — | KP art. 180 § 9 |
| Wniosek o uzupełniający | najpóźniej przed | 21 dni przed końcem macierzyńskiego | — | KP art. 180² § 4 |
| Wniosek o rodzicielski / pracę na część etatu | najpóźniej przed | 21 dni przed | — | KP art. 182¹d, 182¹e |
| Wniosek o ojcowski | najpóźniej przed | 7 dni przed | — | KP art. 182³ § 2 |
| Wykorzystanie ojcowskiego | okno | do 12. miesiąca życia dziecka | prawo wygasa | KP art. 182³ § 1 |
| Wykorzystanie rodzicielskiego | okno | do końca roku, w którym dziecko kończy 6 lat | prawo wygasa | KP art. 182¹c |
| Przyjęcie dziecka: wniosek o urlop | najpóźniej do | 7 dni od przyjęcia; start urlopu do 21 dni | — | KP art. 183 § 6 |
| Spłata zadłużenia | najpóźniej do | 6 miesięcy od dnia powstania prawa | prawo przepada | ustawa art. 2a ust. 2 |
| Roszczenie o zasiłek | przedawnienie | 6 miesięcy od ostatniego dnia okresu | roszczenie przepada | ustawa art. 67 |

„Najbliższy krok" = najwcześniejszy niewykonany termin typu „najpóźniej".

### 8.10. Ryzyka („Na co uważać")

Pokazuj tylko pasujące do faktów:
- koniec ubezpieczenia przed porodem (koniec umowy, odejście z pracy, zamknięcie działalności);
- brak zgłoszenia do dobrowolnego ubezpieczenia chorobowego (zlecenie, działalność); ulga na start = brak ubezpieczenia chorobowego;
- zadłużenie składkowe powyżej progu;
- dobrowolne ubezpieczenie chorobowe ustaje po nabyciu prawa do zasiłku macierzyńskiego — trzeba zgłosić się ponownie;
- przegapienie 21 dni na wariant 81,5%; niewykorzystany urlop ojcowski; przedawnienie;
- podwyższenie podstawy składek w ciąży lub umowa zawarta tuż przed porodem mogą być wyjaśniane przez ZUS (informacja neutralna);
- praca w czasie zwolnienia lekarskiego.

---

## 9. „Co jeśli" / Porównaj warianty

Scenariusz to nakładka na fakty: zmienia wybrane fakty, silnik liczy drugi raz, API zwraca oba wyniki i różnicę (statusy, tygodnie, złote).

Gotowe scenariusze:
1. `odchodze_z_pracy(data)` — koniec ubezpieczenia w podanym dniu.
2. `umowa_nie_zostanie_przedluzona` — koniec umowy na czas określony; uwzględnij KP art. 177 § 3.
3. `przechodze_na_dzialalnosc(data, dobrowolne_chorobowe: tak/nie)`.
4. `drugi_rodzic_bierze(tygodnie)` — podział rodzicielskiego; walidacja 9 tygodni i 5 części.
5. `praca_na_czesc_etatu(wymiar, tygodnie)`.
6. `wariant_wyplaty(standardowy | uśredniony)`.
7. `splacam_zadluzenie(data)`.

---

## 10. Sprawy ZUS

Każda sprawa testowa to fikcyjna osoba w symulatorze, której dane „w systemach" odpowiadają dokumentom sprawy. Plik: `fixtures/personas/<id>.json` (fakty + numer strony źródłowej przy każdym fakcie) oraz `fixtures/reference/case-<id>.json` (oczekiwana decyzja, powód, okresy, kwoty jeśli są w dokumentach).

Rozpoznanie dodanego PDF: suma SHA-256 liczona w przeglądarce → mapa `suma → id sprawy`; zapas: nazwa pliku + liczba stron. Nieznany plik → podgląd i komunikat „nie znam tej sprawy, wprowadź dane".

Podgląd PDF w przeglądarce (pdf.js). Plik nie opuszcza przeglądarki.

**Dane osób przepisują ludzie z dokumentów. Agent nie wymyśla danych spraw.** Do czasu dostarczenia danych utwórz pliki z samą oczekiwaną decyzją i polem `"status": "do_uzupelnienia"`; test dla takiej sprawy jest pomijany, nie zielony.

| Id | Sprawa (z nazwy pliku) | Oczekiwana decyzja |
|---|---|---|
| ewa | zasiłek macierzyński; nie podlega ubezpieczeniu chorobowemu w dniu porodu | odmowa |
| justyna | zasiłek chorobowy w ciąży; osoba współpracująca; zadłużenie składkowe | odmowa |
| zuzanna | zasiłek chorobowy w ciąży | do odczytania z dokumentów |
| dariusz | ojciec; zasiłek za okres rodzicielskiego; matka nieubezpieczona | przyznanie |
| julia | przyjęcie dziecka na wychowanie; przysposobienie | do odczytania |
| alicja | zasiłek za okres macierzyńskiego i rodzicielskiego | do odczytania |
| joanna | zasiłek w wysokości macierzyńskiego i macierzyński; likwidacja pracodawcy | przyznanie |
| andrzej | ojciec; część rodzicielskiego, 9 tygodni | do odczytania |
| teresa | zasiłek chorobowy i macierzyński; umowa zlecenia | do odczytania |
| tomasz | ojciec; rodzicielski, szósta część | odmowa |
| agnieszka-wa | łączenie urlopu rodzicielskiego z pracą | do odczytania |
| piotr | działalność; okres urlopu ojcowskiego; zadłużenie | odmowa |
| agnieszka-poz | pracownica od 2011 r.; skrajny wcześniak, masa < 1000 g | przyznanie |
| karolina | działalność; poród 1.12.2025; zaznaczony pełny wymiar | przyznanie |

Dwie sprawy mają potwierdzone okresy — użyj jako pierwszych testów arytmetyki:

| Sprawa | Macierzyński | Uzupełniający | Rodzicielski |
|---|---|---|---|
| agnieszka-poz (poród 12.06.2025) | 12.06.2025–29.10.2025 = 140 dni | 30.10.2025–11.02.2026 = 105 dni (15 tyg.) | 12.02.2026–23.09.2026 = 224 dni (32 tyg.) |
| karolina (poród 1.12.2025) | 1.12.2025–19.04.2026 = 140 dni | 20.04.2026–14.06.2026 = 56 dni (8 tyg.) | 15.06.2026–24.01.2027 = 224 dni (32 tyg.) |

---

## 11. Testy referencyjne (minimum)

`fixtures/reference/*.json`: `{id, opis, fakty, oczekiwane}`. Testy silnika czytają te pliki.

Okresy i wymiary:
1. Każdy wiersz parametrów wymiaru z 8.1 (liczba dzieci 1–5, „Za życiem", przyjęcie dziecka).
2. Obie sprawy z potwierdzonymi okresami (tabela wyżej).
3. Bliźnięta + „Za życiem": macierzyński 31 tyg., rodzicielski 67 tyg.
4. Wcześniak z 27. tygodnia, 70 dni w szpitalu → uzupełniający 10 tyg.; 120 dni → 15 tyg. (limit).
5. Poród w 39. tygodniu, pobyt od 6. do 20. dnia → uzupełniający 3 tyg. (15 dni ÷ 7 w górę).
6. Martwe urodzenie → 8 tygodni; zgon w 10. tygodniu życia → 7 dni od zgonu.
7. Ojcowski: dziecko ma 13 miesięcy → odmowa. Dwie części po tygodniu → przyznanie.
8. Przysposobienie dziecka 8-letniego → 20 + 41 tyg. Przysposobienie dziecka w wieku 13 lat i 11 miesięcy → 9 + 38 tyg.
9. Rodzina zastępcza niezawodowa: dziecko 5 lat → 20 + 41 tyg.; dziecko 8 lat bez odroczenia → odmowa; dziecko 8 lat z odroczeniem → przysługuje (granica 10 lat). Rodzina zawodowa → odmowa.
10. Szósta część rodzicielskiego → odmowa. Jeden rodzic 33 tyg. z 41 → błąd walidacji (9 tyg. nieprzenoszalne).
11. Pół etatu przez 20 tygodni rodzicielskiego → zasiłek 50% za te tygodnie, urlop dłuższy o 10 tyg.

Kwoty:
12. Podstawa 8 000 zł brutto: 8 000 × 0,8629 = **6 903,20 zł**; miesięcznie 100% = 6 903,20; 81,5% = **5 626,11**; 70% = **4 832,24**. Stawka dzienna 230,11 zł (wg domyślnego zaokrąglania).
13. Minimalna podstawa pełny etat: **4 147,10 zł**; pół etatu: 2 073,55 zł.
14. Limit podstawy dla dobrowolnie ubezpieczonych: **20 321,30 zł**.
15. Kontrola równoważności: 20 tyg. × 100% + 32 tyg. × 70% = 42,4 tygodniowych podstaw; 52 tyg. × 81,5% = 42,38.

Uprawnienia:
16. Brak ubezpieczenia w dniu porodu → odmowa (sprawa ewa).
17. Działalność, zadłużenie 120,00 zł → odmowa, „spłać 120,00 zł do <data + 6 mies.>"; zadłużenie 48,06 zł → przysługuje; 48,07 zł → odmowa.
18. Dobrowolne ubezpieczenie od 60 dni, e-ZLA z kodem B → chorobowy: odmowa (90 dni); macierzyński przy porodzie w okresie ubezpieczenia: przysługuje.
19. Pracownica po 20 dniach zatrudnienia bez wcześniejszych okresów → chorobowy: odmowa; z 10 latami wcześniejszego obowiązkowego ubezpieczenia → przysługuje.
20. Własne wypowiedzenie w 5. miesiącu ciąży → macierzyński: odmowa. Likwidacja pracodawcy → przysługuje + zasiłek do dnia porodu.
21. Umowa na czas określony kończąca się po 3. miesiącu ciąży → przedłużenie do dnia porodu, macierzyński przysługuje.
22. Matka nieubezpieczona, ojciec ubezpieczony → rodzicielski i ojcowski dla ojca przysługują.
23. Płatnik 150 osób → wypłaca płatnik; 15 osób → ZUS + Z-3; działalność → ZUS.
24. Parametr ze stawką 75% obowiązujący od 1.01.2027 nie zmienia wyniku dla dnia zasiłku w 2026 r., zmienia dla 2027 r.

---

## 12. Architektura

Modularny monolit. React + TypeScript (Vite) · ASP.NET Core Web API (aktualny LTS) · PostgreSQL · EF Core · Docker Compose · serwer proxy z HTTPS. REST/JSON.

```
contracts/        openapi.yaml, facts.schema.json, case.schema.json
fixtures/         personas/*.json, reference/*.json, organizations.json, parameters.seed.json
docs/prawo/       wyciągi przepisów (dostarczają ludzie)
docs/             README, zrzuty ekranu, slajdy, film
api/Domain/       Contracts/ Periods/ Amounts/ Eligibility/ Basis/ Catalog/ Deadlines/ Scenarios/
api/Infrastructure/  Parameters/ Simulation/ Submissions/ Persistence/
api/Api/          Program.cs, endpointy, rejestracja modułów
api/Tests/        lustrzane katalogi + ReferenceTests
web/src/app/      router, layout, klient API
web/src/ui/       tokeny, komponenty
web/src/features/ intake/ review/ result/ scenarios/ submissions/ zus-data/ cases/ support/ admin/
ops/              docker-compose.yml, proxy, skrypt wdrożenia, CI
```

Zasady kodu: `Domain` to czysty C# bez bazy i HTTP; typ `decimal` dla kwot, `DateOnly` dla dat; każda reguła to mała klasa z `Id`, `PodstawaPrawna` i metodą oceny; parametry przez `IParameterProvider.Get(klucz, data)`; błędy API jako `ProblemDetails`; walidacja wejścia na granicy API; bez komentarzy opisujących oczywistości; `docker compose up` uruchamia całość.

Symulacje — pięć interfejsów nad jednym zbiorem `fixtures/personas`, każdy z implementacją `Simulated…`, gotowe do podmiany na prawdziwe usługi:
`IIdentityProvider` · `IInsuranceRegistry` · `IContributionAccount` · `IMedicalCertificates` · `ISubmissionGateway` (pracodawca, ZUS, eZUS, urząd stanu cywilnego).

Frontend: React Router, TanStack Query, React Hook Form + Zod, Tailwind + komponenty Radix/shadcn; pdf.js do podglądu. Układ od 360 px; pełna obsługa klawiaturą; etykiety pól; kontrast WCAG 2.1 AA.

### 12.1. API

| Metoda i ścieżka | Cel |
|---|---|
| `POST /api/sim/login` | symulowane logowanie → sesja + tożsamość |
| `GET /api/sim/personas` | lista fikcyjnych osób i spraw |
| `GET /api/sim/me/facts` | fakty z integracji dla zalogowanej osoby |
| `GET /api/sim/me/zus` | ubezpieczenia, podstawy, e-ZLA, saldo |
| `GET /api/sim/me/debt-certificate` | symulowane zaświadczenie o stanie składek (PDF) |
| `POST /api/assessment` | fakty → wynik katalogu |
| `POST /api/assessment/compare` | fakty + scenariusz → wynik bazowy, wariant, różnica |
| `POST /api/submissions` | symulowane zgłoszenie (`wniosek_urlop`, `dane_zam`, `zapytanie_ezus`, `odpis_aktu`) → numer + PDF, jeśli dotyczy |
| `GET /api/submissions` | „Moje zgłoszenia" |
| `GET /api/cases` / `GET /api/cases/by-hash/{sha256}` | lista spraw / rozpoznanie pliku |
| `GET /api/organizations?situation=` | katalog wsparcia |
| `GET/PUT /api/admin/parameters`, `GET /api/admin/parameters/history` | parametry z datami i historią zmian |
| `GET/PUT /api/admin/catalog`, `/api/admin/integrations`, `/api/admin/organizations` | pozostałe ustawienia |
| `GET /health` | stan API i bazy |

Kształt odpowiedzi `POST /api/assessment`:

```json
{
  "rulesetVersion": "2026-11-21",
  "benefits": [
    {
      "id": "macierzynski",
      "status": "przysługuje",
      "period": { "from": "2027-03-15", "to": "2027-08-01", "days": 140, "weeks": 20 },
      "rate": 0.815,
      "amount": { "daily": 187.54, "total": 26255.60, "basis": 6903.20, "basisSource": "szacunek_zus" },
      "payer": "płatnik_składek",
      "whereToApply": "pracodawca",
      "documents": ["wniosek o urlop", "odpis aktu urodzenia pozyskuje ZUS"],
      "deadlines": [
        { "id": "wniosek_usredniona", "type": "najpóźniej_do", "date": "2027-04-05",
          "text": "Złóż wniosek o 81,5% do 5 kwietnia 2027", "consequence": "70% za urlop rodzicielski" }
      ],
      "reason": null,
      "whatWouldChange": null,
      "trace": [
        { "rule": "ubezpieczenie_w_dniu_porodu", "basis": "ustawa art. 29 ust. 1",
          "met": true, "text": "W dniu porodu podlegasz ubezpieczeniu chorobowemu jako pracownik." }
      ]
    }
  ],
  "missingFacts": [ { "fact": "umowa.dataKonca", "question": "Do kiedy trwa Twoja umowa?", "type": "date" } ],
  "nextStep": { "deadlineId": "wniosek_usredniona", "benefitId": "macierzynski" },
  "risks": [ { "id": "termin_21_dni", "text": "…" } ]
}
```

Liczby w przykładzie są ilustracją kształtu, nie wartościami referencyjnymi.

### 12.2. Wnioski

Wnioski o urlopy nie mają urzędowego wzoru; treść określa rozporządzenie z 11.03.2025 (`docs/prawo/`). Zaprojektuj własny, czytelny szablon i generuj PDF. Szablony: urlop macierzyński i rodzicielski łącznie („pełny wymiar"), urlop rodzicielski lub jego część, urlop ojcowski, uzupełniający urlop macierzyński. Pola każdego szablonu wypisz z rozporządzenia; nie zgaduj. Dla ZAM generuj zestawienie „dane do wniosku ZAM" (nie podrabiaj druku ZUS) i link do zus.pl.

---

## 13. Organizacja pracy

| Rola | Robi | Nie robi |
|---|---|---|
| Sonnet — jeden agent na gałąź | kod i testy w swoim katalogu; PR z opisem, listą reguł i wynikiem testów | nie zmienia `contracts/`, plików wspólnych ani cudzych katalogów; nie dodaje zależności bez zgody |
| Opus — koordynator | rozdaje zadania, pilnuje kontraktu, przegląd kodu, scala frontend i infrastrukturę, jako jedyny edytuje `contracts/`, `api/Api/Program.cs`, `web/src/app/` | nie zatwierdza sam reguł prawnych ani zmian kontraktu |
| Fable — zatwierdzający | kontrakt, każdy PR w `api/Domain/**`, testy referencyjne, cięcia zakresu, wydanie końcowe | nie pisze kodu gałęzi |
| Ludzie | dane spraw ZUS, wyciągi przepisów, rozmowy z mentorami, test na telefonach, film i prezentacja | — |

Reguły scalania:
- gałąź zmienia tylko swój katalog; żyje najwyżej 2–3 godziny;
- warunek scalenia: CI zielone, testy referencyjne zielone, brak zmian poza własnym katalogiem;
- PR w `api/Domain/**` zawiera tabelę „reguła → artykuł → przypadki testowe";
- rozbieżność z decyzją ZUS w sprawie testowej zatrzymuje scalanie silnika do wyjaśnienia przez człowieka;
- zmianę kontraktu wprowadza koordynator po zgodzie zatwierdzającego i ogłasza wszystkim gałęziom;
- najwyżej 8 gałęzi naraz.

### 13.1. Faza 0 — szeregowa (koordynator)

`chore/bootstrap`: struktura repo; `docker compose up`; CI (build + testy); `contracts/` z kształtem z 12.1; typy C# i TypeScript; przykładowa odpowiedź `assessment` jako atrapa dla frontendu; `.gitignore` na dokumenty ZUS; `fixtures/parameters.seed.json` z 8.1.
Gotowe, gdy: pusty szkielet działa lokalnie i pod publicznym adresem, a kontrakt jest zatwierdzony.

### 13.2. Fala 1

| Gałąź | Katalog | Zakres | Gotowe, gdy |
|---|---|---|---|
| `data/reference` | `fixtures/reference` | przypadki z sekcji 11 jako pliki | walidacja schematu przechodzi |
| `be/params` | `Infrastructure/Parameters` | tabela z datami, dostawca, seed, endpointy admina, historia | test 24 zielony |
| `be/engine-periods` | `Domain/Periods` | 8.5 | testy 1–11 zielone |
| `be/engine-eligibility` | `Domain/Eligibility` | 8.2–8.4, 8.8 + ścieżka decyzji | testy 16–23 zielone |
| `be/sim` | `Infrastructure/Simulation` | pięć interfejsów, osoby, endpointy `/api/sim/*` | osoba bez ubezpieczenia zwraca pusty rejestr |
| `fe/shell` | `web/src/ui` | tokeny, układ, komponenty pól, błędów, statusów | przegląd na 360 px |
| `fe/intake` | `features/intake`, `features/review` | ekrany 1–3 na atrapie API | przejście klawiaturą |
| `ops/deploy` | `ops` | serwer, proxy HTTPS, skrypt wdrożenia | publiczny adres działa |

**Punkt integracji 1:** jedna osoba przechodzi cały przebieg pod publicznym adresem (okresy i prawo do zasiłku).

### 13.3. Fala 2

| Gałąź | Katalog | Zakres | Gotowe, gdy |
|---|---|---|---|
| `be/engine-amounts` | `Domain/Amounts` | 8.6 | testy 12–15 zielone |
| `be/catalog` | `Domain/Catalog`, `Domain/Deadlines` | sekcja 7, 8.9, 8.10, `POST /api/assessment` | brakujący fakt wraca jako pytanie |
| `be/basis` | `Domain/Basis` | 8.7 | testy podstawy zielone |
| `be/scenarios` | `Domain/Scenarios` | sekcja 9, `POST /api/assessment/compare` | scenariusz 1 zmienia status macierzyńskiego |
| `fe/result` | `features/result` | ekran 4 | zgodność z API dla 5 osób |
| `fe/scenarios` | `features/scenarios` | ekran 5 | różnica widoczna tekstem i liczbą |
| `fe/admin` | `features/admin` | ekran 10 | zmiana stawki zmienia wynik |
| `data/cases` | `fixtures/personas` | osoby dla spraw, gdy ludzie dostarczą dane | sprawy z danymi zielone |

**Punkt integracji 2:** wszystkie kalkulatory na prawdziwym API; koniec nowych funkcji.

### 13.4. Fala 3

| Gałąź | Katalog | Zakres |
|---|---|---|
| `be/submissions` | `Infrastructure/Submissions` | 12.2, zaświadczenie o saldzie, `ISubmissionGateway` |
| `fe/submissions` | `features/submissions`, `features/zus-data` | ekrany 6–7 |
| `fe/cases` | `features/cases` | ekran 8, suma SHA-256, pdf.js |
| `fe/support` | `features/support` | ekran 9, tryb trudnej sytuacji |
| `fix/*` | wg potrzeby | błędy z testów spraw i z telefonów |
| `docs/submission` | `docs`, `README.md` | sekcja 15 |

### 13.5. Szablon zadania dla agenta

```
Gałąź: <nazwa>
Cel: <jedno zdanie>
Twój katalog: <ścieżki>. Niczego poza nim nie zmieniaj.
Czytaj: BABYRADNIK_INSTRUKCJA.md sekcje <…>; docs/prawo/<plik>, artykuły <…>.
Kontrakt (tylko do odczytu): contracts/…
Testy do zaliczenia: fixtures/reference/<…>
Zasady: sekcja 2 instrukcji. Sytuacja nierozpoznana = „nie rozstrzygam".
Gotowe, gdy: <kryterium z tabeli>; PR z tabelą reguła → artykuł → testy.
```

### 13.6. Kolejność cięcia, gdy brakuje czasu

1. pomocnik tekstowy (już P2);
2. podstawa wymiaru → samo pole ręczne;
3. „Porównaj warianty" → przełącznik wariantu wypłaty + liczba tygodni dla drugiego rodzica;
4. szablony wniosków z czterech do jednego;
5. historia zmian parametrów.

Nigdy nie tnij: poprawności silnika, symulowanego logowania, ekranu spraw ZUS, panelu parametrów, responsywności, elementów zgłoszenia.

---

## 14. Otwarte kwestie — nie zgaduj

| Kwestia | Zachowanie do czasu odpowiedzi |
|---|---|
| Zaokrąglanie stawki dziennej i kwot | ustawienie; domyślnie stawka dzienna do 0,01 zł |
| Brutto czy netto | brutto z dopiskiem |
| Kwota świadczenia rodzicielskiego (art. 31 ust. 3a) | parametr pusty; informacja zamiast podwyższenia |
| Czy dane przepisane ze spraw mogą być w publicznym repo | `fixtures/personas` dla spraw ZUS ładowane z katalogu poza repo, wskazanego zmienną środowiskową; w repo tylko osoby wymyślone |
| Czy jury doda pliki identyczne co do bajta | lista wyboru spraw zawsze dostępna obok dodawania pliku |
| Lista organizacji charytatywnych | `fixtures/organizations.json` z polem `zweryfikowano: false`; nie pokazuj niezweryfikowanych |

---

## 15. Zgłoszenie (gałąź `docs/submission`)

Wymagane przez zadanie:
- tytuł i szczegółowy opis projektu;
- prezentacja, najwyżej 10 slajdów;
- film, najwyżej 3 minuty, plik lub link w otwartym repozytorium;
- repozytorium kodu, zrzuty ekranu, materiały graficzne;
- link do działającego demo;
- **wskazanie, które funkcje działają ze wsparciem AI** oraz jakich narzędzi AI użyto do wytworzenia kodu (kod pisały modele językowe — napisz to wprost);
- lista użytych komponentów zewnętrznych z licencjami.

README: trzy komendy do uruchomienia, adres demo, dane do `/admin`, lista fikcyjnych osób, sposób testowania spraw ZUS, mapa wymagań z sekcji 4 na ekrany, wynik testów referencyjnych, ograniczenia (sekcje 3 i 14).

## 16. Definicja ukończenia całości

1. Każdy wiersz tabeli z sekcji 4 da się pokazać w demo pod publicznym adresem HTTPS, na telefonie.
2. Wszystkie testy z sekcji 11 są zielone; sprawy ZUS z dostarczonymi danymi dają decyzję zgodną z decyzją Zakładu.
3. Zmiana parametru w `/admin` zmienia wynik we właściwym okresie.
4. Każda odmowa ma powód i podstawę prawną; każda symulacja jest oznaczona.
5. W repozytorium nie ma dokumentów ZUS, sekretów ani prawdziwych danych osobowych.
6. Elementy z sekcji 15 są gotowe.
