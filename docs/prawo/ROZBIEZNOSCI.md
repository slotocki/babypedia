# ROZBIEZNOSCI: instrukcja i źródła wobec tekstu aktów

Ten plik tylko wypisuje. Niczego nie poprawia i niczego nie interpretuje.

**Zasada rozstrzygania (do stosowania przez autorów reguł):** akt prawny (ustawa, KP, rozporządzenia) > Załącznik ZUS > Poradnik ZUS. Jeśli niżej Poradnik lub Załącznik mówi coś innego niż akt, obowiązuje akt. Jeśli sprawa nie wynika z aktu, nie wolno jej "domyślać"; należy oznaczyć brak (`nie_rozstrzygam` lub `[DO WERYFIKACJI]`).

Skróty miejsc: ustawa = `ustawa-zasilkowa.md` (nagłówek `## Art. N`); KP = `kodeks-pracy.md`; Zał. = `zalacznik-zus.md`; Por. = `poradnik-zus-2026.md` (strona wg znacznika `<!-- s. N -->`, czyli numeru drukowanego); rozp. 2026 = `rozporzadzenie-2026-09-27-dokumenty-zasilki.md`; rozp. 2025 = `rozporzadzenie-2025-03-11-wnioski-pracownicze.md`. W KP numery § z indeksem górnym są w tekście spłaszczone (np. surowe `§ 11` = § 1¹), patrz `INDEKS.md` sekcja 6.

## A. Najważniejsze (od razu wpływa na reguły)

**A1. Formularze i daty z 8.8: treść zadania ZUS je wymienia, ale nie mówi, że ZAM zastąpił Z-3, Z-3a i ZAS-53; Poradnik mówi co innego.**
Instrukcja 8.8: "ZAM ... od 24.10.2026 zastąpił też ZUR i ZAO, Z-3, Z-3a, ZAS-53, Z-12 ... Z-3b formalnie zlikwidowano 24.10.2026".
Treść zadania ZUS (część materiałów organizatora; nie ma jej w `raw/`) wymienia te druki:
- s. 3: pracodawca nieuprawniony do wypłaty "przekazuje wniosek o wypłatę zasiłku chorobowego, w tym przypadku Zaświadczenie płatnika składek Z-3 lub ZAS-12 (na dalszy okres) do ZUS"; zleceniobiorczyni: "zaświadczenie płatnika składek Z-3a do ZUS"; prowadząca działalność: "wniosek o zasiłek chorobowy ZAS-53";
- s. 6: "przesyła Zaświadczenie płatnika składek Z-3 do ZUS", "wniosek o zasiłek macierzyński ZAM", "do ZUS płatnik składek przesyła Zaświadczenie płatnika składek Z-3a";
- s. 11–12, lista "Występujące w zestawach dokumenty": ZAM, ZUR, ZAO, "Z-3, Z-3a, Z3b", ZAS-12; przypisy 3–5: "Formularz ZUR od 24 października 2026 r. został połączony z formularzem ZAM i od tego dnia jest zlikwidowany", "Formularz ZAO od 24 października 2026 r. został połączony z formularzem ZAM i od tego dnia jest zlikwidowany", "Formularz Z-3b od 24 października 2026 r. zlikwidowany".

Treść zadania nie mówi, że ZAM zastąpił Z-3, Z-3a albo ZAS-53: połączenie z ZAM dotyczy tylko ZUR i ZAO, a Z-3, Z-3a i ZAS-53 opisuje jako obecne druki. Pod tym względem instrukcja 8.8 jest szersza niż źródło.
W pozostałych materiałach (grep po wszystkich plikach `raw/`) występują tylko ZAM, Z-3b, Z-12, Z-15A, Z-15B, ZUS ZUA.
Poradnik traktuje Z-3b jako obecny druk: s. 26 "Osoba prowadząca pozarolniczą działalność gospodarczą ubiegająca się o zasiłek chorobowy powinna złożyć do ZUS zaświadczenie płatnika składek na druku Z-3b"; s. 29 "wniosek na zaświadczeniu płatnika składek na druku Z-3b"; s. 35 "wniosek o zasiłek macierzyński na druku ZAM lub zaświadczenie płatnika składek na druku Z-3b". Poradnik ma stan prawny na 1.04.2026, a rozp. 2026 (27.09.2026) zastępuje rozporządzenie z 8.12.2015 (`## § 42`), więc druki w Poradniku mogą być nieaktualne. Rozp. 2026 nie nazywa żadnych formularzy; opisuje tylko zakres danych (`## § 3`, `## Załącznik 2.`, `## Załącznik 3.`, `## Załącznik 6.`).
Status: **potwierdzone treścią zadania ZUS** (nazwy Z-3, Z-3a, ZAS-53, ZAS-12, ZUR, ZAO; data 24.10.2026 dla połączenia ZUR i ZAO z ZAM oraz likwidacji Z-3b). **Niezgodne z instrukcją 8.8** co do Z-3, Z-3a i ZAS-53 (nie zostały zastąpione przez ZAM). Poradnik (stan na 1.04.2026) jest w tej sprawie nieaktualny.
Oznaczenie aktu: rozp. 27.09.2026 to Dz.U. z 2026 r. poz. 1316. Podaje to nagłówek samego rozporządzenia (`rozporzadzenie-2026-09-27-dokumenty-zasilki.md`, wiersz "z dnia 27 września 2026 r. (Dz.U. z 2026 r. poz. 1316)"; tak samo w `raw/`) i treść zadania ZUS (s. 2, "Podstawa prawna"). Spoza materiałów (API Sejmu, niezweryfikowane przez człowieka) pochodzi tylko data ogłoszenia 2026-10-09; z `## § 43` (wejście w życie po 14 dniach od ogłoszenia, § 39 po 12 miesiącach) daje to 24.10.2026, czyli ten sam dzień co w przypisach treści zadania.

