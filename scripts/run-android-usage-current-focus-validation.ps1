param(
    [string]$OutputRoot = "",
    [string]$AdbPath = "",
    [string]$GradleWrapperPath = "",
    [string]$DeviceSerial = "",
    [string]$PackageName = "com.woong.monitorstack",
    [string]$ChromePackageName = "com.android.chrome",
    [int]$ChromeForegroundSeconds = 3,
    [int]$AdbCommandTimeoutSeconds = 45,
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

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $AdbPath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Arguments = ($effectiveArguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_ -replace '"', '\"') + '"'
        } else {
            $_
        }
    }
    ) -join " "

    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        if (-not $process.WaitForExit($AdbCommandTimeoutSeconds * 1000)) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            throw "$Description timed out after $AdbCommandTimeoutSeconds seconds. adb $($effectiveArguments -join ' ')"
        }

        $output = @($process.StandardOutput.ReadToEnd() -split "`r?`n" | Where-Object { $_ -ne "" })
        $errorOutput = @($process.StandardError.ReadToEnd() -split "`r?`n" | Where-Object { $_ -ne "" })

        $exitCode = $process.ExitCode
        if ($exitCode -ne 0) {
            throw "$Description failed with adb exit code $exitCode. $($errorOutput -join ' ')"
        }

        return $output
    }
    finally {
        $process.Dispose()
    }
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

function Test-ScreenshotHasVisibleContent {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $false
    }

    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new($Path)
    try {
        $sampledNonWhitePixels = 0
        $startY = [Math]::Min(160, $bitmap.Height - 1)
        $endY = [Math]::Max($startY, $bitmap.Height - 320)
        $stepX = [Math]::Max(1, [int]($bitmap.Width / 24))
        $stepY = [Math]::Max(1, [int](($endY - $startY) / 36))

        for ($y = $startY; $y -lt $endY; $y += $stepY) {
            for ($x = 0; $x -lt $bitmap.Width; $x += $stepX) {
                $pixel = $bitmap.GetPixel($x, $y)
                if ($pixel.R -lt 245 -or $pixel.G -lt 245 -or $pixel.B -lt 245) {
                    $sampledNonWhitePixels += 1
                }
            }
        }

        return $sampledNonWhitePixels -ge 10
    }
    finally {
        $bitmap.Dispose()
    }
}

function Capture-VisibleScreenshot {
    param(
        [string]$RemotePath,
        [string]$LocalPath
    )

    if ($DryRun) {
        Invoke-AdbChecked -Arguments @("shell", "screencap", "-p", $RemotePath) -Description "Capture Woong screenshot dry run" | Out-Null
        Invoke-AdbChecked -Arguments @("pull", $RemotePath, $LocalPath) -Description "Pull Woong screenshot dry run" | Out-Null
        $notes.Add("DRY RUN screenshot visibility check skipped.")
        return $true
    }

    $maxAttempts = 5
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt += 1) {
        Invoke-AdbChecked -Arguments @("shell", "screencap", "-p", $RemotePath) -Description "Capture Woong screenshot attempt $attempt" | Out-Null
        Invoke-AdbChecked -Arguments @("pull", $RemotePath, $LocalPath) -Description "Pull Woong screenshot attempt $attempt" | Out-Null

        if (Test-ScreenshotHasVisibleContent -Path $LocalPath) {
            $notes.Add("Woong screenshot captured visible content on attempt $attempt.")
            return $true
        }

        $notes.Add("Woong screenshot attempt $attempt looked blank; retrying after UI settle delay.")
        Start-Sleep -Seconds 2
    }

    return $false
}

function Start-WoongApp {
    $activity = "$PackageName/.MainActivity"
    Invoke-AdbChecked -Arguments @(
        "shell",
        "am",
        "start",
        "-W",
        "-n",
        $activity
    ) -Description "Launch Woong" | Out-Null
}

function Start-ChromeAboutBlank {
    Invoke-AdbChecked -Arguments @(
        "shell",
        "sh",
        "-c",
        "am start -a android.intent.action.VIEW -d about:blank -p $ChromePackageName >/dev/null 2>&1 &"
    ) -Description "Launch Chrome about:blank" | Out-Null
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
    $focusLines = @(Invoke-AdbChecked -Arguments @(
        "shell",
        "sh",
        "-c",
        "dumpsys activity activities 2>/dev/null | grep -E 'mCurrentFocus|mFocusedApp|topResumedActivity|mResumedActivity' || true"
    ) -Description "Read foreground activity")
    Save-TextArtifact -Name "Foreground window dump" -FileName "foreground-window.txt" -Lines $focusLines

    $joined = $focusLines -join "`n"
    if ($joined -match "([a-zA-Z0-9_.]+)/(?:[a-zA-Z0-9_.$]+)") {
        return $Matches[1]
    }

    return ""
}

function Wait-ForForegroundPackage {
    param(
        [string]$ExpectedPackageName,
        [int]$MaxAttempts = 6,
        [int]$DelaySeconds = 1
    )

    $lastForegroundPackage = ""
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt += 1) {
        $lastForegroundPackage = Get-ForegroundPackage
        $notes.Add("Foreground package attempt $attempt/$MaxAttempts after Woong return: $lastForegroundPackage")
        if ($lastForegroundPackage -eq $ExpectedPackageName) {
            return $lastForegroundPackage
        }

        Start-WoongApp
        Start-Sleep -Seconds $DelaySeconds
    }

    return $lastForegroundPackage
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

    Invoke-AdbChecked -Arguments @(
        "shell",
        "sh",
        "-c",
        "appops set $PackageName GET_USAGE_STATS allow >/dev/null 2>&1 || true"
    ) -Description "Grant Usage Access app-op" | Out-Null
    $notes.Add("Granted Usage Access app-op to $PackageName when supported by the emulator.")

    Start-WoongApp
    Start-Sleep -Seconds 2

    Start-ChromeAboutBlank
    $notes.Add("Launched Chrome with about:blank. No Chrome screenshot is taken.")

    if ($ChromeForegroundSeconds -gt 0) {
        Start-Sleep -Seconds $ChromeForegroundSeconds
    }

    Start-WoongApp
    Start-Sleep -Seconds 2

    $foregroundPackage = Wait-ForForegroundPackage -ExpectedPackageName $PackageName
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
        if ($uiText -match "Woong Monitor" -and $uiText -match [regex]::Escape($PackageName)) {
            $notes.Add("Woong UI hierarchy shows Woong Monitor and $PackageName as the current foreground app.")
        } else {
            $status = "FAIL"
            $blockedReason = "Woong UI hierarchy did not show Woong Monitor and '$PackageName' as the current foreground app. Review the screenshot and app logs."
        }
    }

    if ($status -ne "FAIL") {
        Start-Sleep -Seconds 1
        if (-not (Capture-VisibleScreenshot -RemotePath $remoteScreenshot -LocalPath $localScreenshot)) {
            $status = "FAIL"
            $blockedReason = "Woong screenshot stayed blank after retries even though foreground package and UI hierarchy were valid."
        }
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
