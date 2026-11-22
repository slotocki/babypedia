# Kompas Rodzica

Tytuł do formularza: **Kompas Rodzica — przewodnik po zasiłkach z ubezpieczenia chorobowego dla przyszłych rodziców**

## Opis projektu (do wklejenia w formularz)

**Problem.** Zasiłki związane z rodzicielstwem zależą od tytułu ubezpieczenia, stażu, płatnika, liczby dzieci, daty zdarzenia i wariantu wypłaty. Rodzic zwykle nie wie, co mu przysługuje, ile dostanie i do kiedy złożyć wniosek. O odmowie dowiaduje się często wtedy, gdy nie może już zmienić swojej sytuacji.

**Rozwiązanie.** Kompas Rodzica to responsywna aplikacja WWW, która ustala uprawnienia dla konkretnej osoby. Na ekranie startowym („Sprawdź, co Ci przysługuje”) są trzy drogi:

- **„Wprowadź dane ręcznie”** — pytania krok po kroku, bez logowania: co się dzieje (planuję dziecko, jestem w ciąży, dziecko się urodziło, przyjmuję dziecko na wychowanie), data i liczba dzieci, dla kogo liczymy (dla mnie albo dla mnie i drugiego rodzica), praca i ubezpieczenie, w razie potrzeby praca drugiego rodzica.
- **„Połącz się przez mObywatela i ZUS”** — symulowane logowanie: wybór fikcyjnej osoby testowej, krok „Zgoda na pobranie danych” ze wskazaniem źródeł (konto ZUS, e-ZLA, a po porodzie dane dziecka) i „Połącz i pobierz dane (symulacja)”. Aplikacja nie łączy się z żadnym zewnętrznym systemem: dane pochodzą z plików osób testowych (fikcyjnych albo przepisanych z dokumentów spraw testowych ZUS).
- **„Sprawdź na rzeczywistych przykładach”** — ekran „Sprawy ZUS” z listą spraw testowych ZUS i rozpoznawaniem pliku PDF sprawy.

Potem użytkownik potwierdza pobrane dane (każda informacja ma źródło i przycisk „Popraw”) i odpowiada na pytania, jeśli czegoś brakuje do oceny. Wynik („Twój plan”) zaczyna się od „W skrócie”: co przysługuje, w jakiej kwocie i czego brakuje. Karty świadczeń pokazują okres i wysokość, kto wypłaca, gdzie złożyć wniosek i jakie dokumenty dołączyć. Przy urlopie rodzicielskim karta pokazuje „Twoja część” obok wymiaru łącznego obojga rodziców. Dalej są „Co zrobić i do kiedy”, „Na co uważać” i wybór wariantu wypłaty (standardowy albo 81,5%). Trudne pojęcia (ZAM, płatnik składek, e-ZLA, Z-3) mają krótkie objaśnienia. Odmowa zawsze ma powód, przepis i następny krok: „Co musiałoby się zmienić”. Gdy przepis nie rozstrzyga sytuacji, aplikacja mówi to wprost i odsyła do ZUS.

**Dla kogo.** Dla osób planujących dziecko, w ciąży, po porodzie i przyjmujących dziecko na wychowanie (przysposobienie, rodzina zastępcza), także dla drugiego rodzica.

