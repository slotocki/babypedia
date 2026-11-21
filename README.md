# BabyRadnik

Przewodnik po zasiłkach z ubezpieczenia chorobowego dla przyszłych rodziców (HackNation 2026, zadanie ZUS).
Uprawnienia, okresy, kwoty i terminy liczy deterministyczny silnik reguł w C#. Wszystkie integracje są symulowane, dane osób fikcyjne.

## Uruchomienie (Docker)

```bash
cp .env.example .env                 # uzupełnij ADMIN_PASSWORD i POSTGRES_PASSWORD
docker compose up -d --build         # postgres + api + web (compose.yaml dołącza ops/docker-compose.yml)
curl http://localhost:8080/health    # aplikacja: http://localhost:8080, API bezpośrednio: http://localhost:5080
```

Profile opcjonalne: `docker compose --profile proxy up -d` (HTTPS przez Caddy, wymaga `DOMAIN`), `--profile assistant` (lokalny model, P2).
Zmiana encji bazy (do czasu migracji): `docker compose down -v`.

## Praca lokalna bez Dockera dla API/web

```bash
dotnet test --solution api/BabyRadnik.slnx       # build + testy (testy referencyjne czytają fixtures/reference)
dotnet run --project api/Api                     # API na http://localhost:5080 (baza: ConnectionStrings__Default)
cd web && npm ci && npm run dev                  # http://localhost:5173, proxy /api → :5080
npm ci --prefix contracts && npm --prefix contracts run validate   # walidacja fixtures schematami
```

## Struktura

```
contracts/   kontrakt: openapi.yaml, *.schema.json, examples/, tools/validate.mjs
fixtures/    parameters.seed.json, cases.json, personas/, reference/
api/         Domain (czysty C#), Infrastructure, Api, Tests
web/         React + TypeScript (Vite): src/app (router, klient API), src/ui, src/features
ops/         docker-compose.yml, proxy/
docs/        KONTRAKT.md (decyzje kontraktu, kto co edytuje), ZALEZNOSCI.md, prawo/
```

Kontrakt i zasady pracy równoległej: [docs/KONTRAKT.md](docs/KONTRAKT.md). Licencje: [docs/ZALEZNOSCI.md](docs/ZALEZNOSCI.md).
Dokumenty spraw ZUS nie trafiają do repozytorium — dane osób dla spraw ładowane są z katalogu `CASES_PRIVATE_DIR` poza repo.
