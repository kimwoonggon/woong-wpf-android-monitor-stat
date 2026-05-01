param(
    [string]$OutputRoot = "",
    [string]$AdbPath = "",
    [string]$GradleWrapperPath = "",
    [string]$DeviceSerial = "",
    [string]$PackageName = "com.woong.monitorstack",
    [string]$ChromePackageName = "com.android.chrome",
    [int]$ChromeForegroundSeconds = 3,
    [int]$AdbCommandTimeoutSeconds = 60,
    [int]$PackageManagerPreflightTimeoutSeconds = 10,
    [int]$InstallTimeoutSeconds = 180,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/android-app-switch-qa"
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
$remoteDir = "/sdcard/Android/data/$PackageName/files/app-switch-qa"
$testRunner = "$PackageName.test/androidx.test.runner.AndroidJUnitRunner"
$testClass = "com.woong.monitorstack.usage.AppSwitchQaEvidenceTest"
$prepareTestName = "com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#prepareCleanRoomForAppSwitchQa"
$collectTestName = "com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#collectUsageStatsAfterChromeReturnPersistsFocusSessionAndOutbox"
$captureTestName = "com.woong.monitorstack.usage.AppSwitchQaEvidenceTest#captureWoongDashboardAndSessionsOnlyAfterReturn"
$installDiagnosticArtifactContract = @(
    "package-manager-preflight.txt",
    "install-debug-apk-stdout.txt",
    "install-debug-apk-stderr.txt",
    "install-android-test-apk-stdout.txt",
    "install-android-test-apk-stderr.txt"
)

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$status = "PASS"
$blockedReason = ""
$installBlocked = $false
$packageManagerBlocked = $false
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

function ConvertTo-ProcessArguments {
    param([string[]]$Arguments)

    return ($Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_ -replace '"', '\"') + '"'
        } else {
            $_
        }
    }) -join " "
}

function Invoke-AdbProcess {
    param(
        [string[]]$Arguments,
        [string]$Description,
        [int]$TimeoutSeconds = $AdbCommandTimeoutSeconds
    )

    $effectiveArguments = Get-AdbArguments -Arguments $Arguments
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $AdbPath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Arguments = ConvertTo-ProcessArguments -Arguments $effectiveArguments

    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
        if ($timedOut) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            $process.WaitForExit(5000) | Out-Null
        }

        $output = @($process.StandardOutput.ReadToEnd() -split "`r?`n" | Where-Object { $_ -ne "" })
        $errorOutput = @($process.StandardError.ReadToEnd() -split "`r?`n" | Where-Object { $_ -ne "" })

        return [pscustomobject]@{
            description = $Description
            arguments = $effectiveArguments
            exitCode = if ($timedOut) { $null } else { $process.ExitCode }
            timedOut = $timedOut
            timeoutSeconds = $TimeoutSeconds
            stdout = $output
            stderr = $errorOutput
        }
    }
    finally {
        $process.Dispose()
    }
}

