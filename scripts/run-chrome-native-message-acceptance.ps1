param(
    [string]$ChromePath = "",
    [string]$OutputRoot = "",
    [string]$Configuration = "Debug",
    [int]$TimeoutSeconds = 45,
    [string]$HostName = "com.woong.monitorstack.chrome_test",
    [switch]$DryRun,
    [switch]$CleanupOnly,
    [switch]$HadPreviousValue,
    [string]$PreviousDefaultValue = "",
    [switch]$InstallChromeForTesting,
    [switch]$AllowInstalledChromeFallback
)

$ErrorActionPreference = "Stop"

function Assert-ScopedNativeHostName {
    param([string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name) -or $Name -notmatch '^[a-z0-9_]+(\.[a-z0-9_]+)+$') {
        throw "HostName must be a scoped native messaging host name such as com.woong.monitorstack.chrome_test."
    }
}

Assert-ScopedNativeHostName $HostName

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/chrome-native-acceptance"
}

$hostName = $HostName
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"
$tempWorkRoot = Join-Path ([System.IO.Path]::GetTempPath()) "woong-chrome-native-$timestamp"
$tempProfile = Join-Path $tempWorkRoot "chrome-profile"
$tempExtension = Join-Path $tempWorkRoot "extension"
$fixtureRoot = Join-Path $tempWorkRoot "fixtures"
$nativeInstallRoot = Join-Path $tempWorkRoot "native-host"
$dbPath = Join-Path $runRoot "chrome-native-acceptance.db"
$chromeLogPath = Join-Path $runRoot "chrome.log"
$nativeHostLogPath = Join-Path $runRoot "native-host.log"
$registryPath = "HKCU:\Software\Google\Chrome\NativeMessagingHosts\$hostName"
$previousHostKeyExisted = $false
$previousDefaultValue = $null
$httpServerProcess = $null
$extensionId = ""
$resolvedChromePath = ""
$cleanupStatus = "Not started"
$status = "PASS"
$blockedReason = ""
$notes = New-Object System.Collections.Generic.List[string]
$rawEvents = @()
$webSessions = @()
$outboxRows = @()

function New-DeterministicExtensionKey {
    return "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAq4g4EBie26a1v8wc+/ilMPq4mXqr7qeCDgAOHtNhi3mBcZJxVsdi7lxIR3HndqrQRtEtY6u9EJnctmlJD72CcYj38E8efAWdiWMHi0g6bb5le7ApQpMATDwZJ1QT7Ecayk1H6M0H7c69+p7QIV691lzxZcjsvv1b4lB/7J19ySAmCe0TeBYoYSD2Nb65eB36fB4TFvMnXoSFN9P5iHjgoFc6P8Y4hSkTbPn8W/vsawBD+QkiLpuE3mWf0BpeG7CJudEKsOI7w+zWTMYI36/eeOMHOH15ZjdXphHN682v3Js5s5/YoUw5eTDA+tPXski5thcFax/gl+pKTw3K//E6vQIDAQAB"
}

function Get-ChromiumExtensionId {
    param([string]$PublicKeyBase64)

    $bytes = [Convert]::FromBase64String($PublicKeyBase64)
    $sha = [System.Security.Cryptography.SHA256Managed]::Create()
    $hash = $sha.ComputeHash($bytes)
    $alphabet = "abcdefghijklmnop".ToCharArray()
    $builder = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt 16; $i++) {
        $b = [int]$hash[$i]
        [void]$builder.Append($alphabet[$b -shr 4])
        [void]$builder.Append($alphabet[$b -band 0x0f])
    }

    return $builder.ToString()
}

