param(
    [string]$OutputRoot = "",
    [string]$AdbPath = "",
    [string]$GradleWrapperPath = "",
    [string]$DeviceSerial = "",
    [string]$PackageName = "com.woong.monitorstack",
    [string]$ChromePackageName = "com.android.chrome",
    [int]$ChromeForegroundSeconds = 3,
    [switch]$SkipBuild,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/android-usage-current-focus"
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

function Get-AdbArguments {
    param([string[]]$Arguments)

    $effectiveArguments = @()
    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $effectiveArguments += "-s"
        $effectiveArguments += $DeviceSerial
    }
    $effectiveArguments += $Arguments

    return $effectiveArguments
}

function Invoke-AdbChecked {
    param(
        [string[]]$Arguments,
        [string]$Description
    )

    $effectiveArguments = Get-AdbArguments -Arguments $Arguments
    if ($DryRun) {
        $notes.Add("DRY RUN adb $($effectiveArguments -join ' ')")
        return @()
    }

    $output = & $AdbPath @effectiveArguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with adb exit code $LASTEXITCODE."
    }

    return $output
}

function Add-Artifact {
    param(
        [string]$Name,
        [string]$FileName,
        [string]$Path
    )

    $artifacts.Add([ordered]@{
        name = $Name
        fileName = $FileName
        path = $Path
    })
}

function Save-TextArtifact {
    param(
        [string]$Name,
        [string]$FileName,
        [string[]]$Lines
    )

    $localPath = Join-Path $runRoot $FileName
    $Lines | Set-Content -Path $localPath -Encoding UTF8
    Add-Artifact -Name $Name -FileName $FileName -Path $localPath
}