**A2. Tabela "Minimalna podstawa wymiaru zasiłku w 2026 roku" w Załączniku jest pusta; liczby 4 147,10 zł nie ma w żadnym materiale.**
Zał. `### 2. Minimalna podstawa wymiaru zasiłku w 2026 roku`: "Wartości wyliczone od płacy minimalnej (4 806 zł brutto) po odjęciu składek społecznych finansowanych przez pracownika (13,71%):" i na tym koniec; w DOCX nie ma po tym zdaniu tabeli, obrazu ani obiektu (sprawdzono strukturę `word/document.xml`). Instrukcja 8.1: `podstawa_minimalna_pelny_etat 4 147,10 zł (4 806 × 0,8629)`.
Wzór jest w ustawie: art. 45 ust. 1 "nie może być niższa od kwoty minimalnego wynagrodzenia za pracę, po odliczeniu kwoty odpowiadającej 13,71% tego wynagrodzenia". 4 806 × 0,8629 = 4 147,0974, czyli 4 147,10 po zaokrągleniu. Status: wzór **zgodny**, wartość liczbowa **nie da się potwierdzić** (wyliczenie, nie cytat).

**A3. Poradnik odejmuje od podstawy wymiaru zaliczkę na PIT; ustawa i Załącznik tego nie robią.**
Por. s. 24 (pracownicy) i s. 25 (niebędący pracownikami): "Tak uzyskaną kwotę pomniejsza się o należne składki na ubezpieczenia społeczne (13,71%) oraz o zaliczkę na podatek dochodowy (PIT). Zasiłków z ubezpieczeń społecznych nie pomniejsza się o koszty uzyskania przychodu."
Ustawa art. 3 pkt 3: "wynagrodzenie - przychód pracownika stanowiący podstawę wymiaru składek na ubezpieczenie chorobowe, po odliczeniu potrąconych przez pracodawcę składek na ubezpieczenie emerytalne, rentowe oraz ubezpieczenie chorobowe"; art. 3 pkt 4 (przychód: odliczenie 13,71%); Zał. `## Podstawa wymiaru zasiłków`: "przeciętne miesięczne wynagrodzenie z 12 miesięcy ... pomniejszone o 13,71%". W żadnym akcie ani w Załączniku nie ma odliczenia zaliczki na podatek od podstawy. Rozstrzygnięcie wg zasady: akt i Załącznik (tylko 13,71%).

**A4. Poradnik porównuje kwotę zasiłku z świadczeniem rodzicielskim po odjęciu zaliczki na podatek; art. 31 ust. 3a tego nie mówi.**
Por. s. 19: "Jeśli miesięczna kwota zasiłku macierzyńskiego pomniejszonego o zaliczkę na podatek dochodowy od osób fizycznych ... jest niższa niż kwota świadczenia rodzicielskiego ..., kwotę zasiłku macierzyńskiego pomniejszonego o zaliczkę na podatek ... podwyższa się do wysokości świadczenia rodzicielskiego."
Ustawa art. 31 ust. 3a: "W przypadku gdy miesięczna kwota zasiłku macierzyńskiego, obliczonego zgodnie z ust. 1-3, jest niższa niż kwota świadczenia rodzicielskiego ... kwotę zasiłku macierzyńskiego podwyższa się do wysokości świadczenia rodzicielskiego." (bez zaliczki). Ust. 3c: za część miesiąca podwyższenie proporcjonalnie. Rozp. 2026 `## § 28` reguluje dokumenty do podwyższenia.

**A5. Kwota świadczenia rodzicielskiego: brak w materiałach; pobrana z oficjalnego źródła.**
Art. 31 ust. 3a odsyła do "świadczenia rodzicielskiego, określonego w ustawie z dnia 28 listopada 2003 r. o świadczeniach rodzinnych (Dz.U. z 2025 r. poz. 1208 oraz z 2026 r. poz. 203)". Żaden materiał nie podaje kwoty (grep: brak "1000 zł" i podobnych).
POBRANE (do potwierdzenia przez człowieka): art. 17c ust. 5 tej ustawy, t.j. Dz.U. 2026 poz. 1240 (stan na 5.08.2026, status w API: obowiązujący): "Świadczenie rodzicielskie przysługuje w wysokości 1000,00 zł miesięcznie, z uwzględnieniem ust. 6." Plik: `pobrane/ustawa-swiadczenia-rodzinne-art-17c.md`. Parametr w instrukcji pozostaje `[DO WERYFIKACJI]` aż człowiek potwierdzi.

