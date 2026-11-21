# Kompas Rodzica

Tytuł do formularza: **Kompas Rodzica — przewodnik po zasiłkach z ubezpieczenia chorobowego dla przyszłych rodziców**

## Opis projektu (do wklejenia w formularz)

**Problem.** Zasiłki związane z rodzicielstwem zależą od tytułu ubezpieczenia, stażu, płatnika, liczby dzieci, daty zdarzenia i wariantu wypłaty. Rodzic zwykle nie wie, co mu przysługuje, ile i do kiedy złożyć wniosek, a o odmowie dowiaduje się często za późno na zmianę sytuacji.

**Rozwiązanie.** Kompas Rodzica to responsywna aplikacja WWW. Zadaje trzy pytania (co się dzieje, kiedy, dla kogo liczymy), pobiera dane z symulowanego mObywatela i ZUS, pokazuje je ze źródłem i pozwala poprawić. Potem podaje wynik: które świadczenia przysługują, za jaki okres i w jakiej wysokości, co trzeba zrobić i do kiedy, na co uważać. Odmowa zawsze ma powód, podstawę prawną i informację, co musiałoby się zmienić. Są też wnioski w PDF, zapytanie do ZUS i zaświadczenie o stanie składek (symulacje).

**Dla kogo.** Dla osób planujących dziecko, w ciąży, po porodzie i przyjmujących dziecko na wychowanie (przysposobienie, rodzina zastępcza), także dla drugiego rodzica.

**Co działa w demo.** Przebieg od trzech pytań do wyniku na fikcyjnych osobach; okresy, kwoty i terminy; siedem scenariuszy „co jeśli" (m.in. odejście z pracy, podział urlopu rodzicielskiego, praca na część etatu, wariant 81,5%); wnioski i zgłoszenia w symulacji; ekran „Sprawy ZUS" z rozpoznaniem pliku PDF po sumie SHA-256 (plik nie opuszcza przeglądarki); panel `/admin` z parametrami prawnymi i datą obowiązywania.

**Co nas odróżnia.**
- Wszystko, co dotyczy uprawnień, okresów, kwot i terminów, liczy deterministyczny silnik reguł w C#. Żaden model językowy nie uczestniczy w decyzji ani w kwocie.
- Każdy wynik ma ścieżkę decyzji: reguła, artykuł, warunek, spełniony lub nie, wyjaśnienie prostym językiem.
- Parametry prawne (wymiary urlopów, stawki, progi, płaca minimalna) są w bazie z datą obowiązywania. Zmiana w panelu zmienia wynik we właściwym okresie, bez wdrażania kodu.
- Gdy przepis nie rozstrzyga sytuacji albo jej nie rozpoznajemy, silnik odpowiada „nie rozstrzygam, skontaktuj się z ZUS" zamiast zgadywać.
- Reguły powstały z wyciągów przepisów i są sprawdzane testami referencyjnymi napisanymi przed kodem reguł.

**Potencjał wdrożeniowy.** Integracje (tożsamość, rejestr ubezpieczeń, konto składkowe, e-ZLA, wysyłka zgłoszeń) są ukryte za interfejsami, a w demo zastępują je implementacje symulowane nad jednym zbiorem fikcyjnych osób. Podmiana na prawdziwe usługi nie wymaga zmian w silniku. Panel parametrów pozwala odzwierciedlać zmiany przepisów bez programisty. Kod jest modularny, a zależności mają licencje permisywne.

**Wykorzystanie AI.** Aplikacja w działaniu nie używa AI do decyzji ani kwot. Kod, testy i dokumentację wytworzyły modele Claude (Anthropic) w narzędziu Claude Code, pod nadzorem ludzi, na podstawie wyciągów przepisów dostarczonych przez zespół.

**Ograniczenia.** Wszystkie integracje są symulowane, a dane osób fikcyjne. Kwoty są brutto. Kilka kwestii (zaokrąglanie, kwota świadczenia rodzicielskiego, minimalna podstawa przy niepełnym etacie) czeka na potwierdzenie z ZUS i jest opisanych w README.

Demo: {{DEMO_URL}} · Repozytorium: https://github.com/slotocki/babypedia · Zespół: 2Stosy (Sylwester Łotocki, Andrzej Laskowski)
