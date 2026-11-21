# Plan: pomocnik „Zapytaj o przepisy” (wyszukiwanie w przepisach + Claude Haiku)

Stan: **plan, niezaimplementowane.** W kodzie jest dziś tylko wyłączony szkielet (`api/Infrastructure/Assistant`,
`POST /api/assistant/ask`), którego interfejs WWW nie wywołuje.

## Po co

Zadanie ZUS wymaga „możliwości zapytania w razie wątpliwości” (PUE/eZUS) i wskazania funkcji działających ze
wsparciem AI. Dziś pytanie do ZUS to symulowane zgłoszenie bez odpowiedzi. Pomocnik daje odpowiedź od razu,
zawsze z cytatem przepisu, i jest jedyną funkcją aplikacji z modelem językowym — łatwo ją opisać w zgłoszeniu.

Czego pomocnik **nie** robi: nie ocenia uprawnień konkretnej osoby, nie podaje kwot, okresów ani terminów
dla sprawy. To liczy wyłącznie silnik reguł. Pomocnik nie dostaje faktów sprawy ani danych osoby.

## Co widzi użytkownik

- Pole „Zapytaj o przepisy” na ekranie **Wsparcie** oraz pod wynikiem („Masz wątpliwość? Zapytaj”).
- Odpowiedź: 2–5 zdań prostym językiem, pod nią **cytaty** — dosłowne fragmenty z nazwą aktu i numerem artykułu
  („Ustawa zasiłkowa, art. 29 ust. 1”), rozwijane do pełnej treści artykułu.
- Stały dopisek: „Odpowiedź wygenerował model językowy na podstawie pokazanych przepisów. Uprawnienia i kwoty
  wylicza kalkulator.” oraz oznaczenie „AI”.
- Gdy pytanie dotyczy konkretnej sytuacji albo kwoty: krótka odmowa i przycisk do kalkulatora.
- Gdy nie znaleziono przepisu: „Nie znalazłem tego w przepisach, z których korzysta aplikacja” + kontakt do ZUS.
- Gdy model jest wyłączony lub niedostępny: **tryb wyszukiwarki** — same pasujące artykuły, bez tekstu modelu.

## Jak to działa

```
pytanie ─► filtr danych osobowych ─► wyszukiwanie artykułów (BM25) ─► Claude Haiku z cytowaniem ─► kontrola odpowiedzi ─► ekran
                                            │                                                         │
                                  docs/prawo/*.md pocięte na artykuły                     brak cytatu / kwota → odrzucenie
```

1. **Indeks.** Przy starcie API wczytuje `docs/prawo/*.md` (ustawa zasiłkowa, Kodeks pracy, dwa rozporządzenia,
   załącznik ZUS, Poradnik ZUS; razem ok. 400 KB) i tnie je po nagłówkach `## Art. …` / `§`. Jeden fragment =
   jeden artykuł z metadanymi (akt, artykuł, tytuł rozdziału). Wychodzi kilkaset fragmentów, wszystko w pamięci.
2. **Wyszukiwanie.** BM25 z normalizacją polszczyzny (małe litery, bez znaków diakrytycznych, przycinanie
   końcówek fleksyjnych) i krótkim słownikiem synonimów („tacierzyński” → „urlop ojcowski”, „L4” → „zaświadczenie
   lekarskie”, „macierzyńskie” → „zasiłek macierzyński”). Zwraca 5–6 najlepszych artykułów.
3. **Model.** Claude Haiku 5.5 (`claude-haiku-5-5`) dostaje pytanie i znalezione artykuły jako dokumenty z włączonym
   cytowaniem. Polecenie systemowe: odpowiadaj wyłącznie na podstawie dokumentów, każde zdanie z cytatem, żadnych
   kwot ani oceny uprawnień konkretnej osoby, przy braku podstawy — powiedz, że jej nie ma.
4. **Kontrola odpowiedzi w kodzie.** Odpowiedź bez ani jednego cytatu jest odrzucana (pokazujemy tryb wyszukiwarki).
   Cytat musi wskazywać fragment, który faktycznie wysłaliśmy. Zdania z kwotą w złotych są usuwane, chyba że kwota
   występuje dosłownie w cytowanym fragmencie.

### Dlaczego bez „embeddera” i bazy wektorowej

Anthropic nie udostępnia modelu do embeddingów — potrzebny byłby drugi dostawca albo własny model na serwerze.
Przy korpusie wielkości jednej książki i pytaniach, w których padają te same słowa co w przepisach („zasiłek”,
„urlop”, „ubezpieczenie”), wyszukiwanie słownikowe ze słownikiem synonimów wystarcza, działa bez sieci i jest
w pełni wytłumaczalne (widać, które słowa zadecydowały). Gdyby testy pytań pokazały słabą trafność, wyszukiwarkę
da się podmienić za interfejsem `ILegalSearch` bez zmiany reszty.

## Zabezpieczenia

- **Dane osobowe:** pytanie do 500 znaków; odrzucamy pytania z ciągiem 11 cyfr, numerem rachunku lub e-mailem.
  Do modelu idzie tylko treść pytania i publiczne przepisy. Pytań nie zapisujemy w bazie ani w logach.
