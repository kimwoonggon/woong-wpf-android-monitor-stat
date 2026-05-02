param(
    [string]$OutputRoot = "",
    [string]$AdbPath = "",
    [string]$GradleWrapperPath = "",
    [string]$DeviceSerial = "",
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
$androidRoot = Split-Path -Parent $GradleWrapperPath

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$expectedScreens = @("dashboard", "settings", "sessions", "daily summary")
$featureScreens = @(
    "Figma 7-screen parity: Splash",
    "Figma 7-screen parity: Permission",
    "Figma 7-screen parity: Dashboard",
    "Figma 7-screen parity: Sessions",
    "Figma 7-screen parity: App Detail",
    "Figma 7-screen parity: Report",
    "Figma 7-screen parity: Settings",
    "main shell",
    "main shell sessions",
    "dashboard overview",
    "dashboard summary and location",
    "dashboard charts",
    "dashboard recent sessions",
    "settings privacy and sync",
    "settings location permission",
    "sessions list",
    "dashboard 1h selected",
    "sessions 6h selected",
    "main shell settings",
    "main shell report",
    "report custom range",
    "permission onboarding",
    "app detail",
    "daily summary"
)
$expectedLocationChecks = @(
    "Dashboard location card: locationContextCard, locationStatusText, locationLatitudeText, locationLongitudeText, locationAccuracyText, locationCapturedAtText",
    "Settings location section: locationContextDefaultText, locationCoordinateBoundaryText, preciseLocationOptInText, locationContextCheckBox, preciseLatitudeLongitudeCheckBox, requestLocationPermissionButton"
)
$status = "PASS"
$blockedReason = ""
$screenshots = @()
$notes = New-Object System.Collections.Generic.List[string]
$beginnerReviewAliases = @(
    [ordered]@{
        Name = "Splash before"
        SourceFileName = "figma-01-splash.png"
        FileName = "01-splash-before.png"
    },
    [ordered]@{
        Name = "Splash after"
        SourceFileName = "figma-01-splash.png"
        FileName = "01-splash-after.png"
    },
    [ordered]@{
        Name = "Permission before"
        SourceFileName = "figma-02-permission.png"
        FileName = "02-permission-before.png"
    },
    [ordered]@{
        Name = "Permission after"
        SourceFileName = "figma-02-permission.png"
        FileName = "02-permission-after.png"
    },
    [ordered]@{
        Name = "Dashboard before"
        SourceFileName = "figma-03-dashboard.png"
        FileName = "03-dashboard-before.png"
    },
    [ordered]@{
        Name = "Dashboard after"
        SourceFileName = "figma-03-dashboard.png"
        FileName = "03-dashboard-after.png"
    },
    [ordered]@{
        Name = "Sessions before"
        SourceFileName = "figma-04-sessions.png"
        FileName = "04-sessions-before.png"
    },
    [ordered]@{
        Name = "Sessions after"
        SourceFileName = "figma-04-sessions.png"
        FileName = "04-sessions-after.png"
    },
    [ordered]@{
        Name = "App Detail before"
        SourceFileName = "figma-05-app-detail.png"
        FileName = "05-app-detail-before.png"
    },
    [ordered]@{
        Name = "App Detail after"
        SourceFileName = "figma-05-app-detail.png"
        FileName = "05-app-detail-after.png"
    },
    [ordered]@{
        Name = "Report before"
        SourceFileName = "figma-06-report.png"
        FileName = "06-report-before.png"
    },
    [ordered]@{
        Name = "Report after"
        SourceFileName = "figma-06-report.png"
        FileName = "06-report-after.png"
    },
    [ordered]@{
        Name = "Settings before"
        SourceFileName = "figma-07-settings.png"
        FileName = "07-settings-before.png"
    },
    [ordered]@{
        Name = "Settings after"
        SourceFileName = "figma-07-settings.png"
        FileName = "07-settings-after.png"
    }
)
$screenTargets = @(
    [ordered]@{
        Name = "Figma Splash"
        FileName = "figma-01-splash.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "Figma Permission"
        FileName = "figma-02-permission.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "Figma Dashboard"
        FileName = "figma-03-dashboard.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "Figma Sessions"
        FileName = "figma-04-sessions.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "Figma App Detail"
        FileName = "figma-05-app-detail.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "Figma Report"
        FileName = "figma-06-report.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "Figma Settings"
        FileName = "figma-07-settings.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "dashboard"
        FileName = "dashboard.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "settings"
        FileName = "settings.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "sessions"
        FileName = "sessions.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "daily summary"
        FileName = "daily-summary.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "dashboard overview"
        FileName = "01-dashboard-overview.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "dashboard summary and location"
        FileName = "02-dashboard-summary-location.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "dashboard charts"
        FileName = "03-dashboard-charts.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "dashboard recent sessions"
        FileName = "04-dashboard-recent-sessions.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "settings privacy and sync"
        FileName = "05-settings-privacy-sync.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "settings location permission"
        FileName = "06-settings-location-permission.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "sessions list"
        FileName = "07-sessions-list.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "daily summary feature"
        FileName = "08-daily-summary.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "main shell"
        FileName = "09-main-shell.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "main shell sessions"
        FileName = "10-main-shell-sessions.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "dashboard 1h selected"
        FileName = "16-dashboard-1h-selected.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "sessions 6h selected"
        FileName = "17-sessions-6h-selected.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "main shell settings"
        FileName = "11-main-shell-settings.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "main shell report"
        FileName = "12-main-shell-report.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "report custom range"
        FileName = "15-report-custom-range.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "permission onboarding"
        FileName = "13-permission-onboarding.png"
        Capture = "SnapshotCaptureTest"
    },
    [ordered]@{
        Name = "app detail"
        FileName = "14-app-detail.png"
        Capture = "SnapshotCaptureTest"
    }
)

function Invoke-AdbChecked {
    param(
        [string[]]$Arguments,
        [string]$Description
    )

    $effectiveArguments = @()
    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $effectiveArguments += "-s"
        $effectiveArguments += $DeviceSerial
    }
    $effectiveArguments += $Arguments

    & $AdbPath @effectiveArguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with adb exit code $LASTEXITCODE."
    }
}