**A6. Art. 7 ustawy i art. 182¹e KP są w wyciągu (wstępne przeszukanie było błędne).**
- Ustawa `## Art. 7 [Prawo do zasiłku po ustaniu tytułu ubezpieczenia]`: "Zasiłek chorobowy przysługuje również osobie, która stała się niezdolna do pracy po ustaniu tytułu ubezpieczenia chorobowego, jeżeli niezdolność do pracy trwała bez przerwy co najmniej 30 dni i powstała: 1) niepóźniej niż w ciągu 14 dni od ustania tytułu ubezpieczenia chorobowego; 2) niepóźniej niż w ciągu 3 miesięcy od ustania tytułu ubezpieczenia chorobowego - w razie choroby zakaźnej, której okres wylęgania jest dłuższy niż 14 dni, lub innej choroby, której objawy chorobowe ujawniają się po okresie dłuższym niż 14 dni od początku choroby." Jest też `## Art. 7a` (żołnierz zwolniony z czynnej służby wojskowej).
- KP `## Art. 182¹e [Łączenie urlopu z pracą, dopuszczalny wymiar]`: § 1 "Pracownik może łączyć korzystanie z urlopu rodzicielskiego z wykonywaniem pracy u pracodawcy udzielającego tego urlopu w wymiarze niewyższym niż połowa pełnego wymiaru czasu pracy"; § 2 wniosek "w terminie niekrótszym niż 21 dni przed rozpoczęciem wykonywania pracy", odmowa możliwa "ze względu na organizację pracy lub rodzaj pracy", odpowiedź "w terminie 7 dni od dnia otrzymania wniosku". W surowym tekście: `Art. 1821e`.
Prawdopodobny powód pomyłki: art. 7 zaczyna stronę surowego tekstu (przed nagłówkiem stoi znak nowej strony, a nie spacje), a art. 182¹e ma indeks górny spłaszczony do cyfry (`Art. 1821e`).
Uwaga do 8.4: art. 8 ust. 2 ogranicza zasiłek po ustaniu tytułu do 91 dni ("Nie dotyczy to niezdolności do pracy, o której mowa w art. 11 ust. 2 pkt 2, oraz spowodowanej gruźlicą lub występującej w trakcie ciąży"; dla ciąży obowiązuje wtedy ust. 1, 270 dni). Instrukcja 8.4 nie wymienia 91 dni.

**A7. Wejście w życie przepisów w wyciągu zależy od daty (dzisiaj 2026-11-21).**
Przypisy w ustawie: art. 9 ust. 4, art. 17 ust. 1d i 1e dodane ustawą z 18.12.2025 (Dz.U. 2026 poz. 26), "która wchodzi w życie 1.01.2027 r." Ich tekst jest w wyciągu (`## Art. 9` ust. 4: "Niezależnie od liczby tytułów ubezpieczenia chorobowego ustala się jeden okres zasiłkowy"; `## Art. 17` ust. 1c–1e: kilka tytułów ubezpieczenia). Art. 61b ust. 1, 2, 2a, 4, 5a, 5b w brzmieniu od 1.07.2026 (przypisy 6–11, Dz.U. 2026 poz. 441). Rozp. 2026 `## § 39` wchodzi w życie po 12 miesiącach od ogłoszenia, reszta po 14 dniach (`## § 43`). Instrukcja tych dat nie uwzględnia.

## B. Tabela parametrów 8.1 wobec tekstu aktów

Status: **zgodna** (wartość i podstawa potwierdzone w tekście), **niezgodna**, **nie da się potwierdzić** (z podaniem powodu).

