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

    [Fact]
    public void Initialize_WhenLegacyWebSessionUrlColumnIsNotNull_AllowsDomainOnlyRowsWithoutLosingRows()
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
                    url TEXT NOT NULL,
                    domain TEXT NOT NULL,
                    page_title TEXT NULL,
                    started_at_utc TEXT NOT NULL,
                    ended_at_utc TEXT NOT NULL,
                    duration_ms INTEGER NOT NULL,
                    capture_method TEXT NULL,
                    capture_confidence TEXT NULL,
                    is_private_or_unknown INTEGER NULL
                );

                INSERT INTO web_session (
                    focus_session_id,
                    browser_family,
                    url,
                    domain,
                    page_title,
                    started_at_utc,
                    ended_at_utc,
                    duration_ms,
                    capture_method,
                    capture_confidence,
                    is_private_or_unknown
                ) VALUES (
                    'focus-existing',
                    'Chrome',
                    'https://github.com/',
                    'github.com',
                    NULL,
                    '2026-04-28T00:00:00.0000000+00:00',
                    '2026-04-28T00:05:00.0000000+00:00',
                    300000,
                    'UIAutomationAddressBar',
                    'High',
                    0
                );
                """;
            _ = command.ExecuteNonQuery();
        }

        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();
        repository.Save(new WebSession(
            focusSessionId: "focus-domain-only",
            browserFamily: "Chrome",
            url: null,
            domain: "chatgpt.com",
            pageTitle: null,
            range: TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 0, 15, 0, TimeSpan.Zero)),
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "Medium",
            isPrivateOrUnknown: false));

        IReadOnlyList<WebSession> saved = repository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));
        Assert.Collection(
            saved,
            existing =>
            {
                Assert.Equal("focus-existing", existing.FocusSessionId);
                Assert.Equal("https://github.com/", existing.Url);
                Assert.Equal("github.com", existing.Domain);
            },
            domainOnly =>
            {
                Assert.Equal("focus-domain-only", domainOnly.FocusSessionId);
                Assert.Null(domainOnly.Url);
                Assert.Equal("chatgpt.com", domainOnly.Domain);
            });
    }

    [Fact]
    public void Initialize_WhenLegacyWebSessionTableContainsDuplicateIdentity_KeepsOneRowAndEnforcesFutureUniqueness()
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
                    duration_ms INTEGER NOT NULL,
                    capture_method TEXT NULL,
                    capture_confidence TEXT NULL,
                    is_private_or_unknown INTEGER NULL
                );

                INSERT INTO web_session (
                    focus_session_id,
                    browser_family,
                    url,
                    domain,
                    page_title,
                    started_at_utc,
                    ended_at_utc,
                    duration_ms,
                    capture_method,
                    capture_confidence,
                    is_private_or_unknown
                ) VALUES
                (
                    'focus-duplicate',
                    'Chrome',
                    'https://github.com/',
                    'github.com',
                    NULL,
                    '2026-04-28T00:00:00.0000000+00:00',
                    '2026-04-28T00:05:00.0000000+00:00',
                    300000,
                    'UIAutomationAddressBar',
                    'High',
                    0
                ),
                (
                    'focus-duplicate',
                    'Chrome',
                    'https://example.com/',
                    'example.com',
                    NULL,
                    '2026-04-28T00:00:00.0000000+00:00',
                    '2026-04-28T00:06:00.0000000+00:00',
                    360000,
                    'UIAutomationAddressBar',
                    'High',
                    0
                );
                """;
            _ = command.ExecuteNonQuery();
        }

        var repository = new SqliteWebSessionRepository($"Data Source={_dbPath};Pooling=False");
        repository.Initialize();

        WebSession saved = Assert.Single(repository.QueryByFocusSessionId("focus-duplicate"));
        Assert.Equal("github.com", saved.Domain);
        Assert.Equal(300_000, saved.DurationMs);

        using var duplicateConnection = new SqliteConnection($"Data Source={_dbPath};Pooling=False");
        duplicateConnection.Open();
        using SqliteCommand duplicateCommand = duplicateConnection.CreateCommand();
        duplicateCommand.CommandText = """
            INSERT INTO web_session (
                focus_session_id,
                browser_family,
                url,
                domain,
                page_title,
                started_at_utc,
                ended_at_utc,
                duration_ms,
                capture_method,
                capture_confidence,
                is_private_or_unknown
            ) VALUES (
                'focus-duplicate',
                'Chrome',
                'https://example.org/',
                'example.org',
                NULL,
                '2026-04-28T00:00:00.0000000+00:00',
                '2026-04-28T00:07:00.0000000+00:00',
                420000,
                'UIAutomationAddressBar',
                'High',
                0
            );
            """;

        SqliteException exception = Assert.Throws<SqliteException>(() => duplicateCommand.ExecuteNonQuery());
        Assert.Equal(19, exception.SqliteErrorCode);
    }

    [Fact]
    public void SaveWithOutbox_WhenDuplicateWebSessionUsesDifferentOutboxId_DoesNotQueueSecondOutboxItem()
    {
        string connectionString = $"Data Source={_dbPath};Pooling=False";
        var repository = new SqliteWebSessionRepository(connectionString);
        var outboxRepository = new SqliteSyncOutboxRepository(connectionString);
        WebSession session = CreateWebSession("focus-outbox-duplicate");

        repository.Initialize();
        outboxRepository.Initialize();

        repository.SaveWithOutbox(
            session,
            SyncOutboxItem.Pending(
                id: "outbox-first",
                aggregateType: "web_session",
                aggregateId: "aggregate-first",
                payloadJson: "{\"clientSessionId\":\"aggregate-first\"}",
                createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero)));
        repository.SaveWithOutbox(
            session,
            SyncOutboxItem.Pending(
                id: "outbox-second",
                aggregateType: "web_session",
                aggregateId: "aggregate-second",
                payloadJson: "{\"clientSessionId\":\"aggregate-second\"}",
                createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 11, 0, TimeSpan.Zero)));

        _ = Assert.Single(repository.QueryByFocusSessionId("focus-outbox-duplicate"));
        SyncOutboxItem savedOutbox = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal("outbox-first", savedOutbox.Id);
        Assert.Equal("aggregate-first", savedOutbox.AggregateId);
    }

    [Fact]
    public async Task SaveWithOutbox_WhenDuplicateWebSessionsAreSavedConcurrently_DoesNotCreateExtraRows()
    {
        string connectionString = $"Data Source={_dbPath};Pooling=False;Default Timeout=30";
        var repository = new SqliteWebSessionRepository(connectionString);
        var outboxRepository = new SqliteSyncOutboxRepository(connectionString);
        WebSession session = CreateWebSession("focus-concurrent-duplicate");

        repository.Initialize();
        outboxRepository.Initialize();

        Task[] saves = Enumerable.Range(0, 12)
            .Select(index => Task.Run(() =>
            {
                var workerRepository = new SqliteWebSessionRepository(connectionString);
                workerRepository.SaveWithOutbox(
                    session,
                    SyncOutboxItem.Pending(
                        id: $"outbox-concurrent-{index}",
                        aggregateType: "web_session",
                        aggregateId: $"aggregate-concurrent-{index}",
                        payloadJson: $"{{\"clientSessionId\":\"aggregate-concurrent-{index}\"}}",
                        createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, index, TimeSpan.Zero)));
            }))
            .ToArray();

        await Task.WhenAll(saves);

        _ = Assert.Single(repository.QueryByFocusSessionId("focus-concurrent-duplicate"));
        SyncOutboxItem savedOutbox = Assert.Single(outboxRepository.QueryAll());
        Assert.Equal("web_session", savedOutbox.AggregateType);
        Assert.StartsWith("aggregate-concurrent-", savedOutbox.AggregateId, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static WebSession CreateWebSession(string focusSessionId)
        => new(
            focusSessionId,
            "Chrome",
            url: null,
            domain: "github.com",
            pageTitle: null,
            TimeRange.FromUtc(
                new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero)),
            captureMethod: "UIAutomationAddressBar",
            captureConfidence: "High",
            isPrivateOrUnknown: false);
}
