using System.Net;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Sync;

namespace Woong.MonitorStack.Windows.Tests.Sync;

public sealed class HttpWindowsSummaryApiClientTests
{
    [Fact]
    public async Task GetDailySummaryAsync_CallsIntegratedSummaryEndpoint()
    {
        var handler = new CapturingHandler(
            """
            {
              "summaryDate":"2026-04-28",
              "totalActiveMs":900000,
              "totalIdleMs":120000,
              "totalWebMs":240000,
              "topApps":[{"key":"Chrome","durationMs":900000}],
              "topDomains":[{"key":"example.com","durationMs":240000}]
            }
            """);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://monitor.example")
        };
        var client = new HttpWindowsSummaryApiClient(httpClient);

        DailySummary summary = await client.GetDailySummaryAsync(
            userId: "user-1",
            summaryDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul");

        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("/api/daily-summaries/2026-04-28", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("user-1", GetQueryValue(handler.Request.RequestUri, "userId"));
        Assert.Equal("Asia/Seoul", GetQueryValue(handler.Request.RequestUri, "timezoneId"));
        Assert.Equal(new DateOnly(2026, 4, 28), summary.SummaryDate);
        Assert.Equal(900_000, summary.TotalActiveMs);
        Assert.Equal("Chrome", Assert.Single(summary.TopApps).Key);
        Assert.Equal("example.com", Assert.Single(summary.TopDomains).Key);
    }

    private static string? GetQueryValue(Uri uri, string key)
    {
        string query = uri.Query.TrimStart('?');
        return query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => Uri.UnescapeDataString(parts[0]) == key)
            .Select(parts => Uri.UnescapeDataString(parts[1]))
            .SingleOrDefault();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public CapturingHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson)
            });
        }
    }
}