| Klucz (8.1) | Status | Cytat i miejsce |
|---|---|---|
| macierzynski_tyg 20/31/33/35/37 | zgodna | KP `## Art. 180` § 1: "1) 20 tygodni ... 2) 31 tygodni ... 3) 33 tygodni ... 4) 35 tygodni ... 5) 37 tygodni - w przypadku urodzenia pięciorga i więcej dzieci przy jednym porodzie"; Zał. tabela "5+ dzieci | 37 tygodni" |
| macierzynski_przed_porodem_max_tyg 6 | zgodna | KP art. 180 § 2: "Przed przewidywaną datą porodu pracownica może wykorzystać niewięcej niż 6 tygodni urlopu macierzyńskiego" |
| matka_min_po_porodzie_tyg 14 | zgodna | KP art. 180 § 4: "po wykorzystaniu po porodzie co najmniej 14 tygodni urlopu macierzyńskiego"; ustawa `## Art. 29` ust. 3: "za okres co najmniej 14 tygodni po porodzie" |
| matka_min_po_porodzie_szczegolne_tyg 8 | zgodna | ustawa art. 29 ust. 5: "co najmniej 8 tygodni po porodzie"; ust. 6 (szpital), ust. 7 (zgon, porzucenie); KP art. 180 § 6 |
| rodzicielski_tyg 41/43 | zgodna | KP `## Art. 182¹a` § 1: "41 tygodni ... 43 tygodni"; Zał. tabela |
| rodzicielski_za_zyciem_tyg 65/67 | zgodna | KP art. 182¹a § 2: "65 tygodni ... 67 tygodni" |
| rodzicielski_nieprzenoszalne_tyg 9 | zgodna | KP art. 182¹a § 4: "wyłączne prawo do 9 tygodni urlopu rodzicielskiego ... Prawa tego nie można przenieść" |
| rodzicielski_max_czesci 5 | zgodna | KP `## Art. 182¹c`: "jednorazowo albo niewięcej niż w 5 częściach" |
| rodzicielski_do_konca_roku_wieku 6 | zgodna | KP art. 182¹c: "niepóźniej niż do zakończenia roku kalendarzowego, w którym dziecko kończy 6 rok życia" |
| praca_na_rodzicielskim_max_etat 0,5 | zgodna | KP `## Art. 182¹e` § 1: "niewyższym niż połowa pełnego wymiaru czasu pracy" |
| rodzicielski_z_praca_max_tyg 82/86; ZŻ 130/134 | zgodna | KP `## Art. 182¹f` § 1: "82 tygodni ... 86 tygodni"; § 1¹ (surowe `§ 11`): "130 tygodni ... 134 tygodni". Por. s. 32 podaje tylko 130/134 (bez 82/86): luka, nie sprzeczność |
| ojcowski_tyg 2; max 2 części, każda ≥ 1 tydzień | zgodna | KP `## Art. 182³` § 1: "w wymiarze do 2 tygodni" (czyli maksimum); § 1¹ (surowe `§ 11`): "nie więcej niż w 2 częściach, z których żadna nie może być krótsza niż tydzień"; Zał. tabela; Por. s. 33 ("dwóch 7-dniowych częściach") |
| ojcowski_do_miesiaca_zycia 12 | zgodna | KP art. 182³ § 1 pkt 1: "ukończenia przez dziecko 12 miesiąca życia"; pkt 2: przysposobienie "upływu 12 miesięcy od dnia uprawomocnienia się postanowienia ... niedłużej niż do ukończenia przez dziecko 14 roku życia" |
| przyjecie_min_tyg 9 | zgodna | KP `## Art. 183` § 3: "ma prawo do 9 tygodni urlopu na warunkach urlopu macierzyńskiego"; Zał. `## Minimalny okres`: "nie może trwać krócej niż 9 tygodni" |
| przyjecie_rodzicielski_przy_minimum_tyg 38; ZŻ 62 | zgodna | KP art. 183: § 4 pkt 3 "38 tygodni - w przypadku, o którym mowa w § 3"; surowe `§ 42` (= § 4²) "38 tygodni urlopu rodzicielskiego"; surowe `§ 43` (= § 4³) pkt 3 "62 tygodni"; surowe `§ 45` (= § 4⁵) też "62 tygodni". Por. s. 32: 62. Odczyt numerów § z indeksem: heurystyka z kolejności (patrz `INDEKS.md` sekcja 6) |
| wiek_przysposobienie_max 14 | zgodna | KP art. 183 § 1¹ (surowe `§ 11`): "niedłużej jednak niż do ukończenia przez dziecko 14 roku życia"; ustawa art. 29 ust. 1 pkt 2: "w wieku do 14 roku życia". Por. s. 30 pomija 14 (patrz D5) |
| wiek_rodzina_zastepcza_max 7 / 10 | zgodna | KP art. 183 § 1; ustawa art. 29 ust. 1 pkt 3: "do 7 roku życia, a w przypadku dziecka, wobec którego podjęto decyzję o odroczeniu obowiązku szkolnego - do 10 roku życia" |
| martwe_urodzenie_tyg_po_porodzie 8; ≥ 7 dni od zgonu | zgodna | KP `## Art. 180¹` § 1: "w wymiarze 8 tygodni po porodzie, niekrócej jednak niż przez okres 7 dni od dnia zgonu dziecka" |
| zgon_po_8_tyg_dni 7 | zgodna | KP art. 180¹ § 2: "przez okres 7 dni od dnia zgonu dziecka" |
| stawka macierzyński / uzupełniający / ojcowski 100% | zgodna | ustawa `## Art. 31` ust. 1: "wynosi 100% podstawy wymiaru zasiłku" |
| stawka_rodzicielski 70% | zgodna | art. 31 ust. 2 |
| stawka_usredniona 81,5% | zgodna | art. 31 ust. 3: "wynosi 81,5% podstawy wymiaru zasiłku, z zastrzeżeniem ust. 3f i 3g" |
| stawka_9_tyg_drugiego_rodzica 70% | zgodna | art. 31 ust. 3f: "w części przysługującej drugiemu rodzicowi dziecka, o której mowa w art. 1821a § 4 Kodeksu pracy, wynosi 70%" |
| termin_wniosku_usredniona_dni 21 | zgodna | ustawa `## Art. 30a` ust. 1: "niepóźniej niż 21 dni po porodzie"; ust. 2: po przyjęciu dziecka (wniosek "pisemny") |
| chorobowy_ciaza_stawka 100% | zgodna | ustawa `## Art. 11` ust. 2 pkt 1: "przypada w okresie ciąży"; KP art. 92 § 1 pkt 2 (wynagrodzenie 100%) |
| chorobowy_ciaza_max_dni 270 | zgodna | ustawa `## Art. 8` ust. 1: "występuje w trakcie ciąży - niedłużej niż przez 270 dni"; Zał. tabela |
| wyczekiwanie 30 / 90 | zgodna | ustawa `## Art. 4` ust. 1 pkt 1 i 2 |
| wyczekiwanie_max_przerwa_dni 30 | zgodna | art. 4 ust. 2: "przerwa między nimi nie przekroczyła 30 dni" (dodatkowo: urlop wychowawczy, urlop bezpłatny, czynna służba wojskowa) |
| wynagrodzenie_chorobowe_dni 33 / 14 | zgodna | Zał. tabela "33 dni ... 14 dni w roku kalendarzowym"; KP `## Art. 92` § 1 pkt 1 ("do 33 dni ... 50 rok życia ... do 14 dni", 80% wynagrodzenia; pkt 2: 100% przy chorobie w ciąży) |
| rehabilitacyjne_stawki 90% / 75% / 100% w ciąży | zgodna | ustawa `## Art. 19` ust. 1 |
| rehabilitacyjne_max_miesiecy 12 | zgodna | ustawa `## Art. 18` ust. 2: "niedłużej jednak niż przez 12 miesięcy" |
| odliczenie_skladek 13,71% | zgodna | ustawa art. 3 pkt 4; art. 45 ust. 1; Zał. (patrz też A3) |
| placa_minimalna 4 806,00 zł od 1.01.2026 | zgodna co do kwoty; data nie do potwierdzenia | Zał.: "płacy minimalnej (4 806 zł brutto)" w podtytule "w 2026 roku"; daty "1.01.2026" literalnie nie ma |
| podstawa_minimalna_pelny_etat 4 147,10 | nie da się potwierdzić | patrz A2 (wzór w art. 45 zgodny, liczby w materiałach nie ma) |
| max_podstawa_dobrowolnej_skladki 23 550,00 → 20 321,30 | zgodna (tylko Zał.) | Zał. `## Dla nie pracowników`: "maksymalna miesięczna podstawa wymiaru dobrowolnej składki chorobowej wynosi 23 550,00 zł (co po odjęciu 13,71% daje limit podstawy zasiłku 20 321,30 zł)". 23 550 × 0,8629 = 20 321,295. Ustawa art. 46 ma inny limit (100% przeciętnego wynagrodzenia, dotyczy zasiłku po ustaniu tytułu) i jego kwoty w materiałach nie ma |
| prog_zadluzenia 1% = 48,06 zł | zgodna co do 1%; kwota nie występuje | ustawa `## Art. 2a` ust. 1: "na kwotę przekraczającą 1 % minimalnego wynagrodzenia za pracę ... w dniu powstania prawa do świadczenia"; 48,06 = 1% z 4 806 (wyliczenie) |
| termin_splaty_zadluzenia_mies 6 | zgodna | art. 2a ust. 2: "nie zostanie uregulowane w ciągu 6 miesięcy od dnia powstania prawa do świadczenia" |
| prog_platnika_ubezpieczonych 20 | zgodna | ustawa `## Art. 61` ust. 1 pkt 1: "powyżej 20 ubezpieczonych"; pkt 2 lit. a: "nie więcej niż 20"; ust. 2: stan na 30 listopada poprzedniego roku |
| przedawnienie_mies 6 | zgodna z zastrzeżeniem | ustawa `## Art. 67` ust. 1: "po upływie 6 miesięcy od ostatniego dnia okresu, za który zasiłek przysługuje"; ust. 3: termin liczy się od ustania przeszkody; ust. 4: przy błędzie płatnika lub ZUS "przedawnia się po upływie 3 lat" (instrukcja tych wyjątków nie podaje; Por. s. 27 podaje) |
| zasilek_pogrzebowy 7 000 zł; Z-12; 12 mies. od pochówku | zgodna (tylko Por.) | Por. s. 35: "przysługuje zasiłek pogrzebowy w wysokości 7000 zł ... w ciągu 12 miesięcy od dnia pochówku"; s. 36: "druk Z-12". Przepisu o zasiłku pogrzebowym nie ma w wyciągu ustawy ani w innym akcie z materiałów |
| kwota_swiadczenia_rodzicielskiego | nie da się potwierdzić z materiałów | patrz A5 (pobrane: 1000,00 zł, do potwierdzenia) |
| zaokraglanie | nie da się potwierdzić | patrz C2 |
| kwoty brutto / netto | nie da się potwierdzić jednoznacznie | patrz C3 |
| akapit pod tabelą: "który dzień wybiera parametr" | częściowo | tylko dla zadłużenia jest wprost: art. 2a ust. 1 "w dniu powstania prawa do świadczenia". Dla wymiarów, stawek i płacy minimalnej przepisu o dniu rozstrzygającym w wyciągu nie ma |

