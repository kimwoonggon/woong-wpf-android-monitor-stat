param(
    [string]$AvdName = "Medium_Phone",
    [string]$DeviceSerial = "emulator-5554",
    [string]$AndroidSdkRoot = "",
    [string]$EmulatorPath = "",
    [string]$AdbPath = "",
    [int]$MemoryMb = 4096,
    [int]$TimeoutSeconds = 180,
    [switch]$Restart,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

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

$emulatorArgs = @(
    "-avd", $AvdName,
    "-gpu", "swiftshader_indirect",
    "-memory", $MemoryMb.ToString(),
    "-no-snapshot-load",
    "-no-boot-anim"
)

Write-Host "Stable Android emulator launcher"
Write-Host "AVD: $AvdName"
Write-Host "Serial: $DeviceSerial"
Write-Host "Emulator: $EmulatorPath"
Write-Host "ADB: $AdbPath"
Write-Host "Args: $($emulatorArgs -join ' ')"
Write-Host "Reason: uses software GPU rendering and extra RAM to avoid Chrome/Chromium host GPU instability such as bad color buffer handle or UpdateLayeredWindowIndirect failures."

if ($DryRun) {
    Write-Host "DRY RUN: no emulator was started."
    exit 0
}

& $AdbPath start-server | Out-Host

if ((Test-AdbDeviceReady -Serial $DeviceSerial) -and -not $Restart) {
    Write-Host "Device $DeviceSerial is already running. Pass -Restart to kill and relaunch it with stable flags."
    Wait-ForBoot -Serial $DeviceSerial -Timeout $TimeoutSeconds
    Write-Host "Android emulator is ready: $DeviceSerial"
    exit 0
}

if ($Restart) {
    Write-Host "Restart requested. Attempting to stop $DeviceSerial before launch."
    try {
        Invoke-Adb -Arguments @("-s", $DeviceSerial, "emu", "kill") | Out-Host
        Start-Sleep -Seconds 4
    }
    catch {
        Write-Host "No running emulator was stopped, or the emulator was already offline: $($_.Exception.Message)"
    }
}

Start-Process -FilePath $EmulatorPath -ArgumentList $emulatorArgs -WindowStyle Hidden
Write-Host "Started emulator process. Waiting for Android boot..."

Wait-ForBoot -Serial $DeviceSerial -Timeout $TimeoutSeconds
Write-Host "Android emulator is ready: $DeviceSerial"
