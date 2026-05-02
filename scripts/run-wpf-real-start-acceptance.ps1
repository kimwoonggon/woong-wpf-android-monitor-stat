param(
    [string]$AppPath = "",
    [string]$DatabasePath = "",
    [int]$Seconds = 3,
    [switch]$AllowServerSync,
    [switch]$VerifyDatabaseOnly
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path ([System.IO.Path]::GetTempPath()) "woong-monitor-stack-real-start\$timestamp"
if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    $dbPath = Join-Path $runRoot "real-start.db"
} else {
    $dbPath = [System.IO.Path]::GetFullPath($DatabasePath)
    $runRoot = Split-Path -Parent $dbPath
}
$appProject = Join-Path $repoRoot "src/Woong.MonitorStack.Windows.App/Woong.MonitorStack.Windows.App.csproj"
$toolProject = Join-Path $repoRoot "tools/Woong.MonitorStack.Windows.RealStartAcceptance/Woong.MonitorStack.Windows.RealStartAcceptance.csproj"

Write-Host "This will observe foreground window metadata for local testing."
Write-Host "It will not record keystrokes."
Write-Host "It will not capture screen contents."
Write-Host "It will use a temp DB unless configured otherwise: $dbPath"
if (-not $AllowServerSync) {
    Write-Host "Server sync is disabled. Pass -AllowServerSync only for an explicit real sync test."
}
if ($VerifyDatabaseOnly) {
    Write-Host "Database-only verification mode reads an existing temp DB and does not launch browsers or capture external app screenshots."
}

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

Push-Location $repoRoot
try {
    dotnet build $toolProject

    if ($VerifyDatabaseOnly) {
        $toolArgs = @(
            "--verify-db-only",
            "--db", $dbPath
        )
    } else {
        dotnet build $appProject

        if ([string]::IsNullOrWhiteSpace($AppPath)) {
            $AppPath = Join-Path $repoRoot "src/Woong.MonitorStack.Windows.App/bin/Debug/net10.0-windows/Woong.MonitorStack.Windows.App.exe"
        }

        $toolArgs = @(
            "--app", $AppPath,
            "--db", $dbPath,
            "--seconds", $Seconds.ToString()
        )
        if ($AllowServerSync) {
            $toolArgs += "--allow-server-sync"
        }
    }

    $env:WOONG_MONITOR_LOCAL_DB = $dbPath
    dotnet run --project $toolProject --no-build -- @toolArgs

    Write-Host "RealStart temp DB: $dbPath"
}
finally {
    Remove-Item Env:\WOONG_MONITOR_LOCAL_DB -ErrorAction SilentlyContinue
    Pop-Location
}
