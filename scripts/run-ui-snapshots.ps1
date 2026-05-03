param(
    [string]$AppPath = "",
    [string]$OutputRoot = "",
    [ValidateSet("EmptyData", "SampleDashboard", "TrackingPipeline")]
    [string]$Mode = "SampleDashboard"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot "src/Woong.MonitorStack.Windows.App/Woong.MonitorStack.Windows.App.csproj"
$toolProject = Join-Path $repoRoot "tools/Woong.MonitorStack.Windows.UiSnapshots/Woong.MonitorStack.Windows.UiSnapshots.csproj"

Push-Location $repoRoot
try {
    Write-Host "Cleanup uses the in-app Exit app button when available."
    Write-Host "X close is close-to-tray behavior and is not treated as app exit."
    Write-Host "If a leftover WPF process must be killed, cleanupEvidence in the report/manifest says so."

    dotnet build $appProject
    dotnet build $toolProject

    $toolArgs = @()
    if (-not [string]::IsNullOrWhiteSpace($AppPath)) {
        $toolArgs += "--app"
        $toolArgs += $AppPath
    }
    if (-not [string]::IsNullOrWhiteSpace($OutputRoot)) {
        $toolArgs += "--output-root"
        $toolArgs += $OutputRoot
    }
    $toolArgs += "--viewport-widths"
    $toolArgs += "1920,1366,1024"
    $toolArgs += "--mode"
    $toolArgs += $Mode

    dotnet run --project $toolProject --no-build -- @toolArgs

    $snapshotRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts/ui-snapshots"
    } else {
        $OutputRoot
    }
    Write-Host "UI snapshot artifacts: $snapshotRoot"
    Write-Host "Latest report: $(Join-Path $snapshotRoot 'latest/report.md')"
}
finally {
    Pop-Location
}
