# Kompas Rodzica

Tytuł do formularza: **Kompas Rodzica — przewodnik po zasiłkach z ubezpieczenia chorobowego dla przyszłych rodziców**

## Opis projektu (do wklejenia w formularz)

**Problem.** Zasiłki związane z rodzicielstwem zależą od tytułu ubezpieczenia, stażu, płatnika, liczby dzieci, daty zdarzenia i wariantu wypłaty. Rodzic zwykle nie wie, co mu przysługuje, ile dostanie i do kiedy złożyć wniosek. O odmowie dowiaduje się często wtedy, gdy nie może już zmienić swojej sytuacji.

**Rozwiązanie.** Kompas Rodzica to responsywna aplikacja WWW, która ustala uprawnienia dla konkretnej osoby. Na ekranie startowym („Sprawdź, co Ci przysługuje”) są trzy drogi:

- **„Wprowadź dane ręcznie”** — kilka pytań krok po kroku, bez logowania: co się dzieje (planuję dziecko, jestem w ciąży, dziecko się urodziło, przyjmuję dziecko na wychowanie), data i liczba dzieci, dla kogo liczymy (dla mnie albo dla mnie i drugiego rodzica), praca i ubezpieczenie, a w razie potrzeby praca drugiego rodzica.
- **„Połącz się przez mObywatela i ZUS”** — symulowane logowanie: wybór fikcyjnej osoby testowej, potem krok „Zgoda na pobranie danych”, w którym użytkownik zaznacza źródła (konto ZUS, e-ZLA, rejestr stanu cywilnego), i przycisk „Połącz i pobierz dane (symulacja)”.
- **„Sprawdź na rzeczywistych przykładach”** — ekran „Sprawy ZUS” z listą spraw testowych ZUS i rozpoznawaniem pliku PDF sprawy.

Potem użytkownik przechodzi przez „Potwierdź pobrane dane” (każda informacja ma oznaczone źródło i przycisk „Popraw”) oraz „Uzupełnij brakujące dane” (pytania tylko o to, czego brakuje do oceny). Wynik („Twój plan”) pokazuje, które świadczenia przysługują, za jaki okres i w jakiej wysokości, kto wypłaca, gdzie złożyć wniosek i jakie dokumenty dołączyć, a także „Co zrobić i do kiedy”, „Na co uważać” i inny wariant wypłaty. Odmowa zawsze ma powód, podstawę prawną i informację „Co musiałoby się zmienić”. Gdy przepis nie rozstrzyga sytuacji, aplikacja mówi to wprost i odsyła do ZUS.

**Dla kogo.** Dla osób planujących dziecko, w ciąży, po porodzie i przyjmujących dziecko na wychowanie (przysposobienie, rodzina zastępcza), także dla drugiego rodzica.

**Co działa w demo.**
- Pełny przebieg od startu do wyniku na fikcyjnych osobach, wpisem ręcznym albo przez symulowane logowanie; po zalogowaniu „Moje konto” prowadzi do wyniku, danych w ZUS i wniosków.
- Okresy, kwoty i terminy dla zasiłku chorobowego w ciąży, zasiłku w wysokości zasiłku macierzyńskiego (do dnia porodu) i zasiłku macierzyńskiego za okres urlopu macierzyńskiego, uzupełniającego, rodzicielskiego, ojcowskiego i urlopu na warunkach urlopu macierzyńskiego (przyjęcie dziecka). Świadczenie rehabilitacyjne aplikacja opisuje, ale prawa do niego nie rozstrzyga, bo zależy od orzeczenia lekarza orzecznika ZUS.
- „Porównaj warianty”: siedem scenariuszy „co jeśli” (odejście z pracy, koniec umowy, przejście na działalność, drugi rodzic bierze część urlopu rodzicielskiego, praca na część etatu, wariant wypłaty 100%/70% lub 81,5%, spłata zadłużenia). W scenariuszu „Drugi rodzic bierze część urlopu rodzicielskiego” wskazana osoba bierze podaną liczbę tygodni, a aplikacja pilnuje 9 tygodni nieprzenoszalnych i najwyżej 5 części.
- „Sprawy ZUS”: 11 spraw z zestawu ZUS do wybrania z listy albo przez dodanie pliku PDF. Plik jest rozpoznawany po sumie SHA-256 liczonej w przeglądarce i nie opuszcza urządzenia. Po wczytaniu sprawy wynik zawiera ramkę „Porównanie z decyzją ZUS (sprawa testowa)”. W jednej sprawie okres i kwota zgadzają się z wydrukiem ZUS w aktach, w ośmiu okres zgadza się co do dnia, w dwóch odmowa jest zgodna z decyzją ZUS.
- „Wnioski”: cztery wzory wniosków o urlop do pracodawcy (PDF), dane do wniosku ZAM, zapytanie do ZUS, wniosek o odpis aktu urodzenia i zaświadczenie o stanie składek — wszystko jako symulacja z numerem zgłoszenia.
- „Moje dane w ZUS”: ubezpieczenia i płatnicy, podstawy składek, zwolnienia e-ZLA, saldo składek (symulacja).
- „Wsparcie”: zweryfikowane organizacje charytatywne oferujące bezpłatną pomoc, z filtrem „Twoja sytuacja”; są organizacje dla poronienia, martwego urodzenia, śmierci dziecka, wcześniactwa, ciężkiej choroby dziecka oraz ciężkiej choroby lub śmierci matki. Do tego kontakt do ZUS i informacja o zasiłku pogrzebowym przy martwym urodzeniu lub zgonie dziecka. W trudnej sytuacji interfejs zmienia ton i stawia pomoc na górze.
- „Baza wiedzy”: Poradnik ZUS dla rodziców (PDF ze spisem rozdziałów) i oficjalne źródła (zus.pl, MRPiPS, PIP).
- Czat „Zapytaj o przepisy” na ekranach Wynik, Wsparcie i Baza wiedzy.
- Panel `/admin`: parametry prawne z datą obowiązywania i historią zmian, włączanie świadczeń, przełączniki integracji, lista organizacji.

