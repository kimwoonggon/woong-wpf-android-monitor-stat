param(
    [switch]$Help,
    [switch]$DryRun,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration,
    [string]$OutputPath,
    [switch]$SelfContained,
    [string]$Runtime = ""
)

$ErrorActionPreference = "Stop"

if ($Help) {
    Write-Host @"
Build a reviewed EF Core migration bundle for the Woong Monitor server.

Usage:
  powershell -ExecutionPolicy Bypass -File scripts\build-server-migration-bundle.ps1 -Configuration Release [-OutputPath artifacts\server-migrations\woong-server-migrations.exe] [-SelfContained] [-Runtime win-x64] [-DryRun]

Safety:
  This script only builds a migration bundle. It does not apply migrations and
  does not accept a production connection string. Review the bundle, take a
  PostgreSQL backup, then run the bundle manually with an explicit production
  connection string.
"@
    return
}

if ([string]::IsNullOrWhiteSpace($Configuration)) {
    throw "Configuration is required. Use -Configuration Release for production bundles."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$serverProject = Join-Path $repoRoot "src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj"

if (-not (Test-Path $serverProject)) {
    throw "Server project not found: $serverProject"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = Join-Path $repoRoot "artifacts\server-migrations"
    $fileName = "woong-server-migrations-$($Configuration.ToLowerInvariant()).exe"
    $OutputPath = Join-Path $artifactRoot $fileName
}

$resolvedOutputDirectory = Split-Path -Parent $OutputPath
if ([string]::IsNullOrWhiteSpace($resolvedOutputDirectory)) {
    throw "OutputPath must include a file name."
}

$bundleArguments = @(
    "ef",
    "migrations",
    "bundle",
    "--project",
    "src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj",
    "--startup-project",
    "src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj",
    "--context",
    "MonitorDbContext",
    "--configuration",
    $Configuration,
    "--output",
    $OutputPath,
    "--self-contained",
    ([bool]$SelfContained).ToString().ToLowerInvariant()
)

if (-not [string]::IsNullOrWhiteSpace($Runtime)) {
    $bundleArguments += "--runtime"
    $bundleArguments += $Runtime
}

if ($DryRun) {
    Write-Host "Dry run: dotnet $($bundleArguments -join ' ')"
    Write-Host "No bundle was built. No migration was applied."
    return
}

New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null

dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet tool restore failed."
}

Push-Location $repoRoot
try {
    & dotnet $bundleArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef migrations bundle failed."
    }
}
finally {
    Pop-Location
}

Write-Host "Server migration bundle written to: $OutputPath"
Write-Host "Do not apply this bundle automatically. Review it, take a PostgreSQL backup, then run it manually with an explicit production connection string."
