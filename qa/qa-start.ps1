# QA Startup Script
# Run this on the QA machine to pull latest code and start the full stack.
# Usage: .\qa\qa-start.ps1

param(
    [string]$ApiRepo  = "$PSScriptRoot\..",
    [string]$WebRepo  = (Join-Path (Split-Path $PSScriptRoot -Parent) "..\dentflow-web" | Resolve-Path -ErrorAction SilentlyContinue),
    [string]$ComposeRoot = (Join-Path (Split-Path $PSScriptRoot -Parent) ".." | Resolve-Path -ErrorAction SilentlyContinue)
)

$ErrorActionPreference = "Stop"

function Write-Step($msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

# ── 1. Pull latest from both repos ───────────────────────────────────────────
Write-Step "Pulling latest — dentflow-api"
git -C $ApiRepo pull

if (Test-Path $WebRepo) {
    Write-Step "Pulling latest — dentflow-web"
    git -C $WebRepo pull
} else {
    Write-Host "dentflow-web not found at $WebRepo — skipping web pull" -ForegroundColor Yellow
}

# ── 2. Print commit info ──────────────────────────────────────────────────────
Write-Step "Current commits"
$apiCommit = git -C $ApiRepo rev-parse --short HEAD
Write-Host "  dentflow-api : $apiCommit"
if (Test-Path $WebRepo) {
    $webCommit = git -C $WebRepo rev-parse --short HEAD
    Write-Host "  dentflow-web : $webCommit"
}

# ── 3. Rebuild and start containers ──────────────────────────────────────────
Write-Step "Rebuilding and starting Docker containers"
Set-Location $ComposeRoot
docker compose down
docker compose up --build -d

# ── 4. Wait for API health check ──────────────────────────────────────────────
Write-Step "Waiting for API to become healthy..."
$maxAttempts = 20
$attempt = 0
do {
    Start-Sleep -Seconds 3
    $attempt++
    try {
        $result = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 3
        if ($result.status -eq "healthy") { break }
    } catch {}
    Write-Host "  Attempt $attempt/$maxAttempts..."
} while ($attempt -lt $maxAttempts)

if ($attempt -ge $maxAttempts) {
    Write-Host "`n[ERROR] API did not become healthy in time. Check: docker logs dentflow-api-1" -ForegroundColor Red
    exit 1
}

Write-Host "  API is healthy!" -ForegroundColor Green

# ── 5. Open browser ───────────────────────────────────────────────────────────
Write-Step "Opening browser at http://localhost:3000"
Start-Process "http://localhost:3000"

# ── 6. Print summary ──────────────────────────────────────────────────────────
Write-Host "`n========================================" -ForegroundColor Green
Write-Host " DentFlow QA Environment Ready" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host " Frontend : http://localhost:3000"
Write-Host " API      : http://localhost:5000"
Write-Host " API docs : http://localhost:5000/swagger"
Write-Host " Commit   : dentflow-api@$apiCommit"
if (Test-Path $WebRepo) { Write-Host "           dentflow-web@$webCommit" }
Write-Host ""
Write-Host " Login    : superadmin@DentFlow.local"
Write-Host " Password : SuperAdmin123!@#"
Write-Host ""
Write-Host " Run test scenarios: qa\test-scenarios.md"
Write-Host "========================================`n" -ForegroundColor Green
