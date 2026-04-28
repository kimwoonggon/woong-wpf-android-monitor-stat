using System.Text.Json;
using System.Text.Json.Serialization;

namespace Woong.MonitorStack.Windows.Browser;

public static class NativeMessagingHostManifestGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string GenerateJson(
        string hostName,
        string hostExecutablePath,
        string chromeExtensionId,
        string description)
    {
        var manifest = new NativeMessagingHostManifest(
            EnsureText(hostName, nameof(hostName)),
            EnsureText(description, nameof(description)),
            EnsureText(hostExecutablePath, nameof(hostExecutablePath)),
            "stdio",
            [$"chrome-extension://{EnsureText(chromeExtensionId, nameof(chromeExtensionId))}/"]);

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    private static string EnsureText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value.Trim();

    private sealed record NativeMessagingHostManifest(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("allowed_origins")] IReadOnlyList<string> AllowedOrigins);
}
