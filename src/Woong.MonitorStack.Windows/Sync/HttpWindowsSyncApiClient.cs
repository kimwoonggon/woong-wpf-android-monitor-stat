using System.Text;
using System.Text.Json;
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

    public HttpWindowsSyncApiClient(HttpClient httpClient, WindowsSyncClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<UploadBatchResult> UploadAsync(
        SyncOutboxItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        using var request = new HttpRequestMessage(HttpMethod.Post, GetEndpointUri(item.AggregateType))
        {
            Content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-Device-Token", _options.DeviceToken);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

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
}