Inne wartości z 8.9 (terminy), sprawdzone przy okazji: wniosek o macierzyński przed porodem 14 dni przed: tylko Por. s. 27 (KP art. 180 § 2 nie podaje terminu; podobny termin 14 dni jest w art. 177 § 1¹ pkt 1 i art. 180 § 9 dla części dla ojca); rezygnacja matki 7 dni (KP art. 180 § 8) zgodna; wniosek ojca 14 dni (art. 180 § 9) zgodna; uzupełniający 21 dni przed końcem macierzyńskiego (art. 180² § 4) zgodna; rodzicielski i praca 21 dni (art. 182¹d § 1, art. 182¹e § 2) zgodna; ojcowski 7 dni (art. 182³ § 2) zgodna; przyjęcie dziecka 7 dni i start do 21 dni (art. 183 § 6) zgodna; płatnik 30 dni (art. 64 ust. 1; ust. 3: dla osób z art. 2a "niepóźniej niż w terminie 60 dni od dnia spłaty całości zadłużenia", czego instrukcja nie wymienia).

## C. Pytania szczegółowe zadane w zadaniu

**C1. Art. 7 ustawy i art. 182¹e KP:** są w wyciągu, patrz A6.

**C2. Zaokrąglanie.** W ustawie, KP, rozporządzeniach i Załączniku nie ma reguły zaokrąglania stawki dziennej ani kwoty zasiłku. Jedyne reguły zaokrąglania w materiałach:
- KP art. 180² § 3: "Niepełny tydzień zaokrągla się w górę do pełnego tygodnia" (tygodnie uzupełniającego; Zał.: "niepełny tydzień zaokrągla w górę"; Por. s. 31).
- Por. (składki, nie zasiłki): "Kwoty składek na ubezpieczenia społeczne należy zaokrąglać do pełnych groszy w górę, jeżeli końcówka jest równa lub wyższa od 0,50 grosza lub w dół, jeżeli jest niższa" (s. 22; podobne zdania o zaokrąglaniu do pełnych groszy przy obliczaniu składek na s. 16–18).
- Ustawa art. 36 ust. 3: "Podstawę wymiaru zasiłku chorobowego za jeden dzień niezdolności do pracy stanowi jedna trzydziesta część wynagrodzenia stanowiącego podstawę wymiaru zasiłku" (bez zaokrąglenia); art. 11 ust. 5: "za miesiąc uważa się 30 dni".
Domyślne zaokrąglenie z instrukcji (0,01 zł) pozostaje `[DO WERYFIKACJI]`. Pobrany art. 17c ust. 7 ustawy o świadczeniach rodzinnych ("zaokrągla się do 10 groszy w górę") dotyczy świadczenia rodzicielskiego, nie zasiłku macierzyńskiego.

