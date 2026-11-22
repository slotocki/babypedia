# INDEKS przepisów przywołanych w instrukcji (sekcja 8 i 12.2)

Cel: dla każdego przepisu, który instrukcja `BABYRADNIK_INSTRUKCJA.md` wskazuje w sekcji 8 (8.1–8.10) i 12.2, podać plik i nagłówek, pod którym leży jego tekst, oraz czy przepis jest w wyciągu.
Nagłówki w plikach `.md` mają postać `## Art. 29` (ustawa, KP) albo `## § 7` (rozporządzenia); tekst każdego przepisu leży pod swoim nagłówkiem. Źródłem prawdy dla wszystkich plików jest `raw/*.txt`.

Legenda statusu: **jest** = pełny tekst artykułu w wyciągu organizatora; **jest (fragment)** = artykuł jest, ale instrukcja przywołuje coś, czego w nim nie ma; **brak** = nie ma w materiałach organizatora (patrz sekcja "Poza materiałami").

Wynik wstępnego przeszukania z zadania ("art. 7 ustawy i art. 182¹e KP nie występują jako osobne nagłówki") jest nieprawdziwy: **oba są w wyciągu**. W surowym tekście art. 7 ustawy stoi na początku strony (po znaku nowej strony, przed nim nie ma spacji), a art. 182¹e KP jest zapisany jako `Art. 1821e` (indeks górny spłaszczony do cyfry). Patrz `ROZBIEZNOSCI.md`, sekcja C.

## 1. Ustawa zasiłkowa (`ustawa-zasilkowa.md`, Dz.U. 2026 poz. 854, wyciąg)

