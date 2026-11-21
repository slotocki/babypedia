#!/usr/bin/env bash
# Wdrożenie na serwer z Dockerem: wysyła archiwum wskazanej gałęzi przez SSH i uruchamia compose z profilem proxy (HTTPS).
# Użycie: ops/deploy.sh [host-ssh] [ref] [--reset-db]      domyślnie: babyradnik-azure main
# Sekrety zostają na serwerze w ~/babyradnik/.env (nie są wysyłane ani nadpisywane).
# --reset-db usuwa WYŁĄCZNIE wolumen bazy. Nigdy nie używaj `docker compose down -v`: kasuje też wolumen proxy
# z certyfikatami, a Let's Encrypt wydaje najwyżej 5 certyfikatów tygodniowo na ten sam adres.
set -euo pipefail

HOST="babyradnik-azure"
REF="main"
RESET_DB=0
POSITIONAL=()
for arg in "$@"; do
  if [ "$arg" = "--reset-db" ]; then RESET_DB=1; else POSITIONAL+=("$arg"); fi
done
[ "${#POSITIONAL[@]}" -ge 1 ] && HOST="${POSITIONAL[0]}"
[ "${#POSITIONAL[@]}" -ge 2 ] && REF="${POSITIONAL[1]}"
APP_DIR="babyradnik"

cd "$(git rev-parse --show-toplevel)"
SHA="$(git rev-parse --short "$REF")"

echo "Wdrażam $REF ($SHA) na $HOST"
ssh -o BatchMode=yes "$HOST" "mkdir -p ~/$APP_DIR && test -f ~/$APP_DIR/.env" \
  || { echo "Brak ~/$APP_DIR/.env na serwerze (wzór: .env.example; wymagane DOMAIN, POSTGRES_PASSWORD, ADMIN_PASSWORD)"; exit 1; }

git archive "$REF" | ssh -o BatchMode=yes "$HOST" "tar -x -C ~/$APP_DIR"

if [ "$RESET_DB" = "1" ]; then
  echo "Reset bazy (tylko wolumen pgdata)"
  ssh -o BatchMode=yes "$HOST" "cd ~/$APP_DIR && sudo docker compose stop api postgres >/dev/null 2>&1; sudo docker compose rm -f postgres >/dev/null 2>&1; sudo docker volume rm babyradnik_pgdata >/dev/null"
fi

ssh -o BatchMode=yes "$HOST" "cd ~/$APP_DIR && echo $SHA > .deployed-ref \
  && sudo docker compose --profile proxy up -d --build --remove-orphans 2>&1 | tail -n 6 \
  && sudo docker image prune -f >/dev/null"

for i in $(seq 1 40); do
  if [ "$(ssh -o BatchMode=yes "$HOST" "curl -s -m 5 -o /dev/null -w '%{http_code}' http://localhost:8080/health" 2>/dev/null)" = "200" ]; then
    echo "OK: $SHA działa (adresy: $(ssh -o BatchMode=yes "$HOST" "grep '^DOMAIN=' ~/$APP_DIR/.env | cut -d= -f2-"))"
    exit 0
  fi
  sleep 3
done
echo "BŁĄD: /health nie odpowiada 200"
ssh -o BatchMode=yes "$HOST" "cd ~/$APP_DIR && sudo docker compose ps && sudo docker compose logs --tail 30 api"
exit 1