function Invoke-AdbBestEffort {
    param(
        [string[]]$Arguments,
        [string]$Description
    )

    $effectiveArguments = @()
    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $effectiveArguments += "-s"
        $effectiveArguments += $DeviceSerial
    }
    $effectiveArguments += $Arguments

    try {
        & $AdbPath @effectiveArguments | Out-Null
        if ($LASTEXITCODE -ne 0) {
            $notes.Add("Warning: $Description returned adb exit code $LASTEXITCODE; continuing.")
        }
    }
    catch {
        $notes.Add("Warning: $Description failed: $($_.Exception.Message); continuing.")
    }
}

function Clear-AndroidSnapshotInterference {
    $notes.Add("Clearing external Android system dialogs before launching screenshot instrumentation.")

    $externalBrowserPackages = @(
        "com.android.chrome",
        "com.google.android.apps.chrome",
        "com.chrome.beta",
        "com.chrome.dev"
    )

    foreach ($packageName in $externalBrowserPackages) {
        Invoke-AdbBestEffort `
            -Arguments @("shell", "am", "force-stop", $packageName) `
            -Description "Force-stop external browser package $packageName"
    }

    Invoke-AdbBestEffort `
        -Arguments @("shell", "am", "broadcast", "-a", "android.intent.action.CLOSE_SYSTEM_DIALOGS") `
        -Description "Broadcast CLOSE_SYSTEM_DIALOGS before Android UI snapshots"

    Invoke-AdbBestEffort `
        -Arguments @("shell", "input", "keyevent", "4") `
        -Description "Dismiss any remaining system dialog with BACK before Android UI snapshots"
}

