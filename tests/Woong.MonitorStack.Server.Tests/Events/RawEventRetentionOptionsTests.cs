using Microsoft.Extensions.Configuration;
using Woong.MonitorStack.Server.Events;

namespace Woong.MonitorStack.Server.Tests.Events;

public sealed class RawEventRetentionOptionsTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AppSettings_DefinesProductionSafeRawEventRetentionDefaults()
    {
        RawEventRetentionOptions options = LoadOptions("appsettings.json");

        Assert.True(options.Enabled);
        Assert.Equal(30, options.RetentionDays);
        Assert.Equal(TimeSpan.FromDays(1), options.Interval);
    }

    [Fact]
    public void DevelopmentAppSettings_DisablesRawEventRetentionForLocalRuns()
    {
        RawEventRetentionOptions options = LoadOptions(
            "appsettings.json",
            "appsettings.Development.json");

        Assert.False(options.Enabled);
        Assert.Equal(30, options.RetentionDays);
        Assert.Equal(TimeSpan.FromDays(1), options.Interval);
    }

    private static RawEventRetentionOptions LoadOptions(params string[] files)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(RepositoryRoot, "src", "Woong.MonitorStack.Server"));

        foreach (string file in files)
        {
            configuration.AddJsonFile(file, optional: false, reloadOnChange: false);
        }

        return configuration.Build()
            .GetSection("RawEventRetention")
            .Get<RawEventRetentionOptions>()
            ?? new RawEventRetentionOptions();
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
