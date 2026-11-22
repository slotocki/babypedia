# Kompas Rodzica: treść prezentacji

Najwyżej 10 slajdów (wymóg formalny zadania, pkt 4). Liczby pochodzą z repozytorium (gałąź `main`, 22.11.2026). Adres demo: https://kompasrodzica.polandcentral.cloudapp.azure.com.

Układ pod kryteria oceny (zadanie, pkt 8):

| Kryterium | Waga | Slajdy |
|---|---|---|
| Związek z wyzwaniem | 25% | 1, 5 |
| Kompletność projektu i user experience | 25% | 2, 3, 6, 10 |
| Pomysł | 20% | 3, 4 |
| Oryginalność | 15% | 7 |
| Potencjał wdrożeniowy | 15% | 8, 9 |

---

## Slajd 1. Kompas Rodzica

<!-- Zrzut: docs/zrzuty/slajd-01-start.png (ekran startowy); opcjonalnie slajd-02-odmowa.png jako przykład problemu -->

Kryterium: związek z wyzwaniem (25%).

- Zadanie ZUS „Zasiłkowy Przewodnik Przyszłego Rodzica”: przewodnik po zasiłkach z ubezpieczenia chorobowego dla przyszłych rodziców
- Problem: prawo do zasiłku zależy od tytułu ubezpieczenia, stażu, płatnika, liczby dzieci i daty zdarzenia. Rodzic nie wie, co mu przysługuje, do kiedy i gdzie złożyć wniosek
- O odmowie często dowiaduje się wtedy, gdy nie może już zmienić swojej sytuacji
- Kompas Rodzica mówi konkretnej osobie: co przysługuje, ile, do kiedy, gdzie złożyć wniosek i na co uważać
- Zespół 2Stosy: Sylwester Łotocki, Andrzej Laskowski

Notatka: Problem to nie brak informacji, tylko jej dopasowanie do jednej osoby. Przykład: zaległość w składkach przy działalności odbiera prawo do zasiłku, jeśli nie zostanie spłacona na czas. Demo działa pod adresem na slajdzie.

## Slajd 2. Jak to działa: trzy drogi na start

<!-- Zrzuty: docs/zrzuty/slajd-03-logowanie.png, slajd-03-pytania-169.png, slajd-03-wynik.png -->

Kryterium: kompletność i UX (25%).

- Start „Sprawdź, co Ci przysługuje”: „Wprowadź dane ręcznie”, „Połącz się przez mObywatela i ZUS”, „Sprawdź na rzeczywistych przykładach”
- Logowanie (symulacja): wybór osoby testowej, potem „Zgoda na pobranie danych” ze wskazaniem źródeł (ZUS, e-ZLA, rejestr stanu cywilnego)
- „Potwierdź pobrane dane”: każda informacja ze źródłem i przyciskiem „Popraw”; pytania tylko o to, czego brakuje, daty w formacie DD.MM.RRRR
- Potem „Twój plan”, „Moje konto”, „Wnioski”, „Wsparcie”, „Baza wiedzy”; ten sam układ na telefonie i komputerze

Notatka: Każdy ekran ma jedną główną akcję, a każda symulacja jest tak nazwana na ekranie. Sprawdź w demo: Start → „Połącz się przez mObywatela i ZUS” → Anna → „Połącz i pobierz dane (symulacja)” → „Potwierdź dane” → „Pokaż wynik”.

## Slajd 3. Wynik, który da się sprawdzić

<!-- Zrzuty: docs/zrzuty/slajd-04-karta.png, slajd-04-sciezka.png. Lepiej pasowałby nowy zrzut wyniku Anny z ramką „W skrócie” na górze i rozwiniętą ścieżką decyzji (6 kroków + „Pokaż wszystkie kroki”); obecny slajd-04-sciezka.png pokazuje starą wersję ścieżki. -->

Kryterium: pomysł (20%).

