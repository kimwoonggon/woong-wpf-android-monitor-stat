using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Server.Data;
using Woong.MonitorStack.Server.Tests.Data;

namespace Woong.MonitorStack.Server.Tests.Sessions;

public sealed class FocusSessionUploadApiRelationalTests
{
    [Fact]
    public async Task UploadFocusSessions_WhenDeviceIsNotRegistered_ReturnsControlledErrorAndDoesNotPersistRows()
    {
        using var factory = new RelationalServerFactory();
        using HttpClient client = factory.CreateClient();
        await factory.EnsureDatabaseCreatedAsync();
        string unknownDeviceId = Guid.NewGuid().ToString("N");
        var request = new UploadFocusSessionsRequest(
            unknownDeviceId,
            [Focus("orphan-focus-session")]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/focus-sessions/upload", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement item = json.RootElement.GetProperty("items")[0];
        Assert.Equal("orphan-focus-session", item.GetProperty("clientId").GetString());
        Assert.Equal((int)UploadItemStatus.Error, item.GetProperty("status").GetInt32());
        Assert.Contains("device", item.GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);

        using IServiceScope scope = factory.Services.CreateScope();
        MonitorDbContext dbContext = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
        Assert.Empty(await dbContext.FocusSessions.ToListAsync());
    }

    private static FocusSessionUploadItem Focus(string clientSessionId)
    {
        var startedAtUtc = new DateTimeOffset(2026, 4, 27, 15, 0, 0, TimeSpan.Zero);

        return new FocusSessionUploadItem(
            clientSessionId,
            platformAppKey: "chrome.exe",
            startedAtUtc,
            startedAtUtc.AddMinutes(5),
            durationMs: 300_000,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "relational-upload-test");
    }
}
