param(
    [string]$OutputRoot = "",
    [string]$AdbPath = "",
    [string]$GradleWrapperPath = "",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/android-ui-snapshots"
}
if ([string]::IsNullOrWhiteSpace($AdbPath)) {
    $sdkAdb = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
    $AdbPath = if (Test-Path $sdkAdb) { $sdkAdb } else { "adb" }
}
if ([string]::IsNullOrWhiteSpace($GradleWrapperPath)) {
    $GradleWrapperPath = Join-Path $repoRoot "android\gradlew.bat"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$expectedScreens = @("dashboard", "settings", "sessions", "daily summary")
$status = "PASS"
$blockedReason = ""
$screenshots = @()
$notes = New-Object System.Collections.Generic.List[string]

function Write-AndroidSnapshotArtifacts {
    param(
        [string]$Status,
        [string]$BlockedReason
    )

    $reportLines = @(
        "# Android UI Snapshot Report",
        "",
        "Status: $Status",
        "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
        "Output: ``$runRoot``",
        "",
        "## Scope",
        "",
        "- dashboard",
        "- settings",
        "- sessions",
        "- daily summary",
        "",
        "## Result",
        ""
    )
    if (-not [string]::IsNullOrWhiteSpace($BlockedReason)) {
        $reportLines += "- BLOCKED: $BlockedReason"
    } else {
        $reportLines += "- PASS: Android UI screenshot flow completed."
    }
    $reportLines += @(
        "",
        "## Privacy Boundary",
        "",
        "- Uses the app UI and Android framework tooling only.",
        "- Does not record text input, touch coordinates from other apps, messages, passwords, or screen contents as product telemetry.",
        "- Screenshots are local developer artifacts only.",
        "",
        "## Notes"
    )
    foreach ($note in $notes) {
        $reportLines += "- $note"
    }
    Set-Content -Path (Join-Path $runRoot "report.md") -Value $reportLines

    $manifest = [ordered]@{
        status = $Status
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        output = $runRoot
        adbPath = $AdbPath
        gradleWrapperPath = $GradleWrapperPath
        expectedScreens = $expectedScreens
        screenshots = $screenshots
        blockedReason = $BlockedReason
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

    $prompt = @(
        "# Android UI Visual Review Prompt",
        "",
        "Review the Android dashboard/settings/sessions/daily summary screenshots in this folder when they exist.",
        "Check that usage totals, permission guidance, sync state, and privacy-safe copy are readable.",
        "If this run is BLOCKED, read `report.md` and fix the device/emulator availability first."
    )
    Set-Content -Path (Join-Path $runRoot "visual-review-prompt.md") -Value $prompt

    if (Test-Path $latestRoot) {
        Remove-Item -Recurse -Force $latestRoot
    }
    Copy-Item -Recurse -Path $runRoot -Destination $latestRoot
}

try {
    if (-not $SkipBuild) {
        if (-not (Test-Path $GradleWrapperPath)) {
            throw "Gradle wrapper not found: $GradleWrapperPath"
        }

        $androidRoot = Split-Path -Parent $GradleWrapperPath
        Push-Location $androidRoot
        try {
            & $GradleWrapperPath assembleDebug --no-daemon --stacktrace
            if ($LASTEXITCODE -ne 0) { throw "assembleDebug failed." }
            & $GradleWrapperPath assembleDebugAndroidTest --no-daemon --stacktrace
            if ($LASTEXITCODE -ne 0) { throw "assembleDebugAndroidTest failed." }
        }
        finally {
            Pop-Location
        }
    } else {
        $notes.Add("Build skipped by -SkipBuild.")
    }

    $adbOutput = & $AdbPath devices -l
    $deviceLines = @($adbOutput | Where-Object {
        $_ -match "\bdevice\b" -and $_ -notmatch "^List of devices attached"
    })

    if ($deviceLines.Count -eq 0) {
        $status = "BLOCKED"
        $blockedReason = "No connected Android device or running emulator was reported by adb devices -l."
        $notes.Add("No connected Android device. Start an emulator such as Medium_Phone or connect a physical device, then rerun this script.")
        Write-AndroidSnapshotArtifacts -Status $status -BlockedReason $blockedReason
        Write-Host "No connected Android device. Android UI snapshots are blocked."
        Write-Host "Android UI snapshot artifacts: $runRoot"
        Write-Host "Latest report: $(Join-Path $latestRoot 'report.md')"
        exit 0
    }

    $status = "BLOCKED"
    $blockedReason = "Connected-device screenshot capture is not implemented in this first local artifact slice."
    $notes.Add("Detected device(s): $($deviceLines -join '; ')")
    $notes.Add("Next slice should run instrumentation screenshot capture for dashboard, settings, sessions, and daily summary.")
    Write-AndroidSnapshotArtifacts -Status $status -BlockedReason $blockedReason
    Write-Host "Android device detected, but screenshot capture is deferred to the next slice."
    Write-Host "Android UI snapshot artifacts: $runRoot"
    exit 0
}
catch {
    $status = "FAIL"
    $blockedReason = $_.Exception.Message
    $notes.Add("Failure: $blockedReason")
    Write-AndroidSnapshotArtifacts -Status $status -BlockedReason $blockedReason
    Write-Error $blockedReason
    exit 1
}
