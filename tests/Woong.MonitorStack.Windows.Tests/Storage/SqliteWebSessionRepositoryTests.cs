using Microsoft.Data.Sqlite;
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

    [Fact]
    public void Initialize_WhenLegacyWebSessionTableIsMissingCaptureColumns_AddsColumnsWithoutLosingRows()
    {
        using (var connection = new SqliteConnection($"Data Source={_dbPath};Pooling=False"))
        {
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE web_session (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    focus_session_id TEXT NOT NULL,
                    browser_family TEXT NOT NULL,
                    url TEXT NULL,
                    domain TEXT NOT NULL,
                    page_title TEXT NULL,
                    started_at_utc TEXT NOT NULL,
                    ended_at_utc TEXT NOT NULL,
                    duration_ms INTEGER NOT NULL
                );

                INSERT INTO web_session (
                    focus_session_id,
                    browser_family,
                    url,
                    domain,
                    page_title,
                    started_at_utc,
                    ended_at_utc,
                    duration_ms
                ) VALUES (
                    'focus-legacy',
                    'Chrome',
                    NULL,
                    'github.com',
                    NULL,
                    '2026-04-28T00:00:00.0000000+00:00',
                    '2026-04-28T00:05:00.0000000+00:00',
                    300000
                );
                """;
            _ = command.ExecuteNonQuery();
        }

        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        WebSession saved = Assert.Single(repository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
        Assert.Equal("focus-legacy", saved.FocusSessionId);
        Assert.Equal("github.com", saved.Domain);
        Assert.Null(saved.CaptureMethod);
        Assert.Null(saved.CaptureConfidence);
        Assert.Null(saved.IsPrivateOrUnknown);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
