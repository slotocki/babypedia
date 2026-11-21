#!/usr/bin/env bash
# Wdrożenie na serwer z Dockerem: wysyła archiwum wskazanej gałęzi przez SSH i uruchamia compose z profilem proxy (HTTPS).
# Użycie: ops/deploy.sh [host-ssh] [ref]      domyślnie: babyradnik-azure main
# Sekrety zostają na serwerze w ~/babyradnik/.env (nie są wysyłane ani nadpisywane).
set -euo pipefail

HOST="${1:-babyradnik-azure}"
REF="${2:-main}"
APP_DIR="babyradnik"

cd "$(git rev-parse --show-toplevel)"
SHA="$(git rev-parse --short "$REF")"

echo "Wdrażam $REF ($SHA) na $HOST"
ssh -o BatchMode=yes "$HOST" "mkdir -p ~/$APP_DIR && test -f ~/$APP_DIR/.env" \
  || { echo "Brak ~/$APP_DIR/.env na serwerze (wzór: .env.example; wymagane DOMAIN, POSTGRES_PASSWORD, ADMIN_PASSWORD)"; exit 1; }

git archive "$REF" | ssh -o BatchMode=yes "$HOST" "tar -x -C ~/$APP_DIR"

ssh -o BatchMode=yes "$HOST" "cd ~/$APP_DIR && echo $SHA > .deployed-ref \
  && sudo docker compose --profile proxy up -d --build --remove-orphans 2>&1 | tail -n 12 \
  && sudo docker image prune -f >/dev/null"

DOMAIN="$(ssh -o BatchMode=yes "$HOST" "grep '^DOMAIN=' ~/$APP_DIR/.env | cut -d= -f2")"
for i in $(seq 1 30); do
  if [ "$(curl -s -m 5 -o /dev/null -w '%{http_code}' "https://$DOMAIN/health")" = "200" ]; then
    echo "OK: https://$DOMAIN ($SHA)"
    exit 0
  fi
  sleep 2
done
echo "BŁĄD: https://$DOMAIN/health nie odpowiada 200"
ssh -o BatchMode=yes "$HOST" "cd ~/$APP_DIR && sudo docker compose ps && sudo docker compose logs --tail 30 api"
exit 1
