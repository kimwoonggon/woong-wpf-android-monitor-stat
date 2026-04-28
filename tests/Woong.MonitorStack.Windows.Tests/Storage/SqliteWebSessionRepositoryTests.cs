using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class SqliteWebSessionRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void SaveAndQueryByFocusSessionId_RoundTripsWebSession()
    {
        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        var webSession = WebSession.FromUtc(
            focusSessionId: "focus-1",
            browserFamily: "Chrome",
            url: "https://www.youtube.com/watch?v=abc",
            pageTitle: "Video",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero));

        repository.Initialize();
        repository.Save(webSession);

        var saved = Assert.Single(repository.QueryByFocusSessionId("focus-1"));
        Assert.Equal("youtube.com", saved.Domain);
        Assert.Equal(300_000, saved.DurationMs);
    }

    [Fact]
    public void SaveAndQueryByFocusSessionId_RoundTripsCaptureMetadata()
    {
        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        var webSession = new WebSession(
            focusSessionId: "focus-1",
            browserFamily: "Chrome",
            url: null,
            domain: "github.com",
            pageTitle: null,
            range: TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero)),
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "High",
            isPrivateOrUnknown: false);

        repository.Initialize();
        repository.Save(webSession);

        WebSession saved = Assert.Single(repository.QueryByFocusSessionId("focus-1"));
        Assert.Null(saved.Url);
        Assert.Equal("github.com", saved.Domain);
        Assert.Equal("UIAutomationAddressBar", saved.CaptureMethod);
        Assert.Equal("High", saved.CaptureConfidence);
        Assert.False(saved.IsPrivateOrUnknown);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
