# Pomocnik „Zapytaj o przepisy”

Pomocnik otwiera okno rozmowy na ekranie **Wsparcie** i pod wynikiem. Domyślnie wyświetla pasujące fragmenty materiałów. Po włączeniu modelu pokazuje wybrane przez niego cytaty po zweryfikowaniu ich w indeksie przepisów. Indywidualne uprawnienia, kwoty, okresy i terminy sprawdza kalkulator Kompas Rodzica.

## Działanie

- Przy starcie API budowany jest indeks BM25 w pamięci. Obejmuje wyciągi z ustawy zasiłkowej, Kodeksu pracy i dwóch rozporządzeń oraz załącznik i poradnik ZUS. Poradnik i załącznik są oznaczone w wynikach jako materiały pomocnicze. Wyszukiwarka sprowadza polskie znaki do podstawowej postaci i uwzględnia popularne odmiany i synonimy.
- Numery z indeksami górnymi, spłaszczone w wyciągach (`§ 11` = § 1¹, `art. 1821a` = art. 182¹a), pokazujemy z indeksem. Mapa pochodzi z nagłówków KP i kolejności paragrafów w artykule, a niejednoznaczne numery zostają bez zmian. Pytanie może wskazać artykuł („art. 180¹”, „art. 180^1”, „art. 1801”). Przy cytacie widać datę wejścia w życie lub nowego brzmienia ustępu odczytaną z przypisów aktu (np. art. 9 ust. 4 od 1.01.2027 r.). Uwagi redakcyjne nie trafiają do indeksu.
- Pytania o inne świadczenia (800+, becikowe) i polecenia zmiany zadania („zignoruj instrukcje”, „napisz wiersz”) dostają odpowiedź bez cytatów. Brak cytatów jest też wtedy, gdy żadne słowo pytania nie wskazuje tematu albo większości słów nie ma w materiałach.
- `GET /api/law/search?q=…` zwraca wyniki wyszukiwania. Formularz używa `POST /api/assistant/ask`, więc pytanie nie pojawia się w adresie strony. Oba endpointy odrzucają długie pytania i ciągi przypominające PESEL, numer telefonu, rachunku lub e-mail. Wpisuj wyłącznie ogólne pytania.
- Wyszukiwarka pokazuje 6 najlepszych fragmentów. Model dostaje do 15 fragmentów, w tym co najmniej 4 z poradnika lub załącznika ZUS (jeśli pasują), z oznaczeniem „akt prawny” albo „materiał pomocniczy ZUS”; przy rozbieżności rozstrzyga akt prawny. Pytania o „dokumenty” wyszukiwarka rozwija o słowa używane w przepisach („dokumentem niezbędnym”, „zaświadczenie”, „odpis”, „wniosek”).
- Model pisze do rodzica prostym językiem, na „Ty”: najpierw bezpośrednia odpowiedź, potem lista konkretów i warianty (np. pracownik / działalność). Odpowiedź ma do 1400 znaków i do 5 cytatów. Przy dopytaniu model może dostać jedno poprzednie ogólne pytanie. Aplikacja weryfikuje każdy cytat dosłownie z własnymi materiałami; wymyślony cytat unieważnia odpowiedź, a tekst z kwotą, linkiem lub danymi osobowymi zastępuje komunikat ogólny. Dokumenty spraw ZUS, dane profilu i fakty kalkulatora są poza tym przepływem.
- Pytanie o indywidualny wynik kieruje do kalkulatora. Gdy w materiałach brakuje wyniku, formularz podaje kontakt do ZUS. Przy niedostępności modelu, odrzuconej odpowiedzi lub przekroczeniu limitu wraca lista źródeł.

## Uruchomienie

Wyszukiwarka działa w standardowym uruchomieniu aplikacji. Obraz API zawiera wyciągi potrzebne do indeksu.

| Tryb | Ustawienia |
| --- | --- |
| Wyszukiwarka | Domyślne `ASSISTANT_ENABLED=false`. |
| DeepSeek przez OpenCode Go | `OPENCODE_ENABLED=true`, `OPENCODE_MODEL=deepseek-v4.1-flash`, `OPENCODE_BASE_URL=https://opencode.ai/inference/go/openai/v1`, `OPENCODE_API_KEY` w `~/babyradnik/.env`. Dostępność modelu zależy od ustawienia regionu Global w OpenCode. |
| Claude Haiku | `ASSISTANT_ENABLED=true`, `ASSISTANT_PROVIDER=anthropic`, `ANTHROPIC_API_KEY` w konfiguracji serwera. Domyślny model to `claude-haiku-5-5`. |
| OpenCode Console / Go | `ASSISTANT_ENABLED=true`, `ASSISTANT_PROVIDER=opencode` dla Console lub `opencode-go` dla Go, `OPENCODE_API_KEY` w `~/babyradnik/.env`. Domyślny model to `claude-haiku-5-5`. |
| Ollama lub zgodny endpoint | `ASSISTANT_ENABLED=true`, `ASSISTANT_PROVIDER=openai`, `ASSISTANT_BASE_URL` z końcówką `/v1` i `ASSISTANT_MODEL`. Model musi obsługiwać wyjście JSON. |

Tryb `OPENCODE_ENABLED` zastępuje wybór dostawcy z `ASSISTANT_PROVIDER`. Klucz wysyłany jest jako Bearer do oficjalnego adresu OpenCode. Claude używa Messages API, a DeepSeek używa Chat Completions z wyjściem JSON. Limit wynosi domyślnie 250 zapytań do modelu na dobę UTC (`ASSISTANT_DAILY_LIMIT`) oraz 10 na minutę dla adresu widocznego przez API. Gdy proxy prezentuje wspólny adres, minutowy limit API jest wspólny dla ruchu z tego proxy. [OpenCode Go](https://opencode.ai/docs/console/go/) · [Cytowania Claude](https://platform.claude.com/docs/en/build-with-claude/citations)

## Odbiór przed włączeniem modelu na demo

1. Sprawdź na ekranie Wsparcie i pod wynikiem, czy wyszukiwarka działa na telefonie i czy można rozwinąć cytat oraz cały fragment aktu.
2. Porównaj wyniki dla pytań o bliźnięta, urlop ojcowski, okres wyczekiwania, zlecenie, zaległe składki, pracę na urlopie rodzicielskim, wysokość zasiłku, martwe urodzenie, płatnika, dokumenty, rodzinę zastępczą i koniec umowy w ciąży z materiałami źródłowymi.
3. Sprawdź przekierowanie dla „Ile dostanę przy pensji 6000?”, brak wyników dla pytania o pogodę i odrzucenie pytania zawierającego przykładowy adres e-mail.
4. Włącz wybranego dostawcę na środowisku testowym. Porównaj odpowiedzi z wyświetlanymi cytatami. Sprawdź, czy awaria dostawcy i wyczerpanie limitu przywracają wyniki wyszukiwarki.
