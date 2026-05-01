using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class ServerCiWorkflowTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ServerCiWorkflow_RestoresBuildsTestsAndPublishesServerOnServerChanges()
    {
        string workflowPath = Path.Combine(RepositoryRoot, ".github", "workflows", "server-ci.yml");
        string validationScriptPath = Path.Combine(RepositoryRoot, "scripts", "validate-server-ci-workflow.ps1");

        Assert.True(File.Exists(workflowPath), "Server CI workflow must exist.");
        Assert.True(File.Exists(validationScriptPath), "Server CI workflow validator must exist.");

        string workflow = File.ReadAllText(workflowPath);

        Assert.Contains("name: Server CI", workflow, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", workflow, StringComparison.Ordinal);
        Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
        Assert.Contains("push:", workflow, StringComparison.Ordinal);
        Assert.Contains("\"src/Woong.MonitorStack.Server/**\"", workflow, StringComparison.Ordinal);
        Assert.Contains("\"tests/Woong.MonitorStack.Server.Tests/**\"", workflow, StringComparison.Ordinal);
        Assert.Contains("\"docs/server-test-db-strategy.md\"", workflow, StringComparison.Ordinal);
        Assert.Contains("\"docs/production-migrations.md\"", workflow, StringComparison.Ordinal);
        Assert.Contains("runs-on: windows-latest", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/checkout@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/setup-dotnet@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: \"10.0.x\"", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet restore tests\\Woong.MonitorStack.Server.Tests\\Woong.MonitorStack.Server.Tests.csproj --configfile NuGet.config", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet build src\\Woong.MonitorStack.Server\\Woong.MonitorStack.Server.csproj -c Release --no-restore -m:1 -v minimal", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test tests\\Woong.MonitorStack.Server.Tests\\Woong.MonitorStack.Server.Tests.csproj -c Release --no-restore -m:1 -v minimal", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet publish src\\Woong.MonitorStack.Server\\Woong.MonitorStack.Server.csproj -c Release --no-restore -o artifacts\\server", workflow, StringComparison.Ordinal);
        Assert.Contains("actions/upload-artifact@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("name: woong-monitor-server", workflow, StringComparison.Ordinal);
        Assert.Contains("artifacts/server/**", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("testDebugUnitTest", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("windows-msix", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServerCiWorkflowValidationScript_PassesAgainstWorkflow()
    {
        RunPowerShell(
            "-NoProfile -ExecutionPolicy Bypass -File scripts\\validate-server-ci-workflow.ps1 -WorkflowPath .github\\workflows\\server-ci.yml");
    }

    private static void RunPowerShell(string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo(
            "powershell.exe",
            arguments)
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Could not start PowerShell.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"PowerShell command failed with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Woong.MonitorStack.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
