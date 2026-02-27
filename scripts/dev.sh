#!/usr/bin/env bash
# Sobe Postgres e roda build + testes + API em modo Development.
# Uso: ./scripts/dev.sh [--test-only | --build-only]
set -euo pipefail

cd "$(dirname "$0")/.."

echo "==> CRM.Clients dev"

MODE="${1:-}"

if [[ "$MODE" != "--test-only" && "$MODE" != "--build-only" ]]; then
  echo "==> Subindo Postgres..."
  docker compose -f docker/docker-compose.yml up postgres -d
  sleep 2
fi

echo "==> Build..."
dotnet build CRM.Clients.sln --no-restore -c Debug

[[ "$MODE" == "--build-only" ]] && exit 0

echo "==> Testes..."
dotnet test --no-build -c Debug --logger "console;verbosity=minimal"

if [[ "$MODE" != "--test-only" ]]; then
  echo "==> Iniciando API em http://localhost:5000 ..."
  dotnet run --project src/CRM.Clients.Api --no-build -c Debug
fi
