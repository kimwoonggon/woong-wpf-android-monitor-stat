param(
    [string]$WorkflowPath = ".github/workflows/server-ci.yml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $WorkflowPath)) {
    throw "Server CI workflow is missing: $WorkflowPath"
}

$workflow = Get-Content -Raw -Path $WorkflowPath

$requiredSnippets = @(
    "name: Server CI",
    "push:",
    "pull_request:",
    "workflow_dispatch:",
    "paths:",
    '"src/Woong.MonitorStack.Server/**"',
    '"tests/Woong.MonitorStack.Server.Tests/**"',
    '"docs/server-test-db-strategy.md"',
    '"docs/production-migrations.md"',
    "runs-on: windows-latest",
    "actions/checkout@v4",
    "actions/setup-dotnet@v4",
    'dotnet-version: "10.0.x"',
    "dotnet restore tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj --configfile NuGet.config",
    "dotnet build src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj -c Release --no-restore -m:1 -v minimal",
    "dotnet test tests\Woong.MonitorStack.Server.Tests\Woong.MonitorStack.Server.Tests.csproj -c Release --no-restore -m:1 -v minimal",
    "dotnet publish src\Woong.MonitorStack.Server\Woong.MonitorStack.Server.csproj -c Release --no-restore -o artifacts\server",
    "actions/upload-artifact@v4",
    "name: woong-monitor-server",
    "artifacts/server/**"
)

foreach ($snippet in $requiredSnippets) {
    if (-not $workflow.Contains($snippet)) {
        throw "Server CI workflow is missing required snippet: $snippet"
    }
}

$forbiddenSnippets = @(
    "testDebugUnitTest",
    "windows-msix"
)

foreach ($snippet in $forbiddenSnippets) {
    if ($workflow.Contains($snippet)) {
        throw "Server CI workflow contains forbidden snippet: $snippet"
    }
}

Write-Host "Server CI workflow validation passed: $WorkflowPath"
