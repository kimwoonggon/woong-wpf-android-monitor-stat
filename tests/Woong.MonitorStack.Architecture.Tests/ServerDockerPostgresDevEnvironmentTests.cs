using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class ServerDockerPostgresDevEnvironmentTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void DockerCompose_DefinesLocalPostgresServiceForServerDashboard()
    {
        string composePath = Path.Combine(RepositoryRoot, "docker-compose.yml");

        Assert.True(File.Exists(composePath), "Root docker-compose.yml must define local PostgreSQL for the integrated dashboard.");
        string compose = File.ReadAllText(composePath);

        Assert.Contains("postgres:", compose, StringComparison.Ordinal);
        Assert.Contains("postgres:16-alpine", compose, StringComparison.Ordinal);
        Assert.Contains("woong-monitor-postgres", compose, StringComparison.Ordinal);
        Assert.Contains("${WOONG_POSTGRES_HOST_PORT:-55432}:5432", compose, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_DB", compose, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_USER", compose, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_PASSWORD", compose, StringComparison.Ordinal);
        Assert.Contains("pg_isready", compose, StringComparison.Ordinal);
        Assert.Contains("woong_monitor_postgres_data", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("5432:5432", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void DockerPostgresEnvExample_UsesNonProductionDevelopmentDefaults()
    {
        string envExamplePath = Path.Combine(RepositoryRoot, ".env.example");

        Assert.True(File.Exists(envExamplePath), ".env.example must document local Docker PostgreSQL settings.");
        string envExample = File.ReadAllText(envExamplePath);

        Assert.Contains("POSTGRES_DB=woong_monitor", envExample, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_USER=woong", envExample, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_PASSWORD=woong_dev_password", envExample, StringComparison.Ordinal);
        Assert.Contains("WOONG_POSTGRES_HOST_PORT=55432", envExample, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings__MonitorDb=Host=localhost;Port=55432;Database=woong_monitor;Username=woong;Password=woong_dev_password", envExample, StringComparison.Ordinal);
        Assert.Contains("Local development only", envExample, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not reuse", envExample, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DockerPostgresScripts_SupportHelpAndDryRunWithoutDocker()
    {
        string startScriptPath = Path.Combine(RepositoryRoot, "scripts", "start-server-postgres.ps1");
        string stopScriptPath = Path.Combine(RepositoryRoot, "scripts", "stop-server-postgres.ps1");

        Assert.True(File.Exists(startScriptPath), "Start script must exist.");
        Assert.True(File.Exists(stopScriptPath), "Stop script must exist.");

        ProcessResult startHelp = RunPowerShell($"-NoProfile -ExecutionPolicy Bypass -File \"{startScriptPath}\" -Help");
        ProcessResult startDryRun = RunPowerShell($"-NoProfile -ExecutionPolicy Bypass -File \"{startScriptPath}\" -DryRun");
        ProcessResult stopDryRun = RunPowerShell($"-NoProfile -ExecutionPolicy Bypass -File \"{stopScriptPath}\" -DryRun");

        Assert.Equal(0, startHelp.ExitCode);
        Assert.Contains("Usage:", startHelp.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, startDryRun.ExitCode);
        Assert.Contains("docker compose", startDryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet ef database update", startDryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConnectionStrings__MonitorDb", startDryRun.StandardOutput, StringComparison.Ordinal);
        Assert.Equal(0, stopDryRun.ExitCode);
        Assert.Contains("docker compose", stopDryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("down", stopDryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServerDevelopmentConfigAndReadme_DocumentDockerPostgresAndBlazorDashboard()
    {
        string appsettingsPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Woong.MonitorStack.Server",
            "appsettings.Development.json");
        string readmePath = Path.Combine(RepositoryRoot, "README.md");

        string appsettings = File.ReadAllText(appsettingsPath);
        string readme = File.ReadAllText(readmePath);

        Assert.Contains("\"MonitorDb\"", appsettings, StringComparison.Ordinal);
        Assert.Contains("Port=55432", appsettings, StringComparison.Ordinal);
        Assert.Contains("Username=woong", appsettings, StringComparison.Ordinal);
        Assert.Contains("Password=woong_dev_password", appsettings, StringComparison.Ordinal);
        Assert.Contains("scripts\\start-server-postgres.ps1", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts\\stop-server-postgres.ps1", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/dashboard?userId=", readme, StringComparison.Ordinal);
        Assert.Contains("55432", readme, StringComparison.Ordinal);
    }

    private static ProcessResult RunPowerShell(string arguments)
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

        return new ProcessResult(process.ExitCode, output, error);
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

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
