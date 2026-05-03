using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Sync;

public interface IWindowsDeviceRegistrationClient
{
    Task<WindowsDeviceRegistrationResponse> RegisterAsync(
        WindowsDeviceRegistrationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record WindowsDeviceRegistrationRequest(
    string UserId,
    Platform Platform,
    string DeviceKey,
    string DeviceName,
    string TimezoneId);

public sealed record WindowsDeviceRegistrationResponse(
    [property: JsonPropertyName("deviceId")] string ServerDeviceId,
    [property: JsonPropertyName("deviceToken")] string DeviceToken);

public sealed class HttpWindowsDeviceRegistrationClient : IWindowsDeviceRegistrationClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpWindowsDeviceRegistrationClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<WindowsDeviceRegistrationResponse> RegisterAsync(
        WindowsDeviceRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/devices/register", UriKind.Relative))
        {
            Content = JsonContent.Create(request, options: SerializerOptions)
        };
        httpRequest.Headers.TryAddWithoutValidation("X-Woong-User-Id", request.UserId);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        WindowsDeviceRegistrationResponse? registration =
            await JsonSerializer.DeserializeAsync<WindowsDeviceRegistrationResponse>(
                stream,
                SerializerOptions,
                cancellationToken);

        return registration is null ||
            string.IsNullOrWhiteSpace(registration.ServerDeviceId) ||
            string.IsNullOrWhiteSpace(registration.DeviceToken)
                ? throw new InvalidOperationException("Device registration returned an invalid response.")
                : registration;
    }
}
