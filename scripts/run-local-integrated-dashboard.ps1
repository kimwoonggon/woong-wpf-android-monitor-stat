param(
    [switch]$Help,
    [switch]$DryRun,
    [switch]$SkipAndroidPull,
    [switch]$SkipWindows,
    [switch]$SkipAndroid,
    [switch]$NoOpenBrowser,
    [int]$Port = 5087,
    [string]$UserId = "local-user",
    [string]$TimezoneId = [TimeZoneInfo]::Local.Id,
    [string]$WindowsDb = "",
    [string]$AndroidPackage = "com.woong.monitorstack",
    [string]$AndroidDb = "",
    [string]$OutputRoot = "",
    [int]$BridgeIntervalSeconds = -1,
    [int]$BridgeMaxIterations = 0,
    [string]$BridgeCheckpointPath = "",
    [switch]$RefreshAndroidDbEachBridgeIteration
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$startPostgresScript = Join-Path $repoRoot "scripts\start-server-postgres.ps1"
$bridgeProject = Join-Path $repoRoot "tools\Woong.MonitorStack.LocalDashboardBridge\Woong.MonitorStack.LocalDashboardBridge.csproj"
$serverProject = Join-Path $repoRoot "src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj"
$connectionString = "Host=localhost;Port=55432;Database=woong_monitor;Username=woong;Password=woong_dev_password"
$baseUrl = "http://127.0.0.1:$Port"
$today = Get-Date -Format "yyyy-MM-dd"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts\local-integrated-dashboard\$timestamp"
}

if ([string]::IsNullOrWhiteSpace($WindowsDb)) {
    $WindowsDb = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "WoongMonitorStack\windows-local.db"
}

if ([string]::IsNullOrWhiteSpace($AndroidDb)) {
    $AndroidDb = Join-Path $OutputRoot "android-emulator\woong-monitor.db"
}

$bridgeCheckpointPathWasProvided = $BridgeCheckpointPath.Length -gt 0
if ($bridgeCheckpointPathWasProvided -and [string]::IsNullOrWhiteSpace($BridgeCheckpointPath)) {
    throw "-BridgeCheckpointPath must not be empty or whitespace."
}

if (!$bridgeCheckpointPathWasProvided -and $BridgeIntervalSeconds -ge 0) {
    $BridgeCheckpointPath = Join-Path $OutputRoot "bridge-checkpoints.json"
}

if ($BridgeIntervalSeconds -lt -1) {
    throw "-BridgeIntervalSeconds must be zero or greater when specified."
}

if ($BridgeMaxIterations -lt 0) {
    throw "-BridgeMaxIterations must be zero or greater. Use 0 for continuous bridge polling."
}

if ($BridgeMaxIterations -gt 0 -and $BridgeIntervalSeconds -lt 0) {
    throw "-BridgeMaxIterations requires -BridgeIntervalSeconds because the default bridge upload mode is one-shot."
}

if ($BridgeIntervalSeconds -eq 0 -and $BridgeMaxIterations -eq 0) {
    throw "-BridgeIntervalSeconds 0 requires -BridgeMaxIterations to avoid an unbounded zero-delay bridge loop."
}

if ($RefreshAndroidDbEachBridgeIteration -and $BridgeIntervalSeconds -lt 0) {
    throw "-RefreshAndroidDbEachBridgeIteration requires -BridgeIntervalSeconds."
}

if ($RefreshAndroidDbEachBridgeIteration -and $BridgeMaxIterations -le 0) {
    throw "-RefreshAndroidDbEachBridgeIteration requires -BridgeMaxIterations greater than zero."
}

if ($RefreshAndroidDbEachBridgeIteration -and $SkipAndroid) {
    throw "-RefreshAndroidDbEachBridgeIteration cannot be used with -SkipAndroid."
}

if ($RefreshAndroidDbEachBridgeIteration -and $SkipAndroidPull) {
    throw "-RefreshAndroidDbEachBridgeIteration cannot be used with -SkipAndroidPull."
}

$scriptManagedBridgeLoop = [bool]$RefreshAndroidDbEachBridgeIteration

