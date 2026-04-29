param(
    [Parameter(Mandatory = $true)]
    [string]$ChromeExtensionId,

    [string]$Configuration = "Release",
    [string]$HostName = "com.woong.monitorstack.chrome",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA "WoongMonitorStack\native-host")
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ChromeExtensionId)) {
    throw "ChromeExtensionId is required. Load the extension first, then copy its extension id from chrome://extensions."
}

$projectPath = Join-Path $RepoRoot "tools\Woong.MonitorStack.ChromeNativeHost\Woong.MonitorStack.ChromeNativeHost.csproj"
$publishDir = Join-Path $InstallRoot "publish"
$manifestPath = Join-Path $InstallRoot "chrome-native-host.json"
$hostExe = Join-Path $publishDir "Woong.MonitorStack.ChromeNativeHost.exe"

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
dotnet publish $projectPath -c $Configuration -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "Failed to publish Woong.MonitorStack.ChromeNativeHost."
}

$manifest = [ordered]@{
    name = $HostName
    description = "Woong Monitor Stack Chrome active-tab metadata native host"
    path = $hostExe
    type = "stdio"
    allowed_origins = @("chrome-extension://$ChromeExtensionId/")
}

New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $manifestPath -Encoding UTF8

$registryPath = "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$HostName"
New-Item -Path $registryPath -Force | Out-Null
Set-Item -Path $registryPath -Value $manifestPath

Write-Host "Registered Chrome native messaging host:"
Write-Host "  Host: $HostName"
Write-Host "  Manifest: $manifestPath"
Write-Host "  Executable: $hostExe"
Write-Host "  Registry: HKCU\Software\Google\Chrome\NativeMessagingHosts\$HostName"
