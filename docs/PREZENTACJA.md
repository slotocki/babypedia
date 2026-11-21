# Kompas Rodzica: treść prezentacji

Najwyżej 10 slajdów. Układ pod kryteria oceny: związek z wyzwaniem 25%, kompletność i UX 25%, pomysł 20%, oryginalność 15%, potencjał wdrożeniowy 15%. Liczby pochodzą z repozytorium (gałąź `main`, 21.11.2026). Adres demo: https://20-215-34-39.sslip.io.

---

## Slajd 1. Kompas Rodzica

Kryterium: związek z wyzwaniem.

- Zadanie ZUS: „Zasiłkowy Przewodnik Przyszłego Rodzica"
- Przewodnik po zasiłkach z ubezpieczenia chorobowego dla przyszłych rodziców
- Co przysługuje, ile, do kiedy i na co uważać, dla konkretnej osoby
- Zespół 2Stosy: Sylwester Łotocki, Andrzej Laskowski

Notatka: Przedstawiamy się jednym zdaniem: aplikacja odpowiada rodzicowi na pytanie, co mu przysługuje, zanim będzie za późno na zmianę sytuacji. Demo działa pod adresem na slajdzie.

## Slajd 2. Problem

Kryterium: związek z wyzwaniem.

- Prawo do zasiłku zależy od tytułu ubezpieczenia, stażu, płatnika, liczby dzieci i daty zdarzenia
- Rodzic nie wie, co mu przysługuje ani do kiedy złożyć wniosek
- O odmowie dowiaduje się, gdy zmiana sytuacji jest już niemożliwa
- Potrzebna odpowiedź z podstawą prawną, nie ogólna broszura

Notatka: Pokaż, że problem to nie brak informacji, tylko jej dopasowanie do jednej osoby. Przykład: własne wypowiedzenie w ciąży odbiera prawo do zasiłku macierzyńskiego, a o tym łatwo nie wiedzieć.

## Slajd 3. Rozwiązanie: od trzech pytań do wyniku

Kryterium: kompletność i UX.

- Trzy pytania: co się dzieje, kiedy, dla kogo liczymy
- Logowanie mObywatel i dane z ZUS (symulacja), każdy fakt ze źródłem i przyciskiem „Popraw"
- Wynik: co przysługuje, co zrobić i do kiedy, na co uważać, inny wariant
- Wnioski w PDF, zaświadczenie o saldzie, wsparcie; działa na telefonie od 360 px

Notatka: To jest główny przebieg i pierwszy, który działa od początku do końca. Każdy ekran ma jedną główną akcję, a każda symulacja jest oznaczona słowem „symulacja".

## Slajd 4. Wynik, który da się sprawdzić

Kryterium: pomysł.

- Karta świadczenia: status, okres od–do, stawka, kwota, kto wypłaca, gdzie złożyć wniosek
- Rozwijana ścieżka decyzji: reguła, artykuł, warunek, spełniony lub nie, wyjaśnienie prostym językiem
- Odmowa zawsze z powodem, podstawą prawną i „co musiałoby się zmienić"
- Jeden „najbliższy krok" na górze listy terminów

Notatka: To nasza główna różnica: nie „system odmówił", tylko „odmowa, bo art. 29 ust. 1, a zmieniłaby się, gdyby…". Jurorzy mogą sprawdzić każdy krok w przepisie.

## Slajd 5. „Co jeśli" i porównanie wariantów

Kryterium: pomysł, kompletność.

- Siedem scenariuszy: odejście z pracy, koniec umowy, przejście na działalność, drugi rodzic bierze część urlopu rodzicielskiego, praca na część etatu, wariant wypłaty (standardowy lub 81,5%), spłata zadłużenia
- Silnik liczy drugi raz na zmienionych faktach
- Wynik jako różnica w tygodniach i złotych oraz zmiana statusów
- Walidacja planu: 9 tygodni nieprzenoszalnych, najwyżej 5 części

Notatka: Tu pokazujemy zagrożenia z zadania: utratę uprawnień, zmianę wymiaru i wysokości. Rodzic widzi skutek decyzji, zanim ją podejmie.