function Get-AndroidCanonicalScreenStatuses {
    $canonicalTargets = @($screenTargets | Where-Object { $_.FileName -like "figma-*.png" })
    $statuses = @()

    foreach ($target in $canonicalTargets) {
        $localPath = Join-Path $runRoot $target.FileName
        $fileInfo = if (Test-Path $localPath) { Get-Item $localPath } else { $null }
        $screenStatus = if ($fileInfo -and $fileInfo.Length -gt 0) { "PASS" } else { "WARN" }
        $note = if ($screenStatus -eq "PASS") {
            "Captured non-empty local screenshot."
        } else {
            "Screenshot missing or empty; rerun emulator capture before visual sign-off."
        }

        $statuses += [ordered]@{
            name = $target.Name
            status = $screenStatus
            fileName = $target.FileName
            note = $note
        }
    }

    return $statuses
}

function Copy-AndroidBeginnerReviewAliases {
    foreach ($alias in $beginnerReviewAliases) {
        $aliasName = $alias["Name"]
        $sourceFileName = $alias["SourceFileName"]
        $aliasFileName = $alias["FileName"]
        $sourcePath = Join-Path $runRoot $sourceFileName
        $destinationPath = Join-Path $runRoot $aliasFileName
        if (-not (Test-Path $sourcePath)) {
            throw "Expected canonical screenshot was not found for beginner review alias: $sourcePath"
        }

        Copy-Item -Force -Path $sourcePath -Destination $destinationPath
        $notes.Add("Copied $sourceFileName to beginner-review evidence alias $aliasFileName.")
        $script:screenshots += [pscustomobject]@{
            name = $aliasName
            fileName = $aliasFileName
            path = $destinationPath
            capture = "Canonical Woong UI screenshot copy"
        }
    }
}

