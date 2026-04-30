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
$trackingPipelineDbPath = Join-Path $runRoot "tracking-pipeline.db"
$snapshotRoot = Join-Path $runRoot "ui-snapshots"
$reportPath = Join-Path $runRoot "report.md"
$rootManifest = Join-Path $runRoot "manifest.json"
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
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for WPF app." }
    dotnet build $realStartProject
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for RealStart acceptance tool." }
    dotnet build $snapshotProject
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for UI snapshot tool." }

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
    if ($LASTEXITCODE -ne 0) { throw "RealStart acceptance failed." }

    $previousAcceptanceMode = $env:WOONG_MONITOR_ACCEPTANCE_MODE
    try {
        $env:WOONG_MONITOR_ACCEPTANCE_MODE = "TrackingPipeline"
        dotnet run --project $snapshotProject --no-build -- --app $AppPath --output-root $snapshotRoot --db $trackingPipelineDbPath --mode TrackingPipeline --viewport-widths "1920,1366,1024"
        if ($LASTEXITCODE -ne 0) { throw "TrackingPipeline UI snapshot acceptance failed." }
    }
    finally {
        $env:WOONG_MONITOR_ACCEPTANCE_MODE = $previousAcceptanceMode
    }

    $snapshotReport = Join-Path $snapshotRoot "latest/report.md"
    $snapshotManifest = Join-Path $snapshotRoot "latest/manifest.json"
    $visualReviewPrompt = Join-Path $snapshotRoot "latest/visual-review-prompt.md"
    $realStartReport = Join-Path $runRoot "real-start-report.md"
    $realStartManifest = Join-Path $runRoot "real-start-manifest.json"
    $lines = @(
        "# WPF UI Acceptance Report",
        "",
        "Status: PASS",
        "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
        "App: ``$AppPath``",
        "RealStart temp DB: ``$dbPath``",
        "TrackingPipeline temp DB: ``$trackingPipelineDbPath``",
        "",
        "## Semantic Checks",
        "",
        "- RealStart acceptance launched the WPF app.",
        "- StartTrackingButton and StopTrackingButton were invoked through FlaUI.",
        "- At least one focus_session row was persisted to temp SQLite.",
        "- At least one sync_outbox row was queued.",
        "- Server sync stayed disabled unless explicitly allowed.",
        "",
        "## RealStart Evidence Artifacts",
        "",
        "- Latest RealStart report: ``$realStartReport``",
        "- Latest RealStart manifest: ``$realStartManifest``",
        "- RealStart manifest evidence arrays: ``realStartEvidence`` and ``realStartSafetyEvidence``",
        "",
        "## Snapshot Evidence",
        "",
        "- Snapshot output root: ``$snapshotRoot``",
        "- Latest snapshot report: ``$snapshotReport``",
        "- Latest snapshot manifest: ``$snapshotManifest``",
        "- Latest visual review prompt: ``$visualReviewPrompt``",
        "",
        "## Privacy Boundary",
        "",
        "- No keystrokes are recorded.",
        "- No screen contents are captured as product telemetry.",
        "- Screenshots are local developer artifacts for this app UI only."
    )
    Set-Content -Path $reportPath -Value $lines

    $manifest = [ordered]@{
        status = "PASS"
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        appPath = $AppPath
        report = $reportPath
        realStart = [ordered]@{
            databasePath = $dbPath
            realStartReport = $realStartReport
            realStartManifest = $realStartManifest
            realStartEvidence = @("realStartEvidence", "realStartSafetyEvidence")
        }
        trackingPipeline = [ordered]@{
            databasePath = $trackingPipelineDbPath
            snapshotRoot = $snapshotRoot
            snapshotReport = $snapshotReport
            snapshotManifest = $snapshotManifest
            visualReviewPrompt = $visualReviewPrompt
        }
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $rootManifest

    if (Test-Path $latestRoot) {
        Remove-Item -Recurse -Force $latestRoot
    }
    Copy-Item -Recurse -Path $runRoot -Destination $latestRoot

    Write-Host "WPF UI acceptance artifacts: $runRoot"
    Write-Host "Latest report: $(Join-Path $latestRoot 'report.md')"
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($AppPath)) {
        Get-Process Woong.MonitorStack.Windows.App -ErrorAction SilentlyContinue |
            Where-Object { $_.Path -eq $AppPath } |
            Stop-Process -Force
    }
    Pop-Location
}
