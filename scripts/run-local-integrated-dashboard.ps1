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
    [string]$OutputRoot = ""
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

if ($Help) {
    Write-Usage
    exit 0
}

$dashboardUrl = "$baseUrl/dashboard?userId=$([uri]::EscapeDataString($UserId))&from=$today&to=$today&timezoneId=$([uri]::EscapeDataString($TimezoneId))"

if ($DryRun) {
    Write-Step "Dry run: would call scripts\start-server-postgres.ps1"
    Write-Step "Dry run: would run ASP.NET Core server at $baseUrl"
    if (!$SkipAndroid -and !$SkipAndroidPull) {
        Write-Step "Dry run: would use adb to pull Android emulator Room DB: $AndroidPackage databases/woong-monitor.db"
    }
    Write-Step "Dry run: would run Woong.MonitorStack.LocalDashboardBridge"
    Write-Step "Dry run: WPF SQLite path: $WindowsDb"
    Write-Step "Dry run: Android Room path: $AndroidDb"
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

    if (!$SkipAndroid -and !$SkipAndroidPull) {
        Pull-AndroidDatabase $AndroidPackage $AndroidDb
    }

    $bridgeArgs = @(
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

    if (!$SkipWindows -and (Test-Path $WindowsDb)) {
        $bridgeArgs += @("--windowsDb", $WindowsDb)
    } elseif (!$SkipWindows) {
        Write-Step "WPF SQLite not found at $WindowsDb. Skipping Windows upload."
    }

    if (!$SkipAndroid -and (Test-Path $AndroidDb)) {
        $bridgeArgs += @("--androidDb", $AndroidDb)
    } elseif (!$SkipAndroid) {
        Write-Step "Android Room DB not found at $AndroidDb. Skipping Android upload."
    }

    Write-Step "Uploading local client data through API DTOs"
    & dotnet @bridgeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Local dashboard bridge failed with exit code $LASTEXITCODE."
    }

    $reportPath = Join-Path $OutputRoot "report.md"
    @(
        "# Local Integrated Dashboard",
        "",
        "- Dashboard: $dashboardUrl",
        "- WPF SQLite: $WindowsDb",
        "- Android Room: $AndroidDb",
        "- Server: $baseUrl",
        "- PostgreSQL: Docker localhost:55432",
        "",
        "This local flow uploads app/window/site/location metadata only. It does not read typed text, passwords, messages, clipboard contents, page contents, screenshots, or Android touch coordinates."
    ) | Set-Content -LiteralPath $reportPath -Encoding UTF8

    Write-Step "Dashboard URL:"
    Write-Host $dashboardUrl
    Write-Step "Report: $reportPath"

    if (!$NoOpenBrowser) {
        Start-Process $dashboardUrl
    }
}
finally {
    Pop-Location
}