function Find-Chrome {
    if (-not [string]::IsNullOrWhiteSpace($ChromePath) -and (Test-Path $ChromePath)) {
        return (Resolve-Path $ChromePath).Path
    }

    $repoChromeForTesting = Get-ChildItem `
        -Path (Join-Path $repoRoot ".cache\chrome-for-testing") `
        -Recurse `
        -Filter "chrome.exe" `
        -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*chrome-win64*" } |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if ($repoChromeForTesting) {
        return $repoChromeForTesting.FullName
    }

    if ($InstallChromeForTesting) {
        $installer = Join-Path $repoRoot "scripts\install-chrome-for-testing.ps1"
        $installed = & $installer -RepoRoot $repoRoot | Select-Object -Last 1
        if (-not [string]::IsNullOrWhiteSpace($installed) -and (Test-Path $installed)) {
            return (Resolve-Path $installed).Path
        }
    }

    if ($AllowInstalledChromeFallback) {
        Write-Warning "Using installed Chrome fallback by explicit request. Prefer Chrome for Testing so user Chrome profiles stay outside acceptance."
    } else {
        return $null
    }

    $candidates = @(
        (Join-Path $env:ProgramFiles "Google\Chrome\Application\chrome.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Google\Chrome\Application\chrome.exe"),
        (Join-Path $env:LOCALAPPDATA "Google\Chrome\Application\chrome.exe")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Get-FreeTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try {
        return $listener.LocalEndpoint.Port
    }
    finally {
        $listener.Stop()
    }
}

