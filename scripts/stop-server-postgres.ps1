param(
    [switch]$Help,
    [switch]$DryRun,
    [switch]$RemoveVolumes
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$composePath = Join-Path $repoRoot "docker-compose.yml"
$envPath = Join-Path $repoRoot ".env"
if (!(Test-Path $envPath)) {
    $envPath = Join-Path $repoRoot ".env.example"
}

function Write-Usage {
    Write-Host "Usage: powershell -ExecutionPolicy Bypass -File scripts\stop-server-postgres.ps1 [-DryRun] [-RemoveVolumes]"
    Write-Host ""
    Write-Host "Stops the local Docker PostgreSQL container. Use -RemoveVolumes only when you intentionally want to delete local dev DB data."
}

function Write-Step([string]$Message) {
    Write-Host "[server-postgres] $Message"
}

if ($Help) {
    Write-Usage
    exit 0
}

$arguments = @("compose", "--env-file", "`"$envPath`"", "-f", "`"$composePath`"", "down")
if ($RemoveVolumes) {
    $arguments += "--volumes"
}

$commandText = "docker " + ($arguments -join " ")

if ($DryRun) {
    Write-Step "Dry run: $commandText"
    exit 0
}

Push-Location $repoRoot
try {
    Write-Step $commandText
    if ($RemoveVolumes) {
        docker compose --env-file $envPath -f $composePath down --volumes
    } else {
        docker compose --env-file $envPath -f $composePath down
    }
}
finally {
    Pop-Location
}
