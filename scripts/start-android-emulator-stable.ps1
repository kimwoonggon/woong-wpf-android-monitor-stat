param(
    [string]$AvdName = "Medium_Phone",
    [string]$DeviceSerial = "emulator-5554",
    [string]$AndroidSdkRoot = "",
    [string]$EmulatorPath = "",
    [string]$AdbPath = "",
    [string]$OutputRoot = "",
    [int]$MemoryMb = 4096,
    [string]$GpuMode = "auto",
    [int]$TimeoutSeconds = 180,
    [switch]$Restart,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/android-emulator-stable"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"
$notes = New-Object System.Collections.Generic.List[string]
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

function Write-StableEmulatorArtifacts {
    param(
        [string]$Status,
        [string]$Classification,
        [string]$Message
    )

    $reportLines = @(
        "# Android Stable Emulator Launch Report",
        "",
        "Status: $Status",
        "Classification: $Classification",
        "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
        "Output: ``$runRoot``",
        "",
        "## Launch",
        "",
        "- AVD: ``$AvdName``",
        "- Serial: ``$DeviceSerial``",
        "- GPU mode: ``$GpuMode``",
        "- Memory MB: ``$MemoryMb``",
        "- Timeout seconds: ``$TimeoutSeconds``",
        "- Restart requested: ``$([bool]$Restart)``",
        "- Message: $Message",
        "",
        "## Privacy Boundary",
        "",
        "- This script starts and verifies the Android emulator only.",
        "- It does not capture screenshots, UI hierarchy, typed text, clipboard, messages, passwords, browser contents, or app data.",
        "",
        "## Notes"
    )
    foreach ($note in $notes) {
        $reportLines += "- $note"
    }

    Set-Content -Path (Join-Path $runRoot "report.md") -Value $reportLines -Encoding UTF8

    $manifest = [ordered]@{
        status = $Status
        classification = $Classification
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        output = $runRoot
        avdName = $AvdName
        deviceSerial = $DeviceSerial
        androidSdkRoot = $sdkRoot
        emulatorPath = $EmulatorPath
        adbPath = $AdbPath
        gpuMode = $GpuMode
        memoryMb = $MemoryMb
        timeoutSeconds = $TimeoutSeconds
        restart = [bool]$Restart
        dryRun = [bool]$DryRun
        message = $Message
        emulatorArgs = $emulatorArgs
        notes = $notes
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

    if (Test-Path -LiteralPath $latestRoot) {
        Remove-Item -LiteralPath $latestRoot -Recurse -Force
    }
    Copy-Item -Path $runRoot -Destination $latestRoot -Recurse
}

function Resolve-AndroidSdkRoot {
    param([string]$ExplicitRoot)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        return $ExplicitRoot
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ANDROID_HOME)) {
        return $env:ANDROID_HOME
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ANDROID_SDK_ROOT)) {
        return $env:ANDROID_SDK_ROOT
    }

    return (Join-Path $env:LOCALAPPDATA "Android\Sdk")
}

function Invoke-Adb {
    param([string[]]$Arguments)
    & $script:ResolvedAdbPath @Arguments
}

function Test-AdbDeviceReady {
    param([string]$Serial)

    $devices = Invoke-Adb -Arguments @("devices") 2>$null
    return ($devices -match "^$([regex]::Escape($Serial))\s+device$")
}