function Write-ValidationArtifacts {
    param(
        [string]$Status,
        [string]$BlockedReason
    )

    $reportLines = @(
        "# Android UsageStats Current Focus Validation",
        "",
        "Status: $Status",
        "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
        "Output: ``$runRoot``",
        "Package: ``$PackageName``",
        "Chrome package: ``$ChromePackageName``",
        "",
        "## Scope",
        "",
        "- Launches Woong Monitor Stack.",
        "- Launches Chrome with ``about:blank`` and does not capture Chrome screenshots.",
        "- Returns to Woong Monitor Stack and captures only Woong UI evidence.",
        "- Uses UsageStats metadata behavior only: package names and foreground intervals.",
        "",
        "## Privacy Boundary",
        "",
        "- No Accessibility scraping.",
        "- No clipboard, text input, form input, passwords, message content, touch-coordinate logging, or external app page-content capture.",
        "- The only screenshot artifact is taken after Woong is confirmed as the foreground package.",
        "- The UI hierarchy dump is taken only after Woong is confirmed as the foreground package.",
        "",
        "## Limitation",
        "",
        "- This is a no-wait emulator smoke check. It verifies that returning from Chrome can refresh Woong Current Focus quickly.",
        "- It does not, by itself, prove the exact resume-before-collection-start boundary unless paired with a test/debug hook that invokes collection with explicit from/to timestamps.",
        "- The anchored-lookback boundary should remain covered by JVM tests for session clamping and runner query range.",
        "",
        "## Result",
        ""
    )

    if (-not [string]::IsNullOrWhiteSpace($BlockedReason)) {
        $reportLines += "- BLOCKED/FAIL: $BlockedReason"
    } elseif ($DryRun) {
        $reportLines += "- DRY RUN: command plan generated; no emulator actions were executed."
    } else {
        $reportLines += "- PASS: validation commands completed."
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
        deviceSerial = $DeviceSerial
        gradleWrapperPath = $GradleWrapperPath
        packageName = $PackageName
        chromePackageName = $ChromePackageName
        chromeForegroundSeconds = $ChromeForegroundSeconds
        skipBuild = [bool]$SkipBuild
        dryRun = [bool]$DryRun
        artifacts = $artifacts
        blockedReason = $BlockedReason
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

    if (Test-Path $latestRoot) {
        Remove-Item -Recurse -Force $latestRoot
    }
    Copy-Item -Recurse -Path $runRoot -Destination $latestRoot
}

function Get-ForegroundPackage {
    $windowOutput = Invoke-AdbChecked -Arguments @("shell", "dumpsys", "window") -Description "Read foreground window"
    $focusLines = @($windowOutput | Where-Object {
        $_ -match "mCurrentFocus|mFocusedApp|topResumedActivity|mResumedActivity"
    })
    Save-TextArtifact -Name "Foreground window dump" -FileName "foreground-window.txt" -Lines $focusLines

    $joined = $focusLines -join "`n"
    if ($joined -match "([a-zA-Z0-9_.]+)/(?:[a-zA-Z0-9_.$]+)") {
        return $Matches[1]
    }

    return ""
}

try {
    if ($DryRun) {
        $notes.Add("Dry run enabled. The script will emit the command plan and artifacts without requiring a device.")
    }

    if (-not $SkipBuild) {
        if (-not (Test-Path $GradleWrapperPath)) {
            throw "Gradle wrapper not found: $GradleWrapperPath"
        }

        if ($DryRun) {
            $notes.Add("DRY RUN build: $GradleWrapperPath assembleDebug --no-daemon --stacktrace")
        } else {
            Push-Location $androidRoot
            try {
                & $GradleWrapperPath assembleDebug --no-daemon --stacktrace
                if ($LASTEXITCODE -ne 0) { throw "assembleDebug failed." }
            }
            finally {
                Pop-Location
            }
        }
    } else {
        $notes.Add("Build skipped by -SkipBuild.")
    }

    if (-not $DryRun) {
        $adbOutput = & $AdbPath @(Get-AdbArguments -Arguments @("devices", "-l"))
        $deviceLines = @($adbOutput | Where-Object {
            $_ -match "\bdevice\b" -and $_ -notmatch "^List of devices attached"
        })

        if ($deviceLines.Count -eq 0) {
            $status = "BLOCKED"
            $blockedReason = "No connected Android device or running emulator was reported by adb devices -l."
            $notes.Add("Start an emulator or connect a physical Android device, then rerun this script.")
            Write-ValidationArtifacts -Status $status -BlockedReason $blockedReason
            Write-Host "Android UsageStats current-focus validation is blocked."
            Write-Host "Artifacts: $runRoot"
            exit 0
        }

        $notes.Add("Detected device(s): $($deviceLines -join '; ')")
    } else {
        $notes.Add("DRY RUN adb devices -l")
    }

    if (-not $SkipBuild) {
        $debugApk = Join-Path $androidRoot "app\build\outputs\apk\debug\app-debug.apk"
        if ($DryRun) {
            $notes.Add("DRY RUN install debug APK: $debugApk")
        } else {
            if (-not (Test-Path $debugApk)) {
                throw "Debug APK not found after build: $debugApk"
            }
            Invoke-AdbChecked -Arguments @("install", "-r", $debugApk) -Description "Install debug APK" | Out-Null
        }
    } else {
        $notes.Add("Install skipped by -SkipBuild; assuming the debug APK is already installed.")
    }

    Invoke-AdbChecked -Arguments @("shell", "appops", "set", $PackageName, "GET_USAGE_STATS", "allow") -Description "Grant Usage Access app-op" | Out-Null
    $notes.Add("Granted Usage Access app-op to $PackageName when supported by the emulator.")

    Invoke-AdbChecked -Arguments @("shell", "monkey", "-p", $PackageName, "-c", "android.intent.category.LAUNCHER", "1") -Description "Launch Woong" | Out-Null
    Start-Sleep -Seconds 1

    Invoke-AdbChecked -Arguments @(
        "shell",
        "am",
        "start",
        "-a",
        "android.intent.action.VIEW",
        "-d",
        "about:blank",
        "-p",
        $ChromePackageName
    ) -Description "Launch Chrome about:blank" | Out-Null
    $notes.Add("Launched Chrome with about:blank. No Chrome screenshot is taken.")

    if ($ChromeForegroundSeconds -gt 0) {
        Start-Sleep -Seconds $ChromeForegroundSeconds
    }

    Invoke-AdbChecked -Arguments @("shell", "monkey", "-p", $PackageName, "-c", "android.intent.category.LAUNCHER", "1") -Description "Return to Woong" | Out-Null
    Start-Sleep -Seconds 3

    $foregroundPackage = Get-ForegroundPackage
    $notes.Add("Foreground package before screenshot: $foregroundPackage")
    if (-not $DryRun -and $foregroundPackage -ne $PackageName) {
        $status = "BLOCKED"
        $blockedReason = "Refusing to capture screenshot because foreground package is '$foregroundPackage', not '$PackageName'."
        Write-ValidationArtifacts -Status $status -BlockedReason $blockedReason
        Write-Host $blockedReason
        Write-Host "Artifacts: $runRoot"
        exit 0
    }

    $remoteDir = "/sdcard/Android/data/$PackageName/files/current-focus-validation"
    $remoteScreenshot = "$remoteDir/current-focus-after-chrome.png"
    $remoteUiDump = "$remoteDir/current-focus-after-chrome.xml"
    $localScreenshot = Join-Path $runRoot "current-focus-after-chrome.png"
    $localUiDump = Join-Path $runRoot "current-focus-after-chrome.xml"

    Invoke-AdbChecked -Arguments @("shell", "mkdir", "-p", $remoteDir) -Description "Create remote artifact directory" | Out-Null
    Invoke-AdbChecked -Arguments @("shell", "uiautomator", "dump", $remoteUiDump) -Description "Capture Woong UI hierarchy" | Out-Null
    Invoke-AdbChecked -Arguments @("pull", $remoteUiDump, $localUiDump) -Description "Pull Woong UI hierarchy" | Out-Null

    if (-not $DryRun -and (Test-Path $localUiDump)) {
        $uiText = Get-Content -Raw $localUiDump
        if ($uiText -match "Chrome" -and $uiText -match [regex]::Escape($ChromePackageName)) {
            $notes.Add("Woong UI hierarchy includes Chrome and $ChromePackageName.")
        } else {
            $status = "FAIL"
            $blockedReason = "Woong UI hierarchy did not include both 'Chrome' and '$ChromePackageName'. Review the screenshot and app logs."
        }
    }

    if ($status -ne "FAIL") {
        Start-Sleep -Seconds 1
        Invoke-AdbChecked -Arguments @("shell", "screencap", "-p", $remoteScreenshot) -Description "Capture Woong screenshot" | Out-Null
        Invoke-AdbChecked -Arguments @("pull", $remoteScreenshot, $localScreenshot) -Description "Pull Woong screenshot" | Out-Null
    }

    if (-not $DryRun) {
        if (Test-Path $localScreenshot) {
            Add-Artifact -Name "Woong Current Focus screenshot" -FileName "current-focus-after-chrome.png" -Path $localScreenshot
        }
        Add-Artifact -Name "Woong Current Focus UI hierarchy" -FileName "current-focus-after-chrome.xml" -Path $localUiDump
    }

    Write-ValidationArtifacts -Status $status -BlockedReason $blockedReason
    Write-Host "Android UsageStats current-focus validation artifacts: $runRoot"
    exit $(if ($status -eq "FAIL") { 1 } else { 0 })
}
catch {
    $status = "FAIL"
    $blockedReason = $_.Exception.Message
    $notes.Add("Failure: $blockedReason")
    Write-ValidationArtifacts -Status $status -BlockedReason $blockedReason
    Write-Error $blockedReason
    exit 1
}
