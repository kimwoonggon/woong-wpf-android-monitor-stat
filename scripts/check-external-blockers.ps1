param(
    [string]$OutputRoot = "",
    [string]$AdbPath = "",
    [string]$DockerPath = "docker"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/external-blockers"
}
if ([string]::IsNullOrWhiteSpace($AdbPath)) {
    $sdkAdb = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
    $AdbPath = if (Test-Path $sdkAdb) { $sdkAdb } else { "adb" }
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null
New-Item -ItemType Directory -Force -Path $latestRoot | Out-Null

$notes = New-Object System.Collections.Generic.List[string]
$physicalAndroidDeviceReady = $false
$dockerDaemonReady = $false
$adbOutputText = ""
$dockerOutputText = ""

# adb devices -l
try {
    $adbOutput = & $AdbPath devices -l 2>&1
    $adbExitCode = $LASTEXITCODE
    $adbOutputText = ($adbOutput -join "`n").Trim()
    if ($adbExitCode -ne 0) {
        $notes.Add("adb devices -l failed with exit code $adbExitCode.")
    } else {
        $deviceLines = @($adbOutput | Where-Object {
            $_ -match "\bdevice\b" -and $_ -notmatch "^List of devices attached"
        })
        $physicalLines = @($deviceLines | Where-Object {
            $_ -notmatch "^emulator-" -and $_ -notmatch "\bmodel:.*Emulator\b"
        })
        $physicalAndroidDeviceReady = $physicalLines.Count -gt 0
        if ($physicalAndroidDeviceReady) {
            $notes.Add("Physical Android device detected: $($physicalLines -join '; ')")
        } elseif ($deviceLines.Count -gt 0) {
            $notes.Add("Only emulator Android device(s) detected: $($deviceLines -join '; ')")
            $notes.Add("Connect a physical Android device before closing the physical-device resource measurement TODO.")
        } else {
            $notes.Add("No Android device or emulator detected.")
        }
    }
} catch {
    $adbOutputText = $_.Exception.Message
    $notes.Add("adb devices -l failed: $adbOutputText")
}

# docker ps
try {
    $dockerOutput = & $DockerPath ps 2>&1
    $dockerExitCode = $LASTEXITCODE
    $dockerOutputText = ($dockerOutput -join "`n").Trim()
    $dockerDaemonReady = $dockerExitCode -eq 0
    if ($dockerDaemonReady) {
        $notes.Add("Docker daemon is reachable via docker ps.")
    } else {
        $notes.Add("Docker daemon unavailable via docker ps. Output: $dockerOutputText")
    }
} catch {
    $dockerOutputText = $_.Exception.Message
    $notes.Add("Docker daemon unavailable via docker ps. Output: $dockerOutputText")
}

$status = if ($physicalAndroidDeviceReady -and $dockerDaemonReady) { "PASS" } else { "BLOCKED" }

$reportLines = @(
    "# External Blocker Check",
    "",
    "Status: $status",
    "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
    "Output: ``$runRoot``",
    "",
    "## Checks",
    "",
    "| Check | Result |",
    "| --- | --- |",
    "| Physical Android device for physical-device resource measurement | $(if ($physicalAndroidDeviceReady) { 'PASS' } else { 'BLOCKED' }) |",
    "| Docker daemon for PostgreSQL/Testcontainers validation | $(if ($dockerDaemonReady) { 'PASS' } else { 'BLOCKED' }) |",
    "",
    "## Notes"
)
foreach ($note in $notes) {
    $reportLines += "- $note"
}

Set-Content -Path (Join-Path $runRoot "report.md") -Value $reportLines -Encoding UTF8

$manifest = [ordered]@{
    status = $status
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    output = $runRoot
    adbPath = $AdbPath
    dockerPath = $DockerPath
    physicalAndroidDeviceReady = $physicalAndroidDeviceReady
    dockerDaemonReady = $dockerDaemonReady
    adbDevicesOutput = $adbOutputText
    dockerPsOutput = $dockerOutputText
    notes = $notes
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

Copy-Item -Path (Join-Path $runRoot "report.md") -Destination (Join-Path $latestRoot "report.md") -Force
Copy-Item -Path (Join-Path $runRoot "manifest.json") -Destination (Join-Path $latestRoot "manifest.json") -Force

Write-Host "External blocker check artifacts: $runRoot"
Write-Host "Status: $status"
exit 0
