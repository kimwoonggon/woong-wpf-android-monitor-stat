param(
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/wpf-check"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"

function Resolve-ArtifactEntry {
    param(
        [string]$Name,
        [string]$SourcePath,
        [string]$ReportRelativePath = "report.md",
        [string]$ManifestRelativePath = "manifest.json"
    )

    $exists = Test-Path $SourcePath
    $reportPath = Join-Path $SourcePath $ReportRelativePath
    $manifestPath = Join-Path $SourcePath $ManifestRelativePath

    [ordered]@{
        name = $Name
        sourcePath = $SourcePath
        reportPath = $reportPath
        manifestPath = $manifestPath
        exists = $exists
        reportExists = Test-Path $reportPath
        manifestExists = Test-Path $manifestPath
    }
}

function Get-PngReferences {
    param([string]$SourcePath)

    if (-not (Test-Path $SourcePath)) {
        return @()
    }

    @(Get-ChildItem -Path $SourcePath -Filter "*.png" -File -Recurse |
        Sort-Object FullName |
        ForEach-Object { $_.FullName })
}

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$wpfUiAcceptancePath = Join-Path $repoRoot "artifacts/wpf-ui-acceptance/latest"
$uiSnapshotsPath = Join-Path $repoRoot "artifacts/ui-snapshots/latest"
$chromeNativeAcceptancePath = Join-Path $repoRoot "artifacts/chrome-native-acceptance/latest"
$realStartSourcePath = $wpfUiAcceptancePath

$wpfUiAcceptance = Resolve-ArtifactEntry -Name "wpfUiAcceptance" -SourcePath $wpfUiAcceptancePath
$uiSnapshots = Resolve-ArtifactEntry -Name "uiSnapshots" -SourcePath $uiSnapshotsPath
$chromeNativeAcceptance = Resolve-ArtifactEntry -Name "chromeNativeAcceptance" -SourcePath $chromeNativeAcceptancePath
$realStart = Resolve-ArtifactEntry `
    -Name "realStart" `
    -SourcePath $realStartSourcePath `
    -ReportRelativePath "real-start-report.md" `
    -ManifestRelativePath "real-start-manifest.json"

$pngReferences = @(
    Get-PngReferences -SourcePath $wpfUiAcceptancePath
    Get-PngReferences -SourcePath $uiSnapshotsPath
)

$reportPath = Join-Path $runRoot "report.md"
$manifestPath = Join-Path $runRoot "manifest.json"
$allEntries = @($wpfUiAcceptance, $uiSnapshots, $chromeNativeAcceptance, $realStart)
$allRequiredFilesPresent = -not ($allEntries | Where-Object { -not $_.reportExists -or -not $_.manifestExists })
$status = if ($allRequiredFilesPresent) { "PASS" } else { "WARN" }

$reportLines = @(
    "# WPF Consolidated Check Package",
    "",
    "Status: $status",
    "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
    "",
    "## Artifact Pointers",
    "",
    "| Area | Source Path | Report | Manifest | Status |",
    "|:---|:---|:---|:---|:---|"
)

foreach ($entry in $allEntries) {
    $entryStatus = if ($entry.reportExists -and $entry.manifestExists) { "PASS" } else { "WARN" }
    $reportLines += "| $($entry.name) | ``$($entry.sourcePath)`` | ``$($entry.reportPath)`` | ``$($entry.manifestPath)`` | $entryStatus |"
}

$reportLines += @(
    "",
    "## PNG Artifact Policy",
    "",
    "- pngArtifactsAreReferencedOnly: true",
    "- doNotCommitPngArtifacts: true",
    "- This package writes report and manifest pointers only; screenshots remain in their owning ignored artifact folders.",
    "",
    "## Referenced PNG Artifacts",
    ""
)

if ($pngReferences.Count -eq 0) {
    $reportLines += "- None found."
}
else {
    foreach ($pngReference in $pngReferences) {
        $reportLines += "- ``$pngReference``"
    }
}

$reportLines | Set-Content -Path $reportPath

$manifest = [ordered]@{
    status = $status
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    reportPath = $reportPath
    manifestPath = $manifestPath
    pngArtifactsAreReferencedOnly = $true
    doNotCommitPngArtifacts = $true
    wpfUiAcceptance = $wpfUiAcceptance
    uiSnapshots = $uiSnapshots
    chromeNativeAcceptance = $chromeNativeAcceptance
    realStart = $realStart
    referencedPngArtifacts = $pngReferences
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $manifestPath

if (Test-Path $latestRoot) {
    Remove-Item -Recurse -Force $latestRoot
}

New-Item -ItemType Directory -Force -Path $latestRoot | Out-Null
Copy-Item -Path $reportPath -Destination (Join-Path $latestRoot "report.md")
Copy-Item -Path $manifestPath -Destination (Join-Path $latestRoot "manifest.json")

Write-Host "WPF check package: $runRoot"
Write-Host "Latest WPF check package: $latestRoot"
