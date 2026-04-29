[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$Channel = "Stable"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

$cacheRoot = Join-Path $RepoRoot ".cache\chrome-for-testing"
$metadataUrl = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json"

New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

$metadata = Invoke-RestMethod -Uri $metadataUrl
$channelData = $metadata.channels.$Channel
if ($null -eq $channelData) {
    throw "Chrome for Testing channel '$Channel' was not found."
}

$download = @($channelData.downloads.chrome | Where-Object { $_.platform -eq "win64" })[0]
if ($null -eq $download -or [string]::IsNullOrWhiteSpace($download.url)) {
    throw "Chrome for Testing win64 download URL was not found for channel '$Channel'."
}

$version = $channelData.version
$versionRoot = Join-Path $cacheRoot $version
$chromeExe = Join-Path $versionRoot "chrome-win64\chrome.exe"
if (Test-Path $chromeExe) {
    Write-Output (Resolve-Path $chromeExe).Path
    return
}

$zipPath = Join-Path $cacheRoot "chrome-win64-$version.zip"
Write-Host "Downloading Chrome for Testing $version from $($download.url)"
Invoke-WebRequest -Uri $download.url -OutFile $zipPath

$extractRoot = Join-Path $cacheRoot "extract-$version"
if (Test-Path $extractRoot) {
    Remove-Item -Recurse -Force $extractRoot
}

Expand-Archive -Path $zipPath -DestinationPath $extractRoot -Force
New-Item -ItemType Directory -Force -Path $versionRoot | Out-Null
Move-Item -Path (Join-Path $extractRoot "chrome-win64") -Destination $versionRoot -Force
Remove-Item -Recurse -Force $extractRoot

if (-not (Test-Path $chromeExe)) {
    throw "Chrome for Testing install did not produce $chromeExe."
}

Write-Output (Resolve-Path $chromeExe).Path