- „W skrócie” na górze: co przysługuje, w jakiej kwocie i czego brakuje, w jednym-dwóch zdaniach
- Karta świadczenia: status, okres od–do, kwota, kto wypłaca, gdzie złożyć wniosek, dokumenty; przy urlopie rodzicielskim „Twoja część” obok wymiaru łącznego obojga rodziców
- „Ścieżka decyzji”: najważniejsze kroki (reguła, przepis, warunek spełniony lub nie), reszta pod „Pokaż wszystkie kroki”; „Co znaczą te pojęcia” objaśnia ZAM, płatnika składek, e-ZLA, Z-3
- Odmowa zawsze z powodem, przepisem i następnym krokiem („Co musiałoby się zmienić”, przy zaległości „To da się naprawić”); „Co dalej?” z najbliższym krokiem i terminami

Notatka: Nasza główna różnica: nie „system odmówił”, tylko „odmowa, bo taki przepis, a zmieniłoby się, gdyby…”. W trudnej sytuacji (strata dziecka) wynik zmienia ton, a wsparcie i najpilniejsze sprawy są na górze. Sprawdź w demo: Edyta (zaległość 120,00 zł) — odmowa z kwotą do spłaty i terminem.

## Slajd 4. „Co jeśli” i porównanie wariantów

<!-- Zrzuty: docs/zrzuty/slajd-05-scenariusze.png, slajd-05-roznica.png -->

Kryterium: pomysł (20%).

- Siedem scenariuszy: odejście z pracy, koniec umowy, przejście na działalność, drugi rodzic bierze część urlopu rodzicielskiego, praca na część etatu, wariant wypłaty (standardowy lub 81,5%), spłata zadłużenia
- Silnik liczy drugi raz na zmienionych faktach; na górze podsumowanie w jednym zdaniu, niżej różnica w tygodniach i złotych oraz zmiana statusów
- Podział urlopu: wskazana osoba bierze podaną liczbę tygodni; pilnujemy 9 tygodni nieprzenoszalnych i najwyżej 5 części
- „Zastosuj ten wariant w moim planie” albo powrót do planu bazowego; wariant 81,5% można też wybrać od razu na wyniku

Notatka: Tu są zagrożenia z zadania (pkt 3 h): utrata uprawnień, zmiana wymiaru i wysokości. Rodzic widzi skutek decyzji, zanim ją podejmie. Sprawdź w demo: Anna → „Porównaj warianty” → „Wariant wypłaty” → „Uśredniony”.

## Slajd 5. Sprawdzone na sprawach ZUS

<!-- Nowy zrzut potrzebny: ekran „Sprawy ZUS” (lista spraw z oznaczeniami, bez podglądu dokumentu) i ramka „Porównanie z decyzją ZUS (sprawa testowa)” z tabelą okresów, przycięta bez kwot i dat ze sprawy. Proponowana nazwa: docs/zrzuty/slajd-05-sprawy-zus.png (zapisać poza repozytorium, jeśli widać dane sprawy). -->

Kryterium: związek z wyzwaniem (25%).

- Ekran „Sprawy ZUS” (link „Sprawy ZUS (dla oceniających)” w stopce): 11 spraw z zestawu ZUS — wybór z listy albo dodanie pliku PDF
- PDF rozpoznawany po sumie SHA-256 liczonej w przeglądarce; plik nie opuszcza urządzenia, aplikacja nie czyta jego treści
- Na wyniku ramka „Porównanie z decyzją ZUS”: decyzja i okresy z akt obok wyniku silnika. 1 sprawa zgodna co do okresu i kwoty z wydrukiem ZUS, 8 co do okresu (co do dnia), 2 odmowy zgodne z decyzją
- Z 43 przeanalizowanych spraw silnik rozstrzyga inaczej niż ZUS w jednej (umowa uznana za zawartą dla pozoru — ustalenie z kontroli)

