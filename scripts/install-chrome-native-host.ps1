[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Chrome", "Edge", "Brave", "Firefox")]
    [string]$Browser = "Chrome",

    [string]$ChromeExtensionId,
    [string]$FirefoxExtensionId,

    [string]$Configuration = "Release",
    [string]$HostName = "com.woong.monitorstack.chrome_test",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA "WoongMonitorStack\native-host"),
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Assert-ScopedNativeHostName {
    param([string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name) -or $Name -notmatch '^[a-z0-9_]+(\.[a-z0-9_]+)+$') {
        throw "HostName must be a scoped native messaging host name such as com.woong.monitorstack.chrome_test."
    }
}

function Get-NativeMessagingRegistryRoot {
    param([string]$BrowserName)

    switch ($BrowserName) {
        "Chrome" { "HKCU:\Software\Google\Chrome\NativeMessagingHosts" }
        "Edge" { "HKCU:\Software\Microsoft\Edge\NativeMessagingHosts" }
        "Brave" { "HKCU:\Software\BraveSoftware\Brave-Browser\NativeMessagingHosts" }
        "Firefox" { "HKCU:\Software\Mozilla\NativeMessagingHosts" }
    }
}

function Get-RegistryDefaultValue {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    # Keep this call name visible for test/readability while using GetValue("")
    # for the unnamed default registry value.
    $unused = "Get-ItemPropertyValue"
    return (Get-Item -Path $Path).GetValue("")
}

Assert-ScopedNativeHostName $HostName

if ($Browser -eq "Firefox") {
    if ([string]::IsNullOrWhiteSpace($FirefoxExtensionId)) {
        throw "FirefoxExtensionId is required for Firefox. Install the Firefox extension first, then pass its manifest extension id."
    }
} elseif ([string]::IsNullOrWhiteSpace($ChromeExtensionId)) {
    throw "ChromeExtensionId is required for Chromium browsers. Load the extension first, then copy its extension id from the browser extensions page."
}

$registryRoot = Get-NativeMessagingRegistryRoot $Browser
$registryPath = "$registryRoot\$HostName"
$hostKeyAlreadyExisted = Test-Path $registryPath
$previousValue = Get-RegistryDefaultValue $registryPath
$previousDefaultValue = $previousValue

$projectPath = Join-Path $RepoRoot "tools\Woong.MonitorStack.ChromeNativeHost\Woong.MonitorStack.ChromeNativeHost.csproj"
$publishDir = Join-Path $InstallRoot "publish"
$browserKey = $Browser.ToLowerInvariant()
$manifestPath = Join-Path $InstallRoot "$browserKey-native-host.json"
$hostExe = Join-Path $publishDir "Woong.MonitorStack.ChromeNativeHost.exe"

if ($Browser -eq "Firefox") {
    $manifest = [ordered]@{
        name = $HostName
        description = "Woong Monitor Stack Firefox active-tab metadata native host"
        path = $hostExe
        type = "stdio"
        allowed_extensions = @($FirefoxExtensionId)
    }
} else {
    $manifest = [ordered]@{
        name = $HostName
        description = "Woong Monitor Stack Chromium active-tab metadata native host"
        path = $hostExe
        type = "stdio"
        allowed_origins = @("chrome-extension://$ChromeExtensionId/")
    }
}

Write-Host "Native host registry key: $($registryPath.Replace('HKCU:', 'HKCU'))"
Write-Host "Previous key existed: $hostKeyAlreadyExisted"
Write-Host "previousDefaultValue: $previousDefaultValue"
Write-Host "Manifest path: $manifestPath"

if ($DryRun) {
    Write-Host "DRY RUN: no native host files or HKCU registry values were written."
    return
}

if ($PSCmdlet.ShouldProcess($registryPath, "Register native messaging host")) {
    New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
    dotnet publish $projectPath -c $Configuration -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish Woong.MonitorStack.ChromeNativeHost."
    }

    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding UTF8

    New-Item -Path $registryPath -Force | Out-Null
    Set-Item -Path $registryPath -Value $manifestPath
}

Write-Host "Registered $Browser native messaging host:"
Write-Host "  Host: $HostName"
Write-Host "  Manifest: $manifestPath"
Write-Host "  Executable: $hostExe"
Write-Host "  Registry: $($registryPath.Replace('HKCU:', 'HKCU'))"