- **Klucz API:** zmienna `ANTHROPIC_API_KEY` wyłącznie w `~/babyradnik/.env` na serwerze. Nie trafia do
  repozytorium ani do przeglądarki. Na koncie Anthropic ustawiony limit wydatków.
- **Nadużycia:** limit 10 pytań na minutę z jednego adresu i dzienny limit liczby pytań (parametr); po
  przekroczeniu działa tryb wyszukiwarki.
- **Niezależność silnika:** `api/Domain` nie zależy od pomocnika (pilnuje tego istniejący `ContractTests`).
  Wyłączenie pomocnika niczego nie psuje.
- **Dokumenty ZUS** (PDF spraw) nie wchodzą do indeksu — tylko akty prawne i Poradnik z `docs/prawo`.

## Koszt

Jedno pytanie to ok. 6–8 tys. tokenów wejścia (artykuły) i do 400 tokenów odpowiedzi. Przy cenie Haiku 5.5
(0,10 USD / 0,50 USD za milion tokenów) wychodzi poniżej 0,001 USD za pytanie; tysiąc pytań jury i testów to
poniżej 1 USD. Ceny do potwierdzenia w cenniku w dniu wdrożenia.

## Kroki realizacji

| # | Krok | Wynik | Szacunek |
|---|---|---|---|
| 1 | `LegalCorpus`: wczytanie i pocięcie `docs/prawo/*.md` na artykuły | test: liczba artykułów, art. 29 ustawy ma właściwą treść | 30 min |
| 2 | `ILegalSearch` + BM25 z normalizacją i synonimami | test: 15 pytań kontrolnych, właściwy artykuł w pierwszej trójce | 45 min |
| 3 | `GET /api/law/search?q=` — tryb wyszukiwarki, bez modelu | działa bez klucza API | 15 min |
| 4 | Klient Claude przez oficjalny pakiet NuGet `Anthropic`, cytowanie, kontrola odpowiedzi | test na podstawionym kliencie: brak cytatu → odrzucenie, kwota → usunięcie | 60 min |
| 5 | Filtr danych osobowych, limity, konfiguracja (`ASSISTANT_PROVIDER=anthropic`) | testy filtrów i limitów | 30 min |
| 6 | Ekran: pole pytania, odpowiedź z cytatami, stany (ładowanie, odmowa, brak wyniku, tryb wyszukiwarki) | działa na telefonie 360 px | 60 min |
| 7 | Przegląd 15 odpowiedzi przez człowieka, poprawki polecenia systemowego | decyzja: włączyć na demo czy zostawić tryb wyszukiwarki | 30 min |
| 8 | README (sekcja „Wykorzystanie AI”), opis zgłoszenia, slajd | dokumentacja zgodna ze stanem | 15 min |

Razem ok. 4–5 godzin pracy. Kroki 1–3 i 6 dają działającą wyszukiwarkę przepisów nawet bez modelu.

Przed krokiem 4 trzeba sprawdzić w dokumentacji: licencję pakietu `Anthropic` (dopuszczamy MIT/Apache-2.0/BSD),
dokładny kształt wywołania z cytowaniem w C# oraz aktualną cenę. Nie piszemy tych wywołań z pamięci.

## Pytania kontrolne (krok 2 i 7)

Ile trwa urlop macierzyński przy bliźniętach? · Czy ojciec może wziąć urlop rodzicielski, gdy matka nie pracuje? ·
Do kiedy trzeba wykorzystać urlop ojcowski? · Co to jest okres wyczekiwania? · Czy na umowie zlecenia dostanę
zasiłek macierzyński? · Co się dzieje z zasiłkiem, gdy mam zaległe składki? · Czy można pracować na urlopie
rodzicielskim? · Ile wynosi zasiłek macierzyński? (ma podać stawkę z przepisu, nie kwotę) · Co po martwym
urodzeniu? · Kto wypłaca zasiłek — pracodawca czy ZUS? · Jakie dokumenty do zasiłku macierzyńskiego? ·
Czy rodzina zastępcza ma prawo do zasiłku? · Co gdy umowa kończy się w ciąży? · Ile dostanę przy pensji 6000?
(ma odmówić i odesłać do kalkulatora) · Jaka będzie jutro pogoda? (ma odmówić)

## Ryzyka

- **Błędne streszczenie przepisu przed jury z ZUS.** Ograniczamy przez: cytat przy każdym zdaniu, kontrolę
  w kodzie, zakaz kwot, przegląd 15 odpowiedzi przed włączeniem. Jeśli przegląd wypadnie słabo, na demo zostaje
  tryb wyszukiwarki (też spełnia „zapytanie w razie wątpliwości”, bez ryzyka zmyślenia).
- **Wyciągi przepisów są niepełne** (to wyciągi, nie całe akty) — pomocnik mówi wprost, gdy nie znajdzie podstawy.
- **Czas.** Do zamrożenia kodu zostaje jedna noc; kroki są ułożone tak, by każdy kończył się działającą całością.

## Decyzje do podjęcia przez zespół

1. Robimy całość, czy tylko tryb wyszukiwarki (kroki 1–3 i 6, ok. 2,5 godziny, bez klucza API)?
2. Klucz `ANTHROPIC_API_KEY` wpisuje do `.env` na serwerze członek zespołu (nie agent) i ustawia limit wydatków.
3. Czy pomocnik ma być widoczny pod wynikiem, czy tylko na ekranie Wsparcie?