Notatka: To odpowiedź na wymóg demo z pkt 4 i 6 zadania: upload lub wybór przykładowych wniosków i sprawdzenie wyliczeń. W większości akt kwota jest zaczerniona, dlatego kwotę porównujemy tam, gdzie jest w dokumentach. Pozostałe pliki z zestawu dają podgląd i komunikat „Nie znam tej sprawy” z możliwością wpisu ręcznego. Dokumentów ZUS ani danych z nich nie ma w repozytorium. Sprawdź w demo: „Sprawy ZUS” → sprawa z oznaczeniem „Okres i kwota zgodne z wydrukiem ZUS” → „Załaduj dane sprawy” → wynik.

## Slajd 6. Pokrycie wymagań zadania

<!-- Zrzuty: docs/zrzuty/slajd-06-wnioski-top.png, slajd-06-telefon-wynik.png -->

Kryterium: kompletność i UX (25%).

- Pozycje a–j z zadania mają ekran, endpoint i moduł kodu (mapa w README, wiersz po wierszu ze statusem)
- Symulacje: logowanie mObywatel, dane ZUS, saldo i zaświadczenie o stanie składek (z datą stanu salda), e-ZLA, zapytanie PUE/eZUS, wnioski o urlopy i dane do ZAM z datami z planu, odpis aktu urodzenia; puste pola w PDF zostają do uzupełnienia odręcznie
- „Wsparcie”: zweryfikowane organizacje charytatywne z filtrem sytuacji, kontakt do ZUS; „Baza wiedzy”: Poradnik ZUS i linki do zus.pl, MRPiPS, PIP
- Status wprost: świadczenie rehabilitacyjne zawsze „nie rozstrzygam” (orzeka lekarz orzecznik ZUS); indywidualną podstawę liczymy dla typowych przypadków, w pozostałych prosimy o podstawę od płatnika

Notatka: Mówimy wprost, co działa w pełni, a co częściowo. Pełna tabela jest w README w części „Mapa wymagań”. Responsywność sprawdziliśmy automatycznie przy 320, 390 i 768 px — bez przewijania w poziomie.

## Slajd 7. Czat „Zapytaj o przepisy”: FAQ i źródła

<!-- Zrzut: docs/zrzuty/slajd-08-czat.png. Lepiej pasowałby nowy zrzut odpowiedzi z FAQ z etykietami źródeł (zwiniętymi) i jednym rozwiniętym cytatem. -->

Kryterium: oryginalność (15%).

- Na ekranach Twój plan, Wsparcie i Baza wiedzy; mały panel, który nie zasłania strony
- Najpierw 71 gotowych odpowiedzi (FAQ) z dosłownymi cytatami z materiałów ZUS i przepisów; test sprawdza każdy cytat słowo w słowo z wyciągiem
- Gdy pytanie nie pasuje do FAQ: wyszukiwarka po wyciągach przepisów; opcjonalnie, gdy ustawiony jest klucz API, model językowy (Claude Haiku) pisze krótkie wyjaśnienie obok sprawdzonych cytatów, oznaczone jako napisane przez model
- Źródła zwinięte do krótkich etykiet; po rozwinięciu cytat i cały fragment aktu. Pytania o własną sprawę czat kieruje do kalkulatora, a dane osobowe odrzuca

Notatka: Bez modelu czat odpowiada z FAQ i pokazuje wyniki wyszukiwania. Model nie może pokazać cytatu, którego nie ma w przepisie, i nie podaje kwot. To jedyne miejsce, w którym aplikacja w działaniu korzysta z AI.

## Slajd 8. Silnik reguł i jakość

<!-- Zrzut: docs/zrzuty/slajd-02-odmowa.png (odmowa z przepisem i „Co musiałoby się zmienić”) -->

Kryterium: potencjał wdrożeniowy (15%), oryginalność (15%).

- Uprawnienia, okresy, kwoty i terminy liczy deterministyczny silnik reguł w C# — ponad 80 małych reguł, każda z podstawą prawną; te same dane dają ten sam wynik
- Gdy przepis nie rozstrzyga: „nie rozstrzygam” i kontakt do ZUS, bez zgadywania; brakujący fakt wraca jako pytanie
- `dotnet test`: 1050 testów, 1049 zielonych, 0 błędów, 1 pominięty (raport dla spraw ZUS z katalogu prywatnego); 55 przypadków referencyjnych napisanych przed regułami
- 102 parametry prawne w bazie z datą obowiązywania; zmiana w `/admin` (polskie nazwy w grupach) zmienia wynik od wskazanego dnia, z historią zmian