**C3. Brutto / netto.**
- Ustawa i Zał.: podstawa wymiaru to wynagrodzenie lub przychód po odliczeniu składek społecznych (13,71%); zasiłek = procent tej podstawy (art. 31). O zaliczce na podatek w ustawie (w wyciągu) nie ma mowy w przepisach o wysokości zasiłku.
- Por. s. 24 i 25: podstawa pomniejszona o 13,71% oraz zaliczkę PIT (patrz A3). Por. s. 19: zasiłek macierzyński "rozumiany według dotychczasowych zasad jako zasiłek w kwocie brutto, łącznie z zaliczką na podatek dochodowy od osób fizycznych".
- Zał.: "płacy minimalnej (4 806 zł brutto)".
Wniosek tylko wskazujący: materiały nie rozstrzygają wprost, czy kwota zasiłku "do wypłaty" jest przed czy po zaliczce; Por. s. 19 sugeruje, że zasiłek jest definiowany jako kwota brutto z zaliczką. Domyślny opis instrukcji ("brutto, przed zaliczką na podatek") jest z tym zgodny co do kierunku, ale to Poradnik, nie akt.

**C4. Kwota świadczenia rodzicielskiego (art. 31 ust. 3a):** brak w materiałach; patrz A5. W materiałach są tylko: ust. 3a–3c (zasada i dotacja), rozp. 2026 `## § 28` (dokumenty), Por. s. 19 (opis z zaliczką).

## D. Poradnik i Załącznik wobec aktów prawnych

W każdym przypadku obowiązuje akt (zasada u góry).

**D1. Druki (Z-3b, ZAM, Z-12, Z-15A/B).** Patrz A1.

**D2. PIT w podstawie.** Patrz A3, A4.

**D3. Uzupełniający urlop macierzyński: Poradnik wobec KP art. 180².**
- Por. s. 31: masa "urodzeniową poniżej 1000 g"; KP art. 180² § 1 pkt 1: "z masą urodzeniową niewiększą niż 1000 g" (czyli ≤ 1000 g; Zał. wiersz 2: "Masa urodzeniowa ≤ 1000 g" zgodny z KP).
- Por. s. 30: "jeśli dziecko przebywa w szpitalu co najmniej 5 dni po narodzinach". KP: warunek "co najmniej 2 kolejne dni, przy czym pierwszy z tych dni będzie przypadał w okresie od 5. do 28. dnia po porodzie" dotyczy tylko porodu po ukończeniu 37. tygodnia (pkt 3); w pkt 1–2 jest "tydzień za każdy tydzień pobytu" bez progu 5 dni.
- Por. s. 31: "do 8 tygodni – gdy dziecko urodziło się po 28. tygodniu ciąży i przebywa w szpitalu po porodzie"; KP pkt 2: "po ukończeniu 28. tygodnia ciąży i przed ukończeniem 37. tygodnia ciąży i z masą urodzeniową większą niż 1000 g".
- Zał. wiersze 1 i 4: "przed 28. tygodniem" / "po 28. tygodniu i przed 37. tygodniem" wobec KP "przed ukończeniem 28. tygodnia" / "po ukończeniu 28. tygodnia i przed ukończeniem 37. tygodnia". Zał. wiersz 3 powtarza wiersze 1–2 jako "i". KP rozstrzyga ("lub": przed 28. tyg. ciąży albo masa ≤ 1000 g).

