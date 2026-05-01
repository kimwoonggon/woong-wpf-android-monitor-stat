using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class ServerProductionMigrationRunbookTests
{
    [Fact]
    public void ServerMigrationBundleScript_BuildsBundleOnlyAndRequiresExplicitReleaseSettings()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "build-server-migration-bundle.ps1");

        Assert.True(File.Exists(scriptPath), "Server migration bundle script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("param(", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[switch]$Help", script, StringComparison.Ordinal);
        Assert.Contains("[switch]$DryRun", script, StringComparison.Ordinal);
        Assert.Contains("[ValidateSet(\"Debug\", \"Release\")]", script, StringComparison.Ordinal);
        Assert.Contains("[string]$Configuration", script, StringComparison.Ordinal);
        Assert.Contains("[string]$OutputPath", script, StringComparison.Ordinal);
        Assert.Contains("artifacts\\server-migrations", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ef", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("migrations", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bundle", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src\\Woong.MonitorStack.Server\\Woong.MonitorStack.Server.csproj", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MonitorDbContext", script, StringComparison.Ordinal);
        Assert.Contains("--output", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--self-contained", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not apply", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database update", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--connection", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings__MonitorDb", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerMigrationBundleScript_HelpAndDryRunDoNotBuildOrApplyBundle()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "build-server-migration-bundle.ps1");
        string outputPath = Path.Combine(
            Path.GetTempPath(),
            $"woong-migration-dry-run-{Guid.NewGuid():N}.exe");

        try
        {
            ProcessResult help = RunPowerShell(
                repoRoot,
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Help");

            Assert.Equal(0, help.ExitCode);
            Assert.Contains("Usage:", help.StandardOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("does not apply migrations", help.StandardOutput, StringComparison.OrdinalIgnoreCase);

            ProcessResult dryRun = RunPowerShell(
                repoRoot,
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Configuration Release -OutputPath \"{outputPath}\" -DryRun");

            Assert.Equal(0, dryRun.ExitCode);
            Assert.Contains("Dry run: dotnet ef migrations bundle", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--output", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(outputPath, dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No bundle was built", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPath), "Dry run must not create a migration bundle.");
            Assert.DoesNotContain("database update", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("--connection", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ProductionMigrationRunbook_DocumentsDeploymentPathAndSafetyChecks()
    {
        string repoRoot = FindRepositoryRoot();
        string runbookPath = Path.Combine(repoRoot, "docs", "production-migrations.md");
        string migrationsDirectory = Path.Combine(
            repoRoot,
            "src",
            "Woong.MonitorStack.Server",
            "Data",
            "Migrations");

        Assert.True(File.Exists(runbookPath), "Production migration runbook must exist.");
        string runbook = File.ReadAllText(runbookPath);

        foreach (string migrationFile in Directory.EnumerateFiles(migrationsDirectory, "*.cs")
                     .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal))
                     .Select(Path.GetFileNameWithoutExtension)
                     .Where(name => name is not null && name != "MonitorDbContextModelSnapshot")
                     .Select(name => name!))
        {
            Assert.Contains(migrationFile, runbook, StringComparison.Ordinal);
        }

        Assert.Contains("dotnet ef migrations add", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet ef migrations bundle", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet ef database update", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MonitorDbContext", runbook, StringComparison.Ordinal);
        Assert.Contains("pg_dump", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("backup", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reset", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rollback", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts\\run-server-postgres-validation.ps1", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WOONG_MONITOR_RUN_POSTGRES_TESTS=1", runbook, StringComparison.Ordinal);
        Assert.Contains("dotnet test tests\\Woong.MonitorStack.Server.Tests\\Woong.MonitorStack.Server.Tests.csproj", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet build src\\Woong.MonitorStack.Server\\Woong.MonitorStack.Server.csproj", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConnectionStrings__MonitorDb", runbook, StringComparison.Ordinal);
        Assert.Contains("production", runbook, StringComparison.OrdinalIgnoreCase);
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

    private static ProcessResult RunPowerShell(string workingDirectory, string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo(
            "powershell.exe",
            arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Could not start PowerShell.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