function Write-AndroidSnapshotArtifacts {
    param(
        [string]$Status,
        [string]$BlockedReason
    )

    $canonicalScreenStatuses = Get-AndroidCanonicalScreenStatuses
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
        "## Feature Screens",
        ""
    )
    foreach ($screen in $featureScreens) {
        $reportLines += "- $screen"
    }
    $reportLines += @(
        "",
        "## Canonical Figma Screen Status",
        "",
        "| Screen | Status | Artifact | Note |",
        "|---|---|---|---|"
    )
    foreach ($screenStatus in $canonicalScreenStatuses) {
        $reportLines += "| $($screenStatus.name) | $($screenStatus.status) | ``$($screenStatus.fileName)`` | $($screenStatus.note) |"
    }
    $reportLines += @(
        "",
        "## Expected Location Context Checks",
        ""
    )
    foreach ($check in $expectedLocationChecks) {
        $reportLines += "- $check"
    }
    $reportLines += @(
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
    if ($screenshots.Count -gt 0) {
        $reportLines += @(
            "",
            "## Screenshots"
        )
        foreach ($screenshot in $screenshots) {
            $reportLines += "- $($screenshot.name): $($screenshot.fileName)"
        }
    }
    Set-Content -Path (Join-Path $runRoot "report.md") -Value $reportLines

    $manifest = [ordered]@{
        status = $Status
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        output = $runRoot
        adbPath = $AdbPath
        deviceSerial = $DeviceSerial
        gradleWrapperPath = $GradleWrapperPath
        expectedScreens = $expectedScreens
        featureScreens = $featureScreens
        screenStatuses = $canonicalScreenStatuses
        expectedLocationChecks = $expectedLocationChecks
        beginnerReviewAliases = $beginnerReviewAliases
        screenshots = $screenshots
        blockedReason = $BlockedReason
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

    $prompt = @(
        "# Android UI Visual Review Prompt",
        "",
        "Review the Android dashboard/settings/sessions/daily summary screenshots in this folder when they exist.",
        "Start with the canonical Figma 7-screen parity captures: figma-01-splash.png through figma-07-settings.png.",
        "Beginner-review before/after aliases 01-splash-before.png through 07-settings-after.png are local copies of the canonical Woong UI screenshots.",
        "Feature screenshots are numbered 01 through 08 so each product surface can be reviewed independently.",
        "Main shell screenshots include Dashboard, Sessions, Report, and Settings tab states.",
        "Screenshots 16 and 17 show selected-period states after changing Dashboard to 1h and Sessions to 6h.",
        "Report custom range screenshot 15 shows the yyyy-MM-dd start/end inputs and applied Room-backed range state.",
        "Permission onboarding screenshot 13 shows the Usage Access guidance shown when permission is missing.",
        "Check that usage totals, permission guidance, sync state, and privacy-safe copy are readable.",
        "Check the Dashboard location card shows seeded opt-in location context when screenshots exist.",
        "Check the Settings location section shows location off by default, precise latitude/longitude explicit opt-in, and the permission action.",
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

    if (-not $SkipBuild) {
        $debugApk = Join-Path $androidRoot "app\build\outputs\apk\debug\app-debug.apk"
        $debugAndroidTestApk = Join-Path $androidRoot "app\build\outputs\apk\androidTest\debug\app-debug-androidTest.apk"
        if (-not (Test-Path $debugApk)) {
            throw "Debug APK not found after build: $debugApk"
        }
        if (-not (Test-Path $debugAndroidTestApk)) {
            throw "Debug androidTest APK not found after build: $debugAndroidTestApk"
        }

        $notes.Add("Installing debug APK: $debugApk")
        Invoke-AdbChecked -Arguments @("install", "-r", $debugApk) -Description "Install debug APK"
        $notes.Add("Installing debug androidTest APK: $debugAndroidTestApk")
        Invoke-AdbChecked -Arguments @("install", "-r", $debugAndroidTestApk) -Description "Install debug androidTest APK"
    } else {
        $notes.Add("Install skipped by -SkipBuild; assuming the debug app and androidTest APK are already installed on the connected device.")
    }

    $notes.Add("Detected device(s): $($deviceLines -join '; ')")
    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $notes.Add("Pinned adb device serial: $DeviceSerial")
    }
    $seedTestClass = "com.woong.monitorstack.snapshots.SnapshotSeedTest"
    $captureTestClass = "com.woong.monitorstack.snapshots.SnapshotCaptureTest"
    $testRunner = "com.woong.monitorstack.test/androidx.test.runner.AndroidJUnitRunner"
    Clear-AndroidSnapshotInterference
    $notes.Add("Seeding deterministic sample sessions and location context with $seedTestClass.")
    Invoke-AdbChecked -Arguments @("shell", "am", "instrument", "-w", "-e", "class", $seedTestClass, $testRunner) -Description "Seed Android snapshot sample data"
    Clear-AndroidSnapshotInterference
    $notes.Add("Capturing screenshots through instrumentation with $captureTestClass so non-exported activities stay private.")
    Invoke-AdbChecked -Arguments @("shell", "am", "instrument", "-w", "-e", "class", $captureTestClass, $testRunner) -Description "Capture Android snapshot screens"

    $remoteSnapshotDir = "/sdcard/Android/data/com.woong.monitorstack/files/ui-snapshots"

    foreach ($target in $screenTargets) {
        $notes.Add("Pulling $($target.Name) screenshot captured by instrumentation.")
        $remotePath = "$remoteSnapshotDir/$($target.FileName)"
        $localPath = Join-Path $runRoot $target.FileName
        Invoke-AdbChecked -Arguments @("pull", $remotePath, $localPath) -Description "Pull $($target.Name) screenshot"

        if (-not (Test-Path $localPath)) {
            throw "Expected screenshot was not created: $localPath"
        }

        $screenshots += [ordered]@{
            name = $target.Name
            fileName = $target.FileName
            path = $localPath
            capture = $target.Capture
        }
    }
    Copy-AndroidBeginnerReviewAliases
    Write-AndroidSnapshotArtifacts -Status $status -BlockedReason $blockedReason
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
