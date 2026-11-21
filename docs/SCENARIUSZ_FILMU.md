# Kompas Rodzica: scenariusz filmu (do 3 minut)

Cel: pokazać w 3 minuty pełny przebieg, odmowę z powodem, porównanie wariantów, wniosek PDF, panel parametrów i telefon. Nagrywaj na adresie demo https://20-215-34-39.sslip.io. Zalecane: panel `/admin` nagraj na lokalnej kopii (`docker compose up -d --build`, osobna baza), żeby zmiana parametru nie została na publicznym demo. Hasło administratora nie może być widoczne w kadrze (pole hasła jest maskowane; nie pokazuj pliku `.env`).

Przed nagraniem sprawdź na demo każdy krok: wyniki poniżej są oczekiwane na podstawie reguł i przypadków referencyjnych, a konkretne kwoty i daty odczytaj z ekranu i wstaw do tekstu lektora (oznaczone `[z ekranu]`).

Osoby: **Anna Przykładowa** (`anna-przyklad`: pracownica, ciąża, termin 15.03.2027) do ścieżki głównej, **Edyta Pokazowa** (`edyta-demo`: działalność, zadłużenie 120,00 zł, termin 5.05.2027) do odmowy.

Lektor: ok. 2,3 słowa na sekundę, czyli około 400 słów na cały film. Teksty poniżej mieszczą się w tym limicie.

| Czas | Na ekranie (kliknięcia) | Lektor |
|---|---|---|
| 0:00–0:12 | Plansza tytułowa „Kompas Rodzica", potem strona startowa demo. | Rodzic w ciąży nie wie, co mu przysługuje i do kiedy złożyć wniosek. Kompas Rodzica odpowiada na to pytanie dla konkretnej osoby. |
| 0:12–0:30 | Start (`/`). Pytanie 1: „jestem w ciąży". Pytanie 2: data porodu 15.03.2027. Pytanie 3: „dla mnie". Przycisk dalej. | Trzy pytania: co się dzieje, kiedy i dla kogo liczymy. Nic więcej na początku. |
| 0:30–0:42 | Logowanie (`/logowanie`). Widoczny baner „symulacja". Kliknij „Zaloguj" przy Annie Przykładowej. Pokaż zamaskowany PESEL i liczbę pobranych danych. | Logowanie przez mObywatel to symulacja i jest tak oznaczona. Osoby są fikcyjne. Dane „z ZUS" pobiera symulator. |
| 0:42–0:58 | Sprawdź dane (`/sprawdz-dane`). Przewiń listę faktów: przy każdym plakietka źródła (mObywatel, ZUS, Ty, założenie). Kliknij „Popraw" przy jednym fakcie. | Każdy fakt ma źródło i można go poprawić. Pytamy tylko o to, czego silnik nie wie. |
| 0:58–1:25 | Wynik (`/wynik`). Pokaż blok „Co Ci przysługuje": karta zasiłku macierzyńskiego z okresem i kwotą `[z ekranu]`. Rozwiń ścieżkę decyzji. Pokaż „Kto wypłaca" i „Gdzie złożyć wniosek". Przewiń do „Co zrobić i do kiedy" i do jednego „najbliższego kroku". | Zasiłek macierzyński: okres od–do i kwota `[z ekranu]`. Rozwinięta ścieżka decyzji pokazuje każdą regułę, artykuł i warunek. Wynik liczy deterministyczny silnik reguł, nie model językowy. Na górze jest najbliższy krok z terminem. |
| 1:25–1:50 | Wróć na Start, wpisz termin 5.05.2027 i zaloguj się jako Edyta Pokazowa. Na wyniku wskaż kartę ze statusem „nie przysługuje". Pokaż powód, podstawę prawną i ramkę „Co musiałoby się zmienić" (kwota do spłaty i data). | Tu zasiłku nie ma: zadłużenie składkowe 120 złotych w dniu powstania prawa. Aplikacja podaje powód i artykuł, i mówi, co musiałoby się zmienić: spłata kwoty do wskazanej daty. |
| 1:50–2:10 | „Porównaj warianty" (`/porownaj-warianty`). Dla Edyty wybierz „Spłacam zadłużenie" i datę. Pokaż zmianę statusu. Dla Anny (wróć) wybierz „Wariant wypłaty", „Uśredniony", pokaż różnicę w tygodniach i złotych. | „Co jeśli": spłacam zadłużenie, i status się zmienia. Albo wariant uśredniony: widać różnicę w złotych i w tygodniach, a nowe ryzyka i terminy są wypisane. |
| 2:10–2:25 | Wnioski (`/wnioski`). Wybierz „Urlop rodzicielski lub jego część", wypełnij pola, kliknij „Wyślij (symulacja)". Pokaż potwierdzenie z numerem i „Pobierz PDF". | Wniosek do pracodawcy w PDF, pola z rozporządzenia. Wysyłka to symulacja: dostajemy numer i wpis w „Moje zgłoszenia". |
| 2:25–2:38 | „Moje dane w ZUS" (`/moje-dane-zus`): ubezpieczenia, podstawy składek, e-ZLA, saldo. Na krótko „Sprawy ZUS" (`/sprawy`): lista i „Dodaj PDF sprawy". | Podgląd konta w ZUS, e-ZLA i saldo składek, też z symulatora. Na ekranie spraw ZUS plik PDF jest rozpoznawany po sumie SHA-256 i nie opuszcza przeglądarki. |
| 2:38–2:52 | Panel `/admin` (lokalna kopia). Zaloguj hasłem (bez pokazywania). Zakładka „Parametry", pole „Szukaj parametru": `stawka_rodzicielski`. Kliknij „Dodaj wartość od dnia…", wpisz nową wartość i „Obowiązuje od" 1.01.2027, „Zapisz wartość". Pokaż „Historia zmian". Wróć do wyniku Anny i pokaż zmianę kwoty rodzicielskiego. | Parametry prawne są w bazie z datą obowiązywania. Zmieniamy stawkę od 1 stycznia 2027, a wynik zmienia się tylko dla tego okresu. Każda zmiana trafia do historii. |
| 2:52–3:00 | Widok na telefonie (okno 390 px w przeglądarce albo prawdziwy telefon): Start i Wynik. Plansza końcowa z adresem demo i repozytorium. | Całość działa też na telefonie. Kompas Rodzica: informuje, nie orzeka, i mówi „nie rozstrzygam", gdy przepis nie daje odpowiedzi. |

Uwagi do montażu:

- Każdy kadr z aplikacją musi mieć widoczny napis „symulacja" tam, gdzie występuje symulowana integracja (baner na logowaniu, plakietki przy wnioskach).
- Wniosek o odpis aktu urodzenia, zapytanie do ZUS i zaświadczenie o saldzie składek można dopowiedzieć jednym zdaniem przy ekranie „Wnioski" (jeśli zostanie czas), bez osobnych ujęć.
- AI: w napisach końcowych dodaj jedno zdanie: „Kod i testy powstały z udziałem modeli Claude (Anthropic) w Claude Code; w działaniu aplikacji AI nie liczy ani nie decyduje".
- Jeśli konkretny wynik na demo różni się od opisanego w tabeli, zmień tekst lektora na zgodny z ekranem, nie odwrotnie.
- Film ma mieć najwyżej 3 minuty; przy przekroczeniu skróć najpierw ujęcie „Moje dane w ZUS".
