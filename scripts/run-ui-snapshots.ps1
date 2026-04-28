param(
    [string]$AppPath = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot "src/Woong.MonitorStack.Windows.App/Woong.MonitorStack.Windows.App.csproj"
$toolProject = Join-Path $repoRoot "tools/Woong.MonitorStack.Windows.UiSnapshots/Woong.MonitorStack.Windows.UiSnapshots.csproj"

Push-Location $repoRoot
try {
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