**D4. Wariant 81,5%: "wniosek lub deklaracja".** Por. s. 34: "mama musi w ciągu 21 dni po porodzie złożyć wniosek o zasiłek macierzyński za urlop w pełnym wymiarze ... lub deklarację, że zamierza korzystać z zasiłku w takim wymiarze". Ustawa art. 30a ust. 1: "może złożyć pisemny wniosek". Deklaracji jako odrębnej czynności ustawa (w wyciągu) nie przewiduje.

**D5. Wiek dziecka przy przyjęciu.** Por. s. 30: "Prawo do tego zasiłku przysługuje zazwyczaj do ukończenia przez dziecko 7. roku życia (lub 10. roku życia w przypadku odroczenia obowiązku szkolnego)". Ustawa art. 29 ust. 1 pkt 2 i KP art. 183 § 1¹ mają dodatkowo ścieżkę przysposobienia do 14. roku życia (Zał. tabela ojcowski: "nie później niż do 14 roku życia"; Por. s. 32 ma 14 przy Za życiem).

**D6. Zbieg tytułów w zadłużeniu (art. 2a).** Por. s. 24 (i s. 36): "Dotyczy to osób, które prowadzą działalność gospodarczą lub współpracują z taką osobą albo z osobą fizyczną, która prowadzi działalność gospodarczą i korzysta z ulgi na start w zakresie obowiązkowych ubezpieczeń społecznych albo jest duchownym". Ustawa art. 2a ust. 1: osoby prowadzące pozarolniczą działalność i osoby z nimi współpracujące, osoby współpracujące z osobami fizycznymi z art. 18 ust. 1 Prawa przedsiębiorców, "duchownym będącym płatnikami składek na własne ubezpieczenia oraz marynarzom, o których mowa w art. 6 ust. 1 pkt 24" ustawy o systemie ubezpieczeń społecznych. Art. 2a nie używa nazwy "ulga na start", ale odsyła do osób fizycznych z art. 18 ust. 1 Prawa przedsiębiorców, czyli przepisu o uldze na start (tekstu Prawa przedsiębiorców nie ma w materiałach). Opis w Poradniku odpowiada więc art. 2a co do osób współpracujących z przedsiębiorcą korzystającym z tej ulgi; Poradnik pomija marynarzy. Por. s. 14–15 dodaje, że w okresie ulgi na start przedsiębiorca "nie podlega między innymi ubezpieczeniu chorobowemu".

**D7. Podstawa macierzyńskiego przed porodem.** Por. s. 28: "Jeśli w czasie ciąży ubezpieczona pobierała zasiłek chorobowy, to podstawa wymiaru zasiłku macierzyńskiego za okres przed porodem jest taka sama, jak tego zasiłku, który go poprzedzał". Instrukcja 8.7 tego nie wymienia (ustawa art. 43: brak przeliczenia przy przerwie krótszej niż miesiąc kalendarzowy). Por. s. 25 dodaje wyjątek do reguły braku przeliczenia: "Wyjątkiem jest sytuacja, gdy między poszczególnymi okresami pobierania zasiłków zmieniono wymiar czasu pracy"; w art. 43 takiego wyjątku nie ma (art. 40 dotyczy zmiany wymiaru czasu pracy w miesiącu niezdolności lub miesiącach z art. 36).

**D8. Zał. "Choroba w drugim miesiącu pracy".** Zał. tabela 1: "Wynagrodzenie za pełny miesiąc (po uzupełnieniu 1. miesiąca, jeśli był niepełny), pomniejszone o 13,71%". W ustawie art. 36 ust. 2 ("przeciętne miesięczne wynagrodzenie za pełne miesiące kalendarzowe ubezpieczenia") i art. 37 (pierwszy miesiąc) nie ma osobnej reguły dla drugiego miesiąca. Nie da się potwierdzić z aktu. Zał. wiersz "Składniki zmienne: ... pomnożona przez dni robocze w miesiącu" wobec art. 37 ust. 2 pkt 2: "pomnożenie przez liczbę dni, które ubezpieczony ... był obowiązany przepracować w tym miesiącu" (to nie zawsze to samo co dni robocze).

**D9. Zał. wiersz "Pomijane w podstawie: ... Dodatek stażowy (gdy przysługuje w czasie zawonienia)".** Literówka w Zał.; treść odpowiada art. 41 ust. 1.

**D10. Aktualność Poradnika.** Por. ma "Stan prawny na 1 kwietnia 2026 r."; KP w wyciągu to tekst z 1.09.2026 (Dz.U. 2026 poz. 1245), rozp. 2026 z 27.09.2026. Liczby z Poradnika zgodne z aktami w badanych miejscach (33/14, 270, 20/31/33/35/37, 41/43, 65/67, 81,5%, 21 dni, 8 tygodni, 7 000 zł), poza D1–D8.

## E. Dla autorów reguł prawnych (luki, nie sprzeczności)

