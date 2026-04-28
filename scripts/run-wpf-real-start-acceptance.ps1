param(
    [string]$AppPath = "",
    [int]$Seconds = 3,
    [switch]$AllowServerSync
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path ([System.IO.Path]::GetTempPath()) "woong-monitor-stack-real-start\$timestamp"
$dbPath = Join-Path $runRoot "real-start.db"
$appProject = Join-Path $repoRoot "src/Woong.MonitorStack.Windows.App/Woong.MonitorStack.Windows.App.csproj"
$toolProject = Join-Path $repoRoot "tools/Woong.MonitorStack.Windows.RealStartAcceptance/Woong.MonitorStack.Windows.RealStartAcceptance.csproj"

Write-Host "This will observe foreground window metadata for local testing."
Write-Host "It will not record keystrokes."
Write-Host "It will not capture screen contents."
Write-Host "It will use a temp DB unless configured otherwise: $dbPath"
if (-not $AllowServerSync) {
    Write-Host "Server sync is disabled. Pass -AllowServerSync only for an explicit real sync test."
}

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

Push-Location $repoRoot
try {
    dotnet build $appProject
    dotnet build $toolProject

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

    $env:WOONG_MONITOR_LOCAL_DB = $dbPath
    dotnet run --project $toolProject --no-build -- @toolArgs

    Write-Host "RealStart temp DB: $dbPath"
}
finally {
    Remove-Item Env:\WOONG_MONITOR_LOCAL_DB -ErrorAction SilentlyContinue
    Pop-Location
}
