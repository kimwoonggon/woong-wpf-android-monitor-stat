using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.Tests.Storage;

public sealed class SqliteFocusSessionRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    [Fact]
    public void SaveAndQueryByRange_RoundTripsFocusSession()
    {
        var repository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        var session = FocusSession.FromUtc(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

        repository.Initialize();
        repository.Save(session);

        var sessions = repository.QueryByRange(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));

        var saved = Assert.Single(sessions);
        Assert.Equal("session-1", saved.ClientSessionId);
        Assert.Equal("chrome.exe", saved.PlatformAppKey);
        Assert.Equal(600_000, saved.DurationMs);
    }

    [Fact]
    public void SaveAndQueryByRange_RoundTripsProcessWindowMetadata()
    {
        var repository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        var session = FocusSession.FromUtc(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "Code.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window",
            processId: 1234,
            processName: "Code.exe",
            processPath: @"C:\Apps\Code.exe",
            windowHandle: 9876,
            windowTitle: null);

        repository.Initialize();
        repository.Save(session);

        var sessions = repository.QueryByRange(
            new DateTimeOffset(2026, 4, 27, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));

        var saved = Assert.Single(sessions);
        Assert.Equal(1234, saved.ProcessId);
        Assert.Equal("Code.exe", saved.ProcessName);
        Assert.Equal(@"C:\Apps\Code.exe", saved.ProcessPath);
        Assert.Equal(9876, saved.WindowHandle);
        Assert.Null(saved.WindowTitle);
    }

    [Fact]
    public void Save_WhenClientSessionIdAlreadyExists_DoesNotInsertDuplicate()
    {
        var repository = new SqliteFocusSessionRepository($"Data Source={_dbPath};Pooling=False");
        var session = FocusSession.FromUtc(
            clientSessionId: "session-1",
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

        repository.Initialize();
        repository.Save(session);
        repository.Save(session);

        var sessions = repository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));

        Assert.Single(sessions);
    }

    [Fact]
    public void SaveWithOutbox_WhenSameSessionIsSavedAgainWithDifferentOutboxId_DoesNotQueueDuplicateOutboxItem()
    {
        string connectionString = $"Data Source={_dbPath};Pooling=False";
        var repository = new SqliteFocusSessionRepository(connectionString);
        var outboxRepository = new SqliteSyncOutboxRepository(connectionString);
        FocusSession session = CreateFocusSession("session-1");
        SyncOutboxItem firstOutboxItem = CreateOutboxItem(
            id: "outbox-1",
            aggregateId: session.ClientSessionId,
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero));
        SyncOutboxItem replayedOutboxItem = CreateOutboxItem(
            id: "outbox-2",
            aggregateId: session.ClientSessionId,
            createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero));

        repository.Initialize();
        outboxRepository.Initialize();
        repository.SaveWithOutbox(session, firstOutboxItem);
        repository.SaveWithOutbox(session, replayedOutboxItem);

        var sessions = repository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));
        SyncOutboxItem savedOutboxItem = Assert.Single(outboxRepository.QueryAll());
        Assert.Single(sessions);
        Assert.Equal("outbox-1", savedOutboxItem.Id);
        Assert.Equal("focus_session", savedOutboxItem.AggregateType);
        Assert.Equal("session-1", savedOutboxItem.AggregateId);
    }

    [Fact]
    public async Task SaveWithOutbox_WhenSameSessionIsSavedConcurrently_DoesNotInsertDuplicateRows()
    {
        string connectionString = $"Data Source={_dbPath};Pooling=False;Default Timeout=5";
        var firstRepository = new SqliteFocusSessionRepository(connectionString);
        var secondRepository = new SqliteFocusSessionRepository(connectionString);
        var outboxRepository = new SqliteSyncOutboxRepository(connectionString);
        FocusSession session = CreateFocusSession("session-1");

        firstRepository.Initialize();
        outboxRepository.Initialize();

        using var ready = new CountdownEvent(2);
        using var start = new ManualResetEventSlim();
        Task firstSave = Task.Run(() =>
        {
            ready.Signal();
            start.Wait();
            firstRepository.SaveWithOutbox(
                session,
                CreateOutboxItem(
                    id: "outbox-1",
                    aggregateId: session.ClientSessionId,
                    createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero)));
        });
        Task duplicateSave = Task.Run(() =>
        {
            ready.Signal();
            start.Wait();
            secondRepository.SaveWithOutbox(
                session,
                CreateOutboxItem(
                    id: "outbox-2",
                    aggregateId: session.ClientSessionId,
                    createdAtUtc: new DateTimeOffset(2026, 4, 28, 0, 1, 0, TimeSpan.Zero)));
        });

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5)));
        start.Set();
        await Task.WhenAll(firstSave, duplicateSave);

        var sessions = firstRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero));
        Assert.Single(sessions);
        SyncOutboxItem savedOutboxItem = Assert.Single(outboxRepository.QueryAll());
        Assert.Contains(savedOutboxItem.Id, new[] { "outbox-1", "outbox-2" });
        Assert.Equal("session-1", savedOutboxItem.AggregateId);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static FocusSession CreateFocusSession(string clientSessionId)
        => FocusSession.FromUtc(
            clientSessionId: clientSessionId,
            deviceId: "windows-device-1",
            platformAppKey: "chrome.exe",
            startedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            endedAtUtc: new DateTimeOffset(2026, 4, 28, 0, 10, 0, TimeSpan.Zero),
            timezoneId: "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

    private static SyncOutboxItem CreateOutboxItem(
        string id,
        string aggregateId,
        DateTimeOffset createdAtUtc)
        => SyncOutboxItem.Pending(
            id: id,
            aggregateType: "focus_session",
            aggregateId: aggregateId,
            payloadJson: $$"""{"clientSessionId":"{{aggregateId}}"}""",
            createdAtUtc: createdAtUtc);
}
