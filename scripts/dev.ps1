#!/usr/bin/env pwsh
# Sobe Postgres e roda build + testes + API em modo Development.
# Uso: .\scripts\dev.ps1 [-TestOnly] [-BuildOnly]
param(
    [switch]$TestOnly,
    [switch]$BuildOnly
)

Set-Location "$PSScriptRoot/.."

Write-Host "==> CRM.Clients dev" -ForegroundColor Cyan

if (-not $TestOnly -and -not $BuildOnly) {
    Write-Host "==> Subindo Postgres..." -ForegroundColor Yellow
    docker compose -f docker/docker-compose.yml up postgres -d
    Start-Sleep -Seconds 2
}

Write-Host "==> Build..." -ForegroundColor Yellow
dotnet build CRM.Clients.sln --no-restore -c Debug
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($BuildOnly) { exit 0 }

Write-Host "==> Testes..." -ForegroundColor Yellow
dotnet test --no-build -c Debug --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $TestOnly) {
    Write-Host "==> Iniciando API em http://localhost:5000 ..." -ForegroundColor Green
    dotnet run --project src/CRM.Clients.Api --no-build -c Debug
}
