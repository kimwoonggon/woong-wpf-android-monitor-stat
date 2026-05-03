using Woong.MonitorStack.Domain.Contracts;
using Woong.MonitorStack.Domain.Common;

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

    [Fact]
    public void WebSessionUploadItem_RequiresClientSessionIdForIdempotency()
    {
        Assert.Throws<ArgumentException>(() => new WebSessionUploadItem(
            clientSessionId: "",
            focusSessionId: "focus-session-1",
            browserFamily: "Chrome",
            url: null,
            domain: "github.com",
            pageTitle: null,
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            durationMs: 600_000));
    }

    [Fact]
    public void UploadFocusSessionsRequest_RejectsNullSessions()
    {
        Assert.Throws<ArgumentNullException>(() => new UploadFocusSessionsRequest(
            deviceId: "windows-device-1",
            sessions: null!));
    }

    [Fact]
    public void CurrentAppStateUploadItem_AllowsObservedAtUtcWithoutEndedAtUtcOrDuration()
    {
        DateTimeOffset observedAtUtc = new(2026, 5, 3, 12, 34, 56, TimeSpan.Zero);

        var item = new CurrentAppStateUploadItem(
            clientStateId: "current-state-1",
            platform: Platform.Windows,
            platformAppKey: "chrome.exe",
            observedAtUtc: observedAtUtc,
            localDate: new DateOnly(2026, 5, 3),
            timezoneId: "UTC",
            status: "Active",
            source: "foreground_window",
            processId: 4321,
            processName: "chrome.exe",
            processPath: @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            windowHandle: 123456,
            windowTitle: "Allowed by existing privacy setting");

        Assert.Equal("current-state-1", item.ClientStateId);
        Assert.Equal(Platform.Windows, item.Platform);
        Assert.Equal("chrome.exe", item.PlatformAppKey);
        Assert.Equal(observedAtUtc, item.ObservedAtUtc);
        Assert.Equal(new DateOnly(2026, 5, 3), item.LocalDate);
        Assert.Equal("UTC", item.TimezoneId);
        Assert.Equal("Active", item.Status);
        Assert.Equal("foreground_window", item.Source);
        Assert.Equal(4321, item.ProcessId);
        Assert.Equal("chrome.exe", item.ProcessName);
        Assert.Equal(@"C:\Program Files\Google\Chrome\Application\chrome.exe", item.ProcessPath);
        Assert.Equal(123456, item.WindowHandle);
        Assert.Equal("Allowed by existing privacy setting", item.WindowTitle);

        Assert.DoesNotContain(
            typeof(CurrentAppStateUploadItem).GetProperties(),
            property => property.Name is "EndedAtUtc" or "DurationMs");
    }

    [Fact]
    public void CurrentAppStateUploadItem_ExposesMetadataOnlyFields()
    {
        string[] propertyNames = typeof(CurrentAppStateUploadItem)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ClientStateId",
                "LocalDate",
                "ObservedAtUtc",
                "Platform",
                "PlatformAppKey",
                "ProcessId",
                "ProcessName",
                "ProcessPath",
                "Source",
                "Status",
                "TimezoneId",
                "WindowHandle",
                "WindowTitle"
            ],
            propertyNames);

        string joinedNames = string.Join("|", propertyNames);
        Assert.DoesNotContain("TypedText", joinedNames, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PageContent", joinedNames, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Screenshot", joinedNames, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Clipboard", joinedNames, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", joinedNames, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FormInput", joinedNames, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TouchCoordinate", joinedNames, StringComparison.OrdinalIgnoreCase);
    }
}
