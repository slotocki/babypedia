# Pomocnik „Zapytaj o przepisy”

Pomocnik jest dostępny na ekranie **Wsparcie** i pod wynikiem. Domyślnie wyświetla pasujące fragmenty materiałów. Po włączeniu modelu może również udzielić krótkiej odpowiedzi z odsyłaczami do cytatów. Indywidualne uprawnienia, kwoty, okresy i terminy sprawdza kalkulator Kompas Rodzica.

## Działanie

- Przy starcie API budowany jest indeks BM25 w pamięci. Obejmuje wyciągi z ustawy zasiłkowej, Kodeksu pracy i dwóch rozporządzeń oraz załącznik i poradnik ZUS. Poradnik i załącznik są oznaczone w wynikach jako materiały pomocnicze. Wyszukiwarka sprowadza polskie znaki do podstawowej postaci i uwzględnia popularne odmiany i synonimy.
- `GET /api/law/search?q=…` zwraca wyniki wyszukiwania. Formularz używa `POST /api/assistant/ask`, więc pytanie nie pojawia się w adresie strony. Oba endpointy odrzucają długie pytania i ciągi przypominające PESEL, numer telefonu, rachunku lub e-mail. Wpisuj wyłącznie ogólne pytania.
- Do modelu idzie pytanie i wybrane fragmenty aktów. Claude dostaje dokumenty z włączonym cytowaniem. Lokalny model otrzymuje te same fragmenty i zwraca ustrukturyzowane cytaty. Aplikacja dopuszcza odpowiedź po sprawdzeniu odwołań do wysłanych fragmentów i odrzuca tekst z kwotami. Dokumenty spraw ZUS, dane profilu i fakty kalkulatora są poza tym przepływem.
- Pytanie o indywidualny wynik kieruje do kalkulatora. Gdy w materiałach brakuje wyniku, formularz podaje kontakt do ZUS. Przy niedostępności modelu, odrzuconej odpowiedzi lub przekroczeniu limitu wraca lista źródeł.

## Uruchomienie

Wyszukiwarka działa w standardowym uruchomieniu aplikacji. Obraz API zawiera wyciągi potrzebne do indeksu.

| Tryb | Ustawienia |
| --- | --- |
| Wyszukiwarka | Domyślne `ASSISTANT_ENABLED=false`. |
| Claude Haiku | `ASSISTANT_ENABLED=true`, `ASSISTANT_PROVIDER=anthropic`, `ANTHROPIC_API_KEY` w konfiguracji serwera. Domyślny model to `claude-haiku-5-5`. |
| Ollama lub zgodny endpoint | `ASSISTANT_ENABLED=true`, `ASSISTANT_PROVIDER=openai`, `ASSISTANT_BASE_URL` z końcówką `/v1` i `ASSISTANT_MODEL`. Model musi obsługiwać wyjście JSON. |

Claude jest wywoływany przez Messages API za pomocą istniejącego `HttpClient`. Limit wynosi domyślnie 250 zapytań do modelu na dobę UTC (`ASSISTANT_DAILY_LIMIT`) oraz 10 na minutę dla adresu widocznego przez API. Gdy proxy prezentuje wspólny adres, minutowy limit API jest wspólny dla ruchu z tego proxy. Aktualne ceny i format cytatów publikuje Anthropic. [Cennik](https://platform.claude.com/docs/en/about-claude/pricing) · [Cytowania](https://platform.claude.com/docs/en/build-with-claude/citations)

## Odbiór przed włączeniem modelu na demo

1. Sprawdź na ekranie Wsparcie i pod wynikiem, czy wyszukiwarka działa na telefonie i czy można rozwinąć cytat oraz cały fragment aktu.
2. Porównaj wyniki dla pytań o bliźnięta, urlop ojcowski, okres wyczekiwania, zlecenie, zaległe składki, pracę na urlopie rodzicielskim, wysokość zasiłku, martwe urodzenie, płatnika, dokumenty, rodzinę zastępczą i koniec umowy w ciąży z materiałami źródłowymi.
3. Sprawdź przekierowanie dla „Ile dostanę przy pensji 6000?”, brak wyników dla pytania o pogodę i odrzucenie pytania zawierającego przykładowy adres e-mail.
4. Włącz wybranego dostawcę na środowisku testowym. Porównaj odpowiedzi z wyświetlanymi cytatami. Sprawdź, czy awaria dostawcy i wyczerpanie limitu przywracają wyniki wyszukiwarki.
