param(
    [string]$OutputRoot = "",
    [string]$AdbPath = "",
    [string]$GradleWrapperPath = "",
    [string]$PackageName = "com.woong.monitorstack",
    [int]$DurationSeconds = 10,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/android-resource-measurements"
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

$status = "PASS"
$blockedReason = ""
$notes = New-Object System.Collections.Generic.List[string]
$artifacts = New-Object System.Collections.Generic.List[object]

function Invoke-AdbChecked {
    param(
        [string[]]$Arguments,
        [string]$Description
    )

    $output = & $AdbPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with adb exit code $LASTEXITCODE."
    }

    return $output
}

function Save-AdbOutput {
    param(
        [string[]]$Arguments,
        [string]$Description,
        [string]$FileName
    )

    $localPath = Join-Path $runRoot $FileName
    $output = Invoke-AdbChecked -Arguments $Arguments -Description $Description
    $output | Set-Content -Path $localPath -Encoding UTF8
    $artifacts.Add([ordered]@{
        name = $Description
        fileName = $FileName
        path = $localPath
    })
}

function Write-MeasurementArtifacts {
    param(
        [string]$Status,
        [string]$BlockedReason
    )

    $reportLines = @(
        "# Android Resource Measurement Report",
        "",
        "Status: $Status",
        "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
        "Output: ``$runRoot``",
        "Package: ``$PackageName``",
        "",
        "## Scope",
        "",
        "- Launches the app through the Android launcher entry point.",
        "- Collects package-scoped process, memory, and graphics frame diagnostics.",
        "- Works with an emulator or a physical device.",
        "",
        "## Privacy Boundary",
        "",
        "- Does not capture screenshots.",
        "- Does not record typed text, passwords, form input, messages, clipboard data, or other app content.",
        "- Uses Android framework diagnostics for this app package only.",
        "",
        "## Result",
        ""
    )

    if (-not [string]::IsNullOrWhiteSpace($BlockedReason)) {
        $reportLines += "- BLOCKED: $BlockedReason"
    } else {
        $reportLines += "- PASS: Android resource measurement completed."
    }

    $reportLines += @(
        "",
        "## Notes"
    )
    foreach ($note in $notes) {
        $reportLines += "- $note"
    }

    if ($artifacts.Count -gt 0) {
        $reportLines += @(
            "",
            "## Artifacts"
        )
        foreach ($artifact in $artifacts) {
            $reportLines += "- $($artifact.name): $($artifact.fileName)"
        }
    }

    Set-Content -Path (Join-Path $runRoot "report.md") -Value $reportLines -Encoding UTF8

    $manifest = [ordered]@{
        status = $Status
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        output = $runRoot
        adbPath = $AdbPath
        gradleWrapperPath = $GradleWrapperPath
        packageName = $PackageName
        durationSeconds = $DurationSeconds
        artifacts = $artifacts
        blockedReason = $BlockedReason
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

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
        $notes.Add("Start an emulator or connect a physical Android device, then rerun this script.")
        Write-MeasurementArtifacts -Status $status -BlockedReason $blockedReason
        Write-Host "No connected Android device. Android resource measurement is blocked."
        Write-Host "Android resource measurement artifacts: $runRoot"
        exit 0
    }

    $notes.Add("Detected device(s): $($deviceLines -join '; ')")

    if (-not $SkipBuild) {
        $debugApk = Join-Path $androidRoot "app\build\outputs\apk\debug\app-debug.apk"
        if (-not (Test-Path $debugApk)) {
            throw "Debug APK not found after build: $debugApk"
        }

        $notes.Add("Installing debug APK: $debugApk")
        Invoke-AdbChecked -Arguments @("install", "-r", $debugApk) -Description "Install debug APK" | Out-Null
    } else {
        $notes.Add("Install skipped by -SkipBuild; assuming the debug app is already installed.")
    }

    $notes.Add("Launching $PackageName through the launcher entry point.")
    Invoke-AdbChecked -Arguments @(
        "shell",
        "monkey",
        "-p",
        $PackageName,
        "-c",
        "android.intent.category.LAUNCHER",
        "1"
    ) -Description "Launch app" | Out-Null

    if ($DurationSeconds -gt 0) {
        $notes.Add("Waiting $DurationSeconds seconds before sampling resource diagnostics.")
        Start-Sleep -Seconds $DurationSeconds
    }

    Save-AdbOutput -Arguments @("shell", "pidof", "-s", $PackageName) -Description "Process id" -FileName "process.txt"
    Save-AdbOutput -Arguments @("shell", "dumpsys", "meminfo", $PackageName) -Description "Memory info" -FileName "meminfo.txt"
    Save-AdbOutput -Arguments @("shell", "dumpsys", "gfxinfo", $PackageName) -Description "Graphics frame info" -FileName "gfxinfo.txt"

    Write-MeasurementArtifacts -Status $status -BlockedReason $blockedReason
    Write-Host "Android resource measurement artifacts: $runRoot"
    exit 0
}
catch {
    $status = "FAIL"
    $blockedReason = $_.Exception.Message
    $notes.Add("Failure: $blockedReason")
    Write-MeasurementArtifacts -Status $status -BlockedReason $blockedReason
    Write-Error $blockedReason
    exit 1
}
