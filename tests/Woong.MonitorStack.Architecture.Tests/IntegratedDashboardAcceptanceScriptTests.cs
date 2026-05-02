using System.Diagnostics;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class IntegratedDashboardAcceptanceScriptTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void IntegratedDashboardAcceptanceScript_SeedsWindowsAndroidDataAndCapturesDashboard()
    {
        string scriptPath = Path.Combine(RepositoryRoot, "scripts", "run-integrated-dashboard-acceptance.ps1");

        Assert.True(File.Exists(scriptPath), "Integrated dashboard acceptance script must exist.");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("scripts\\start-server-postgres.ps1", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet run --project src\\Woong.MonitorStack.Server\\Woong.MonitorStack.Server.csproj", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/devices/register", script, StringComparison.Ordinal);
        Assert.Contains("/api/focus-sessions/upload", script, StringComparison.Ordinal);
        Assert.Contains("/api/web-sessions/upload", script, StringComparison.Ordinal);
        Assert.Contains("/api/location-contexts/upload", script, StringComparison.Ordinal);
        Assert.Contains("/api/dashboard/integrated", script, StringComparison.Ordinal);
        Assert.Contains("/dashboard?userId=", script, StringComparison.Ordinal);
        Assert.Contains("windows-wpf-work", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("android-usage-work", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npx", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("playwright", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Invoke-PlaywrightScreenshot", script, StringComparison.Ordinal);
        Assert.Contains("1440,1000", script, StringComparison.Ordinal);
        Assert.Contains("390,900", script, StringComparison.Ordinal);
        Assert.Contains("$LASTEXITCODE", script, StringComparison.Ordinal);
        Assert.Contains("dashboard-1440.png", script, StringComparison.Ordinal);
        Assert.Contains("dashboard-390.png", script, StringComparison.Ordinal);
        Assert.Contains("report.md", script, StringComparison.Ordinal);
        Assert.Contains("manifest.json", script, StringComparison.Ordinal);
        Assert.Contains("deviceToken", script, StringComparison.Ordinal);
        Assert.Contains("Forbidden privacy marker", script, StringComparison.Ordinal);
    }

    [Fact]
    public void IntegratedDashboardDesignSvg_IsFigmaImportFriendlyAndShowsDashboardStructure()
    {
        string svgPath = Path.Combine(
            RepositoryRoot,
            "artifacts",
            "blazor-dashboard-design",
            "integrated-dashboard-design.svg");

        Assert.True(File.Exists(svgPath), "Integrated dashboard design SVG must exist.");
        string svg = File.ReadAllText(svgPath);

        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("Integrated Device Dashboard", svg, StringComparison.Ordinal);
        Assert.Contains("Windows + Android", svg, StringComparison.Ordinal);
        Assert.Contains("Active Focus", svg, StringComparison.Ordinal);
        Assert.Contains("Platform Totals", svg, StringComparison.Ordinal);
        Assert.Contains("Top Apps", svg, StringComparison.Ordinal);
        Assert.Contains("Top Domains", svg, StringComparison.Ordinal);
        Assert.Contains("Location Samples", svg, StringComparison.Ordinal);
        Assert.Contains("Devices", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IntegratedDashboardAcceptanceScript_HelpAndDryRunAreSafe()
    {
        string scriptPath = Path.Combine(RepositoryRoot, "scripts", "run-integrated-dashboard-acceptance.ps1");

        ProcessResult help = RunPowerShell($"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Help");
        ProcessResult dryRun = RunPowerShell($"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -DryRun");

        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Usage:", help.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, dryRun.ExitCode);
        Assert.Contains("Dry run", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("synthetic Windows WPF", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("synthetic Android", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Playwright", dryRun.StandardOutput, StringComparison.OrdinalIgnoreCase);
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