**Co działa w demo.**
- Pełny przebieg od startu do wyniku na fikcyjnych osobach, wpisem ręcznym albo przez symulowane logowanie. Po zalogowaniu „Moje konto” prowadzi do planu, danych w ZUS i wniosków.
- Okresy, kwoty i terminy dla zasiłku chorobowego w ciąży, zasiłku w wysokości zasiłku macierzyńskiego (do dnia porodu) i zasiłku macierzyńskiego za okres urlopu macierzyńskiego, uzupełniającego, rodzicielskiego, ojcowskiego i urlopu na warunkach urlopu macierzyńskiego (przyjęcie dziecka). Świadczenie rehabilitacyjne aplikacja opisuje, ale prawa do niego nie rozstrzyga, bo zależy od orzeczenia lekarza orzecznika ZUS.
- „Ścieżka decyzji” na każdej karcie: najważniejsze kroki (reguła, przepis, warunek spełniony lub nie, wyjaśnienie prostym językiem), pozostałe pod „Pokaż wszystkie kroki”.
- „Porównaj warianty”: siedem scenariuszy „co jeśli” (odejście z pracy, koniec umowy, przejście na działalność, drugi rodzic bierze część urlopu rodzicielskiego, praca na część etatu, wariant wypłaty 100%/70% lub 81,5%, spłata zadłużenia). Każde porównanie zaczyna się od podsumowania w jednym zdaniu. Przy podziale urlopu aplikacja pilnuje 9 tygodni nieprzenoszalnych i najwyżej 5 części.
- „Sprawy ZUS” (także link „Sprawy ZUS (dla oceniających)” w stopce): 11 spraw z zestawu ZUS do wybrania z listy albo przez dodanie pliku PDF. Plik jest rozpoznawany po sumie SHA-256 liczonej w przeglądarce i nie opuszcza urządzenia. Wynik zawiera ramkę „Porównanie z decyzją ZUS (sprawa testowa)” z decyzją i okresami z akt obok wyniku silnika. W jednej sprawie okres i kwota zgadzają się z wydrukiem ZUS w aktach, w ośmiu okres zgadza się co do dnia, w dwóch odmowa jest zgodna z decyzją ZUS.
- „Wnioski”: cztery wzory wniosków o urlop do pracodawcy (PDF), dane do wniosku ZAM, zapytanie do ZUS, wniosek o odpis aktu urodzenia i zaświadczenie o stanie składek z datą stanu salda — jako symulacja z numerem zgłoszenia. Daty podpowiada plan, a pola bez danych zostają puste do uzupełnienia odręcznie.
- „Moje dane w ZUS”: ubezpieczenia i płatnicy, podstawy składek, zwolnienia e-ZLA, saldo składek (symulacja).
- „Wsparcie”: zweryfikowane organizacje charytatywne oferujące bezpłatną pomoc, z filtrem „Twoja sytuacja” (poronienie, martwe urodzenie, śmierć dziecka, wcześniactwo, ciężka choroba dziecka, ciężka choroba lub śmierć matki), kontakt do ZUS i informacja o zasiłku pogrzebowym przy martwym urodzeniu lub zgonie dziecka. W trudnej sytuacji interfejs zmienia ton i stawia wsparcie na górze.
- „Baza wiedzy”: Poradnik ZUS dla rodziców (PDF ze spisem rozdziałów) i oficjalne źródła (zus.pl, MRPiPS, PIP).
- Czat „Zapytaj o przepisy” na ekranach Twój plan, Wsparcie i Baza wiedzy: 71 gotowych odpowiedzi z cytatami, wyszukiwarka przepisów, źródła przy każdej odpowiedzi.
- Panel `/admin`: parametry prawne z polskimi nazwami w grupach, z datą obowiązywania i historią zmian; włączanie świadczeń, przełączniki integracji, lista organizacji. Hasło do panelu przekazujemy jury osobno.
- Interfejs sprawdzony automatycznie przy szerokości 320, 390 i 768 px, bez przewijania w poziomie.

**Jak przetestować.** Fikcyjne osoby (np. Anna — pracownica w ciąży, Edyta — działalność z zaległością w składkach, Halina — mały płatnik i e-ZLA, Tadeusz — ojciec, gdy matka nie ma ubezpieczenia) są na ekranie logowania i opisane w README. Sprawy ZUS są na ekranie „Sprawy ZUS”: wybierz sprawę z listy albo dodaj jej plik PDF i porównaj wynik z decyzją ZUS. Lokalnie aplikacja uruchamia się bez Dockera i bez bazy danych (`dotnet run --project api/Api` oraz `npm run dev` w `web/`), a z PostgreSQL przez `docker compose up -d --build`.

**Co nas odróżnia.**
- Uprawnienia, okresy, kwoty i terminy liczy deterministyczny silnik reguł w C#. Model językowy nie bierze udziału w decyzji ani w kwocie.
- Każdy wynik ma „Ścieżkę decyzji” z przepisem i wyjaśnieniem prostym językiem.
- Parametry prawne (102 wpisy: wymiary urlopów, stawki, progi, płaca minimalna) są w bazie z datą obowiązywania. Zmiana w panelu zmienia wynik od wskazanego dnia, bez wdrażania kodu.
- Gdy przepis nie rozstrzyga sytuacji, silnik odpowiada „nie rozstrzygam” i odsyła do ZUS zamiast zgadywać. Brakujący fakt wraca jako pytanie do użytkownika.
- Reguły powstały z wyciągów przepisów i są sprawdzane 55 przypadkami referencyjnymi napisanymi przed kodem reguł oraz sprawami testowymi ZUS.

**Potencjał wdrożeniowy.** Integracje (tożsamość, rejestr ubezpieczeń, konto składkowe, e-ZLA, wysyłka zgłoszeń, rejestr stanu cywilnego) są ukryte za interfejsami. W demo zastępują je implementacje symulowane nad jednym zbiorem fikcyjnych osób; podmiana na prawdziwe usługi nie wymaga zmian w silniku. Panel parametrów pozwala odzwierciedlać zmiany przepisów bez programisty. Architektura: React + TypeScript, ASP.NET Core (.NET 10), PostgreSQL, REST, Docker Compose; zależności na licencjach permisywnych.

