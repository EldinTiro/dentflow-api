#!/bin/bash
# ============================================================
# DentFlow — Hetzner VPS Initial Setup
# Pokreni jednom na svježem Ubuntu 24.04 serveru kao root.
# Upotreba: bash setup.sh
# ============================================================
set -euo pipefail

APP_DIR="/app"
COMPOSE_FILE="$APP_DIR/docker-compose.prod.yml"
CADDYFILE="$APP_DIR/Caddyfile"

echo "==> Ažuriram sistem..."
apt-get update -qq && apt-get upgrade -y -qq

echo "==> Instaliram Docker..."
curl -fsSL https://get.docker.com | sh
systemctl enable --now docker

echo "==> Konfiguriram firewall (UFW)..."
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP (Cloudflare → VPS)
ufw allow 443/tcp   # HTTPS (rezerva)
ufw --force enable
echo "Firewall status:"
ufw status

echo "==> Kreiram /app direktorij..."
mkdir -p "$APP_DIR"

echo ""
echo "============================================================"
echo "  Setup završen. Sljedeći koraci (ručno):"
echo "============================================================"
echo ""
echo "1. Kopiraj infrastructure fajlove na server:"
echo "   scp dentflow-api/infrastructure/docker-compose.prod.yml root@VPS_IP:/app/"
echo "   scp dentflow-api/infrastructure/Caddyfile root@VPS_IP:/app/"
echo "   scp dentflow-api/infrastructure/.env.prod.example root@VPS_IP:/app/.env"
echo ""
echo "2. Na serveru — uredi .env sa pravim vrijednostima:"
echo "   ssh root@VPS_IP"
echo "   nano /app/.env"
echo ""
echo "3. Pokretanje (prvi put):"
echo "   cd /app"
echo "   docker compose -f docker-compose.prod.yml --env-file .env up -d"
echo ""
echo "4. Provjeri logove:"
echo "   docker compose -f docker-compose.prod.yml logs -f api"
echo ""
echo "5. GitHub Secrets koje trebaš dodati u oba repo-a:"
echo "   VPS_HOST  = $(curl -s ifconfig.me 2>/dev/null || echo 'IP_SERVERA')"
echo "   VPS_USER  = root"
echo "   VPS_SSH_KEY = (sadržaj tvog private SSH ključa)"
echo ""