| Przepis | Gdzie w instrukcji | Plik i nagłówek | Status |
|---|---|---|---|
| art. 2a ust. 1–2 (zadłużenie, 1% płacy minimalnej, 6 miesięcy) | 8.1 (prog_zadluzenia, termin_splaty), 8.2, 8.3 pkt 7, 8.9 | `ustawa-zasilkowa.md` → `## Art. 2a` | jest |
| art. 3 pkt 3–4 (wynagrodzenie, przychód, 13,71%) | 8.1 (odliczenie_skladek), 8.7 | `ustawa-zasilkowa.md` → `## Art. 3` | jest (w pkt 3 brak liczby 13,71%; jest tylko w pkt 4, art. 45 i Załączniku) |
| art. 4 ust. 1–3 (wyczekiwanie 30/90 dni, przerwa 30 dni, wyjątki) | 8.1, 8.2, 8.4 | `ustawa-zasilkowa.md` → `## Art. 4` | jest |
| art. 6 (przyczyny niezdolności; "wg art. 6–7") | 8.4 | `ustawa-zasilkowa.md` → `## Art. 6` | jest |
| art. 7 (zasiłek po ustaniu tytułu ubezpieczenia) | 8.4 | `ustawa-zasilkowa.md` → `## Art. 7` (oraz `## Art. 7a`) | jest |
| art. 8 ust. 1 (270 dni w ciąży) | 8.1, 8.4 | `ustawa-zasilkowa.md` → `## Art. 8` | jest |
| art. 11 ust. 2 (100% w ciąży) | 8.1, 8.4 | `ustawa-zasilkowa.md` → `## Art. 11` | jest |
| art. 17 (utrata prawa do zasiłku) | 8.4 | `ustawa-zasilkowa.md` → `## Art. 17` | jest (ust. 1d, 1e i art. 9 ust. 4 wchodzą w życie 1.01.2027, patrz przypisy 3–5) |
| art. 18 (świadczenie rehabilitacyjne, 12 miesięcy) | 8.1, 8.4 | `ustawa-zasilkowa.md` → `## Art. 18` | jest |
| art. 19 ust. 1 (stawki rehabilitacyjnego) | 8.1 | `ustawa-zasilkowa.md` → `## Art. 19` | jest |
| art. 29 ust. 1, 3, 5–7 (prawo do zasiłku macierzyńskiego) | 8.1, 8.3 pkt 1, 4, 5, 6 | `ustawa-zasilkowa.md` → `## Art. 29` | jest |
| art. 29a (okres zasiłku) | 8.3 (okres, część dla ojca) | `ustawa-zasilkowa.md` → `## Art. 29a` | jest |
| art. 30 (zasiłek po ustaniu ubezpieczenia w ciąży; ust. 3 zasiłek do dnia porodu; ust. 4 przedłużona umowa) | 8.3 pkt 2 | `ustawa-zasilkowa.md` → `## Art. 30` | jest |
| art. 30a (wniosek o wariant 81,5%, 21 dni) | 8.1, 8.6, 8.9 | `ustawa-zasilkowa.md` → `## Art. 30a` | jest |
| art. 31 ust. 1–5 (stawki 100/70/81,5%, ust. 3a, 3f, 3g, 4, 4a) | 8.1, 8.6 | `ustawa-zasilkowa.md` → `## Art. 31` | jest (kwota świadczenia rodzicielskiego z ust. 3a nie występuje w materiałach; patrz sekcja 5) |
| art. 36 (podstawa wymiaru pracownika; ust. 3: 1/30) | 8.6, 8.7 | `ustawa-zasilkowa.md` → `## Art. 36` | jest |
| art. 37 (miesiąc niepełny, uzupełnianie) | 8.7 pkt 3 | `ustawa-zasilkowa.md` → `## Art. 37` | jest |
| art. 38 (przeciętne wynagrodzenie, miesiące do pominięcia) | 8.7 pkt 1, 3 | `ustawa-zasilkowa.md` → `## Art. 38` | jest |
| art. 39 (zasiłek wyrównawczy jak wynagrodzenie) | 8.7 | `ustawa-zasilkowa.md` → `## Art. 39` | jest |
| art. 40 (zmiana wymiaru czasu pracy) | 8.7 | `ustawa-zasilkowa.md` → `## Art. 40` | jest |
| art. 41 (składniki wyłączone) | 8.7 | `ustawa-zasilkowa.md` → `## Art. 41` | jest |
| art. 42 (składniki kwartalne i roczne, 1/12) | 8.7 pkt 4 | `ustawa-zasilkowa.md` → `## Art. 42` | jest |
| art. 43 (brak przeliczenia przy przerwie krótszej niż miesiąc) | 8.7 pkt 6 | `ustawa-zasilkowa.md` → `## Art. 43` | jest |
| art. 45 (minimalna podstawa, 13,71%) | 8.1, 8.2, 8.7 pkt 5 | `ustawa-zasilkowa.md` → `## Art. 45` | jest (mowa o pracy "w pełnym wymiarze czasu pracy"; proporcji do etatu w tekście nie ma) |
| art. 48 (podstawa niebędącego pracownikiem) | 8.7 | `ustawa-zasilkowa.md` → `## Art. 48` | jest |
| art. 48a (ubezpieczenie krótsze niż 12 miesięcy, wzór) | 8.7 pkt 2 | `ustawa-zasilkowa.md` → `## Art. 48a` (tytuł w wyciągu: "[Rozwinięcie]") | jest (ust. 5 odsyła do art. 18 ust. 8 ustawy o systemie ubezpieczeń społecznych, którego nie ma w materiałach; patrz sekcja 5) |
| art. 49 (okres ubezpieczenia krótszy niż miesiąc) | 8.7 | `ustawa-zasilkowa.md` → `## Art. 49` | jest |
| art. 50 (niewykonywanie działalności przez część okresu) | 8.7 | `ustawa-zasilkowa.md` → `## Art. 50` | jest |
| art. 61 ust. 1 (płatnik > 20 ubezpieczonych; ust. 2: stan na 30 listopada) | 8.1, 8.8 | `ustawa-zasilkowa.md` → `## Art. 61` | jest |
| art. 64 (termin wypłaty 30 dni) | 8.8 | `ustawa-zasilkowa.md` → `## Art. 64` | jest |
| art. 67 (przedawnienie 6 miesięcy) | 8.1, 8.3, 8.9 | `ustawa-zasilkowa.md` → `## Art. 67` | jest (ust. 3 i 4: wyjątki, których instrukcja nie wymienia) |
| art. 61b ust. 3, 5 (treść wniosku, przekazanie wniosku przez płatnika) | 8.8, 12.2 (zestawienie "dane do wniosku ZAM") pośrednio | `ustawa-zasilkowa.md` → `## Art. 61b` | jest (przywołany pośrednio; ust. 1, 2, 2a, 4, 5a, 5b w brzmieniu od 1.07.2026, przypisy 6–11) |

