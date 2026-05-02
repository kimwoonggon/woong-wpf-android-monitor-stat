param(
    [string]$ReportPath = "",
    [string]$ManifestPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $repoRoot "artifacts/android-ui-snapshots/latest/report.md"
}
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $repoRoot "artifacts/android-ui-snapshots/latest/manifest.json"
}

$canonicalScreens = @(
    "figma-01-splash.png",
    "figma-02-permission.png",
    "figma-03-dashboard.png",
    "figma-04-sessions.png",
    "figma-05-app-detail.png",
    "figma-06-report.png",
    "figma-07-settings.png"
)

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "Android UI snapshot report is missing: $ReportPath"
}

$report = Get-Content -Raw -Path $ReportPath
if ($report -notmatch "(?m)^Status:\s+PASS\s*$") {
    throw "Android UI snapshot report is not PASS: $ReportPath"
}

foreach ($screen in $canonicalScreens) {
    $escapedScreen = [regex]::Escape($screen)
    if ($report -notmatch "\|\s*Figma\s+.*?\|\s*PASS\s*\|\s*``?$escapedScreen``?\s*\|") {
        throw "Android UI snapshot report does not include PASS canonical screen: $screen"
    }
}

if (Test-Path -LiteralPath $ManifestPath) {
    $manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json
    if ($manifest.status -ne "PASS") {
        throw "Android UI snapshot manifest is not PASS: $ManifestPath"
    }
    foreach ($screen in $canonicalScreens) {
        $match = @($manifest.screenStatuses | Where-Object {
                $_.fileName -eq $screen -and $_.status -eq "PASS"
            })
        if ($match.Count -ne 1) {
            throw "Android UI snapshot manifest does not include exactly one PASS canonical screen: $screen"
        }
    }
}

Write-Host "Android UI snapshot report canonical 7-screen acceptance: PASS"