Notatka: Model językowy nie liczy i nie decyduje. Pełny ślad decyzji widać na każdej karcie. Wdrożenie przepisu to nowa wartość parametru z datą, nie zmiana kodu.

## Slajd 9. Rozwiązania zewnętrzne i użycie AI

<!-- Bez zrzutu; slajd tekstowy (tabela albo dwie kolumny). Opcjonalnie logotypy technologii. -->

Kryterium: potencjał wdrożeniowy (15%). Wymóg regulaminu (§ 4 ust. 1): „W przypadku stosowania rozwiązań zewnętrznych zostanie to wyraźnie wskazane w prezentacji zadania.”

- Frontend: React, React Router, TanStack Query, React Hook Form, Zod, Radix UI (MIT), TypeScript (Apache-2.0), Vite, Tailwind CSS (MIT), pdf.js — podgląd PDF (Apache-2.0)
- Backend: ASP.NET Core i .NET 10, Entity Framework Core z InMemory (MIT), PostgreSQL i sterownik Npgsql (PostgreSQL License), PDFsharp-MigraDoc 6.2.4 (MIT), font Roboto (Apache-2.0), xUnit (Apache-2.0)
- Infrastruktura i usługi: Docker i Docker Compose, nginx (BSD-2-Clause), Caddy (Apache-2.0), Microsoft Azure (maszyna wirtualna demo), Let's Encrypt; Claude Haiku przez Anthropic API — opcjonalnie, tylko czat
- AI poza aplikacją: kod, testy, treść FAQ i dokumentację wytworzyły modele Claude w Claude Code pod nadzorem zespołu; dokumenty spraw ZUS odczytano z pomocą AI; ilustracja na ekranie startowym jest wygenerowana przez AI. Materiały źródłowe: przepisy i Poradnik ZUS z materiałów zadania

Notatka: Wszystkie zależności mają licencje permisywne. Pełna lista z wersjami jest w `docs/ZALEZNOSCI.md`, a opis użycia AI w README, „Wykorzystanie AI”.

## Slajd 10. Założenia, ograniczenia i jak przetestować

<!-- Zrzut: docs/zrzuty/slajd-10-telefon-start.png -->

Kryterium: potencjał wdrożeniowy (15%), kompletność i UX (25%).

- Założenia tam, gdzie materiały nie rozstrzygają, każde jako parametr w `/admin`: stawka dzienna zaokrąglana raz do 1 gr (jak w wyliczeniach ZUS w sprawach testowych); kwoty brutto; minimalna podstawa przy niepełnym etacie proporcjonalnie do etatu; druki Z-3, Z-3a, ZAS-53 i ZAM według treści zadania
- Ograniczenia: podwyższenia do kwoty świadczenia rodzicielskiego (art. 31 ust. 3a) nie wyliczamy, tylko o nim informujemy; wszystkie integracje są symulowane, dane osób fikcyjne
- Jak przetestować: osoby testowe na ekranie logowania (Anna — ścieżka standardowa, Edyta — odmowa za zaległość w składkach, Halina — e-ZLA i mały płatnik, Tadeusz — ojciec, gdy matka nie ma ubezpieczenia) oraz „Sprawy ZUS (dla oceniających)” w stopce
- Lokalnie bez Dockera: `dotnet run --project api/Api` i `npm run dev` w `web/` (baza w pamięci); kod: github.com/slotocki/babypedia

Notatka: Każde założenie przy wdrożeniu zmienia się jedną wartością parametru z datą, bez zmiany kodu. Opis osób jest w README w części „Fikcyjne osoby demo”, a oznaczenia spraw w części „Sprawy testowe ZUS”.
