param(
    [switch]$Help,
    [switch]$DryRun,
    [switch]$SkipMigrations,
    [switch]$RunServer
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$composePath = Join-Path $repoRoot "docker-compose.yml"
$envPath = Join-Path $repoRoot ".env"
$envExamplePath = Join-Path $repoRoot ".env.example"
$connectionString = "Host=localhost;Port=55432;Database=woong_monitor;Username=woong;Password=woong_dev_password"

function Write-Usage {
    Write-Host "Usage: powershell -ExecutionPolicy Bypass -File scripts\start-server-postgres.ps1 [-DryRun] [-SkipMigrations] [-RunServer]"
    Write-Host ""
    Write-Host "Starts local PostgreSQL through Docker Compose for Woong Monitor Stack."
    Write-Host "Default connection string:"
    Write-Host "ConnectionStrings__MonitorDb=$connectionString"
    Write-Host ""
    Write-Host "This is a local development database only. Do not use these credentials in production."
}

function Write-Step([string]$Message) {
    Write-Host "[server-postgres] $Message"
}

if ($Help) {
    Write-Usage
    exit 0
}

if (!(Test-Path $composePath)) {
    throw "Missing docker-compose.yml at $composePath"
}

if (!(Test-Path $envPath)) {
    if ($DryRun) {
        Write-Step "Dry run: would copy .env.example to .env"
    } else {
        Copy-Item -Path $envExamplePath -Destination $envPath
        Write-Step "Created .env from .env.example"
    }
}

$composeCommand = "docker compose --env-file `"$envPath`" -f `"$composePath`" up -d postgres"
$migrationCommand = "dotnet ef database update --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj --startup-project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj --context MonitorDbContext"
$runCommand = "dotnet run --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj"

if ($DryRun) {
    Write-Step "Dry run: $composeCommand"
    if (!$SkipMigrations) {
        Write-Step "Dry run: dotnet tool restore"
        Write-Step "Dry run: $migrationCommand"
    }
    Write-Step "Dry run: set ConnectionStrings__MonitorDb=$connectionString"
    if ($RunServer) {
        Write-Step "Dry run: $runCommand"
    }
    exit 0
}

Push-Location $repoRoot
try {
    Write-Step $composeCommand
    docker compose --env-file $envPath -f $composePath up -d postgres

    Write-Step "Waiting for woong-monitor-postgres health check..."
    $deadline = (Get-Date).AddSeconds(90)
    do {
        $health = docker inspect --format "{{.State.Health.Status}}" woong-monitor-postgres 2>$null
        if ($health -eq "healthy") {
            break
        }

        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    if ($health -ne "healthy") {
        throw "PostgreSQL container did not become healthy. Last health state: $health"
    }

    $env:ConnectionStrings__MonitorDb = $connectionString
    Write-Step "ConnectionStrings__MonitorDb=$connectionString"

    if (!$SkipMigrations) {
        Write-Step "dotnet tool restore"
        dotnet tool restore
        Write-Step "dotnet ef database update"
        dotnet ef database update `
            --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj `
            --startup-project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj `
            --context MonitorDbContext
    }

    if ($RunServer) {
        Write-Step $runCommand
        dotnet run --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj
    } else {
        Write-Step "PostgreSQL is ready. Run the server with:"
        Write-Host $runCommand
        Write-Step "Open the integrated dashboard at:"
        Write-Host "http://localhost:5000/dashboard?userId=user-1&from=2026-04-30&to=2026-04-30&timezoneId=UTC"
    }
}
finally {
    Pop-Location
}