Nie przywołane w instrukcji, ale potrzebne do prawidłowego odczytu: art. 9 (okres zasiłkowy, przerwa 60 dni; Załącznik i Poradnik się na to powołują), art. 12–16, art. 46–47, art. 61a.
Artykuły obecne w wyciągu ustawy: 1, 2, 2a, 3, 4, 5, 6, 7, 7a, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 29, 29a, 30, 30a, 31, 36–47, 48, 48a, 49, 50, 51, 52, 52a, 52b, 53, 54, 54a, 55, 55a, 55b, 56, 57, 58, 58a, 61, 61a, 61b, 62, 63, 64, 65, 66, 66a, 67. Brak w wyciągu m.in. art. 23–28 (zasiłek wyrównawczy), 32–35 (zasiłek opiekuńczy), 59–60; w sekcji 8 instrukcji nie są przywoływane.

## 2. Kodeks pracy (`kodeks-pracy.md`, tekst ujednolicony z 1.09.2026, Dz.U. 2026 poz. 1245, wyciąg)

Numeracja artykułów z indeksami górnymi w nagłówkach jest odtworzona znakami indeksu (`## Art. 182¹a`). Numery § w treści pozostały w postaci surowej (spłaszczonej); mapowanie niejednoznacznych w sekcji 6 poniżej.

| Przepis | Gdzie w instrukcji | Plik i nagłówek | Status |
|---|---|---|---|
| art. 177 (ochrona stosunku pracy; § 1¹ terminy 14/21/7 dni; § 3 przedłużenie umowy do dnia porodu) | 8.2, 8.3 pkt 2 | `kodeks-pracy.md` → `## Art. 177` | jest |
| art. 180 § 1 (wymiar 20/31/33/35/37) | 8.1 | `kodeks-pracy.md` → `## Art. 180` | jest |
| art. 180 § 2 (max 6 tygodni przed porodem) | 8.1 | `kodeks-pracy.md` → `## Art. 180` | jest |
| art. 180 § 4 (rezygnacja po 14 tygodniach) | 8.1 | `kodeks-pracy.md` → `## Art. 180` | jest |
| art. 180 § 8 (wniosek o rezygnację 7 dni przed) | 8.9 | `kodeks-pracy.md` → `## Art. 180` | jest |
| art. 180 § 9 (wniosek ojca 14 dni przed) | 8.9 | `kodeks-pracy.md` → `## Art. 180` | jest |
| art. 180¹ § 1–2 (martwe urodzenie, zgon dziecka) | 8.1, 8.5 | `kodeks-pracy.md` → `## Art. 180¹` | jest |
| art. 180² (uzupełniający: § 1 przypadki, § 2 wieloraczki, § 3 zaokrąglanie, § 4 wniosek 21 dni) | 8.5, 8.9 | `kodeks-pracy.md` → `## Art. 180²` | jest |
| art. 182¹a § 1–2, 4 (rodzicielski: 41/43, 65/67, 9 tygodni nieprzenoszalne) | 8.1, 8.5 | `kodeks-pracy.md` → `## Art. 182¹a` | jest |
| art. 182¹c (5 części, do końca roku, w którym dziecko kończy 6 lat) | 8.1, 8.5, 8.9 | `kodeks-pracy.md` → `## Art. 182¹c` | jest |
| art. 182¹d (wniosek o rodzicielski, 21 dni przed) | 8.9 | `kodeks-pracy.md` → `## Art. 182¹d` | jest |
| art. 182¹e (łączenie z pracą, max połowa etatu, 21 dni przed) | 8.1, 8.9 | `kodeks-pracy.md` → `## Art. 182¹e` | jest (w surowym tekście: `Art. 1821e`) |
| art. 182¹f (wydłużenie: 82/86, Za życiem 130/134) | 8.1, 8.5 | `kodeks-pracy.md` → `## Art. 182¹f` | jest |
| art. 182³ (ojcowski: § 1 12 miesięcy, § 1¹ części, § 2 wniosek 7 dni) | 8.1, 8.5, 8.9 | `kodeks-pracy.md` → `## Art. 182³` | jest |
| art. 183 (§ 1, § 1¹ wiek 7/10/14; § 3 minimum 9 tygodni; § 4, 4², 4³ urlop rodzicielski 38/62; § 6 wniosek 7 dni / start do 21 dni) | 8.1, 8.3 pkt 6, 8.9 | `kodeks-pracy.md` → `## Art. 183` | jest |
| art. 186⁸a (delegacja do rozporządzenia o wnioskach) | 12.2 pośrednio | `kodeks-pracy.md` → `## Art. 186⁸a` | jest |

