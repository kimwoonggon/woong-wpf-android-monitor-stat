using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Sync;

public sealed class HttpWindowsSyncApiClient : IWindowsSyncApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly WindowsSyncClientOptions _options;
    private readonly IWindowsSyncTokenStore? _tokenStore;
    private readonly IWindowsSyncRegistrationStore? _registrationStore;

    public HttpWindowsSyncApiClient(HttpClient httpClient, WindowsSyncClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public HttpWindowsSyncApiClient(
        HttpClient httpClient,
        WindowsSyncClientOptions options,
        IWindowsSyncTokenStore tokenStore,
        IWindowsSyncRegistrationStore registrationStore)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _registrationStore = registrationStore ?? throw new ArgumentNullException(nameof(registrationStore));
    }

    public async Task<UploadBatchResult> UploadAsync(
        SyncOutboxItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        string deviceToken = ResolveDeviceToken();
        string payloadJson = ResolvePayloadJson(item.PayloadJson);
        using var request = new HttpRequestMessage(HttpMethod.Post, GetEndpointUri(item.AggregateType))
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-Device-Token", deviceToken);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Sync upload failed with HTTP {(int)response.StatusCode}.");
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        UploadBatchResult? result = await JsonSerializer.DeserializeAsync<UploadBatchResult>(
            stream,
            SerializerOptions,
            cancellationToken);

        return result ?? new UploadBatchResult([]);
    }

    private Uri GetEndpointUri(string aggregateType)
        => new(_options.ServerBaseUri, GetEndpointPath(aggregateType));

    private static string GetEndpointPath(string aggregateType)
        => aggregateType switch
        {
            "focus_session" => "/api/focus-sessions/upload",
            "web_session" => "/api/web-sessions/upload",
            "raw_event" => "/api/raw-events/upload",
            _ => throw new InvalidOperationException($"Unsupported sync aggregate type: {aggregateType}.")
        };

    private string ResolveDeviceToken()
    {
        string? deviceToken = _tokenStore is null
            ? _options.DeviceToken
            : _tokenStore.GetDeviceToken();

        return string.IsNullOrWhiteSpace(deviceToken)
            ? throw new InvalidOperationException("Windows sync device token is missing.")
            : deviceToken.Trim();
    }

    private string ResolvePayloadJson(string payloadJson)
    {
        if (_registrationStore is null)
        {
            return payloadJson;
        }

        WindowsSyncRegistration registration = _registrationStore.GetRegistration()
            ?? throw new InvalidOperationException("Windows sync device registration is missing.");

        return RewriteRootDeviceId(payloadJson, registration.ServerDeviceId);
    }

    private static string RewriteRootDeviceId(string payloadJson, string serverDeviceId)
    {
        JsonNode? node = JsonNode.Parse(payloadJson);
        if (node is not JsonObject root || !root.ContainsKey("deviceId"))
        {
            return payloadJson;
        }

        root["deviceId"] = serverDeviceId;
        return root.ToJsonString();
    }
}
