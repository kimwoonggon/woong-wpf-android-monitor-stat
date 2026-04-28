using System.Globalization;
using System.Text.Json;
using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Sync;

public sealed class HttpWindowsSummaryApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpWindowsSummaryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DailySummary> GetDailySummaryAsync(
        string userId,
        DateOnly summaryDate,
        string timezoneId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);

        string date = summaryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string path =
            $"/api/daily-summaries/{date}?userId={Uri.EscapeDataString(userId)}&timezoneId={Uri.EscapeDataString(timezoneId)}";

        using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        DailySummary? summary = await JsonSerializer.DeserializeAsync<DailySummary>(
            stream,
            SerializerOptions,
            cancellationToken);

        return summary ?? throw new InvalidOperationException("Daily summary response was empty.");
    }
}