function Stop-SandboxChromeProcesses {
    param([string]$ProfilePath)

    if ([string]::IsNullOrWhiteSpace($ProfilePath)) {
        return
    }

    $escapedProfile = $ProfilePath.Replace("\", "\\")
    Write-Host "Stopping only sandbox Chrome processes for temp profile: $ProfilePath"
    $processes = @(Get-CimInstance Win32_Process -Filter "Name = 'chrome.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            $_.CommandLine -like "*--user-data-dir=$ProfilePath*" -or
            $_.CommandLine -like "*--user-data-dir=`"$ProfilePath`"*" -or
            $_.CommandLine -like "*--user-data-dir=$escapedProfile*"
        })

    foreach ($process in $processes) {
        try {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
        }
        catch {
            Write-Warning "Could not stop sandbox Chrome process $($process.ProcessId): $($_.Exception.Message)"
        }
    }
}

function Update-CleanupStatusArtifacts {
    param([string]$CleanupStatus)

    if ([string]::IsNullOrWhiteSpace($CleanupStatus)) {
        return
    }

    $reportPath = Join-Path $runRoot "report.md"
    if (Test-Path $reportPath) {
        $report = Get-Content -Path $reportPath
        $updatedReport = @()
        $replaced = $false
        foreach ($line in $report) {
            if ($line -like "- Cleanup status:*") {
                $updatedReport += "- Cleanup status: $CleanupStatus"
                $replaced = $true
            } else {
                $updatedReport += $line
            }
        }

        if (-not $replaced) {
            $updatedReport += "- Cleanup status: $CleanupStatus"
        }

        Set-Content -Path $reportPath -Value $updatedReport -Encoding UTF8
    }

    $manifestPath = Join-Path $runRoot "manifest.json"
    if (Test-Path $manifestPath) {
        $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
        $manifest.cleanupStatus = $CleanupStatus
        $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $manifestPath -Encoding UTF8
    }

    if (Test-Path $latestRoot) {
        Remove-Item -Recurse -Force $latestRoot
    }
    if (Test-Path $runRoot) {
        Copy-Item -Recurse -Path $runRoot -Destination $latestRoot
    }
}

function Invoke-SqliteJson {
    param(
        [string]$DatabasePath,
        [string]$Sql
    )

    $python = @"
import json
import sqlite3
import sys

db_path = sys.argv[1]
sql = sys.argv[2]
connection = sqlite3.connect(db_path)
connection.row_factory = sqlite3.Row
try:
    rows = [dict(row) for row in connection.execute(sql).fetchall()]
    print(json.dumps(rows))
finally:
    connection.close()
"@

    $output = & python -c $python $DatabasePath $Sql
    if ($LASTEXITCODE -ne 0) {
        throw "SQLite query failed: $Sql"
    }

    $parsed = $output | ConvertFrom-Json
    if ($null -eq $parsed) {
        return
    }

    if ($parsed -is [System.Array]) {
        foreach ($item in $parsed) {
            $item
        }
        return
    }

    $parsed
}

function Wait-ForSqliteCondition {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (Test-Path $dbPath) {
            if (& $Condition) {
                return $true
            }
        }

        Start-Sleep -Milliseconds 500
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    return $false
}

function Write-AcceptanceArtifacts {
    param(
        [string]$Status,
        [string]$BlockedReason,
        [string]$ExtensionId,
        [string]$ChromeExecutable,
        [object[]]$RawEvents,
        [object[]]$WebSessions,
        [object[]]$OutboxRows,
        [string]$CleanupStatus
    )

    $report = @(
        "# Chrome Native Messaging Acceptance Report",
        "",
        "Status: $Status",
        "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
        "Output: ``$runRoot``",
        "",
        "## Pipeline",
        "",
        "Chrome Extension -> Native Messaging Host -> SQLite -> Dashboard data source.",
        "",
        "## Privacy Boundary",
        "",
        "- Chrome is launched with a temporary ``--user-data-dir`` sandbox profile for acceptance.",
        "- Cleanup stops only Chrome processes whose command line contains that temporary profile path.",
        "- Existing user Chrome windows/profiles are outside the sandbox and must not be closed.",
        "- Does not use desktop UI automation for Chrome URL/domain tracking.",
        "- Does not scrape Chrome address bars, accessibility trees, screenshots, page contents, forms, messages, passwords, clipboard, typed text, or keystrokes.",
        "- Full URL is not persisted in the default domain-only mode.",
        "",
        "## Result",
        ""
    )

    if (-not [string]::IsNullOrWhiteSpace($BlockedReason)) {
        $report += "- BLOCKED: $BlockedReason"
    } elseif ((@($RawEvents).Count -eq 0) -and (@($WebSessions).Count -eq 0) -and (@($OutboxRows).Count -eq 0)) {
        $report += "- PASS: Safety, dry-run, or cleanup-only path completed without launching Chrome browser metadata ingestion."
    } else {
        $report += "- PASS: Native messaging wrote browser metadata to SQLite."
        $report += "- Raw event domains: $((@($RawEvents) | ForEach-Object { $_.domain }) -join ', ')"
        $report += "- Web session domains: $((@($WebSessions) | ForEach-Object { $_.domain }) -join ', ')"
        $report += "- Outbox rows: $(@($OutboxRows).Count)"
    }

    $report += @(
        "",
        "## Notes"
    )
    foreach ($note in $notes) {
        $report += "- $note"
    }
    $report += "- Cleanup status: $CleanupStatus"

    Set-Content -Path (Join-Path $runRoot "report.md") -Value $report -Encoding UTF8

    $manifest = [ordered]@{
        status = $Status
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        output = $runRoot
        chromePath = $ChromeExecutable
        databasePath = $dbPath
        tempProfilePath = $tempProfile
        chromeLogPath = $chromeLogPath
        nativeHostLogPath = $nativeHostLogPath
        extensionId = $ExtensionId
        nativeHostName = $hostName
        registryPath = $registryPath.Replace("HKCU:", "HKCU")
        rawEvents = @($RawEvents)
        webSessions = @($WebSessions)
        outboxRows = @($OutboxRows)
        blockedReason = $BlockedReason
        cleanupStatus = $CleanupStatus
    }
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

    if (Test-Path $latestRoot) {
        Remove-Item -Recurse -Force $latestRoot
    }
    Copy-Item -Recurse -Path $runRoot -Destination $latestRoot
}

function New-TempExtension {
    param(
        [string]$ExtensionKey
    )

    Copy-Item -Recurse -Path (Join-Path $repoRoot "extensions\chrome") -Destination $tempExtension
    $manifestPath = Join-Path $tempExtension "manifest.json"
    $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
    $manifest | Add-Member -NotePropertyName "key" -NotePropertyValue $ExtensionKey -Force
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $manifestPath -Encoding UTF8

    $backgroundPath = Join-Path $tempExtension "background.js"
    $background = Get-Content -Raw $backgroundPath
    $background = $background.Replace("com.woong.monitorstack.chrome", $hostName)
    Set-Content -Path $backgroundPath -Value $background -Encoding UTF8
}

function New-FixturePages {
    New-Item -ItemType Directory -Force -Path $fixtureRoot | Out-Null
    Set-Content -Path (Join-Path $fixtureRoot "github.html") -Encoding UTF8 -Value @"
<!doctype html>
<html><head><title>GitHub fixture</title></head><body>GitHub fixture</body></html>
"@
    Set-Content -Path (Join-Path $fixtureRoot "chatgpt.html") -Encoding UTF8 -Value @"
<!doctype html>
<html><head><title>ChatGPT fixture</title></head><body>ChatGPT fixture</body></html>
"@
    Set-Content -Path (Join-Path $fixtureRoot "docs.html") -Encoding UTF8 -Value @"
<!doctype html>
<html><head><title>Docs fixture</title></head><body>Docs fixture</body></html>
"@
}

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

try {
    Write-Host "Chrome native messaging acceptance observes active-tab metadata only."
    Write-Host "It will not record keystrokes, page contents, screenshots, forms, passwords, messages, or clipboard contents."
    Write-Host "Chrome for Testing is required by default so user Chrome windows/profiles stay outside this acceptance sandbox."
    Write-Host "Official Google Chrome stable builds can also block command-line unpacked extension loading."

    $resolvedChromePath = Find-Chrome
    if ([string]::IsNullOrWhiteSpace($resolvedChromePath)) {
        $status = "BLOCKED"
        $blockedReason = "Chrome for Testing executable was not found."
        $notes.Add("Run scripts/install-chrome-for-testing.ps1, pass -InstallChromeForTesting, or pass -ChromePath explicitly.")
        $notes.Add("Use -AllowInstalledChromeFallback only for isolated manual debugging; default acceptance must not touch the user's installed Chrome.")
        Write-AcceptanceArtifacts $status $blockedReason "" "" @() @() @() $cleanupStatus
        Write-Host "Chrome native messaging acceptance blocked: $blockedReason"
        exit 0
    }

    if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
        $status = "BLOCKED"
        $blockedReason = "Python was not found; Python is used only for the local fixture HTTP server and SQLite assertions."
        $notes.Add("Install Python or add a PowerShell-only fixture server/query implementation.")
        Write-AcceptanceArtifacts $status $blockedReason "" $resolvedChromePath @() @() @() $cleanupStatus
        Write-Host "Chrome native messaging acceptance blocked: $blockedReason"
        exit 0
    }

    if ($CleanupOnly) {
        & (Join-Path $repoRoot "scripts\uninstall-chrome-native-host.ps1") `
            -Browser Chrome `
            -HostName $hostName `
            -HadPreviousValue:$HadPreviousValue `
            -PreviousDefaultValue $PreviousDefaultValue `
            -DryRun:$DryRun
        Write-AcceptanceArtifacts "PASS" "" "" "" @() @() @() $cleanupStatus
        Write-Host "Chrome native messaging cleanup-only run completed."
        exit 0
    }

    $previousHostKeyExisted = Test-Path $registryPath
    if ($previousHostKeyExisted) {
        $previousDefaultValue = (Get-Item -Path $registryPath).GetValue("")
    }

    Write-Host "Native host test key: $($registryPath.Replace('HKCU:', 'HKCU'))"
    Write-Host "Previous key existed: $previousHostKeyExisted"

    $extensionKey = New-DeterministicExtensionKey
    $extensionId = Get-ChromiumExtensionId $extensionKey
    $allowed_origins = @("chrome-extension://$extensionId/")
    New-TempExtension $extensionKey
    New-FixturePages

    $installScript = Join-Path $repoRoot "scripts\install-chrome-native-host.ps1"
    & $installScript `
        -Browser Chrome `
        -ChromeExtensionId $extensionId `
        -Configuration $Configuration `
        -HostName $hostName `
        -RepoRoot $repoRoot `
        -InstallRoot $nativeInstallRoot `
        -DryRun:$DryRun
    if (-not $?) {
        throw "Native host registration failed."
    }

    if ($DryRun) {
        $notes.Add("Dry run completed without launching Chrome or writing HKCU registry values.")
        Write-AcceptanceArtifacts "PASS" "" $extensionId $resolvedChromePath @() @() @() $cleanupStatus
        Write-Host "Chrome native messaging acceptance dry run completed."
        exit 0
    }

    $env:WOONG_MONITOR_LOCAL_DB = $dbPath
    $env:WOONG_MONITOR_DEVICE_ID = "windows-chrome-acceptance"
    $env:WOONG_MONITOR_NATIVE_HOST_FOCUS_SESSION_ID = "chrome-native-acceptance-focus"
    $env:WOONG_MONITOR_REQUIRE_EXPLICIT_DB = "1"
    $env:WOONG_MONITOR_NATIVE_HOST_LOG = $nativeHostLogPath

    $port = Get-FreeTcpPort
    $httpServerProcess = Start-Process `
        -FilePath "python" `
        -ArgumentList @("-m", "http.server", "$port", "--bind", "127.0.0.1") `
        -WorkingDirectory $fixtureRoot `
        -PassThru `
        -WindowStyle Hidden
    Start-Sleep -Seconds 1

    $githubUrl = "http://github.example:$port/github.html"
    $chatgptUrl = "http://chatgpt.example:$port/chatgpt.html"
    $docsUrl = "http://docs.example:$port/docs.html"
    $resolverRules = "MAP github.example 127.0.0.1,MAP chatgpt.example 127.0.0.1,MAP docs.example 127.0.0.1"
    $quotedResolverRules = "--host-resolver-rules=`"$resolverRules`""

    $chromeArgs = @(
        "--user-data-dir=$tempProfile",
        "--no-first-run",
        "--no-default-browser-check",
        "--disable-background-networking",
        "--enable-logging",
        "--v=1",
        "--log-file=$chromeLogPath",
        "--disable-extensions-except=$tempExtension",
        "--load-extension=$tempExtension",
        $quotedResolverRules,
        "--new-window",
        $githubUrl
    )
    Start-Process -FilePath $resolvedChromePath -ArgumentList $chromeArgs | Out-Null
    Start-Sleep -Seconds 4
    Start-Process -FilePath $resolvedChromePath -ArgumentList @("--user-data-dir=$tempProfile", $chatgptUrl) | Out-Null
    Start-Sleep -Seconds 4
    Start-Process -FilePath $resolvedChromePath -ArgumentList @("--user-data-dir=$tempProfile", $docsUrl) | Out-Null

    $conditionMet = Wait-ForSqliteCondition -TimeoutSeconds $TimeoutSeconds -Condition {
        $raw = @(Invoke-SqliteJson $dbPath "SELECT domain, url, title FROM browser_raw_event WHERE domain IN ('github.example', 'chatgpt.example') ORDER BY observed_at_utc")
        $web = @(Invoke-SqliteJson $dbPath "SELECT domain, url, duration_ms FROM web_session WHERE domain IN ('github.example', 'chatgpt.example') ORDER BY started_at_utc")
        $outbox = @(Invoke-SqliteJson $dbPath "SELECT aggregate_type, payload_json FROM sync_outbox WHERE aggregate_type = 'web_session'")
        return ($raw.Count -ge 2) -and ($web.Count -ge 2) -and ($outbox.Count -ge 2)
    }

    if (-not $conditionMet) {
        throw "Timed out waiting for Chrome native messages to reach SQLite."
    }

    $rawEvents = @(Invoke-SqliteJson $dbPath "SELECT domain, url, title FROM browser_raw_event WHERE domain IN ('github.example', 'chatgpt.example') ORDER BY observed_at_utc")
    $webSessions = @(Invoke-SqliteJson $dbPath "SELECT domain, url, duration_ms FROM web_session WHERE domain IN ('github.example', 'chatgpt.example') ORDER BY started_at_utc")
    $outboxRows = @(Invoke-SqliteJson $dbPath "SELECT aggregate_type, payload_json FROM sync_outbox WHERE aggregate_type = 'web_session' ORDER BY created_at_utc")

    if (@($rawEvents | Where-Object { $_.url -ne $null }).Count -gt 0) {
        throw "Full URL is not persisted in domain-only raw events."
    }
    if (@($webSessions | Where-Object { $_.url -ne $null }).Count -gt 0) {
        throw "Full URL is not persisted in domain-only web sessions."
    }
    foreach ($row in $outboxRows) {
        if ($row.payload_json -match "github.html|chatgpt.html|\?") {
            throw "Outbox payload leaked a path/query even though full URL is off."
        }
    }

    $notes.Add("Deterministic extension id: $extensionId")
    $notes.Add("Fixture domains: github.example, chatgpt.example, docs.example")
    $notes.Add("SQLite assertions prove native host persistence; no browser UI scraping was used.")
    Write-AcceptanceArtifacts $status "" $extensionId $resolvedChromePath $rawEvents $webSessions $outboxRows $cleanupStatus
    Write-Host "Chrome native messaging acceptance passed."
    Write-Host "Artifacts: $runRoot"
}
catch {
    $status = "FAIL"
    $blockedReason = $_.Exception.Message
    $notes.Add("Failure: $blockedReason")
    $rawEvents = if (Test-Path $dbPath) { @(Invoke-SqliteJson $dbPath "SELECT domain, url, title FROM browser_raw_event ORDER BY observed_at_utc") } else { @() }
    $webSessions = if (Test-Path $dbPath) { @(Invoke-SqliteJson $dbPath "SELECT domain, url, duration_ms FROM web_session ORDER BY started_at_utc") } else { @() }
    $outboxRows = if (Test-Path $dbPath) { @(Invoke-SqliteJson $dbPath "SELECT aggregate_type, payload_json FROM sync_outbox ORDER BY created_at_utc") } else { @() }
    Write-AcceptanceArtifacts $status $blockedReason $extensionId $resolvedChromePath $rawEvents $webSessions $outboxRows $cleanupStatus
    Write-Error $blockedReason
    exit 1
}
finally {
    try {
        Stop-SandboxChromeProcesses $tempProfile
    } catch {}

    try {
        if ($httpServerProcess -and -not $httpServerProcess.HasExited) {
            Stop-Process -Id $httpServerProcess.Id -Force -ErrorAction SilentlyContinue
        }
    } catch {}

    try {
        & (Join-Path $repoRoot "scripts\uninstall-chrome-native-host.ps1") `
            -Browser Chrome `
            -HostName $hostName `
            -HadPreviousValue:$previousHostKeyExisted `
            -PreviousDefaultValue "$previousDefaultValue" `
            -DryRun:$DryRun | Out-Null
        $cleanupStatus = if ($previousHostKeyExisted) {
            "Restored previous default value for $($registryPath.Replace('HKCU:', 'HKCU'))."
        } else {
            "Removed only scoped test host key $($registryPath.Replace('HKCU:', 'HKCU'))."
        }
    } catch {
        $cleanupStatus = "Cleanup failed for $($registryPath.Replace('HKCU:', 'HKCU')): $($_.Exception.Message)"
        Write-Warning "Chrome native messaging acceptance cleanup failed for $($registryPath.Replace('HKCU:', 'HKCU')): $($_.Exception.Message)"
        Write-Warning "Manual cleanup command: powershell -ExecutionPolicy Bypass -File scripts\uninstall-chrome-native-host.ps1 -Browser Chrome -HostName $hostName"
    }

    try {
        if (Test-Path $tempProfile) {
            Remove-Item -Recurse -Force $tempProfile
        }
    } catch {}

    try {
        if (Test-Path $tempWorkRoot) {
            Remove-Item -Recurse -Force $tempWorkRoot
        }
    } catch {}

    Remove-Item Env:WOONG_MONITOR_LOCAL_DB -ErrorAction SilentlyContinue
    Remove-Item Env:WOONG_MONITOR_DEVICE_ID -ErrorAction SilentlyContinue
    Remove-Item Env:WOONG_MONITOR_NATIVE_HOST_FOCUS_SESSION_ID -ErrorAction SilentlyContinue
    Remove-Item Env:WOONG_MONITOR_REQUIRE_EXPLICIT_DB -ErrorAction SilentlyContinue
    Remove-Item Env:WOONG_MONITOR_NATIVE_HOST_LOG -ErrorAction SilentlyContinue

    try {
        Update-CleanupStatusArtifacts $cleanupStatus
    } catch {
        Write-Warning "Could not update Chrome acceptance cleanup status artifacts: $($_.Exception.Message)"
    }
}
