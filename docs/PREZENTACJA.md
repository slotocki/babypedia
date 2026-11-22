# Kompas Rodzica: treść prezentacji

Najwyżej 10 slajdów (wymóg formalny zadania, pkt 4). Liczby pochodzą z repozytorium (gałąź `main`, 22.11.2026). Adres demo: https://kompasrodzica.polandcentral.cloudapp.azure.com.

Układ pod kryteria oceny (zadanie, pkt 8):

| Kryterium | Waga | Slajdy |
|---|---|---|
| Związek z wyzwaniem | 25% | 1, 2, 6 |
| Kompletność projektu i user experience | 25% | 3, 5, 7 |
| Pomysł | 20% | 4, 5 |
| Oryginalność | 15% | 8 |
| Potencjał wdrożeniowy | 15% | 9, 10 |

---

## Slajd 1. Kompas Rodzica

Kryterium: związek z wyzwaniem (25%).

- Zadanie ZUS: „Zasiłkowy Przewodnik Przyszłego Rodzica”
- Przewodnik po zasiłkach z ubezpieczenia chorobowego dla przyszłych rodziców
- Co przysługuje, ile, do kiedy i na co uważać — dla konkretnej osoby
- Zespół 2Stosy: Sylwester Łotocki, Andrzej Laskowski

Notatka: Jedno zdanie: aplikacja odpowiada rodzicowi, co mu przysługuje, zanim będzie za późno na zmianę sytuacji. Demo działa pod adresem na slajdzie.

## Slajd 2. Problem

Kryterium: związek z wyzwaniem (25%).

- Prawo do zasiłku zależy od tytułu ubezpieczenia, stażu, płatnika, liczby dzieci i daty zdarzenia
- Rodzic nie wie, co mu przysługuje ani do kiedy złożyć wniosek i do kogo
- O odmowie dowiaduje się, gdy zmiana sytuacji jest już niemożliwa
- ZUS chce mniej źle złożonych dokumentów i mniej sporów

Notatka: Problem to nie brak informacji, tylko jej dopasowanie do jednej osoby. Przykład: zadłużenie składkowe przy działalności odbiera prawo do zasiłku, jeśli nie zostanie spłacone na czas.

## Slajd 3. Jak to działa: trzy drogi na start

Kryterium: kompletność i UX (25%).

- Start „Sprawdź, co Ci przysługuje”: „Wprowadź dane ręcznie”, „Połącz się przez mObywatela i ZUS”, „Sprawdź na rzeczywistych przykładach”
- Logowanie (symulacja): wybór osoby testowej, potem „Zgoda na pobranie danych” ze wskazaniem źródeł (ZUS, e-ZLA, rejestr stanu cywilnego)
- „Potwierdź pobrane dane”: każda informacja ze źródłem i przyciskiem „Popraw”; potem tylko brakujące pytania
- Wynik „Twój plan”, „Moje konto”, „Wnioski”, „Wsparcie”, „Baza wiedzy”; układ od 360 px

Notatka: Każdy ekran ma jedną główną akcję, a każda symulacja jest tak nazwana na ekranie. Sprawdź w demo: Start → „Połącz się przez mObywatela i ZUS” → Anna → „Połącz i pobierz dane (symulacja)”.

## Slajd 4. Wynik, który da się sprawdzić

Kryterium: pomysł (20%).

- Karta świadczenia: status, okres od–do, kwota, kto wypłaca, gdzie złożyć wniosek, dokumenty (Z-3, Z-3a, ZAS-53, ZAM wg treści zadania)
- „Ścieżka decyzji”: reguła, przepis, warunek, spełniony lub nie, wyjaśnienie prostym językiem
- Odmowa zawsze z powodem, podstawą prawną i „Co musiałoby się zmienić”
- „Co dalej?” z najbliższym krokiem i lista „Co zrobić i do kiedy”

Notatka: Nasza główna różnica: nie „system odmówił”, tylko „odmowa, bo taki przepis, a zmieniłoby się, gdyby…”. Sprawdź w demo: Edyta (zadłużenie 120,00 zł) — odmowa z kwotą do spłaty i datą.

## Slajd 5. „Co jeśli” i porównanie wariantów

Kryterium: pomysł (20%), kompletność i UX (25%).

- Siedem scenariuszy: odejście z pracy, koniec umowy, przejście na działalność, drugi rodzic bierze część urlopu rodzicielskiego, praca na część etatu, wariant wypłaty (standardowy lub 81,5%), spłata zadłużenia
- Silnik liczy drugi raz na zmienionych faktach; różnica w tygodniach i złotych oraz zmiana statusów
- Podział urlopu: wskazana osoba bierze podaną liczbę tygodni; pilnujemy 9 tygodni nieprzenoszalnych i najwyżej 5 części
- „Zastosuj ten wariant w moim planie” albo powrót do planu bazowego

Notatka: Tu są zagrożenia z zadania (pkt 3 h): utrata uprawnień, zmiana wymiaru i wysokości. Rodzic widzi skutek decyzji, zanim ją podejmie. Sprawdź w demo: Anna → „Porównaj warianty” → „Wariant wypłaty” → „Uśredniony”.

## Slajd 6. Sprawdzone na sprawach ZUS

Kryterium: związek z wyzwaniem (25%).