**Jakość.** `dotnet test`: 1050 testów, 1049 zielonych, 0 błędów, 1 pominięty (raport dla spraw ZUS z katalogu prywatnego, uruchamiany osobno). Do tego 55 przypadków referencyjnych w `fixtures/reference/`, test cytatów FAQ i walidacja fixtures schematami JSON w CI.

**Rozwiązania zewnętrzne.** React, TypeScript, Vite, Tailwind CSS, ASP.NET Core i .NET 10, Entity Framework Core, PostgreSQL (Npgsql), PDFsharp-MigraDoc, pdf.js, Docker, Microsoft Azure (hosting demo), opcjonalnie Claude Haiku przez Anthropic API (czat). Pełna lista z licencjami: `docs/ZALEZNOSCI.md`.

**Wykorzystanie AI.** Wskazujemy wszystkie miejsca, w których użyto sztucznej inteligencji:
1. **W działającej aplikacji — tylko czat „Zapytaj o przepisy”.** Czat najpierw szuka gotowej odpowiedzi w FAQ (71 pytań z dosłownymi cytatami z materiałów ZUS i przepisów; każdy cytat sprawdza test). Gdy pytanie nie pasuje do FAQ, wyszukuje fragmenty w wyciągach przepisów. Opcjonalnie, gdy ustawiony jest klucz API, model językowy (Claude Haiku firmy Anthropic) pisze krótką odpowiedź prostym językiem i wybiera do 5 cytatów spośród znalezionych fragmentów. Aplikacja pokazuje tylko cytaty zgodne słowo w słowo z tekstem w repozytorium, a wyjaśnienie modelu wyłącznie obok nich, z dopiskiem, że napisał je model i że wiążące są przepisy. Wyjaśnienie z kwotą, linkiem albo danymi osobowymi, a także zbyt długie, aplikacja zastępuje zdaniem ogólnym. Bez modelu czat odpowiada z FAQ i pokazuje wyniki wyszukiwania. Pytania o własną sprawę czat kieruje do kalkulatora.
2. **Kalkulator, uprawnienia, okresy, kwoty i terminy nie używają AI.** Liczy je deterministyczny silnik reguł. Aplikacja nie używa OCR i nie czyta treści PDF.
3. **Przygotowanie danych testowych.** Dokumenty spraw ZUS odczytano z pomocą modeli językowych poza aplikacją; w aplikacji te odczyty są tylko danymi wejściowymi silnika.
4. **Grafika.** Ilustracja na ekranie startowym została wygenerowana przez model AI.
5. **Wytworzenie projektu.** Kod, testy, treść FAQ i dokumentację wytworzyły modele Claude (Anthropic) w narzędziu Claude Code, pracujące pod nadzorem ludzi. Ludzie dostarczyli materiały prawne, zdecydowali o zakresie i weryfikowali wyniki; reguły pisano z wyciągów przepisów, nie z pamięci modelu.

**Założenia i ograniczenia.** Wszystkie integracje są symulowane, a dane osób fikcyjne. Dane o urodzeniu dziecka mają w symulacji źródło „Rejestr stanu cywilnego”, bo gdy zasiłek wypłaca ZUS, to ZUS sam pozyskuje odpis aktu urodzenia z tego rejestru (§ 13 ust. 2–4 rozporządzenia z 27.09.2026). Zadanie tego nie wymaga; wymaga symulacji wniosku o odpis aktu urodzenia, który jest w „Wnioskach”. Kwoty są brutto. Tam, gdzie materiały zadania nie rozstrzygają kwestii, przyjęliśmy jawne założenia, każde jako parametr w panelu: stawka dzienna zaokrąglana raz do 1 gr, jak w wyliczeniach ZUS w sprawach testowych; minimalna podstawa przy niepełnym etacie proporcjonalnie do wymiaru czasu pracy; zasiłek do dnia porodu według art. 30 ust. 3 ustawy. Podwyższenia do kwoty świadczenia rodzicielskiego (art. 31 ust. 3a) aplikacja nie wylicza, tylko o nim informuje. Indywidualną podstawę wymiaru silnik liczy dla typowych przypadków (średnia z 12 miesięcy); w pozostałych prosi o podstawę ustaloną przez płatnika. Szczegóły są w README w części „Ograniczenia i założenia”. Dokumenty spraw ZUS i dane z nich nie są w repozytorium; demo wczytuje je z osobnego katalogu na serwerze.

Demo: https://kompasrodzica.polandcentral.cloudapp.azure.com · Repozytorium: https://github.com/slotocki/babypedia · Zespół: 2Stosy (Sylwester Łotocki, Andrzej Laskowski)
