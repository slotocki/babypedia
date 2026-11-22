# Zależności i licencje

Zasada projektu (instrukcja, pkt 2.11): licencje permisywne — MIT, Apache-2.0, BSD i równoważne. Każda nowa zależność trafia na tę listę.
Świadomie **nie** używamy: FluentAssertions ≥ 8, MediatR, AutoMapper, QuestPDF (licencje komercyjne / niestandardowe).

## Backend (.NET 10, NuGet) — zależności bezpośrednie

| Paczka | Wersja | Licencja | Projekt |
|---|---|---|---|
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.0 | MIT | Domain (tylko rejestracja modułów) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | PostgreSQL License | Infrastructure |
| ↳ Npgsql | 10.0.3 | PostgreSQL License | Infrastructure (przechodnia) |
| ↳ Microsoft.EntityFrameworkCore (+ Relational, Abstractions) | 10.0.4 | MIT | Infrastructure (przechodnia) |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.4 | MIT | Infrastructure (baza w pamięci, gdy brak `ConnectionStrings__Default`) |
| Microsoft.AspNetCore.App (framework) | 10.0 | MIT | Api, Infrastructure |
| xunit.v3 | 4.0.2 | Apache-2.0 | Tests |
| xunit.runner.visualstudio | 4.0.0 | Apache-2.0 | Tests |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT | Tests |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.14 | MIT | Tests |

Przechodnie tylko w testach: Microsoft.Testing.Platform 2.5.1 (MIT), Microsoft.CodeCoverage 17.14.1 (MIT), Microsoft.ApplicationInsights 2.23.0 (MIT, telemetria platformy testowej), Newtonsoft.Json 13.0.3 (MIT).

**PostgreSQL License** — licencja permisywna zatwierdzona przez OSI, w treści równoważna MIT/BSD (bez ograniczeń przeniesienia praw). To jedyny dojrzały sterownik .NET dla PostgreSQL. Licencja spełnia zasadę licencji permisywnych.

## Frontend (npm, `web/`) — zależności bezpośrednie

| Paczka | Wersja | Licencja | Rodzaj |
|---|---|---|---|
| react, react-dom | 19.3.1 | MIT | runtime |
| react-router | 7.6.2 | MIT | runtime |
| @tanstack/react-query | 5.80.7 | MIT | runtime |
| react-hook-form | 7.57.0 | MIT | runtime |
| @hookform/resolvers | 5.1.1 | MIT | runtime |
| zod | 3.25.67 | MIT | runtime |
| radix-ui | 1.4.2 | MIT | runtime (prymitywy Radix; komponenty shadcn kopiujemy do `web/src/ui`, shadcn = MIT) |
| class-variance-authority | 0.7.1 | Apache-2.0 | runtime |
| clsx | 2.1.1 | MIT | runtime |
| tailwind-merge | 3.3.1 | MIT | runtime |
| pdfjs-dist | 5.3.31 | Apache-2.0 | runtime (podgląd PDF w przeglądarce) |
| vite | 6.4.6 | MIT | build |
| @vitejs/plugin-react | 4.7.0 | MIT | build |
| tailwindcss, @tailwindcss/vite | 4.1.10 | MIT | build |
| typescript | 5.8.3 | Apache-2.0 | build |
| eslint, @eslint/js | 9.39.5 | MIT | dev |
| typescript-eslint | 8.73.0 | MIT | dev |
| eslint-plugin-react-hooks | 5.2.0 | MIT | dev |
| eslint-plugin-react-refresh | 0.4.26 | MIT | dev |
| globals | 16.5.0 | MIT | dev |
| @types/react, @types/react-dom | 19.3.0 | MIT | dev |
| @types/node | 22.15.32 | MIT | dev |

Drzewo runtime (`npm ls --omit=dev`): 93 × MIT, 2 × Apache-2.0, 1 × 0BSD (`tslib`, przez Radix). Opcjonalne natywne `@napi-rs/canvas-*` (MIT; zależność pdfjs-dist tylko dla Node.js, w przeglądarce nieużywana, na Windows niezainstalowana).

Przechodnie narzędzia budowania/lintowania (nie trafiają do aplikacji): ISC (np. semver, minimatch, picocolors), BlueOak-1.0.0 (np. minipass, tar), BSD-2/3, CC-BY-4.0 (`caniuse-lite` — dane), **MPL-2.0 (`lightningcss`, kompilator CSS używany przez Tailwind 4 przy budowaniu)**, Python-2.0 (`argparse`). Te licencje dotyczą wyłącznie narzędzi uruchamianych przy budowaniu i nie trafiają do aplikacji. MPL-2.0 (`lightningcss`) obejmuje tylko pliki samej biblioteki i nie nakłada obowiązków na nasz kod.

## Narzędzia kontraktu (npm, `contracts/`) — tylko dev

| Paczka | Wersja | Licencja |
|---|---|---|
| ajv | 8.17.1 | MIT |
| ajv-formats | 3.0.1 | MIT |
| js-yaml | 4.1.0 | MIT (przechodnio `argparse`, Python-2.0, tylko CLI) |

## Obrazy kontenerów (`ops/docker-compose.yml`)

| Obraz | Licencja |
|---|---|
| mcr.microsoft.com/dotnet/sdk:10.0, aspnet:10.0 | MIT |
| node:22-alpine (tylko budowanie) | MIT |
| nginx:1.27-alpine | BSD-2-Clause |
| postgres:17-alpine | PostgreSQL License |
| caddy:2.10-alpine (profil `proxy`) | Apache-2.0 |
| ollama/ollama:0.9.6 (profil opcjonalny `assistant`, nieużywany na demo) | MIT (wagi modelu mają własne licencje) |

## Generowanie PDF (`api/Infrastructure/Submissions`)

| Paczka / zasób | Wersja | Licencja | Uwagi |
|---|---|---|---|
| PDFsharp-MigraDoc (+ PDFsharp, MigraDoc.*) | 6.2.4 | MIT | Nie podnosić do 6.5.0 / 7.x bez kontroli licencji — wg nuspec 6.5.0 to AGPL-3.0 z kluczem licencyjnym. |
| Microsoft.Extensions.Logging.Abstractions | 8.0.3 | MIT | przechodnia z PDFsharp |
| System.Security.Cryptography.Pkcs | 10.0.14 | MIT | przypięcie nad przechodnią 8.0.1 (NU1902/NU1903) |
| Font Roboto (Regular, Bold) | 2.138 | Apache-2.0 | osadzony w bibliotece; tekst licencji w `Submissions/Fonts/Roboto-LICENSE.txt` |