Artykuły obecne w wyciągu KP: 1, 2, 3, 92, 175¹, 177, 180, 180¹, 180², 181, 182, 182¹, 182¹a–182¹g, 182², 182³, 182⁴, 183, 183¹, 183², 184, 185, 186⁴, 186⁸a, 189¹.
Instrukcja przywołuje wszystkie potrzebne artykuły KP; żaden nie brakuje. Art. 92 KP (wynagrodzenie chorobowe 80% / 100% w ciąży; 33/14 dni) jest w wyciągu i potwierdza parametr, który instrukcja opiera tylko na Załączniku.

## 3. Rozporządzenia

| Przepis | Gdzie w instrukcji | Plik i nagłówek | Status |
|---|---|---|---|
| rozp. 11.03.2025, § 2 (wniosek o część macierzyńskiego przed porodem) | 12.2 | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 2` | jest |
| rozp. 11.03.2025, § 3–13 (rezygnacja, wnioski ojca / innego członka rodziny) | 12.2 (szablony macierzyński) | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 3` … `## § 13` | jest |
| rozp. 11.03.2025, § 14 (uzupełniający urlop macierzyński) | 12.2 | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 14` | jest |
| rozp. 11.03.2025, § 15 (urlop rodzicielski lub jego część) | 12.2 | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 15` | jest |
| rozp. 11.03.2025, § 16 (łączenie urlopu rodzicielskiego z pracą) | 12.2, 8.9 | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 16` | jest |
| rozp. 11.03.2025, § 17 (urlop ojcowski) | 12.2 | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 17` | jest |
| rozp. 11.03.2025, § 18 (urlop na warunkach urlopu macierzyńskiego, przyjęcie dziecka) | 12.2 | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 18` | jest |
| rozp. 11.03.2025, § 21–23 (dokumenty zagraniczne, akta osobowe, oryginał na żądanie) | 12.2 | `rozporzadzenie-2025-03-11-wnioski-pracownicze.md` → `## § 21`–`## § 23` | jest |
| wniosek "urlop macierzyński i rodzicielski łącznie (pełny wymiar)" | 12.2 | brak odpowiednika w rozp. 11.03.2025 (patrz `ROZBIEZNOSCI.md`, sekcja D) | brak (jako osobny wniosek do pracodawcy); wniosek o pełny wymiar to art. 30a ustawy, adresowany do płatnika zasiłku |
| rozp. 27.09.2026, § 12–30 i Załącznik 6 (dokumenty i dane do zasiłku macierzyńskiego; § 28: podwyższenie do świadczenia rodzicielskiego), § 3, Załączniki 2–3 (zaświadczenie płatnika składek) | 8.8, 12.2 ("dane do wniosku ZAM") | `rozporzadzenie-2026-09-27-dokumenty-zasilki.md` → `## § 12` … `## § 30`, `## Załącznik 6. …`, `## § 3`, `## Załącznik 2. …`, `## Załącznik 3. …` | jest (rozporządzenie opisuje zakres danych i dokumenty, nie nazywa formularzy ZAM, Z-3, Z-3a) |
| rozp. 27.09.2026, § 43 (wejście w życie), § 42 (utrata mocy rozp. z 8.12.2015) | 8.8 (daty zmian formularzy) | `rozporzadzenie-2026-09-27-dokumenty-zasilki.md` → `## § 43`, `## § 42` | jest (oznaczenie Dz.U. z 2026 r. poz. 1316 jest w nagłówku wyciągu i w treści zadania ZUS, s. 2; daty ogłoszenia wyciąg nie podaje. Datę 24.10.2026 dla połączenia ZUR i ZAO z ZAM oraz likwidacji Z-3b podaje treść zadania ZUS, s. 12, przypisy 3–5; patrz `ROZBIEZNOSCI.md`, A1) |