function Write-Usage {
    Write-Host "Usage: powershell -ExecutionPolicy Bypass -File scripts\run-local-integrated-dashboard.ps1"
    Write-Host ""
    Write-Host "Starts local Docker PostgreSQL and ASP.NET Core, uploads local WPF SQLite and Android emulator Room metadata through API DTOs, then opens Blazor /dashboard."
    Write-Host ""
    Write-Host "Useful options:"
    Write-Host "  -SkipAndroidPull    Use an existing -AndroidDb file instead of pulling from emulator with adb."
    Write-Host "  -SkipWindows        Do not upload WPF SQLite data."
    Write-Host "  -SkipAndroid        Do not upload Android Room data."
    Write-Host "  -NoOpenBrowser      Print the dashboard URL without opening it."
    Write-Host "  -DryRun             Print planned commands only."
    Write-Host "  -BridgeIntervalSeconds <seconds>"
    Write-Host "                      Run bridge uploads repeatedly at this interval instead of one-shot."
    Write-Host "  -BridgeMaxIterations <count>"
    Write-Host "                      Stop repeated bridge uploads after this many iterations; omit for continuous mode."
    Write-Host "  -BridgeCheckpointPath <path>"
    Write-Host "                      Pass a metadata-only checkpoint file to the bridge; interval mode defaults to OutputRoot\bridge-checkpoints.json."
    Write-Host "  -RefreshAndroidDbEachBridgeIteration"
    Write-Host "                      In bounded interval mode, pull the Android emulator Room DB before each one-shot bridge run."
}

function Write-Step([string]$Message) {
    Write-Host "[local-dashboard] $Message"
}

function Test-ServerReady {
    try {
        Invoke-RestMethod -Method Get -Uri "$baseUrl/api/dashboard/integrated?userId=health-check&from=$today&to=$today&timezoneId=UTC" | Out-Null
        return $true
    } catch {
        return $false
    }
}

