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

$requiredPixelScreens = @(
    "figma-01-splash.png",
    "figma-02-permission.png",
    "figma-03-dashboard.png",
    "figma-04-sessions.png",
    "figma-05-app-detail.png",
    "figma-06-report.png",
    "figma-07-settings.png",
    "02-dashboard-summary-location.png",
    "06-settings-location-permission.png",
    "09-main-shell.png",
    "10-main-shell-sessions.png",
    "11-main-shell-settings.png",
    "12-main-shell-report.png"
)

$hierarchyContracts = @(
    [ordered]@{
        fileName = "figma-01-splash.xml"
        name = "Splash branding"
        requiredIds = @(
            "splashRoot",
            "splashLogoContainer",
            "appTitleText",
            "appSubtitleText",
            "loadingIndicator"
        )
        requiredText = @(
            "Woong Monitor",
            "Android Focus Tracker"
        )
    },
    [ordered]@{
        fileName = "figma-02-permission.xml"
        name = "Permission guidance"
        requiredIds = @(
            "permissionScrollRoot",
            "permissionTitle",
            "permissionCollectedMetadataText",
            "permissionNotCollectedDataText",
            "openUsageAccessSettingsButton"
        )
        requiredText = @(
            "Usage Access",
            "app name",
            "package name",
            "keyboard input",
            "screen contents",
            "passwords",
            "touch coordinates"
        )
    },
    [ordered]@{
        fileName = "figma-03-dashboard.xml"
        name = "Dashboard current focus"
        requiredIds = @(
            "currentFocusCard",
            "currentFocusTitle",
            "currentForegroundLabel",
            "currentAppText",
            "currentPackageText",
            "latestCollectedExternalAppText",
            "lastCollectedText"
        )
        requiredText = @(
            "Current Focus",
            "Current foreground app"
        )
    },
    [ordered]@{
        fileName = "02-dashboard-summary-location.xml"
        name = "Dashboard location map"
        requiredIds = @(
            "locationContextCard",
            "locationStatusText",
            "locationMiniMapView",
            "locationMapProviderStatusText",
            "locationLatitudeText",
            "locationLongitudeText",
            "locationCapturedAtText"
        )
        requiredText = @(
            "Location context",
            "Latitude",
            "Longitude"
        )
    },
    [ordered]@{
        fileName = "figma-06-report.xml"
        name = "Report tab"
        requiredIds = @(
            "reportTitle",
            "reportSevenDayButton",
            "reportTrendChartCard",
            "reportTopAppsCard"
        )
        requiredText = @(
            "Report"
        )
    },
    [ordered]@{
        fileName = "figma-07-settings.xml"
        name = "Settings tab"
        requiredIds = @(
            "settingsTitle",
            "permissionsSettingsCard",
            "collectionSettingsCard",
            "syncSettingsCard"
        )
        requiredText = @(
            "Settings"
        )
    },
    [ordered]@{
        fileName = "06-settings-location-permission.xml"
        name = "Settings location permission"
        requiredIds = @(
            "locationSettingsCard",
            "locationContextDefaultText",
            "locationCoordinateBoundaryText",
            "preciseLocationOptInText",
            "locationContextCheckBox",
            "preciseLatitudeLongitudeCheckBox",
            "requestLocationPermissionButton"
        )
        requiredText = @(
            "Location context",
            "Location context is off by default",
            "latitude/longitude",
            "explicit opt-in"
        )
    }
)

$bottomNavHierarchyFiles = @(
    "figma-03-dashboard.xml",
    "figma-04-sessions.xml",
    "figma-06-report.xml",
    "figma-07-settings.xml",
    "09-main-shell.xml",
    "10-main-shell-sessions.xml",
    "11-main-shell-settings.xml",
    "12-main-shell-report.xml"
)

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "Android UI snapshot report is missing: $ReportPath"
}

$artifactRoot = Split-Path -Parent $ReportPath
if (Test-Path -LiteralPath $ManifestPath) {
    $artifactRoot = Split-Path -Parent $ManifestPath
}

function Get-ArtifactPath {
    param([string]$FileName)

    return Join-Path $artifactRoot $FileName
}

function Get-XmlDocument {
    param([string]$Path)

    $raw = Get-Content -Raw -LiteralPath $Path
    $endIndex = $raw.IndexOf("</hierarchy>", [System.StringComparison]::OrdinalIgnoreCase)
    if ($endIndex -lt 0) {
        throw "Android UI hierarchy does not contain a closing hierarchy element: $Path"
    }

    $xmlText = $raw.Substring(0, $endIndex + "</hierarchy>".Length)
    return [xml]$xmlText
}

function Test-HierarchyHasResourceId {
    param(
        [xml]$Document,
        [string]$Id
    )

    $resourceId = "com.woong.monitorstack:id/$Id"
    return @($Document.SelectNodes("//*[@resource-id='$resourceId']")).Count -gt 0
}

