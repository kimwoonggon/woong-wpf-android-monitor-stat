using Woong.MonitorStack.Domain.Contracts;

namespace Woong.MonitorStack.Domain.Tests.Contracts;

public sealed class UploadContractTests
{
    [Fact]
    public void FocusSessionUploadItem_RequiresClientSessionIdForIdempotency()
    {
        Assert.Throws<ArgumentException>(() => new FocusSessionUploadItem(
            clientSessionId: "",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            durationMs: 600_000,
            localDate: new DateOnly(2026, 4, 28),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window"));
    }
}