function Wait-ForBoot {
    param(
        [string]$Serial,
        [int]$Timeout
    )

    $deadline = (Get-Date).AddSeconds($Timeout)
    while ((Get-Date) -lt $deadline) {
        if (Test-AdbDeviceReady -Serial $Serial) {
            $bootCompleted = (Invoke-Adb -Arguments @("-s", $Serial, "shell", "getprop", "sys.boot_completed") 2>$null | Select-Object -First 1)
            if ($bootCompleted -eq "1") {
                return
            }
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for Android emulator '$Serial' to boot within $Timeout seconds."
}

$sdkRoot = Resolve-AndroidSdkRoot -ExplicitRoot $AndroidSdkRoot

if ([string]::IsNullOrWhiteSpace($EmulatorPath)) {
    $EmulatorPath = Join-Path $sdkRoot "emulator\emulator.exe"
}

if ([string]::IsNullOrWhiteSpace($AdbPath)) {
    $AdbPath = Join-Path $sdkRoot "platform-tools\adb.exe"
}

if (-not (Test-Path -LiteralPath $EmulatorPath)) {
    throw "Android emulator.exe was not found: $EmulatorPath"
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb.exe was not found: $AdbPath"
}

$script:ResolvedAdbPath = $AdbPath

$emulatorArgs = @("-avd", $AvdName)

if (-not [string]::IsNullOrWhiteSpace($GpuMode)) {
    $emulatorArgs += @("-gpu", $GpuMode)
}

if ($MemoryMb -gt 0) {
    $emulatorArgs += @("-memory", $MemoryMb.ToString())
}

$emulatorArgs += @("-no-snapshot-load", "-no-boot-anim")

Write-Host "Stable Android emulator launcher"
Write-Host "AVD: $AvdName"
Write-Host "Serial: $DeviceSerial"
Write-Host "Emulator: $EmulatorPath"
Write-Host "ADB: $AdbPath"
Write-Host "Args: $($emulatorArgs -join ' ')"
Write-Host "Reason: avoids snapshot restore and allows controlled GPU/RAM settings for Chrome/app-switch QA."
$notes.Add("Stable launch args: $($emulatorArgs -join ' ')")
$notes.Add("Reason: avoids snapshot restore and allows controlled GPU/RAM settings for Chrome/app-switch QA.")

if ($DryRun) {
    Write-Host "DRY RUN: no emulator was started."
    Write-StableEmulatorArtifacts -Status "PASS" -Classification "dry-run" -Message "Dry run completed without starting an emulator."
    exit 0
}

try {
    & $AdbPath start-server | Out-Host

    if ((Test-AdbDeviceReady -Serial $DeviceSerial) -and -not $Restart) {
        $message = "Device $DeviceSerial is already running. Pass -Restart to kill and relaunch it with stable flags."
        Write-Host $message
        $notes.Add($message)
        Wait-ForBoot -Serial $DeviceSerial -Timeout $TimeoutSeconds
        Write-Host "Android emulator is ready: $DeviceSerial"
        Write-StableEmulatorArtifacts -Status "PASS" -Classification "already-running" -Message "Android emulator is ready: $DeviceSerial"
        exit 0
    }

    if ($Restart) {
        Write-Host "Restart requested. Attempting to stop $DeviceSerial before launch."
        $notes.Add("Restart requested. Attempting to stop $DeviceSerial before launch.")
        try {
            Invoke-Adb -Arguments @("-s", $DeviceSerial, "emu", "kill") | Out-Host
            Start-Sleep -Seconds 4
        }
        catch {
            $notes.Add("No running emulator was stopped, or the emulator was already offline: $($_.Exception.Message)")
            Write-Host "No running emulator was stopped, or the emulator was already offline: $($_.Exception.Message)"
        }
    }

    Start-Process -FilePath $EmulatorPath -ArgumentList $emulatorArgs -WindowStyle Hidden
    $notes.Add("Started emulator process. Waiting for Android boot.")
    Write-Host "Started emulator process. Waiting for Android boot..."

    Wait-ForBoot -Serial $DeviceSerial -Timeout $TimeoutSeconds
    Write-Host "Android emulator is ready: $DeviceSerial"
    Write-StableEmulatorArtifacts -Status "PASS" -Classification "launched" -Message "Android emulator is ready: $DeviceSerial"
    exit 0
}
catch {
    $message = $_.Exception.Message
    if ($message -like "Timed out waiting for Android emulator*") {
        Write-StableEmulatorArtifacts -Status "BLOCKED" -Classification "boot-timeout" -Message $message
        Write-Host "BLOCKED: $message"
        Write-Host "Android stable emulator artifacts: $runRoot"
        exit 0
    }

    Write-StableEmulatorArtifacts -Status "FAIL" -Classification "script-error" -Message $message
    Write-Error $message
    exit 1
}
