using System.Diagnostics;
using System.Text.Json;

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

    [Fact]
    public void ServerPostgresValidationScript_WhenDockerIsUnavailable_WritesBlockedArtifactsAndDoesNotRunTests()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-server-postgres-validation.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-postgres-blocked-{Guid.NewGuid():N}");
        string fakeBin = Path.Combine(tempRoot, "bin");
        string outputRoot = Path.Combine(tempRoot, "artifacts");
        string dotnetMarker = Path.Combine(tempRoot, "dotnet-called.txt");

        try
        {
            Directory.CreateDirectory(fakeBin);
            WriteCommand(
                Path.Combine(fakeBin, "docker.cmd"),
                """
                @echo off
                echo Docker daemon unavailable 1>&2
                exit /b 1
                """);
            WriteCommand(
                Path.Combine(fakeBin, "dotnet.cmd"),
                """
                @echo off
                echo dotnet should not run>"%FAKE_DOTNET_MARKER%"
                exit /b 1
                """);

            ProcessResult result = RunPowerShell(
                repoRoot,
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{outputRoot}\"",
                fakeBin,
                new Dictionary<string, string?>
                {
                    ["FAKE_DOTNET_MARKER"] = dotnetMarker
                });

            Assert.Equal(2, result.ExitCode);
            Assert.False(File.Exists(dotnetMarker), "PostgreSQL tests must not run when Docker capacity is unavailable.");

            string latestManifestPath = Path.Combine(outputRoot, "latest", "manifest.json");
            string latestReportPath = Path.Combine(outputRoot, "latest", "report.md");
            string latestTestOutputPath = Path.Combine(outputRoot, "latest", "dotnet-test-output.txt");
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(latestManifestPath));

            Assert.Equal("BLOCKED", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("docker-unavailable", manifest.RootElement.GetProperty("classification").GetString());
            Assert.Contains("Docker daemon unavailable", manifest.RootElement.GetProperty("blockedReason").GetString());
            Assert.DoesNotContain(
                "dotnet-test-output.txt",
                manifest.RootElement.GetProperty("artifacts").EnumerateArray().Select(artifact => artifact.GetString()));
            Assert.False(File.Exists(latestTestOutputPath), "Blocked runs must not publish stale dotnet test output.");
            string report = File.ReadAllText(latestReportPath);
            Assert.Contains("Status: BLOCKED", report, StringComparison.Ordinal);
            Assert.DoesNotContain("dotnet-test-output.txt", report, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ServerPostgresValidationScript_WhenPostgresTestsFail_WritesFailArtifacts()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-server-postgres-validation.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"woong-postgres-fail-{Guid.NewGuid():N}");
        string fakeBin = Path.Combine(tempRoot, "bin");
        string outputRoot = Path.Combine(tempRoot, "artifacts");
        string dotnetMarker = Path.Combine(tempRoot, "dotnet-called.txt");

        try
        {
            Directory.CreateDirectory(fakeBin);
            WriteCommand(
                Path.Combine(fakeBin, "docker.cmd"),
                """
                @echo off
                echo Docker OK
                exit /b 0
                """);
            WriteCommand(
                Path.Combine(fakeBin, "dotnet.cmd"),
                """
                @echo off
                echo dotnet called>"%FAKE_DOTNET_MARKER%"
                echo fake postgres test failure
                exit /b 1
                """);

            ProcessResult result = RunPowerShell(
                repoRoot,
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -OutputRoot \"{outputRoot}\"",
                fakeBin,
                new Dictionary<string, string?>
                {
                    ["FAKE_DOTNET_MARKER"] = dotnetMarker
                });

            Assert.Equal(1, result.ExitCode);
            Assert.True(File.Exists(dotnetMarker), "PostgreSQL tests should run after Docker preflight succeeds.");

            string latestManifestPath = Path.Combine(outputRoot, "latest", "manifest.json");
            string latestReportPath = Path.Combine(outputRoot, "latest", "report.md");
            string latestTestOutputPath = Path.Combine(outputRoot, "latest", "dotnet-test-output.txt");
            using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(latestManifestPath));

            Assert.Equal("FAIL", manifest.RootElement.GetProperty("status").GetString());
            Assert.Equal("postgres-tests-failed", manifest.RootElement.GetProperty("classification").GetString());
            Assert.Contains("dotnet test exit code 1", manifest.RootElement.GetProperty("blockedReason").GetString());
            Assert.Contains("Status: FAIL", File.ReadAllText(latestReportPath), StringComparison.Ordinal);
            Assert.Contains("fake postgres test failure", File.ReadAllText(latestTestOutputPath), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ServerPostgresValidationScript_ScopesPostgresOptInEnvironmentVariable()
    {
        string repoRoot = FindRepositoryRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "run-server-postgres-validation.ps1");

        string script = File.ReadAllText(scriptPath);

        Assert.Contains("$previousValue = $env:WOONG_MONITOR_RUN_POSTGRES_TESTS", script, StringComparison.Ordinal);
        Assert.Contains("$env:WOONG_MONITOR_RUN_POSTGRES_TESTS = \"1\"", script, StringComparison.Ordinal);
        Assert.Contains("finally", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Remove-Item Env:\\WOONG_MONITOR_RUN_POSTGRES_TESTS", script, StringComparison.Ordinal);
        Assert.Contains("$env:WOONG_MONITOR_RUN_POSTGRES_TESTS = $previousValue", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerPostgresValidationDocs_DocumentBlockedVsFailAndScopedOptIn()
    {
        string repoRoot = FindRepositoryRoot();
        string docsPath = Path.Combine(repoRoot, "docs", "server-test-db-strategy.md");

        Assert.True(File.Exists(docsPath), "Server test database strategy documentation must exist.");
        string docs = File.ReadAllText(docsPath);

        Assert.Contains("BLOCKED", docs, StringComparison.Ordinal);
        Assert.Contains("exit `2`", docs, StringComparison.Ordinal);
        Assert.Contains("Docker/Testcontainers capacity was unavailable", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FAIL", docs, StringComparison.Ordinal);
        Assert.Contains("exit `1`", docs, StringComparison.Ordinal);
        Assert.Contains("Docker was available but the PostgreSQL validation command failed", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WOONG_MONITOR_RUN_POSTGRES_TESTS=1", docs, StringComparison.Ordinal);
        Assert.Contains("previous environment value is restored", docs, StringComparison.OrdinalIgnoreCase);
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
        => RunPowerShell(workingDirectory, arguments, pathPrefix: null, environmentVariables: null);

    private static ProcessResult RunPowerShell(
        string workingDirectory,
        string arguments,
        string? pathPrefix,
        IReadOnlyDictionary<string, string?>? environmentVariables)
    {
        var startInfo = new ProcessStartInfo(
            "powershell.exe",
            arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (!string.IsNullOrWhiteSpace(pathPrefix))
        {
            startInfo.Environment["PATH"] = pathPrefix + Path.PathSeparator + startInfo.Environment["PATH"];
        }

        if (environmentVariables is not null)
        {
            foreach (KeyValuePair<string, string?> variable in environmentVariables)
            {
                if (variable.Value is null)
                {
                    startInfo.Environment.Remove(variable.Key);
                }
                else
                {
                    startInfo.Environment[variable.Key] = variable.Value;
                }
            }
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start PowerShell.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private static void WriteCommand(string path, string contents)
    {
        File.WriteAllText(path, contents.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