## 4. Załącznik ZUS i Poradnik ZUS

| Źródło wskazane w instrukcji | Gdzie w instrukcji | Plik i miejsce | Status |
|---|---|---|---|
| Załącznik: 33 / 14 dni wynagrodzenia chorobowego | 8.1 | `zalacznik-zus.md` → `## Zasiłek chorobowy w okresie ciąży` (tabela) | jest |
| Załącznik: 13,71% | 8.1 | `zalacznik-zus.md` → `## Podstawa wymiaru zasiłków` | jest |
| Załącznik: płaca minimalna 4 806 zł | 8.1 | `zalacznik-zus.md` → `### 2. Minimalna podstawa wymiaru zasiłku w 2026 roku` (tylko w zdaniu wprowadzającym; tabela pod nim jest pusta w DOCX) | jest (kwota); brak (tabeli z wynikami) |
| Załącznik: 23 550,00 zł → 20 321,30 zł | 8.1 | `zalacznik-zus.md` → `## Dla nie pracowników` | jest |
| Poradnik s. 27: wniosek 14 dni przed urlopem macierzyńskim przed porodem | 8.9 | `poradnik-zus-2026.md` → `#### Zasiłek macierzyński za okres urlopu macierzyńskiego przed porodem` (znacznik `<!-- s. 27 -->`) | jest |
| Poradnik s. 35–36: zasiłek pogrzebowy 7 000 zł, Z-12, 12 miesięcy | 8.1 | `poradnik-zus-2026.md` → `#### Zasiłek pogrzebowy, gdy dziecko urodzi się martwe` (znaczniki `<!-- s. 35 -->`, `<!-- s. 36 -->`) | jest |

Znaczniki stron w `poradnik-zus-2026.md` mają postać `<!-- s. N -->` i oznaczają początek strony N wg numeracji drukowanej w Poradniku.

## 5. Poza materiałami organizatora

Przepisy, których instrukcja potrzebuje lub które przywołuje, a których nie ma w wyciągach przepisów. Wiersz o formularzach zostaje tu dla porządku: te informacje są w treści zadania ZUS, a nie w żadnym akcie z wyciągów.