function Invoke-AdbChecked {
    param(
        [string[]]$Arguments,
        [string]$Description,
        [int]$TimeoutSeconds = $AdbCommandTimeoutSeconds
    )

    $result = Invoke-AdbProcess `
        -Arguments $Arguments `
        -Description $Description `
        -TimeoutSeconds $TimeoutSeconds

    if ($result.timedOut) {
        throw "$Description timed out after $TimeoutSeconds seconds. adb $($result.arguments -join ' ')"
    }

    if ($result.exitCode -ne 0) {
        throw "$Description failed with adb exit code $($result.exitCode). $($result.stderr -join ' ')"
    }

    return $result.stdout
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
    if (-not (Test-Path $localPath)) {
        New-Item -ItemType File -Force -Path $localPath | Out-Null
    }
    Add-Artifact -Name $Name -FileName $FileName -Path $localPath
}

function Save-AdbTextArtifact {
    param(
        [string]$Name,
        [string]$FileName,
        [string[]]$Arguments,
        [string]$Description
    )

    $lines = Invoke-AdbChecked -Arguments $Arguments -Description $Description
    Save-TextArtifact -Name $Name -FileName $FileName -Lines $lines
    return $lines
}

function Write-RoomAssertionPlaceholder {
    param(
        [string]$Status,
        [string]$Reason
    )

    $path = Join-Path $runRoot "room-assertions.json"
    [ordered]@{
        status = $Status
        reason = $Reason
        chromePackageName = $ChromePackageName
        focusSessionChromeRows = 0
        syncOutboxChromeRows = 0
    } | ConvertTo-Json -Depth 4 | Set-Content -Path $path -Encoding UTF8
    Add-Artifact -Name "Room assertions" -FileName "room-assertions.json" -Path $path
}

function Write-AppSwitchArtifacts {
    param(
        [string]$Status,
        [string]$BlockedReason
    )

    if (-not (Test-Path (Join-Path $runRoot "room-assertions.json"))) {
        Write-RoomAssertionPlaceholder -Status $Status -Reason $BlockedReason
    }

    $reportLines = @(
        "# Android UsageStats App-Switch QA",
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
        "- Installs debug and androidTest APKs unless installed APK hashes already match the build artifacts.",
        "- Launches Chrome with ``about:blank``.",
        "- Returns to Woong Monitor Stack.",
        "- Runs production UsageStats collection through Android instrumentation.",
        "- Asserts Room ``focus_session`` and ``sync_outbox`` metadata evidence.",
        "- Captures Dashboard and Sessions evidence only after Woong is foreground.",
        "",
        "## Privacy Boundary",
        "",
        "- No Chrome screenshots are captured.",
        "- No Chrome UI hierarchy dumps are captured.",
        "- Chrome participation is proven with ``dumpsys window`` foreground metadata, process metadata, and Room package/time rows.",
        "- No Accessibility scraping, clipboard, text input, form input, passwords, message content, browser/page contents, or global touch-coordinate logging.",
        "",
        "## Result",
        ""
    )

    if (-not [string]::IsNullOrWhiteSpace($BlockedReason)) {
        $reportLines += "- BLOCKED/FAIL: $BlockedReason"
    } else {
        $reportLines += "- PASS: Android app-switch QA commands completed."
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
        packageManagerPreflightTimeoutSeconds = $PackageManagerPreflightTimeoutSeconds
        installTimeoutSeconds = $InstallTimeoutSeconds
        skipBuild = [bool]$SkipBuild
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
    param([string]$FileName)

    $lines = Save-AdbTextArtifact `
        -Name $FileName `
        -FileName $FileName `
        -Arguments @(
            "shell",
            "sh",
            "-c",
            "dumpsys window 2>/dev/null | grep -E 'mCurrentFocus|mFocusedApp|topResumedActivity|mResumedActivity' || true"
        ) `
        -Description "Read foreground window"

    $preferredLineMarkers = @(
        "mCurrentFocus=Window",
        "mFocusedApp=ActivityRecord",
        "mResumedActivity=ActivityRecord",
        "topResumedActivity=ActivityRecord"
    )

    foreach ($marker in $preferredLineMarkers) {
        foreach ($line in $lines) {
            if ($line -like "*$marker*" -and $line -match "([a-zA-Z0-9_.]+)/(?:[a-zA-Z0-9_.$]+)") {
                return $Matches[1]
            }
        }
    }

    foreach ($line in $lines) {
        if ($line -match "([a-zA-Z0-9_.]+)/(?:[a-zA-Z0-9_.$]+)") {
            return $Matches[1]
        }
    }

    return ""
}

function Capture-ProcessMetadata {
    param([string]$FileName)

    Save-AdbTextArtifact `
        -Name $FileName `
        -FileName $FileName `
        -Arguments @(
            "shell",
            "sh",
            "-c",
            "echo Woong pid:; pidof $PackageName || true; echo Chrome pid:; pidof $ChromePackageName || true; echo Matching processes:; ps -A | grep -E '$PackageName|$ChromePackageName' || true"
        ) `
        -Description "Capture process metadata" | Out-Null
}

function Clear-AppSwitchInterference {
    Invoke-AdbChecked -Arguments @(
        "shell",
        "am",
        "force-stop",
        $PackageName
    ) -Description "Force-stop Woong before app-switch QA" | Out-Null
    Invoke-AdbChecked -Arguments @(
        "shell",
        "am",
        "force-stop",
        $ChromePackageName
    ) -Description "Force-stop Chrome before app-switch QA" | Out-Null
    Invoke-AdbChecked -Arguments @(
        "shell",
        "am",
        "broadcast",
        "-a",
        "android.intent.action.CLOSE_SYSTEM_DIALOGS"
    ) -Description "Close system dialogs before app-switch QA" | Out-Null
}

function Start-WoongApp {
    param([switch]$WaitForLaunch)

    $arguments = @(
        "shell",
        "am",
        "start"
    )
    if ($WaitForLaunch) {
        $arguments += "-W"
    }
    $arguments += @(
        "-n",
        "$PackageName/.MainActivity"
    )

    Invoke-AdbChecked -Arguments $arguments -Description "Launch Woong" | Out-Null
}

function Wait-ForWoongForegroundAfterChrome {
    param(
        [int]$MaxAttempts = 5,
        [int]$DelaySeconds = 2
    )

    $foregroundPackage = ""
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt += 1) {
        Start-WoongApp -WaitForLaunch
        Start-Sleep -Seconds $DelaySeconds

        $foregroundPackage = Get-ForegroundPackage -FileName "foreground-after-return.txt"
        if ($foregroundPackage -eq $PackageName) {
            if ($attempt -gt 1) {
                $notes.Add("Woong returned to foreground on attempt $attempt.")
            }
            return $foregroundPackage
        }

        $notes.Add("Woong return attempt $attempt saw foreground package '$foregroundPackage'; retrying explicit Woong launch.")
    }

    return $foregroundPackage
}

function Start-ChromeAboutBlank {
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
}

function Invoke-AppSwitchInstrumentation {
    param(
        [string]$TestName,
        [hashtable]$ExtraArguments
    )

    $arguments = @(
        "shell",
        "am",
        "instrument",
        "-w",
        "-e",
        "class",
        $TestName
    )

    foreach ($key in $ExtraArguments.Keys) {
        $arguments += "-e"
        $arguments += $key
        $arguments += [string]$ExtraArguments[$key]
    }

    $arguments += $testRunner

    Invoke-AdbChecked -Arguments $arguments -Description "Run $TestName instrumentation" | Out-Null
}

function Pull-AppSwitchArtifact {
    param(
        [string]$Name,
        [string]$FileName
    )

    $localPath = Join-Path $runRoot $FileName
    Invoke-AdbChecked -Arguments @(
        "pull",
        "$remoteDir/$FileName",
        $localPath
    ) -Description "Pull $Name" | Out-Null
    Add-Artifact -Name $Name -FileName $FileName -Path $localPath
}

function Test-PackageManagerPreflight {
    $result = Invoke-AdbProcess `
        -Arguments @("shell", "pm", "list", "packages", $PackageName) `
        -Description "Package manager preflight" `
        -TimeoutSeconds $PackageManagerPreflightTimeoutSeconds

    $preflightLines = @(
        "description: Package manager preflight",
        "command: adb $($result.arguments -join ' ')",
        "timeoutSeconds: $PackageManagerPreflightTimeoutSeconds",
        "timedOut: $($result.timedOut)",
        "exitCode: $($result.exitCode)",
        "",
        "stdout:"
    )
    $preflightLines += $result.stdout
    $preflightLines += @(
        "",
        "stderr:"
    )
    $preflightLines += $result.stderr

    Save-TextArtifact `
        -Name "Package manager preflight" `
        -FileName "package-manager-preflight.txt" `
        -Lines $preflightLines

    if ($result.timedOut) {
        $script:packageManagerBlocked = $true
        throw "Package manager preflight timed out after $PackageManagerPreflightTimeoutSeconds seconds. Android package service appears unresponsive; reboot the emulator, wait for boot completion, then rerun app-switch QA."
    }

    if ($result.exitCode -ne 0) {
        $script:packageManagerBlocked = $true
        throw "Package manager preflight failed with adb exit code $($result.exitCode). See package-manager-preflight.txt. Reboot the emulator if package manager commands continue timing out."
    }

    $notes.Add("Package manager preflight completed before install/hash checks.")
}

function Save-InstallDiagnosticArtifacts {
    param(
        [string]$DisplayName,
        [string]$ArtifactPrefix,
        [string[]]$StdoutLines,
        [string[]]$StderrLines
    )

    Save-TextArtifact `
        -Name "$DisplayName install stdout" `
        -FileName "$ArtifactPrefix-stdout.txt" `
        -Lines $StdoutLines
    Save-TextArtifact `
        -Name "$DisplayName install stderr" `
        -FileName "$ArtifactPrefix-stderr.txt" `
        -Lines $StderrLines
}

function Get-InstalledApkSha256 {
    param([string]$InstalledPackageName)

    $hashCommand = "basePath=`$(pm path $InstalledPackageName | head -n 1 | cut -d: -f2); if [ -n `"`$basePath`" ]; then sha256sum `"`$basePath`" 2>/dev/null | cut -d' ' -f1; fi || true"
    try {
        $hashLines = Invoke-AdbChecked `
            -Arguments @("shell", "sh", "-c", $hashCommand) `
            -Description "Read installed APK hash for $InstalledPackageName"
        $hash = @($hashLines | Where-Object { $_ -match "^[a-fA-F0-9]{64}$" } | Select-Object -First 1)
        if ($hash.Count -gt 0) {
            return $hash[0].ToLowerInvariant()
        }
    }
    catch {
        $notes.Add("Unable to compare installed $InstalledPackageName APK hash: $($_.Exception.Message)")
    }

    return ""
}

function Install-ApkIfNeeded {
    param(
        [string]$DisplayName,
        [string]$ArtifactPrefix,
        [string]$ApkPath,
        [string]$InstalledPackageName
    )

    if (-not (Test-Path $ApkPath)) {
        $script:installBlocked = $true
        Save-InstallDiagnosticArtifacts `
            -DisplayName $DisplayName `
            -ArtifactPrefix $ArtifactPrefix `
            -StdoutLines @() `
            -StderrLines @("APK file was not found: $ApkPath")
        throw "Install $DisplayName blocked because APK file was not found: $ApkPath"
    }

    $localHash = (Get-FileHash -Path $ApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $installedHash = Get-InstalledApkSha256 -InstalledPackageName $InstalledPackageName
    if (-not [string]::IsNullOrWhiteSpace($installedHash) -and $installedHash -eq $localHash) {
        Save-InstallDiagnosticArtifacts `
            -DisplayName $DisplayName `
            -ArtifactPrefix $ArtifactPrefix `
            -StdoutLines @("Skipped $DisplayName install because installed APK hash matches $localHash.") `
            -StderrLines @()
        $notes.Add("Skipped $DisplayName install; installed $InstalledPackageName APK hash matches the build artifact.")
        return
    }

    $result = Invoke-AdbProcess `
        -Arguments @("install", "-r", $ApkPath) `
        -Description "Install $DisplayName" `
        -TimeoutSeconds $InstallTimeoutSeconds

    Save-InstallDiagnosticArtifacts `
        -DisplayName $DisplayName `
        -ArtifactPrefix $ArtifactPrefix `
        -StdoutLines $result.stdout `
        -StderrLines $result.stderr

    if ($result.timedOut) {
        $script:installBlocked = $true
        throw "Install $DisplayName timed out after $InstallTimeoutSeconds seconds. See $ArtifactPrefix-stdout.txt and $ArtifactPrefix-stderr.txt. adb $($result.arguments -join ' ')"
    }

    if ($result.exitCode -ne 0) {
        $script:installBlocked = $true
        throw "Install $DisplayName failed with adb exit code $($result.exitCode). See $ArtifactPrefix-stdout.txt and $ArtifactPrefix-stderr.txt. $($result.stderr -join ' ')"
    }

    $notes.Add("Installed $DisplayName with timeout $InstallTimeoutSeconds seconds.")
}

function Save-WoongAppLogcatAfterEvidence {
    $pidResult = Invoke-AdbProcess `
        -Arguments @("shell", "pidof", $PackageName) `
        -Description "Read Woong pid"
    $woongPid = @($pidResult.stdout | Where-Object { $_ -match "^\d+$" } | Select-Object -First 1)

    if (-not $pidResult.timedOut -and $pidResult.exitCode -eq 0 -and $woongPid.Count -gt 0) {
        $logcatResult = Invoke-AdbProcess `
            -Arguments @("logcat", "-d", "-v", "time", "--pid", $woongPid[0]) `
            -Description "Capture Woong app logcat"
        if (-not $logcatResult.timedOut -and $logcatResult.exitCode -eq 0) {
            Save-TextArtifact `
                -Name "Woong app logcat" `
                -FileName "logcat-woong-app.txt" `
                -Lines $logcatResult.stdout
            return
        }

        $notes.Add("WARNING: Woong pid logcat capture failed after core evidence passed; using package-filtered logcat fallback.")
    } else {
        $notes.Add("WARNING: Woong pid was unavailable after core evidence passed; using package-filtered logcat fallback.")
    }

    $fallbackResult = Invoke-AdbProcess `
        -Arguments @("shell", "sh", "-c", "logcat -d -v time | grep -F '$PackageName' || true") `
        -Description "Capture package-filtered Woong logcat fallback"

    $fallbackLines = @(
        "WARNING: Woong pid was unavailable; captured package-filtered logcat fallback.",
        "pidofTimedOut: $($pidResult.timedOut)",
        "pidofExitCode: $($pidResult.exitCode)",
        "",
        "pidof stdout:"
    )
    $fallbackLines += $pidResult.stdout
    $fallbackLines += @(
        "",
        "pidof stderr:"
    )
    $fallbackLines += $pidResult.stderr
    $fallbackLines += @(
        "",
        "fallbackTimedOut: $($fallbackResult.timedOut)",
        "fallbackExitCode: $($fallbackResult.exitCode)",
        "",
        "fallback stdout:"
    )
    $fallbackLines += $fallbackResult.stdout
    $fallbackLines += @(
        "",
        "fallback stderr:"
    )
    $fallbackLines += $fallbackResult.stderr

    if ($fallbackResult.timedOut -or $fallbackResult.exitCode -ne 0) {
        $notes.Add("WARNING: Package-filtered Woong logcat fallback also failed; see logcat-woong-app.txt.")
    }

    Save-TextArtifact `
        -Name "Woong app logcat" `
        -FileName "logcat-woong-app.txt" `
        -Lines $fallbackLines
}

try {
    if (-not $SkipBuild) {
        if (-not (Test-Path $GradleWrapperPath)) {
            throw "Gradle wrapper not found: $GradleWrapperPath"
        }

        Push-Location $androidRoot
        try {
            & $GradleWrapperPath assembleDebug assembleDebugAndroidTest --no-daemon --stacktrace
            if ($LASTEXITCODE -ne 0) { throw "Android debug build failed." }
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
        Write-AppSwitchArtifacts -Status $status -BlockedReason $blockedReason
        Write-Host "No connected Android device. Android app-switch QA is blocked."
        Write-Host "Android app-switch QA artifacts: $runRoot"
        exit 0
    }

    $notes.Add("Detected device(s): $($deviceLines -join '; ')")
    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $notes.Add("Pinned adb device serial: $DeviceSerial")
    }

    Test-PackageManagerPreflight

    if (-not $SkipBuild) {
        $debugApk = Join-Path $androidRoot "app\build\outputs\apk\debug\app-debug.apk"
        $debugAndroidTestApk = Join-Path $androidRoot "app\build\outputs\apk\androidTest\debug\app-debug-androidTest.apk"
        Install-ApkIfNeeded `
            -DisplayName "debug APK" `
            -ArtifactPrefix "install-debug-apk" `
            -ApkPath $debugApk `
            -InstalledPackageName $PackageName
        Install-ApkIfNeeded `
            -DisplayName "debug androidTest APK" `
            -ArtifactPrefix "install-android-test-apk" `
            -ApkPath $debugAndroidTestApk `
            -InstalledPackageName "$PackageName.test"
    } else {
        $notes.Add("Install skipped by -SkipBuild; assuming debug and androidTest APKs are already installed.")
    }

    Invoke-AdbChecked -Arguments @(
        "shell",
        "appops",
        "set",
        $PackageName,
        "GET_USAGE_STATS",
        "allow"
    ) -Description "Grant Usage Access app-op" | Out-Null
    $notes.Add("Granted Usage Access app-op to $PackageName when supported by the device.")

    Clear-AppSwitchInterference
    $notes.Add("Cleared stale Woong/Chrome process state and system dialogs before app-switch QA.")

    Invoke-AppSwitchInstrumentation `
        -TestName $prepareTestName `
        -ExtraArguments @{}

    $collectionFromUtcMillis = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()

    Start-WoongApp
    Start-Sleep -Seconds 2
    Get-ForegroundPackage -FileName "foreground-before.txt" | Out-Null
    Capture-ProcessMetadata -FileName "process-before.txt"

    Start-ChromeAboutBlank
    $notes.Add("Launched Chrome with about:blank. No Chrome screenshots are captured.")
    if ($ChromeForegroundSeconds -gt 0) {
        Start-Sleep -Seconds $ChromeForegroundSeconds
    }
    Get-ForegroundPackage -FileName "foreground-during-chrome.txt" | Out-Null
    Capture-ProcessMetadata -FileName "process-during-chrome.txt"

    $foregroundAfterReturn = Wait-ForWoongForegroundAfterChrome
    Capture-ProcessMetadata -FileName "process-after-return.txt"
    $notes.Add("Foreground package after return: $foregroundAfterReturn")

    if ($foregroundAfterReturn -ne $PackageName) {
        $status = "BLOCKED"
        $blockedReason = "Refusing to capture Woong screenshots because foreground package is '$foregroundAfterReturn', not '$PackageName'."
        Write-AppSwitchArtifacts -Status $status -BlockedReason $blockedReason
        Write-Host $blockedReason
        Write-Host "Android app-switch QA artifacts: $runRoot"
        exit 0
    }

    $collectionToUtcMillis = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()

    Invoke-AppSwitchInstrumentation `
        -TestName $collectTestName `
        -ExtraArguments @{
            fromUtcMillis = $collectionFromUtcMillis
            toUtcMillis = $collectionToUtcMillis
            chromePackageName = $ChromePackageName
        }

    Pull-AppSwitchArtifact -Name "Room assertions" -FileName "room-assertions.json"

    Invoke-AppSwitchInstrumentation `
        -TestName $captureTestName `
        -ExtraArguments @{
            chromePackageName = $ChromePackageName
        }

    Pull-AppSwitchArtifact -Name "Dashboard after app-switch screenshot" -FileName "dashboard-after-app-switch.png"
    Pull-AppSwitchArtifact -Name "Dashboard after app-switch UI hierarchy" -FileName "dashboard-after-app-switch.xml"
    Pull-AppSwitchArtifact -Name "Sessions after app-switch screenshot" -FileName "sessions-after-app-switch.png"
    Pull-AppSwitchArtifact -Name "Sessions after app-switch UI hierarchy" -FileName "sessions-after-app-switch.xml"

    Save-AdbTextArtifact `
        -Name "Crash logcat" `
        -FileName "logcat-crash.txt" `
        -Arguments @("logcat", "-d", "-b", "crash") `
        -Description "Capture crash logcat" | Out-Null

    Save-WoongAppLogcatAfterEvidence

    Save-AdbTextArtifact `
        -Name "Meminfo after app-switch" `
        -FileName "meminfo-after-app-switch.txt" `
        -Arguments @("shell", "dumpsys", "meminfo", $PackageName) `
        -Description "Capture meminfo after app-switch" | Out-Null

    Save-AdbTextArtifact `
        -Name "Gfxinfo after app-switch" `
        -FileName "gfxinfo-after-app-switch.txt" `
        -Arguments @("shell", "dumpsys", "gfxinfo", $PackageName) `
        -Description "Capture gfxinfo after app-switch" | Out-Null

    Write-AppSwitchArtifacts -Status $status -BlockedReason $blockedReason
    Write-Host "Android app-switch QA artifacts: $runRoot"
    exit 0
}
catch {
    $status = if ($installBlocked -or $packageManagerBlocked) { "BLOCKED" } else { "FAIL" }
    $blockedReason = $_.Exception.Message
    $notes.Add("Failure: $blockedReason")
    Write-AppSwitchArtifacts -Status $status -BlockedReason $blockedReason
    if ($installBlocked -or $packageManagerBlocked) {
        Write-Host $blockedReason
        Write-Host "Android app-switch QA artifacts: $runRoot"
        exit 0
    }

    Write-Error $blockedReason
    exit 1
}
