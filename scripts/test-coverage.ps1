$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testResults = Join-Path $repoRoot "artifacts/TestResults"
$coverageReport = Join-Path $repoRoot "artifacts/coverage"

foreach ($path in @($testResults, $coverageReport)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Force -Path $testResults | Out-Null
New-Item -ItemType Directory -Force -Path $coverageReport | Out-Null

Push-Location $repoRoot
try {
    dotnet test Woong.MonitorStack.sln `
        -maxcpucount:1 `
        --collect:"XPlat Code Coverage" `
        --settings coverage.runsettings `
        --results-directory $testResults

    dotnet tool restore
    dotnet tool run reportgenerator `
        "-reports:artifacts/TestResults/**/coverage.cobertura.xml" `
        "-targetdir:artifacts/coverage" `
        "-reporttypes:Html;MarkdownSummaryGithub;Cobertura"
}
finally {
    Pop-Location
}