- Ekran „Sprawy ZUS”: 11 spraw z zestawu ZUS — wybór z listy albo dodanie pliku PDF
- PDF rozpoznawany po sumie SHA-256 liczonej w przeglądarce; plik nie opuszcza urządzenia, aplikacja nie czyta jego treści
- Na wyniku ramka „Porównanie z decyzją ZUS”: 1 sprawa zgodna co do okresu i kwoty z wydrukiem ZUS, 8 co do okresu (co do dnia), 2 odmowy zgodne z decyzją
- Z 43 przeanalizowanych spraw silnik rozstrzyga inaczej niż ZUS w jednej (umowa uznana za zawartą dla pozoru — ustalenie z kontroli)

Notatka: To odpowiedź na wymóg demo z pkt 4 i 6 zadania: upload lub wybór przykładowych wniosków i sprawdzenie wyliczeń. W większości akt kwota jest zaczerniona, dlatego kwotę porównujemy tylko tam, gdzie jest w dokumentach. Sprawdź w demo: „Sprawy ZUS” → dodaj PDF sprawy Andrzeja (zasiłek za część urlopu rodzicielskiego, 9 tygodni) → „Załaduj dane sprawy” → wynik.

## Slajd 7. Pokrycie wymagań zadania

Kryterium: kompletność i UX (25%).

- Pozycje a–j z zadania mają ekran, endpoint i moduł kodu (mapa w README, wiersz po wierszu ze statusem)
- Symulacje: logowanie mObywatel, dane ZUS, saldo i zaświadczenie o stanie składek, e-ZLA, zapytanie PUE/eZUS, wnioski o urlopy i ZAM, odpis aktu urodzenia
- „Wsparcie”: zweryfikowane organizacje charytatywne dla sześciu trudnych sytuacji, kontakt do ZUS; „Baza wiedzy”: Poradnik ZUS i linki do zus.pl, MRPiPS, PIP; panel `/admin`
- Uczciwy status: świadczenie rehabilitacyjne zawsze „nie rozstrzygam”; podwyższenia do świadczenia rodzicielskiego nie wyliczamy, tylko o nim informujemy

Notatka: Mówimy wprost, co działa w pełni, a co częściowo. Pełna tabela jest w README w części „Mapa wymagań”.

## Slajd 8. Silnik reguł i AI tylko przy przepisach

Kryterium: oryginalność (15%).

- Uprawnienia, okresy, kwoty i terminy liczy deterministyczny silnik reguł w C# — ponad 80 małych reguł, każda z podstawą prawną
- Gdy przepis nie rozstrzyga: „nie rozstrzygam” i kontakt do ZUS, bez zgadywania; brakujący fakt wraca jako pytanie
- AI w aplikacji: tylko czat „Zapytaj o przepisy” (na demo Claude Haiku, Anthropic) — krótkie wyjaśnienie obok cytatów sprawdzonych słowo w słowo z przepisem, oznaczone jako napisane przez model; bez modelu czat pokazuje wyniki wyszukiwania
- AI poza aplikacją: kod, testy i dokumentacja (Claude w Claude Code pod nadzorem ludzi), odczyt dokumentów spraw ZUS, ilustracja na ekranie startowym

Notatka: Ta sama odpowiedź na te same dane i pełny ślad decyzji. Model językowy nie liczy i nie decyduje; w czacie nie może pokazać cytatu, którego nie ma w przepisie. Pełny opis: README, „Wykorzystanie AI”.

## Slajd 9. Jakość i potencjał wdrożeniowy

Kryterium: potencjał wdrożeniowy (15%).

- `dotnet test`: 946 testów, 945 zielonych, 0 błędów, 1 pominięty (raport dla spraw ZUS z katalogu prywatnego)
- 55 przypadków referencyjnych (okresy, uprawnienia, kwoty, parametry) w `fixtures/reference/`, napisane przed regułami
- Parametry prawne w bazie z datą obowiązywania (102 wpisy); zmiana w `/admin` zmienia wynik od wskazanego dnia, z historią zmian
- Integracje za interfejsami: podmiana symulacji na prawdziwe usługi bez zmian w silniku; Docker Compose, CI, licencje permisywne

Notatka: Wdrożenie to podmiana implementacji `Simulated…` i potwierdzenie parametrów, nie przepisywanie silnika. Lista licencji jest w `docs/ZALEZNOSCI.md`.

## Slajd 10. Otwarte kwestie i jak przetestować

Kryterium: potencjał wdrożeniowy (15%), kompletność i UX (25%).

- Do potwierdzenia z ZUS: zaokrąglanie stawki i kwoty, minimalna podstawa przy niepełnym etacie, podwyższenie do kwoty świadczenia rodzicielskiego, zasiłek do dnia porodu po rozwiązaniu umowy z naruszeniem prawa
- Druki Z-3, Z-3a, ZAS-53 i połączenie ZUR/ZAO z ZAM od 24.10.2026 potwierdza treść zadania; Poradnik ZUS opisuje jeszcze Z-3b
- Wszystkie integracje symulowane, dane osób fikcyjne, kwoty brutto
- Jak przetestować: osoby testowe na ekranie logowania (Anna — ścieżka standardowa, Edyta — odmowa za zadłużenie, Halina — e-ZLA i mały płatnik, Tadeusz — ojciec, wynik dla obojga rodziców, gdy matka nie ma ubezpieczenia) oraz „Sprawy ZUS” (lista albo dodanie PDF, porównanie z decyzją ZUS)

Notatka: Kończymy listą rzeczy, których nie rozstrzygnęliśmy, bo z nimi trzeba iść do ZUS, i zaproszeniem do demo. Opis osób jest w README w części „Fikcyjne osoby demo”, a oznaczenia spraw w części „Sprawy testowe ZUS”. Kod: github.com/slotocki/babypedia.