| Przepis / informacja | Gdzie w instrukcji | Wynik | Plik |
|---|---|---|---|
| kwota świadczenia rodzicielskiego (art. 31 ust. 3a odsyła do ustawy o świadczeniach rodzinnych) | 8.1 (kwota_swiadczenia_rodzicielskiego, [DO WERYFIKACJI]) | POBRANE z oficjalnego API Sejmu: art. 17c ust. 5 ustawy o świadczeniach rodzinnych, "1000,00 zł miesięcznie" (t.j. Dz.U. 2026 poz. 1240, stan na 5.08.2026, status w API: obowiązujący). Do potwierdzenia przez człowieka. | `pobrane/ustawa-swiadczenia-rodzinne-art-17c.md` |
| najniższa podstawa wymiaru składek na ubezpieczenie chorobowe (art. 48a ust. 1 pkt 1 i ust. 5 odsyłają do art. 18 ust. 8 ustawy o systemie ubezpieczeń społecznych) | 8.7 pkt 2 (wzór z art. 48a, "najniższa podstawa") | POBRANE: art. 18 ustawy o systemie ubezpieczeń społecznych (ust. 8: "nieniższa jednak niż 60 % prognozowanego przeciętnego wynagrodzenia miesięcznego"). Uwaga: pobrano t.j. Dz.U. 2026 poz. 199 (stan na 6.02.2026); API oznacza to obwieszczenie jako "wygaśnięcie aktu" (zastąpione nowszym tekstem), a ustawa zasiłkowa powołuje zmiany z poz. 252, 426, 473, 507, 734. Brzmienie mogło się zmienić; do potwierdzenia przez człowieka. Konkretnej kwoty nie ma w żadnym materiale ani w pobranym tekście. | `pobrane/ustawa-o-systemie-ubezpieczen-spolecznych-art-18.md` |
| formularze ZAM, Z-3, Z-3a, ZAS-53, ZAS-12, ZUR, ZAO oraz data 24.10.2026 | 8.8 | JEST w materiałach, ale nie w wyciągach przepisów: treść zadania ZUS, s. 3 i 6 (Z-3, Z-3a, ZAS-53, ZAS-12, ZAM) oraz s. 11–12 z przypisami 3–5 (ZUR i ZAO połączone z ZAM od 24 października 2026 r., Z-3b zlikwidowany od tego dnia). Treść zadania nie mówi, że ZAM zastąpił Z-3, Z-3a albo ZAS-53. Poradnik wymienia tylko ZAM, Z-3b, Z-12, Z-15A, Z-15B, ZUS ZUA. Szczegóły: `ROZBIEZNOSCI.md`, A1. | nie dotyczy (treść zadania nie jest w `raw/`) |
| minimalna podstawa wymiaru (wartość liczbowa 4 147,10 zł) | 8.1 | BRAK jako liczba w materiałach; wynika rachunkowo z art. 45 ust. 1 i kwoty 4 806 zł z Załącznika (4 806 × 0,8629 = 4 147,0974, po zaokrągleniu 4 147,10). Tabela, która miała ją zawierać, jest pusta w DOCX. | nie dotyczy |
| zaokrąglanie kwot zasiłków | 8.1 | BRAK w ustawie i KP; patrz `ROZBIEZNOSCI.md`, sekcja B | nie dotyczy |

Nie pobierano aktów, które już są w materiałach.

## 6. Numery § z indeksem górnym w KP (odczyt z kolejności ustępów)

Surowy tekst spłaszcza indeksy: `§ 11` może oznaczać § 1¹, a nie § 11. Ustalono to z kolejności ustępów w każdym artykule (jeśli po `§ 1` następuje `§ 11`, a dopiero potem `§ 2`, to jest to § 1¹). Dla przepisów z instrukcji:

| Instrukcja | Surowy tekst | Uwaga |
|---|---|---|
| art. 183 § 1¹ (przysposobienie, 14 lat) | `§ 11.` w `## Art. 183` | pewne (kolejność § 1, § 11, § 2, § 3) |
| art. 183 § 3¹ (uzupełniający urlop przy przyjęciu dziecka) | `§ 31.` | pewne |
| art. 183 § 4 (rodzicielski, rodzina zastępcza: 41/43/38) | `§ 4.` | |
| art. 183 § 4¹ | `§ 41.` | |
| art. 183 § 4² (38 tygodni) | `§ 42.` | |
| art. 183 § 4³ (Za życiem: 65/67/62) | `§ 43.` | |
| art. 183 § 4⁴, 4⁵, 4⁶ | `§ 44.`, `§ 45.`, `§ 46.` | § 4⁵ też daje 62 tygodnie (przysposobienie, Za życiem); instrukcja przywołuje tylko 4³ |
| art. 182¹f § 1¹ (Za życiem 130/134) | `§ 11.` w `## Art. 182¹f` | pewne |
| art. 182³ § 1¹ (ojcowski, 2 części) | `§ 11.` w `## Art. 182³` | pewne |
| art. 177 § 1¹ (14/21/7 dni), § 3¹, § 4¹ | `§ 11.`, `§ 31.`, `§ 41.` | pewne |
| art. 180 § 10, § 11 | `§ 10.`, `§ 11.` | tu `§ 11` to rzeczywiście § 11 (po § 10), nie § 1¹ |

Pełna lista rozpoznanych przypadków jest na początku `kodeks-pracy.md` (uwaga redakcyjna). Heurystykę należy potwierdzić w PDF, zanim reguła zacznie na niej polegać.