**Jak przetestować.** Fikcyjne osoby (np. Anna — pracownica w ciąży, Edyta — działalność z zadłużeniem składkowym, Halina — mały płatnik i e-ZLA) są w kafelkach na ekranie logowania i opisane w README. Sprawy ZUS są na ekranie „Sprawy ZUS”: wybierz sprawę z listy albo dodaj jej plik PDF i porównaj wynik z decyzją ZUS.

**Co nas odróżnia.**
- Uprawnienia, okresy, kwoty i terminy liczy deterministyczny silnik reguł w C#. Model językowy nie bierze udziału w decyzji ani w kwocie.
- Każdy wynik ma „Ścieżkę decyzji”: reguła, przepis, warunek, spełniony lub nie, wyjaśnienie prostym językiem.
- Parametry prawne (wymiary urlopów, stawki, progi, płaca minimalna) są w bazie z datą obowiązywania. Zmiana w panelu zmienia wynik od wskazanego dnia, bez wdrażania kodu.
- Gdy przepis nie rozstrzyga sytuacji, silnik odpowiada „nie rozstrzygam” i odsyła do ZUS zamiast zgadywać. Brakujący fakt wraca jako pytanie do użytkownika.
- Reguły powstały z wyciągów przepisów i są sprawdzane przypadkami referencyjnymi napisanymi przed kodem reguł oraz sprawami testowymi ZUS.

**Potencjał wdrożeniowy.** Integracje (tożsamość, rejestr ubezpieczeń, konto składkowe, e-ZLA, wysyłka zgłoszeń, rejestr stanu cywilnego) są ukryte za interfejsami. W demo zastępują je implementacje symulowane nad jednym zbiorem fikcyjnych osób; podmiana na prawdziwe usługi nie wymaga zmian w silniku. Panel parametrów pozwala odzwierciedlać zmiany przepisów bez programisty. Architektura: React + TypeScript, ASP.NET Core (.NET 10), PostgreSQL, REST, Docker Compose; zależności na licencjach permisywnych.

**Jakość.** `dotnet test`: 966 testów, 965 zielonych, 0 błędów, 1 pominięty (raport dla spraw ZUS z katalogu prywatnego, uruchamiany osobno) — stan z 22.11.2026. Do tego 55 przypadków referencyjnych w `fixtures/reference/` i walidacja fixtures schematami JSON w CI.

**Wykorzystanie AI.** Wskazujemy wszystkie miejsca, w których użyto sztucznej inteligencji:
1. **W działającej aplikacji — tylko czat „Zapytaj o przepisy”.** Czat wyszukuje fragmenty przepisów w wyciągach z materiałów ZUS. Opcjonalnie (na demo: model Claude Haiku firmy Anthropic) model językowy pisze 2–3 zdania wyjaśnienia prostym językiem i wybiera cytaty spośród znalezionych przepisów. Aplikacja pokazuje tylko cytaty sprawdzone słowo w słowo z tekstem przepisu w repozytorium, a wyjaśnienie wyłącznie obok takich cytatów, z dopiskiem, że napisał je model językowy i że wiążące są przepisy. Wyjaśnienie z kwotą, linkiem albo danymi osobowymi, a także zbyt długie, aplikacja zastępuje zdaniem ogólnym. Gdy model jest wyłączony, zwróci niesprawdzony cytat albo wyczerpie się limit, czat pokazuje same wyniki wyszukiwania. Pytania o własną sprawę czat kieruje do kalkulatora.
2. **Kalkulator, uprawnienia, okresy, kwoty i terminy nie używają AI.** Liczy je deterministyczny silnik reguł. Aplikacja nie używa OCR i nie czyta treści PDF.
3. **Przygotowanie danych testowych.** Dokumenty spraw ZUS odczytano z pomocą modeli językowych poza aplikacją; w aplikacji te odczyty są tylko danymi wejściowymi silnika.
4. **Grafika.** Ilustracja na ekranie startowym została wygenerowana przez model AI.
5. **Wytworzenie projektu.** Kod, testy i dokumentację wytworzyły modele Claude (Anthropic) w narzędziu Claude Code, pracujące pod nadzorem ludzi. Ludzie dostarczyli materiały prawne, zdecydowali o zakresie i weryfikowali wyniki; reguły pisano z wyciągów przepisów, nie z pamięci modelu.

**Ograniczenia.** Wszystkie integracje są symulowane, a dane osób fikcyjne. Kwoty są brutto. Kilka kwestii czeka na potwierdzenie z ZUS: zaokrąglanie, minimalna podstawa przy niepełnym etacie, podwyższenie do kwoty świadczenia rodzicielskiego (art. 31 ust. 3a — aplikacja o nim informuje, ale go nie wylicza) i zasiłek do dnia porodu po rozwiązaniu umowy z naruszeniem prawa. Szczegóły są w README w części „Otwarte kwestie”. Dokumenty spraw ZUS i dane z nich nie są w repozytorium; demo wczytuje je z osobnego katalogu na serwerze.

Demo: https://kompasrodzica.polandcentral.cloudapp.azure.com · Repozytorium: https://github.com/slotocki/babypedia · Zespół: 2Stosy (Sylwester Łotocki, Andrzej Laskowski)
