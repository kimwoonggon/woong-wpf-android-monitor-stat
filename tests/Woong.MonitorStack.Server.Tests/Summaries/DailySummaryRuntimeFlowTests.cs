using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Tests.Data;

namespace Woong.MonitorStack.Server.Tests.Summaries;

public sealed class DailySummaryRuntimeFlowTests
{
    [Fact]
    public async Task DailySummaryApi_WhenWindowsAndAndroidClientsUploadSessions_ReturnsIntegratedSummary()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        string windowsDeviceId = await RegisterDeviceAsync(
            client,
            Platform.Windows,
            "windows-runtime-key",
            "Windows Workstation",
            userId: "user-1");
        string androidDeviceId = await RegisterDeviceAsync(
            client,
            Platform.Android,
            "android-runtime-key",
            "Android Phone",
            userId: "user-1");
        string otherUserDeviceId = await RegisterDeviceAsync(
            client,
            Platform.Windows,
            "other-runtime-key",
            "Other Workstation",
            userId: "user-2");

        await UploadFocusSessionAsync(
            client,
            windowsDeviceId,
            Focus("windows-active", "chrome.exe", 600_000, isIdle: false));
        await UploadFocusSessionAsync(
            client,
            androidDeviceId,
            Focus("android-active", "com.android.chrome", 300_000, isIdle: false));
        await UploadFocusSessionAsync(
            client,
            windowsDeviceId,
            Focus("windows-idle", "chrome.exe", 120_000, isIdle: true));
        await UploadFocusSessionAsync(
            client,
            otherUserDeviceId,
            Focus("other-user-active", "chrome.exe", 999_000, isIdle: false));
        await UploadWebSessionAsync(
            client,
            windowsDeviceId,
            Web("windows-web", "windows-active", "example.com", 240_000));

        HttpResponseMessage response = await client.GetAsync(
            "/api/daily-summaries/2026-04-28?userId=user-1&timezoneId=Asia%2FSeoul");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement root = json.RootElement;
        Assert.Equal(900_000, root.GetProperty("totalActiveMs").GetInt64());
        Assert.Equal(120_000, root.GetProperty("totalIdleMs").GetInt64());
        Assert.Equal(240_000, root.GetProperty("totalWebMs").GetInt64());
        JsonElement topApp = root.GetProperty("topApps")[0];
        Assert.Equal("Chrome", topApp.GetProperty("key").GetString());
        Assert.Equal(900_000, topApp.GetProperty("durationMs").GetInt64());
        JsonElement topDomain = root.GetProperty("topDomains")[0];
        Assert.Equal("example.com", topDomain.GetProperty("key").GetString());
        Assert.Equal(240_000, topDomain.GetProperty("durationMs").GetInt64());

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Equal(4, await dbContext.FocusSessions.CountAsync());
        WebSessionEntity webSession = Assert.Single(await dbContext.WebSessions.ToListAsync());
        Assert.Null(webSession.Url);
        Assert.Null(webSession.PageTitle);
    }

    private static async Task<string> RegisterDeviceAsync(
        HttpClient client,
        Platform platform,
        string deviceKey,
        string deviceName,
        string userId)
    {
        var request = new RegisterDeviceRequest(
            userId,
            platform,
            deviceKey,
            deviceName,
            timezoneId: "Asia/Seoul");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/devices/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("deviceId").GetString()!;
    }

    private static async Task UploadFocusSessionAsync(
        HttpClient client,
        string deviceId,
        FocusSessionUploadItem session)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/focus-sessions/upload",
            new UploadFocusSessionsRequest(deviceId, [session]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, json.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
    }

    private static async Task UploadWebSessionAsync(
        HttpClient client,
        string deviceId,
        WebSessionUploadItem session)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/web-sessions/upload",
            new UploadWebSessionsRequest(deviceId, [session]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal((int)UploadItemStatus.Accepted, json.RootElement.GetProperty("items")[0].GetProperty("status").GetInt32());
    }

    private static FocusSessionUploadItem Focus(
        string clientSessionId,
        string platformAppKey,
        long durationMs,
        bool isIdle)
    {
        var startedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero);

        return new FocusSessionUploadItem(
            clientSessionId,
            platformAppKey,
            startedAtUtc,
            startedAtUtc.AddMilliseconds(durationMs),
            durationMs,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle,
            source: "runtime-flow-test",
            processName: platformAppKey,
            windowTitle: null);
    }

    private static WebSessionUploadItem Web(
        string clientSessionId,
        string focusSessionId,
        string domain,
        long durationMs)
    {
        var startedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 1, 0, TimeSpan.Zero);

        return new WebSessionUploadItem(
            clientSessionId,
            focusSessionId,
            browserFamily: "Chrome",
            url: null,
            domain,
            pageTitle: null,
            startedAtUtc,
            startedAtUtc.AddMilliseconds(durationMs),
            durationMs,
            captureMethod: "BrowserExtensionFuture",
            captureConfidence: "High",
            isPrivateOrUnknown: false);
    }

}
