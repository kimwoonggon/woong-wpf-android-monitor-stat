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
}