**E1. Instrukcja 8.2, "Minimalna podstawa z płacy minimalnej: tak (proporcjonalnie do etatu)".** Art. 45 ust. 1 mówi o podstawie "z tytułu pracy w pełnym wymiarze czasu pracy"; Por. s. 25: "Dla pracownic zatrudnionych w pełnym wymiarze czasu pracy podstawa wymiaru nie może być niższa od kwoty minimalnego wynagrodzenia po odliczeniu kwoty odpowiadającej 13,71% tego wynagrodzenia". Proporcjonalnego przeliczania dla niepełnego etatu w materiałach nie ma. Nie da się potwierdzić.

**E2. Instrukcja 8.3 pkt 7 (zadłużenie).** Instrukcja pisze o "osobach prowadzących działalność i współpracujących"; art. 2a ust. 1 obejmuje też duchownych, marynarzy i osoby współpracujące z osobami fizycznymi z Prawa przedsiębiorców; dotyczy świadczeń z art. 2 pkt 1, 2, 5 i 6 (chorobowy, rehabilitacyjny, macierzyński, opiekuńczy). Instrukcja wymienia "chorobowy" i "ojcowski".

**E3. Instrukcja 8.7 pkt 2 (art. 48a).** Wzór w instrukcji upraszcza art. 48a ust. 1: suma (1) przeciętnej miesięcznej najniższej podstawy "po odliczeniach, o których mowa w art. 3 pkt 4" i (2) iloczynu jednej dwunastej przeciętnej zadeklarowanej kwoty w części przewyższającej najniższą podstawę (po odliczeniach) oraz liczby pełnych miesięcy. Dodatkowo ust. 2–5: miesiące z poprzedniego tytułu po przerwie do 30 dni (razem max 12), przerwa z powodu nieopłacenia składki, ust. 5 (najniższa podstawa z art. 18 ust. 8 ustawy o systemie ubezpieczeń społecznych). Kwota najniższej podstawy nie występuje w materiałach (pobrano art. 18: ust. 8 "nieniższa jednak niż 60 % prognozowanego przeciętnego wynagrodzenia miesięcznego ...", t.j. z 6.02.2026, status "wygaśnięcie aktu" w API; do potwierdzenia przez człowieka). Brak w materiałach wartości "prognozowanego przeciętnego wynagrodzenia" na 2026 r.

**E4. Instrukcja 8.7 pkt 1 ("przed miesiącem powstania prawa").** Art. 36 ust. 1: "12 miesięcy kalendarzowych poprzedzających miesiąc, w którym powstała niezdolność do pracy"; dla macierzyńskiego art. 47: przepisy art. 36–42 i 45 "stosuje się odpowiednio". Ustawa (w wyciągu) nie mówi wprost, że dla zasiłku macierzyńskiego miesiącem odniesienia jest miesiąc porodu; Zał.: "z 12 miesięcy poprzedzających chorobę" / "miesiąc choroby".

**E5. Instrukcja 8.4 (art. 6–7).** Art. 7 (30 dni niezdolności; początek w ciągu 14 dni lub 3 miesięcy po ustaniu tytułu) i art. 8 ust. 2 (91 dni po ustaniu tytułu) nie są w instrukcji rozwinięte; instrukcja każe "zakodować dokładnie" albo `nie_rozstrzygam`.

**E6. Instrukcja 12.2: szablon "urlop macierzyński i rodzicielski łącznie ('pełny wymiar')".** Rozp. 2025 `## § 1` wylicza wnioski: część urlopu macierzyńskiego (przed porodem: `## § 2`), rezygnacja (`## § 3`–`## § 4`), części dla ojca i członków rodziny (`## § 5`–`## § 13`), uzupełniający (`## § 14`), rodzicielski lub jego część (`## § 15`), łączenie z pracą (`## § 16`), ojcowski (`## § 17`), urlop na warunkach urlopu macierzyńskiego (`## § 18`), wychowawczy (`## § 19`–`## § 20`). Wniosku o "pełny wymiar" (macierzyński + rodzicielski razem) rozporządzenie nie przewiduje; ustawa art. 30a to "pisemny wniosek o wypłacenie zasiłku macierzyńskiego" (do płatnika zasiłku), bez określonej treści w wyciągu. Treść pól trzeba brać z `## § 2`, `## § 15` itd., nie z instrukcji.

**E7. Wyciąg ustawy a nowsze zmiany.** Rozp. 2026 w preambule powołuje ustawę zasiłkową jako "Dz.U. z 2026 r. poz. 854 i 1123"; wyciąg w materiałach ma tekst ujednolicony poz. 854. Dz.U. 2026 poz. 1123 (wg API Sejmu: ustawa z 31.07.2026 o zmianie ustawy o opiece nad dziećmi w wieku do lat 3 oraz niektórych innych ustaw; wejście w życie 2028-01-01) może zmieniać ustawę zasiłkową. Czy zmienia przepisy z sekcji 8, nie sprawdzono.

**E8. Inne akty, na które odsyłają przepisy z wyciągu, a których nie ma w materiałach:** ustawa o systemie ubezpieczeń społecznych (m.in. art. 18 ust. 8, art. 6 ust. 1 pkt 24), ustawa o świadczeniach rodzinnych (art. 17c, pobrane), ustawa o minimalnym wynagrodzeniu za pracę, ustawa "Za życiem" (art. 4 ust. 3: zaświadczenie). Dla warunków z sekcji 8 potrzebne są tylko dwa pierwsze (patrz `INDEKS.md` sekcja 5).