## Slajd 6. Pokrycie wymagań zadania

Kryterium: związek z wyzwaniem, kompletność.

- Wszystkie pozycje a–j z zadania mają ekran, endpoint i moduł kodu (mapa w README)
- Symulacje S1–S7: logowanie, dane ZUS, saldo i zaświadczenie, e-ZLA, zapytanie PUE, wnioski, odpis aktu
- S8–S10: wsparcie i linki, panel `/admin`; T1–T2: responsywność i ekran „Sprawy ZUS"
- Uczciwy status: lista organizacji charytatywnych jest pusta, dane spraw ZUS są poza repozytorium

Notatka: Mówimy wprost, co działa w pełni, a co częściowo. Pełna tabela jest w README, wiersz po wierszu, ze statusem.

## Slajd 7. Silnik reguł: deterministyczny, z datą obowiązywania

Kryterium: oryginalność.

- Uprawnienia, okresy, kwoty i terminy liczy wyłącznie silnik reguł w C#, bez udziału modelu językowego
- Ponad 80 małych reguł, każda z identyfikatorem i podstawą prawną
- Parametry prawne w bazie z datą obowiązywania; zmiana w `/admin` zmienia wynik we właściwym okresie
- Gdy przepis nie rozstrzyga: „nie rozstrzygam, skontaktuj się z ZUS", bez zgadywania

Notatka: To odpowiedź na pytanie, czy można ufać kalkulatorowi: ta sama odpowiedź na te same dane i pełny ślad. Uczciwe „nie rozstrzygam" jest cechą, nie brakiem.

## Slajd 8. Jak powstał projekt i rola AI

Kryterium: oryginalność, uczciwość.

- W działaniu aplikacji AI nie decyduje o niczym; opcjonalny pomocnik tekstowy jest domyślnie wyłączony i nie dostaje danych osoby
- Kod, testy i dokumentację napisały modele Claude (Anthropic) w narzędziu Claude Code
- Zespół agentów: modele pisały gałęzie, osobny model przeglądał i zatwierdzał zmiany silnika
- Ludzie: materiały prawne, decyzje zakresu, weryfikacja; reguły z wyciągów przepisów, nie z pamięci modelu

Notatka: Mówimy wprost, że kod pisały modele, i pokazujemy, jak to kontrolowaliśmy: testy referencyjne przed regułami i przegląd silnika przez osobny model.

## Slajd 9. Jakość i potencjał wdrożeniowy

Kryterium: potencjał wdrożeniowy.

- `dotnet test`: 807 testów, 793 zielone, 0 błędów, 14 pominiętych (sprawy ZUS czekają na dane)
- 54 aktywne przypadki referencyjne (okresy, uprawnienia, kwoty, parametry) z `fixtures/reference/`
- Integracje za interfejsami (tożsamość, rejestr ubezpieczeń, konto składkowe, e-ZLA, wysyłka): podmiana symulacji na usługi bez zmian w silniku
- Panel parametrów, Docker Compose, CI; zależności na licencjach permisywnych

Notatka: Wdrożenie to podmiana pięciu implementacji `Simulated…` i uzupełnienie parametrów, nie przepisywanie silnika. Pokaż listę licencji w repozytorium.

## Slajd 10. Ograniczenia, następne kroki, demo

Kryterium: potencjał wdrożeniowy, kompletność.

- Do potwierdzenia z ZUS: zaokrąglanie, brutto/netto, minimalna podstawa przy niepełnym etacie, druki Z-3 i Z-3a, kwota świadczenia rodzicielskiego
- Wszystkie integracje symulowane, dane osób fikcyjne, kwoty brutto
- Dalej: prawdziwe integracje, zweryfikowana lista organizacji, dane spraw ZUS w testach
- Demo: https://20-215-34-39.sslip.io, kod: github.com/slotocki/babypedia

Notatka: Kończymy listą rzeczy, których nie rozstrzygnęliśmy, bo to z nimi trzeba iść do ZUS. Zapraszamy do przetestowania demo na osobach z README.
