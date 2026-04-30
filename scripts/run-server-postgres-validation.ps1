param(
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/server-postgres-validation"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $OutputRoot $timestamp
$latestRoot = Join-Path $OutputRoot "latest"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null
New-Item -ItemType Directory -Force -Path $latestRoot | Out-Null

$testOutputPath = Join-Path $runRoot "dotnet-test-output.txt"
$status = "PASS"
$notes = New-Object System.Collections.Generic.List[string]
$previousValue = $env:WOONG_MONITOR_RUN_POSTGRES_TESTS

try {
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $dockerInfo = docker info 2>&1
    $dockerExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($dockerExitCode -ne 0) {
        throw "Docker daemon is unavailable. docker info output: $($dockerInfo -join "`n")"
    }

    $env:WOONG_MONITOR_RUN_POSTGRES_TESTS = "1"
    $notes.Add("Enabled WOONG_MONITOR_RUN_POSTGRES_TESTS=1 for this process only.")
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $testOutput = dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~PostgresMonitorDbContextTests" -maxcpucount:1 -v minimal 2>&1
    $testExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    $testOutput | Set-Content -Path $testOutputPath -Encoding UTF8
    if ($testExitCode -ne 0) {
        throw "PostgreSQL/Testcontainers validation failed with dotnet test exit code $testExitCode."
    }

    $notes.Add("PostgreSQL/Testcontainers validation passed.")
} catch {
    $status = "FAIL"
    $notes.Add($_.Exception.Message)
} finally {
    if ($null -eq $previousValue) {
        Remove-Item Env:\WOONG_MONITOR_RUN_POSTGRES_TESTS -ErrorAction SilentlyContinue
    } else {
        $env:WOONG_MONITOR_RUN_POSTGRES_TESTS = $previousValue
    }
}

$reportLines = @(
    "# Server PostgreSQL Validation Report",
    "",
    "Status: $status",
    "Generated at UTC: $([DateTimeOffset]::UtcNow.ToString('O'))",
    "Output: ``$runRoot``",
    "",
    "## Scope",
    "",
    "- Starts PostgreSQL through Testcontainers.",
    "- Applies EF Core migrations through the Npgsql provider.",
    "- Verifies PostgreSQL-specific migration/backfill behavior and relational constraints.",
    "- Does not use EF InMemory as proof of PostgreSQL behavior.",
    "",
    "## Artifacts",
    "",
    "- dotnet-test-output.txt",
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
    testProject = "tests/Woong.MonitorStack.Server.Tests/Woong.MonitorStack.Server.Tests.csproj"
    filter = "FullyQualifiedName~PostgresMonitorDbContextTests"
    artifacts = @("dotnet-test-output.txt", "report.md", "manifest.json")
    notes = $notes
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $runRoot "manifest.json") -Encoding UTF8

Copy-Item -Path (Join-Path $runRoot "report.md") -Destination (Join-Path $latestRoot "report.md") -Force
Copy-Item -Path (Join-Path $runRoot "manifest.json") -Destination (Join-Path $latestRoot "manifest.json") -Force
if (Test-Path $testOutputPath) {
    Copy-Item -Path $testOutputPath -Destination (Join-Path $latestRoot "dotnet-test-output.txt") -Force
}

Write-Host "Server PostgreSQL validation artifacts: $runRoot"
Write-Host "Status: $status"

if ($status -ne "PASS") {
    exit 1
}

exit 0
