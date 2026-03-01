#!/usr/bin/env bash
# deploy.sh — sync code to server and run docker compose
set -euo pipefail

SERVER="adriano@192.168.15.106"
REMOTE_DIR="$HOME/apps/initial-aspire"

echo "==> Syncing project to $SERVER:$REMOTE_DIR ..."
rsync -avz --progress \
  --exclude='.git/' \
  --exclude='**/bin/' \
  --exclude='**/obj/' \
  --exclude='.claude/' \
  --exclude='InitialAspireProject.AppHost/.aspire/' \
  --exclude='InitialAspireProject.AppHost/infra/' \
  . "$SERVER:$REMOTE_DIR"

echo "==> Deploying on server ..."
ssh "$SERVER" bash << 'REMOTE'
set -euo pipefail

cd ~/apps/initial-aspire

if [ ! -f .env ]; then
  echo ""
  echo "ERROR: .env file not found on the server."
  echo "  1. Copy .env.example to .env"
  echo "  2. Fill in real values (passwords, JWT key, etc.)"
  echo "  3. Run ./deploy.sh again"
  echo ""
  exit 1
fi

echo "==> Building images (this may take a few minutes on first run) ..."
docker compose build --parallel

echo "==> Starting containers ..."
docker compose up -d --remove-orphans

echo ""
echo "Done! Services:"
docker compose ps
REMOTE

echo ""
echo "Web app: http://192.168.15.106:$(grep WEB_PORT .env 2>/dev/null | cut -d= -f2 || echo 8081)"
