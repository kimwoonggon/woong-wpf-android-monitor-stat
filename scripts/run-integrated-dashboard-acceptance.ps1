param(
    [switch]$Help,
    [switch]$DryRun,
    [switch]$SkipBrowser,
    [int]$Port = 5087,
    [string]$UserId = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$startPostgresScript = Join-Path $repoRoot "scripts\start-server-postgres.ps1"
$serverProject = "src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj"
$serverCommandText = "dotnet run --project src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj --no-launch-profile"
$connectionString = "Host=localhost;Port=55432;Database=woong_monitor;Username=woong;Password=woong_dev_password"
$date = "2026-05-02"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
if ([string]::IsNullOrWhiteSpace($UserId)) {
    $UserId = "integrated-acceptance-$timestamp"
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts\integrated-dashboard-acceptance\$timestamp"
}
$latestRoot = Join-Path $repoRoot "artifacts\integrated-dashboard-acceptance\latest"
$baseUrl = "http://127.0.0.1:$Port"
$apiUrl = "$baseUrl/api/dashboard/integrated?userId=$UserId&from=$date&to=$date&timezoneId=UTC"
$dashboardUrl = "$baseUrl/dashboard?userId=$UserId&from=$date&to=$date&timezoneId=UTC"
$deviceToken = $null

function Write-Usage {
    Write-Host "Usage: powershell -ExecutionPolicy Bypass -File scripts\run-integrated-dashboard-acceptance.ps1 [-DryRun] [-SkipBrowser] [-Port 5087] [-UserId <id>]"
    Write-Host ""
    Write-Host "Seeds synthetic Windows WPF and synthetic Android metadata through server APIs, verifies PostgreSQL-backed /api/dashboard/integrated, and captures the Blazor /dashboard page with Playwright."
    Write-Host "No user local DB, keylogging, typed text, clipboard, page contents, passwords, messages, Android touch coordinates, or external app screenshots are used."
}

function Write-Step([string]$Message) {
    Write-Host "[integrated-dashboard] $Message"
}

function ConvertTo-JsonBody($Body) {
    return $Body | ConvertTo-Json -Depth 10
}

function Invoke-JsonPost([string]$Path, $Body, [hashtable]$Headers = @{}) {
    return Invoke-RestMethod `
        -Method Post `
        -Uri "$baseUrl$Path" `
        -ContentType "application/json" `
        -Headers $Headers `
        -Body (ConvertTo-JsonBody $Body)
}

function Assert-Contains([string]$Text, [string]$Needle, [string]$Label) {
    if ($Text.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Expected $Label to contain '$Needle'."
    }
}

function Assert-NotContains([string]$Text, [string]$Needle, [string]$Label) {
    if ($Text.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Forbidden privacy marker '$Needle' appeared in $Label."
    }
}

function Wait-Server {
    $deadline = (Get-Date).AddSeconds(90)
    do {
        try {
            Invoke-RestMethod -Method Get -Uri "$baseUrl/api/dashboard/integrated?userId=health-check&from=$date&to=$date&timezoneId=UTC" | Out-Null
            return
        } catch {
            Start-Sleep -Seconds 2
        }
    } while ((Get-Date) -lt $deadline)

    throw "Server did not become reachable at $baseUrl."
}

function Invoke-PlaywrightScreenshot([string]$Viewport, [string]$Url, [string]$Path) {
    & npx playwright screenshot "--viewport-size=$Viewport" $Url $Path
    if ($LASTEXITCODE -ne 0) {
        throw "Playwright screenshot failed for viewport $Viewport with exit code $LASTEXITCODE."
    }
    if (!(Test-Path $Path)) {
        throw "Playwright reported success but did not create screenshot '$Path'."
    }
}

function New-FocusSession(
    [string]$ClientSessionId,
    [string]$PlatformAppKey,
    [string]$Start,
    [string]$End,
    [bool]$IsIdle,
    [string]$Source,
    [string]$ProcessName = "",
    [string]$WindowTitle = ""
) {
    $started = [DateTimeOffset]::Parse($Start)
    $ended = [DateTimeOffset]::Parse($End)
    return @{
        clientSessionId = $ClientSessionId
        platformAppKey = $PlatformAppKey
        startedAtUtc = $started.ToString("o")
        endedAtUtc = $ended.ToString("o")
        durationMs = [int64]($ended - $started).TotalMilliseconds
        localDate = $date
        timezoneId = "UTC"
        isIdle = $IsIdle
        source = $Source
        processName = $(if ([string]::IsNullOrWhiteSpace($ProcessName)) { $null } else { $ProcessName })
        windowTitle = $(if ([string]::IsNullOrWhiteSpace($WindowTitle)) { $null } else { $WindowTitle })
    }
}

function New-WebSession(
    [string]$ClientSessionId,
    [string]$FocusSessionId,
    [string]$Domain,
    [string]$Start,
    [string]$End
) {
    $started = [DateTimeOffset]::Parse($Start)
    $ended = [DateTimeOffset]::Parse($End)
    return @{
        clientSessionId = $ClientSessionId
        focusSessionId = $FocusSessionId
        browserFamily = "Chrome"
        url = $null
        domain = $Domain
        pageTitle = $null
        startedAtUtc = $started.ToString("o")
        endedAtUtc = $ended.ToString("o")
        durationMs = [int64]($ended - $started).TotalMilliseconds
        captureMethod = "BrowserExtensionFuture"
        captureConfidence = "High"
        isPrivateOrUnknown = $false
    }
}

if ($Help) {
    Write-Usage
    exit 0
}

if ($DryRun) {
    Write-Step "Dry run: would call scripts\start-server-postgres.ps1 to start Docker PostgreSQL."
    Write-Step "Dry run: would run '$serverCommandText' on $baseUrl."
    Write-Step "Dry run: would seed synthetic Windows WPF data from windows-wpf-work through /api/devices/register, /api/focus-sessions/upload, and /api/web-sessions/upload."
    Write-Step "Dry run: would seed synthetic Android data from android-usage-work through /api/devices/register, /api/focus-sessions/upload, and /api/location-contexts/upload."
    Write-Step "Dry run: would verify /api/dashboard/integrated and /dashboard?userId=$UserId."
    Write-Step "Dry run: would use npx playwright screenshot for dashboard-1440.png and dashboard-390.png unless -SkipBrowser is provided."
    Write-Step "Dry run: would write report.md and manifest.json under artifacts\integrated-dashboard-acceptance."
    exit 0
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
New-Item -ItemType Directory -Force -Path $latestRoot | Out-Null

$serverProcess = $null
$previousAspNetUrls = $env:ASPNETCORE_URLS
$previousConnectionString = $env:ConnectionStrings__MonitorDb
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT

try {
    Push-Location $repoRoot

    Write-Step "Starting Docker PostgreSQL with scripts\start-server-postgres.ps1"
    & $startPostgresScript

    $env:ASPNETCORE_URLS = $baseUrl
    $env:ConnectionStrings__MonitorDb = $connectionString
    $env:ASPNETCORE_ENVIRONMENT = "Development"

    $serverOut = Join-Path $OutputRoot "server.stdout.log"
    $serverErr = Join-Path $OutputRoot "server.stderr.log"
    Write-Step $serverCommandText
    $serverProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $serverProject, "--no-launch-profile") `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $serverOut `
        -RedirectStandardError $serverErr `
        -WindowStyle Hidden `
        -PassThru

    Wait-Server

    Write-Step "Registering synthetic Windows WPF and Android devices"
    $windowsDevice = Invoke-JsonPost "/api/devices/register" @{
        userId = $UserId
        platform = 1
        deviceKey = "$UserId-windows-wpf-work"
        deviceName = "Windows WPF Acceptance"
        timezoneId = "UTC"
    }
    $androidDevice = Invoke-JsonPost "/api/devices/register" @{
        userId = $UserId
        platform = 2
        deviceKey = "$UserId-android-usage-work"
        deviceName = "Android Acceptance"
        timezoneId = "UTC"
    }
    $deviceToken = $windowsDevice.deviceToken

    $windowsHeaders = @{ "X-Device-Token" = $windowsDevice.deviceToken }
    $androidHeaders = @{ "X-Device-Token" = $androidDevice.deviceToken }

    Write-Step "Uploading Windows WPF focus and web sessions"
    Invoke-JsonPost "/api/focus-sessions/upload" @{
        deviceId = $windowsDevice.deviceId
        sessions = @(
            (New-FocusSession "win-vscode-$timestamp" "Code.exe" "2026-05-02T08:00:00Z" "2026-05-02T08:45:00Z" $false "windows-wpf-work" "Code.exe" "VS Code acceptance workspace"),
            (New-FocusSession "win-chrome-$timestamp" "chrome.exe" "2026-05-02T09:00:00Z" "2026-05-02T09:30:00Z" $false "windows-wpf-work" "chrome.exe" ""),
            (New-FocusSession "win-idle-$timestamp" "chrome.exe" "2026-05-02T09:30:00Z" "2026-05-02T09:35:00Z" $true "windows-wpf-work" "chrome.exe" "")
        )
    } $windowsHeaders | Out-Null
    Invoke-JsonPost "/api/web-sessions/upload" @{
        deviceId = $windowsDevice.deviceId
        sessions = @(
            (New-WebSession "web-github-$timestamp" "win-chrome-$timestamp" "github.com" "2026-05-02T09:00:00Z" "2026-05-02T09:18:00Z"),
            (New-WebSession "web-chatgpt-$timestamp" "win-chrome-$timestamp" "chatgpt.com" "2026-05-02T09:18:00Z" "2026-05-02T09:30:00Z")
        )
    } $windowsHeaders | Out-Null

    Write-Step "Uploading Android UsageStats focus and opted-in coarse location metadata"
    Invoke-JsonPost "/api/focus-sessions/upload" @{
        deviceId = $androidDevice.deviceId
        sessions = @(
            (New-FocusSession "android-chrome-$timestamp" "com.android.chrome" "2026-05-02T10:00:00Z" "2026-05-02T10:25:00Z" $false "android-usage-work"),
            (New-FocusSession "android-youtube-$timestamp" "com.google.android.youtube" "2026-05-02T10:30:00Z" "2026-05-02T10:50:00Z" $false "android-usage-work")
        )
    } $androidHeaders | Out-Null
    Invoke-JsonPost "/api/location-contexts/upload" @{
        deviceId = $androidDevice.deviceId
        contexts = @(
            @{
                clientContextId = "android-location-$timestamp"
                capturedAtUtc = "2026-05-02T10:10:00.0000000Z"
                localDate = $date
                timezoneId = "UTC"
                latitude = 37.5665
                longitude = 126.9780
                accuracyMeters = 80.0
                captureMode = "coarse_opt_in"
                permissionState = "granted"
                source = "android-usage-work"
            }
        )
    } $androidHeaders | Out-Null

    Write-Step "Verifying integrated JSON dashboard"
    $snapshot = Invoke-RestMethod -Method Get -Uri $apiUrl
    $snapshotJson = $snapshot | ConvertTo-Json -Depth 10
    Set-Content -Path (Join-Path $OutputRoot "api-dashboard.json") -Value $snapshotJson -Encoding UTF8

    if ($snapshot.totalActiveMs -le 0) { throw "Expected positive integrated active focus." }
    if ($snapshot.totalWebMs -le 0) { throw "Expected positive integrated web focus." }
    if (($snapshot.devices | Measure-Object).Count -lt 2) { throw "Expected Windows and Android devices." }
    $snapshotText = $snapshotJson
    Assert-Contains $snapshotText "Windows WPF Acceptance" "dashboard JSON"
    Assert-Contains $snapshotText "Android Acceptance" "dashboard JSON"
    Assert-Contains $snapshotText "github.com" "dashboard JSON"
    Assert-Contains $snapshotText "chatgpt.com" "dashboard JSON"
    Assert-Contains $snapshotText "37.5665,126.9780" "dashboard JSON"

    Write-Step "Verifying Blazor dashboard HTML"
    $html = Invoke-RestMethod -Method Get -Uri $dashboardUrl
    Set-Content -Path (Join-Path $OutputRoot "dashboard.html") -Value $html -Encoding UTF8
    Assert-Contains $html "Integrated Device Dashboard" "dashboard HTML"
    Assert-Contains $html "Windows + Android" "dashboard HTML"
    Assert-Contains $html "Active Focus" "dashboard HTML"
    Assert-Contains $html "github.com" "dashboard HTML"
    Assert-Contains $html "chatgpt.com" "dashboard HTML"
    Assert-NotContains $html "deviceToken" "dashboard HTML"
    Assert-NotContains $html "password" "dashboard HTML"
    Assert-NotContains $html "clipboard" "dashboard HTML"
    Assert-NotContains $html "typed text" "dashboard HTML"

    $screenshotResults = @()
    if ($SkipBrowser) {
        $screenshotResults += "Skipped browser screenshots because -SkipBrowser was supplied."
    } else {
        Write-Step "Capturing Blazor dashboard screenshots with npx playwright"
        $desktopPng = Join-Path $OutputRoot "dashboard-1440.png"
        $mobilePng = Join-Path $OutputRoot "dashboard-390.png"
        Invoke-PlaywrightScreenshot "1440,1000" $dashboardUrl $desktopPng
        Invoke-PlaywrightScreenshot "390,900" $dashboardUrl $mobilePng
        $screenshotResults += "dashboard-1440.png"
        $screenshotResults += "dashboard-390.png"
    }

    $designSource = Join-Path $repoRoot "artifacts\blazor-dashboard-design\integrated-dashboard-design.svg"
    if (Test-Path $designSource) {
        Copy-Item -Path $designSource -Destination (Join-Path $OutputRoot "integrated-dashboard-design.svg") -Force
    }

    $manifest = [ordered]@{
        userId = $UserId
        date = $date
        baseUrl = $baseUrl
        apiUrl = $apiUrl
        dashboardUrl = $dashboardUrl
        windowsDeviceId = $windowsDevice.deviceId
        androidDeviceId = $androidDevice.deviceId
        screenshots = $screenshotResults
        privacy = "Metadata only. No typed text, clipboard, passwords, messages, page contents, screenshots of other apps, or Android touch coordinates."
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $OutputRoot "manifest.json") -Encoding UTF8

    $report = @"
# Integrated Dashboard Acceptance

Result: PASS

## Verified

- Synthetic Windows WPF focus sessions uploaded through `/api/focus-sessions/upload`.
- Synthetic Windows browser domain sessions uploaded through `/api/web-sessions/upload`.
- Synthetic Android UsageStats-style sessions uploaded through `/api/focus-sessions/upload`.
- Synthetic Android opted-in coarse location context uploaded through `/api/location-contexts/upload`.
- PostgreSQL-backed `/api/dashboard/integrated` returned Windows + Android totals.
- Blazor `/dashboard?userId=$UserId&from=$date&to=$date&timezoneId=UTC` rendered the expected dashboard content.
- Playwright screenshots: $($screenshotResults -join ", ")

## Privacy Boundary

This acceptance uses synthetic metadata only. It does not collect keylogging, typed text, passwords, messages, clipboard contents, browser page contents, Android touch coordinates, user local SQLite/Room databases, or external app screenshots.
"@
    Set-Content -Path (Join-Path $OutputRoot "report.md") -Value $report -Encoding UTF8

    if (Test-Path $latestRoot) {
        Get-ChildItem -Path $latestRoot -Force | Remove-Item -Recurse -Force
    }
    Copy-Item -Path (Join-Path $OutputRoot "*") -Destination $latestRoot -Recurse -Force

    Write-Step "PASS. Artifacts: $OutputRoot"
}
finally {
    if ($serverProcess -ne $null -and !$serverProcess.HasExited) {
        Write-Step "Stopping server process $($serverProcess.Id)"
        Stop-Process -Id $serverProcess.Id -Force
    }
    $env:ASPNETCORE_URLS = $previousAspNetUrls
    $env:ConnectionStrings__MonitorDb = $previousConnectionString
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    Pop-Location
}
