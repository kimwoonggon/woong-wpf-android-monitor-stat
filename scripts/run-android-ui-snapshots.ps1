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
$androidRoot = Split-Path -Parent $GradleWrapperPath

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$expectedScreens = @("dashboard", "settings", "sessions", "daily summary")
$status = "PASS"
$blockedReason = ""
$screenshots = @()
$notes = New-Object System.Collections.Generic.List[string]
$screenTargets = @(
    [ordered]@{
        Name = "dashboard"
        FileName = "dashboard.png"
        Activity = "com.woong.monitorstack/.dashboard.DashboardActivity"
    },
    [ordered]@{
        Name = "settings"
        FileName = "settings.png"
        Activity = "com.woong.monitorstack/.settings.SettingsActivity"
    },
    [ordered]@{
        Name = "sessions"
        FileName = "sessions.png"
        Activity = "com.woong.monitorstack/.sessions.SessionsActivity"
    },
    [ordered]@{
        Name = "daily summary"
        FileName = "daily-summary.png"
        Activity = "com.woong.monitorstack/.summary.DailySummaryActivity"
    }
)

function Invoke-AdbChecked {
    param(
        [string[]]$Arguments,
        [string]$Description
    )

    & $AdbPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with adb exit code $LASTEXITCODE."
    }
}

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
    $seedTestClass = "com.woong.monitorstack.snapshots.SnapshotSeedTest"
    $testRunner = "com.woong.monitorstack.test/androidx.test.runner.AndroidJUnitRunner"
    $notes.Add("Seeding deterministic sample sessions with $seedTestClass.")
    Invoke-AdbChecked -Arguments @("shell", "am", "instrument", "-w", "-e", "class", $seedTestClass, $testRunner) -Description "Seed Android snapshot sample data"

    foreach ($target in $screenTargets) {
        $notes.Add("Capturing $($target.Name) via $($target.Activity).")
        Invoke-AdbChecked -Arguments @("shell", "am", "start", "-W", "-n", $target.Activity) -Description "Launch $($target.Name)"
        Start-Sleep -Milliseconds 750

        $remotePath = "/sdcard/Download/woong-monitor-$($target.Name -replace '\s+', '-').png"
        $localPath = Join-Path $runRoot $target.FileName
        Invoke-AdbChecked -Arguments @("shell", "screencap", "-p", $remotePath) -Description "Capture $($target.Name)"
        Invoke-AdbChecked -Arguments @("pull", $remotePath, $localPath) -Description "Pull $($target.Name) screenshot"
        & $AdbPath shell rm $remotePath | Out-Null

        if (-not (Test-Path $localPath)) {
            throw "Expected screenshot was not created: $localPath"
        }

        $screenshots += [ordered]@{
            name = $target.Name
            fileName = $target.FileName
            path = $localPath
            activity = $target.Activity
        }
    }
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