function Test-HierarchyTextContains {
    param(
        [xml]$Document,
        [string]$ExpectedText
    )

    $nodes = @($Document.SelectNodes("//*[@text]"))
    foreach ($node in $nodes) {
        $text = [string]$node.GetAttribute("text")
        if ($text.IndexOf($ExpectedText, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

function Assert-HierarchyContract {
    param($Contract)

    $path = Get-ArtifactPath -FileName $Contract.fileName
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Android UI hierarchy is missing for $($Contract.name): $($Contract.fileName)"
    }

    $document = Get-XmlDocument -Path $path
    foreach ($id in $Contract.requiredIds) {
        if (-not (Test-HierarchyHasResourceId -Document $document -Id $id)) {
            throw "Android UI hierarchy $($Contract.fileName) is missing required id '$id' for $($Contract.name)."
        }
    }

    foreach ($text in $Contract.requiredText) {
        if (-not (Test-HierarchyTextContains -Document $document -ExpectedText $text)) {
            throw "Android UI hierarchy $($Contract.fileName) is missing required text '$text' for $($Contract.name)."
        }
    }
}

function Assert-BottomNavigationTabs {
    param([string]$FileName)

    $path = Get-ArtifactPath -FileName $FileName
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Android bottom navigation UI hierarchy is missing: $FileName"
    }

    $document = Get-XmlDocument -Path $path
    if (-not (Test-HierarchyHasResourceId -Document $document -Id "bottomNavigation")) {
        throw "Android bottom navigation hierarchy $FileName is missing bottomNavigation."
    }

    foreach ($tab in @("Dashboard", "Sessions", "Report", "Settings")) {
        if (-not (Test-HierarchyTextContains -Document $document -ExpectedText $tab)) {
            throw "Android bottom navigation hierarchy $FileName is missing tab text '$tab'."
        }
    }
}

function Assert-MeaningfulScreenshotPixels {
    param([string]$FileName)

    $path = Get-ArtifactPath -FileName $FileName
    $file = Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
    if ($null -eq $file) {
        throw "Android UI snapshot screenshot is missing: $FileName"
    }
    if ($file.Length -le 0) {
        throw "Android UI snapshot screenshot is empty: $FileName"
    }

    try {
        Add-Type -AssemblyName System.Drawing -ErrorAction Stop
        $bitmap = [System.Drawing.Bitmap]::FromFile($path)
        try {
            if ($bitmap.Width -le 0 -or $bitmap.Height -le 0) {
                throw "Android UI snapshot screenshot has invalid dimensions: $FileName"
            }

            $sampleCount = 0
            $allDark = $true
            $allBright = $true
            $firstColor = $null
            $hasDifferentColor = $false
            $xStep = [Math]::Max(1, [int][Math]::Floor($bitmap.Width / 12))
            $yStep = [Math]::Max(1, [int][Math]::Floor($bitmap.Height / 12))

            for ($x = 0; $x -lt $bitmap.Width; $x += $xStep) {
                for ($y = 0; $y -lt $bitmap.Height; $y += $yStep) {
                    $pixel = $bitmap.GetPixel($x, $y)
                    $brightness = ($pixel.R + $pixel.G + $pixel.B) / 3
                    $sampleCount += 1
                    if ($brightness -gt 8) {
                        $allDark = $false
                    }
                    if ($brightness -lt 247) {
                        $allBright = $false
                    }

                    if ($null -eq $firstColor) {
                        $firstColor = $pixel
                    } elseif (
                        [Math]::Abs($pixel.R - $firstColor.R) -gt 4 -or
                        [Math]::Abs($pixel.G - $firstColor.G) -gt 4 -or
                        [Math]::Abs($pixel.B - $firstColor.B) -gt 4) {
                        $hasDifferentColor = $true
                    }
                }
            }

            if ($sampleCount -eq 0 -or $allDark -or $allBright -or -not $hasDifferentColor) {
                throw "Android UI snapshot screenshot does not contain meaningful varied pixels: $FileName"
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }
    catch {
        throw "Android UI snapshot screenshot is not a parseable nonblank PNG: $FileName. $($_.Exception.Message)"
    }
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

foreach ($screen in $requiredPixelScreens) {
    Assert-MeaningfulScreenshotPixels -FileName $screen
}

foreach ($contract in $hierarchyContracts) {
    Assert-HierarchyContract -Contract $contract
}

$bottomNavHierarchyPaths = @()
foreach ($fileName in $bottomNavHierarchyFiles) {
    Assert-BottomNavigationTabs -FileName $fileName
    $bottomNavHierarchyPaths += Get-ArtifactPath -FileName $fileName
}

& (Join-Path $PSScriptRoot "validate-android-bottom-nav-floor.ps1") `
    -HierarchyPath $bottomNavHierarchyPaths

Write-Host "Android UI snapshot report canonical 7-screen acceptance: PASS"
Write-Host "Android UI snapshot hierarchy and pixel acceptance: PASS"
