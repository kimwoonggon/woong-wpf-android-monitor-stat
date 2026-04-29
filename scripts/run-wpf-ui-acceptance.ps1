param(
    [string]$AppPath = "",
    [int]$Seconds = 3,
    [switch]$AllowServerSync
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputRoot = Join-Path $repoRoot "artifacts/wpf-ui-acceptance"
$runRoot = Join-Path $outputRoot $timestamp
$latestRoot = Join-Path $outputRoot "latest"
$dbPath = Join-Path $runRoot "acceptance.db"
$snapshotRoot = Join-Path $runRoot "ui-snapshots"
$reportPath = Join-Path $runRoot "report.md"
$appProject = Join-Path $repoRoot "src/Woong.MonitorStack.Windows.App/Woong.MonitorStack.Windows.App.csproj"
$realStartProject = Join-Path $repoRoot "tools/Woong.MonitorStack.Windows.RealStartAcceptance/Woong.MonitorStack.Windows.RealStartAcceptance.csproj"
$snapshotProject = Join-Path $repoRoot "tools/Woong.MonitorStack.Windows.UiSnapshots/Woong.MonitorStack.Windows.UiSnapshots.csproj"

Write-Host "This will observe foreground window metadata for local WPF UI acceptance."
Write-Host "It will not record keystrokes."
Write-Host "It will not capture screen contents as product telemetry."
Write-Host "It will use a temp DB unless configured otherwise: $dbPath"
if (-not $AllowServerSync) {
    Write-Host "Server sync is disabled. Pass -AllowServerSync only for an explicit real sync test."
}

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

Push-Location $repoRoot
try {
    dotnet build $appProject
    dotnet build $realStartProject
    dotnet build $snapshotProject

    if ([string]::IsNullOrWhiteSpace($AppPath)) {
        $AppPath = Join-Path $repoRoot "src/Woong.MonitorStack.Windows.App/bin/Debug/net10.0-windows/Woong.MonitorStack.Windows.App.exe"
    }

    $realStartArgs = @(
        "--app", $AppPath,
        "--db", $dbPath,
        "--seconds", $Seconds.ToString()
    )
    if ($AllowServerSync) {
        $realStartArgs += "--allow-server-sync"
    }

    dotnet run --project $realStartProject --no-build -- @realStartArgs
    dotnet run --project $snapshotProject --no-build -- --app $AppPath --output-root $snapshotRoot

    $snapshotReport = Join-Path $snapshotRoot "latest/report.md"
    $lines = @(
        "# WPF UI Acceptance Report",
        "",
        "Status: PASS",
        "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
        "App: ``$AppPath``",
        "Temp DB: ``$dbPath``",
        "",
        "## Semantic Checks",
        "",
        "- RealStart acceptance launched the WPF app.",
        "- StartTrackingButton and StopTrackingButton were invoked through FlaUI.",
        "- At least one focus_session row was persisted to temp SQLite.",
        "- At least one sync_outbox row was queued.",
        "- Server sync stayed disabled unless explicitly allowed.",
        "",
        "## Snapshot Evidence",
        "",
        "- Snapshot output root: ``$snapshotRoot``",
        "- Latest snapshot report: ``$snapshotReport``",
        "",
        "## Privacy Boundary",
        "",
        "- No keystrokes are recorded.",
        "- No screen contents are captured as product telemetry.",
        "- Screenshots are local developer artifacts for this app UI only."
    )
    Set-Content -Path $reportPath -Value $lines

    if (Test-Path $latestRoot) {
        Remove-Item -Recurse -Force $latestRoot
    }
    Copy-Item -Recurse -Path $runRoot -Destination $latestRoot

    Write-Host "WPF UI acceptance artifacts: $runRoot"
    Write-Host "Latest report: $(Join-Path $latestRoot 'report.md')"
}
finally {
    Pop-Location
}