function Wait-Server {
    $deadline = (Get-Date).AddSeconds(90)
    do {
        if (Test-ServerReady) {
            return
        }

        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "Server did not become reachable at $baseUrl."
}

function Get-BridgePollingArguments {
    $arguments = @()

    if ($BridgeIntervalSeconds -ge 0 -and !$scriptManagedBridgeLoop) {
        $arguments += @("--intervalSeconds", $BridgeIntervalSeconds.ToString([Globalization.CultureInfo]::InvariantCulture))

        if ($BridgeMaxIterations -gt 0) {
            $arguments += @("--maxIterations", $BridgeMaxIterations.ToString([Globalization.CultureInfo]::InvariantCulture))
        }
    }

    if (![string]::IsNullOrWhiteSpace($BridgeCheckpointPath)) {
        $arguments += @("--checkpointPath", $BridgeCheckpointPath)
    }

    return $arguments
}

function Get-BridgePollingDescription {
    if ($scriptManagedBridgeLoop) {
        return "script-managed bridge loop every $BridgeIntervalSeconds second(s), up to $BridgeMaxIterations iteration(s)"
    }

    if ($BridgeIntervalSeconds -lt 0) {
        return "one-shot bridge upload"
    }

    if ($BridgeMaxIterations -gt 0) {
        return "bridge uploads every $BridgeIntervalSeconds second(s), up to $BridgeMaxIterations iteration(s)"
    }

    return "bridge uploads every $BridgeIntervalSeconds second(s) until stopped"
}

function Get-BridgeArguments {
    $arguments = @(
        "run",
        "--project",
        $bridgeProject,
        "--",
        "--server",
        $baseUrl,
        "--userId",
        $UserId,
        "--timezoneId",
        $TimezoneId
    )
    $arguments += $bridgePollingArgs

    if (!$SkipWindows -and (Test-Path $WindowsDb)) {
        $arguments += @("--windowsDb", $WindowsDb)
    } elseif (!$SkipWindows) {
        Write-Step "WPF SQLite not found at $WindowsDb. Skipping Windows upload."
    }

    if (!$SkipAndroid -and (Test-Path $AndroidDb)) {
        $arguments += @("--androidDb", $AndroidDb)
    } elseif (!$SkipAndroid) {
        Write-Step "Android Room DB not found at $AndroidDb. Skipping Android upload."
    }

    return $arguments
}

function Pull-AndroidDatabase([string]$PackageName, [string]$DestinationPath) {
    $destinationDirectory = Split-Path -Parent $DestinationPath
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null

    Write-Step "Pulling Android emulator Room DB from $PackageName to $DestinationPath"
    $escapedDestination = $DestinationPath.Replace('"', '\"')
    $command = "adb exec-out run-as $PackageName cat databases/woong-monitor.db > `"$escapedDestination`""
    cmd.exe /c $command
    if ($LASTEXITCODE -ne 0) {
        throw "adb failed to pull Android emulator Room database. Make sure the emulator is running and the debug app is installed."
    }
    if (!(Test-Path $DestinationPath)) {
        throw "adb completed but '$DestinationPath' was not created."
    }
}

function Get-IntegratedDashboardPlatformPresence([object]$Snapshot, [string]$Platform) {
    $currentApps = @($Snapshot.currentApps | Where-Object { $_.platform -eq $Platform })
    $devices = @($Snapshot.devices | Where-Object { $_.platform -eq $Platform })
    $platformTotals = @($Snapshot.platformTotals | Where-Object { $_.platform -eq $Platform })
    $platformUsage = @($Snapshot.platformUsage | Where-Object { $_.platform -eq $Platform })
    [Int64]$durationMs = 0

    foreach ($total in $platformTotals + $platformUsage) {
        foreach ($propertyName in @("activeMs", "idleMs", "webMs")) {
            if ($null -ne $total.$propertyName) {
                $durationMs += [Int64]$total.$propertyName
            }
        }
    }

    [pscustomobject]@{
        DataPresent = ($currentApps.Count -gt 0) -or ($durationMs -gt 0)
        CurrentAppCount = $currentApps.Count
        DeviceCount = $devices.Count
        UsageDurationMs = $durationMs
    }
}

function Test-IntegratedDashboardDataPresence(
    [string]$ApiUrl,
    [bool]$RequireWindows,
    [bool]$RequireAndroid) {
    Write-Step "Verifying integrated dashboard data presence"

    $messages = [System.Collections.Generic.List[string]]::new()

    try {
        $snapshot = Invoke-RestMethod -Method Get -Uri $ApiUrl
    } catch {
        $messages.Add("Integrated dashboard API request failed: $($_.Exception.Message)")

        return [pscustomobject]@{
            Succeeded = $false
            ApiUrl = $ApiUrl
            WindowsStatus = if ($RequireWindows) { "missing" } else { "skipped" }
            AndroidStatus = if ($RequireAndroid) { "missing" } else { "skipped" }
            WindowsDataPresent = $false
            AndroidDataPresent = $false
            WindowsCurrentAppCount = 0
            AndroidCurrentAppCount = 0
            WindowsDeviceCount = 0
            AndroidDeviceCount = 0
            WindowsUsageDurationMs = 0
            AndroidUsageDurationMs = 0
            Messages = $messages.ToArray()
        }
    }

    $windowsPresence = Get-IntegratedDashboardPlatformPresence $snapshot "windows"
    $androidPresence = Get-IntegratedDashboardPlatformPresence $snapshot "android"

    if ($RequireWindows -and !$windowsPresence.DataPresent) {
        $messages.Add("Missing required Windows data in /api/dashboard/integrated after bridge upload. Use -SkipWindows only when Windows data is intentionally absent.")
    }

    if ($RequireAndroid -and !$androidPresence.DataPresent) {
        $messages.Add("Missing required Android data in /api/dashboard/integrated after bridge upload. Use -SkipAndroid only when Android data is intentionally absent.")
    }

    [pscustomobject]@{
        Succeeded = $messages.Count -eq 0
        ApiUrl = $ApiUrl
        WindowsStatus = if (!$RequireWindows) { "skipped" } elseif ($windowsPresence.DataPresent) { "present" } else { "missing" }
        AndroidStatus = if (!$RequireAndroid) { "skipped" } elseif ($androidPresence.DataPresent) { "present" } else { "missing" }
        WindowsDataPresent = [bool]$windowsPresence.DataPresent
        AndroidDataPresent = [bool]$androidPresence.DataPresent
        WindowsCurrentAppCount = $windowsPresence.CurrentAppCount
        AndroidCurrentAppCount = $androidPresence.CurrentAppCount
        WindowsDeviceCount = $windowsPresence.DeviceCount
        AndroidDeviceCount = $androidPresence.DeviceCount
        WindowsUsageDurationMs = $windowsPresence.UsageDurationMs
        AndroidUsageDurationMs = $androidPresence.UsageDurationMs
        Messages = $messages.ToArray()
    }
}

if ($Help) {
    Write-Usage
    exit 0
}

$dashboardUrl = "$baseUrl/dashboard?userId=$([uri]::EscapeDataString($UserId))&from=$today&to=$today&timezoneId=$([uri]::EscapeDataString($TimezoneId))"
$integratedDashboardApiUrl = "$baseUrl/api/dashboard/integrated?userId=$([uri]::EscapeDataString($UserId))&from=$today&to=$today&timezoneId=$([uri]::EscapeDataString($TimezoneId))"
$bridgePollingArgs = @(Get-BridgePollingArguments)
$bridgePollingArgsText = if ($bridgePollingArgs.Count -gt 0) { " " + ($bridgePollingArgs -join " ") } else { "" }
$bridgeRunsContinuously = $BridgeIntervalSeconds -ge 0 -and $BridgeMaxIterations -eq 0

if ($DryRun) {
    Write-Step "Dry run: would call scripts\start-server-postgres.ps1"
    Write-Step "Dry run: would run ASP.NET Core server at $baseUrl"
    if (!$SkipAndroid -and !$SkipAndroidPull) {
        if ($scriptManagedBridgeLoop) {
            Write-Step "Dry run: would pull Android emulator Room DB before each bridge iteration: $AndroidPackage databases/woong-monitor.db"
        } else {
            Write-Step "Dry run: would use adb to pull Android emulator Room DB: $AndroidPackage databases/woong-monitor.db"
        }
    }
    if ($scriptManagedBridgeLoop) {
        Write-Step "Dry run: would run script-managed bridge loop for $BridgeMaxIterations iteration(s), sleeping $BridgeIntervalSeconds second(s) between iterations."
        Write-Step "Dry run: would run Woong.MonitorStack.LocalDashboardBridge once per script-managed iteration$bridgePollingArgsText"
    } else {
        Write-Step "Dry run: would run Woong.MonitorStack.LocalDashboardBridge$bridgePollingArgsText"
    }
    Write-Step "Dry run: bridge polling: $(Get-BridgePollingDescription)"
    Write-Step "Dry run: bridge checkpoint: $(if ([string]::IsNullOrWhiteSpace($BridgeCheckpointPath)) { "disabled" } else { $BridgeCheckpointPath })"
    Write-Step "Dry run: WPF SQLite path: $WindowsDb"
    Write-Step "Dry run: Android Room path: $AndroidDb"
    Write-Step "Dry run: would check /api/dashboard/integrated and write Windows/Android data-presence status to report.md"
    Write-Step "Dry run: would open $dashboardUrl"
    exit 0
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

Push-Location $repoRoot
try {
    Write-Step "Starting local PostgreSQL"
    & $startPostgresScript

    if (!(Test-ServerReady)) {
        $env:ASPNETCORE_URLS = $baseUrl
        $env:ConnectionStrings__MonitorDb = $connectionString
        $env:ASPNETCORE_ENVIRONMENT = "Development"

        $stdout = Join-Path $OutputRoot "server.stdout.log"
        $stderr = Join-Path $OutputRoot "server.stderr.log"
        Write-Step "Starting ASP.NET Core server at $baseUrl"
        Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--project", $serverProject, "--no-launch-profile") `
            -WorkingDirectory $repoRoot `
            -RedirectStandardOutput $stdout `
            -RedirectStandardError $stderr `
            -WindowStyle Hidden | Out-Null
    } else {
        Write-Step "Server already reachable at $baseUrl"
    }

    Wait-Server

    if (!$scriptManagedBridgeLoop -and !$SkipAndroid -and !$SkipAndroidPull) {
        Pull-AndroidDatabase $AndroidPackage $AndroidDb
    }

    if ($scriptManagedBridgeLoop) {
        Write-Step "Running script-managed bridge loop for $BridgeMaxIterations iteration(s)."
        for ($iteration = 1; $iteration -le $BridgeMaxIterations; $iteration++) {
            Write-Step "Bridge loop iteration $iteration of $BridgeMaxIterations"
            Pull-AndroidDatabase $AndroidPackage $AndroidDb

            $bridgeArgs = @(Get-BridgeArguments)
            Write-Step "Uploading local client data through API DTOs (one-shot bridge iteration)"
            & dotnet @bridgeArgs
            if ($LASTEXITCODE -ne 0) {
                throw "Local dashboard bridge failed with exit code $LASTEXITCODE."
            }

            if ($iteration -lt $BridgeMaxIterations -and $BridgeIntervalSeconds -gt 0) {
                Start-Sleep -Seconds $BridgeIntervalSeconds
            }
        }
    } else {
        $bridgeArgs = @(Get-BridgeArguments)

        if ($bridgeRunsContinuously) {
        Write-Step "Bridge polling is continuous; press Ctrl+C to stop bridge uploads."
        Write-Step "Dashboard URL:"
        Write-Host $dashboardUrl

            if (!$NoOpenBrowser) {
                Start-Process $dashboardUrl
            }
        }

        Write-Step "Uploading local client data through API DTOs ($(Get-BridgePollingDescription))"
        & dotnet @bridgeArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Local dashboard bridge failed with exit code $LASTEXITCODE."
        }
    }

    $verification = Test-IntegratedDashboardDataPresence `
        -ApiUrl $integratedDashboardApiUrl `
        -RequireWindows:(-not $SkipWindows) `
        -RequireAndroid:(-not $SkipAndroid)

    $reportPath = Join-Path $OutputRoot "report.md"
    $reportLines = @(
        "# Local Integrated Dashboard",
        "",
        "- Dashboard: $dashboardUrl",
        "- Integrated dashboard API: $integratedDashboardApiUrl",
        "- WPF SQLite: $WindowsDb",
        "- Android Room: $AndroidDb",
        "- Android DB refreshed per bridge iteration: $([bool]$RefreshAndroidDbEachBridgeIteration)",
        "- Bridge checkpoint: $(if ([string]::IsNullOrWhiteSpace($BridgeCheckpointPath)) { "disabled" } else { $BridgeCheckpointPath })",
        "- Server: $baseUrl",
        "- PostgreSQL: Docker localhost:55432",
        "",
        "## Post-upload verification",
        "",
        "- Windows data present: $($verification.WindowsDataPresent) (status: $($verification.WindowsStatus); current apps: $($verification.WindowsCurrentAppCount); devices: $($verification.WindowsDeviceCount); usage duration ms: $($verification.WindowsUsageDurationMs))",
        "- Android data present: $($verification.AndroidDataPresent) (status: $($verification.AndroidStatus); current apps: $($verification.AndroidCurrentAppCount); devices: $($verification.AndroidDeviceCount); usage duration ms: $($verification.AndroidUsageDurationMs))",
        "",
        "This local flow uploads app/window/site/location metadata only. It does not read typed text, passwords, messages, clipboard contents, page contents, screenshots, or Android touch coordinates."
    )

    $verificationMessages = @($verification.Messages)
    if ($verificationMessages.Count -gt 0) {
        $reportLines += ""
        $reportLines += "## Verification warnings"
        $reportLines += ""
        foreach ($message in $verificationMessages) {
            $reportLines += "- $message"
        }
    }

    $reportLines | Set-Content -LiteralPath $reportPath -Encoding UTF8

    foreach ($message in $verificationMessages) {
        Write-Warning $message
    }

    Write-Step "Dashboard URL:"
    Write-Host $dashboardUrl
    Write-Step "Report: $reportPath"

    if (!$verification.Succeeded) {
        throw "Integrated dashboard verification failed. See report: $reportPath"
    }

    if (!$NoOpenBrowser -and !$bridgeRunsContinuously) {
        Start-Process $dashboardUrl
    }
}
finally {
    Pop-Location
}
