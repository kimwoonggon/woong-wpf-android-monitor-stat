namespace Woong.MonitorStack.Architecture.Tests;

public sealed class ServerApiContractDocumentationTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ContractsDoc_DocumentsDeviceTokenRevocationContract()
    {
        string contractsPath = Path.Combine(RepositoryRoot, "docs", "contracts.md");

        Assert.True(File.Exists(contractsPath), "Public API contract documentation must exist.");
        string contracts = File.ReadAllText(contractsPath);

        Assert.Contains("POST /api/devices/{deviceId}/token/revoke", contracts, StringComparison.Ordinal);
        Assert.Contains("X-Device-Token", contracts, StringComparison.Ordinal);
        Assert.Contains("204 NoContent", contracts, StringComparison.Ordinal);
        Assert.Contains("401 Unauthorized", contracts, StringComparison.Ordinal);
        string normalizedContracts = NormalizeWhitespace(contracts);
        Assert.Contains("existing focus session, web session, raw event, and location context rows are preserved", normalizedContracts, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicReleaseDocs_DocumentUserAuthRegistrationBlockerAndSkippedPolicyTests()
    {
        string contractsPath = Path.Combine(RepositoryRoot, "docs", "contracts.md");
        string releaseChecklistPath = Path.Combine(RepositoryRoot, "docs", "release-checklist.md");

        Assert.True(File.Exists(contractsPath), "Public API contract documentation must exist.");
        Assert.True(File.Exists(releaseChecklistPath), "Release checklist documentation must exist.");

        string combinedDocs = NormalizeWhitespace(
            File.ReadAllText(contractsPath) + " " + File.ReadAllText(releaseChecklistPath));

        Assert.Contains("device registration currently uses dev/MVP payload userId", StripMarkdownCodeTicks(combinedDocs), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("authenticated user/session identity", combinedDocs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("before public release", combinedDocs, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("RegisterDevice_WhenUserSessionAuthIsMissing_ReturnsUnauthorized", combinedDocs, StringComparison.Ordinal);
        Assert.Contains("RegisterDevice_UsesAuthenticatedUserIdInsteadOfPayloadUserId", combinedDocs, StringComparison.Ordinal);
        Assert.Contains("RegisterDevice_WhenSameDeviceKeyIsRegisteredByDifferentUsers_CreatesSeparateDevices", combinedDocs, StringComparison.Ordinal);
        Assert.Contains("RegisterDevice_WhenPayloadUserIdTargetsAnotherUser_DoesNotReturnExistingDeviceToken", combinedDocs, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicReleaseDocs_RequireProductionStrictAuthAndRealUserSessionProviderDecision()
    {
        string releaseChecklistPath = Path.Combine(RepositoryRoot, "docs", "release-checklist.md");
        string hardeningPlanPath = Path.Combine(RepositoryRoot, "docs", "android-server-sync-hardening-plan.md");

        Assert.True(File.Exists(releaseChecklistPath), "Release checklist documentation must exist.");
        Assert.True(File.Exists(hardeningPlanPath), "Android/server sync hardening plan must exist.");

        string releaseChecklist = NormalizeWhitespace(File.ReadAllText(releaseChecklistPath));
        string hardeningPlan = NormalizeWhitespace(File.ReadAllText(hardeningPlanPath));
        string combinedDocs = StripMarkdownCodeTicks($"{releaseChecklist} {hardeningPlan}");

        Assert.Contains("dev/MVP payload mode", combinedDocs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production strict-auth mode", combinedDocs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DeviceRegistrationAuth:RequireAuthenticatedUser=true", combinedDocs, StringComparison.Ordinal);
        Assert.Contains("real user/session provider", combinedDocs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("header stub", combinedDocs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must remain an open release blocker", combinedDocs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[x] Production user/session provider selected", releaseChecklist, StringComparison.OrdinalIgnoreCase);
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
    {
        return string.Join(
            " ",
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string StripMarkdownCodeTicks(string value)
    {
        return value.Replace("`", string.Empty, StringComparison.Ordinal);
    }
}
