using System.Xml.Linq;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class AndroidProductionEndpointPolicyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AndroidProductionEndpointPolicy_IsDocumentedForReleaseBuildsAndLocalDevelopment()
    {
        string hardeningPlan = NormalizeWhitespace(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "android-server-sync-hardening-plan.md")));
        string releaseChecklist = NormalizeWhitespace(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "release-checklist.md")));
        string readme = NormalizeWhitespace(File.ReadAllText(Path.Combine(RepositoryRoot, "README.md")));

        Assert.Contains("Release builds must not silently fall back to a local, blank, or example endpoint.", hardeningPlan, StringComparison.Ordinal);
        Assert.Contains("If the production endpoint is unset, Android/server sync remains disabled", hardeningPlan, StringComparison.Ordinal);
        Assert.Contains("Release builds may accept user-entered endpoints only as an explicit advanced/manual configuration path", hardeningPlan, StringComparison.Ordinal);
        Assert.Contains("Local developer HTTP endpoints are limited to loopback", hardeningPlan, StringComparison.Ordinal);
        Assert.Contains("must be labeled nonproduction", hardeningPlan, StringComparison.Ordinal);

        Assert.Contains("production endpoint is unset", releaseChecklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sync must remain disabled", releaseChecklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user-entered endpoint", releaseChecklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicit advanced/manual configuration", releaseChecklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("loopback-only HTTP exceptions", releaseChecklist, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("must not silently fall back to a local, blank, or example endpoint", readme, StringComparison.Ordinal);
        Assert.Contains("user-entered endpoints only as explicit advanced/manual configuration", readme, StringComparison.Ordinal);
        Assert.Contains("woongProductionSyncBaseUrl", hardeningPlan, StringComparison.Ordinal);
        Assert.Contains("WOONG_ANDROID_PRODUCTION_SYNC_BASE_URL", hardeningPlan, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidProductionEndpointSource_IsBuildConfigBackedAndDefaultsBlank()
    {
        string buildGradle = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "android",
            "app",
            "build.gradle.kts"));
        string syncSettings = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "android",
            "app",
            "src",
            "main",
            "java",
            "com",
            "woong",
            "monitorstack",
            "settings",
            "AndroidSyncSettings.kt"));
        string validator = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "android",
            "app",
            "src",
            "main",
            "java",
            "com",
            "woong",
            "monitorstack",
            "settings",
            "AndroidSyncServerUrlValidator.kt"));

        Assert.Contains("buildConfig = true", buildGradle, StringComparison.Ordinal);
        Assert.Contains("woongProductionSyncBaseUrl", buildGradle, StringComparison.Ordinal);
        Assert.Contains("WOONG_ANDROID_PRODUCTION_SYNC_BASE_URL", buildGradle, StringComparison.Ordinal);
        Assert.Contains("\"PRODUCTION_SYNC_BASE_URL\"", buildGradle, StringComparison.Ordinal);
        Assert.Contains(".getOrElse(\"\")", buildGradle, StringComparison.Ordinal);

        Assert.Contains("BuildConfig.PRODUCTION_SYNC_BASE_URL", syncSettings, StringComparison.Ordinal);
        Assert.Contains("productionEndpointOrBlank", syncSettings, StringComparison.Ordinal);
        Assert.Contains("isValidProductionEndpoint", validator, StringComparison.Ordinal);
        Assert.Contains("!isLoopbackHost(host)", validator, StringComparison.Ordinal);
        Assert.Contains("!isExampleHost(host)", validator, StringComparison.Ordinal);
    }

    [Fact]
    public void AndroidNetworkSecurity_AllowsCleartextOnlyForLoopbackLocalDevelopment()
    {
        XDocument manifest = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "android",
            "app",
            "src",
            "main",
            "AndroidManifest.xml"));
        XNamespace android = "http://schemas.android.com/apk/res/android";

        XElement application = manifest.Root?.Element("application")
            ?? throw new InvalidOperationException("Android manifest must contain application element.");
        Assert.Equal(
            "@xml/network_security_config",
            application.Attribute(android + "networkSecurityConfig")?.Value);

        XDocument networkConfig = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "android",
            "app",
            "src",
            "main",
            "res",
            "xml",
            "network_security_config.xml"));

        XElement baseConfig = networkConfig.Root?.Element("base-config")
            ?? throw new InvalidOperationException("Network security config must contain base-config.");
        Assert.Equal("false", baseConfig.Attribute("cleartextTrafficPermitted")?.Value);

        var cleartextDomainConfigs = networkConfig
            .Descendants("domain-config")
            .Where(config => string.Equals(
                config.Attribute("cleartextTrafficPermitted")?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Single(cleartextDomainConfigs);

        XElement[] cleartextDomainElements = cleartextDomainConfigs[0]
            .Elements("domain")
            .ToArray();

        string[] cleartextDomains = cleartextDomainElements
            .Select(domain => domain.Value.Trim())
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["127.0.0.1", "::1", "localhost"], cleartextDomains);
        Assert.All(
            cleartextDomainElements,
            domain => Assert.Equal("true", domain.Attribute("includeSubdomains")?.Value));
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

    private static string NormalizeWhitespace(string value)
        => string.Join(
            " ",
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
