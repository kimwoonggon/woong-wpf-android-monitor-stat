using System.IO;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WindowsLocalDatabaseControllerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public void CreateNewDatabase_SwitchesRepositoriesToSelectedSqliteFile()
    {
        string initialPath = Path.Combine(_directory, "initial.db");
        string createdPath = Path.Combine(_directory, "created.db");
        var picker = new FakeDatabaseFilePicker { NewDatabasePath = createdPath };
        WindowsLocalDatabaseController controller = CreateController(initialPath, picker, out WindowsLocalDatabaseState state, out SqliteFocusSessionRepository focusRepository);

        var result = controller.CreateNewDatabase();
        focusRepository.Save(Session("created-session", "chrome.exe"));

        Assert.True(result.Succeeded);
        Assert.Equal(createdPath, state.DatabasePath);
        Assert.True(File.Exists(createdPath));
        FocusSession saved = Assert.Single(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
        Assert.Equal("created-session", saved.ClientSessionId);
    }

    [Fact]
    public void LoadExistingDatabase_SwitchesDashboardRepositoriesToExistingFile()
    {
        string initialPath = Path.Combine(_directory, "initial.db");
        string existingPath = Path.Combine(_directory, "existing.db");
        Directory.CreateDirectory(_directory);
        var existingRepository = new SqliteFocusSessionRepository(WindowsAppOptions.BuildConnectionString(existingPath));
        existingRepository.Initialize();
        existingRepository.Save(Session("existing-session", "Code.exe"));
        var picker = new FakeDatabaseFilePicker { ExistingDatabasePath = existingPath };
        WindowsLocalDatabaseController controller = CreateController(initialPath, picker, out WindowsLocalDatabaseState state, out SqliteFocusSessionRepository focusRepository);

        var result = controller.LoadExistingDatabase();

        Assert.True(result.Succeeded);
        Assert.Equal(existingPath, state.DatabasePath);
        FocusSession saved = Assert.Single(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
        Assert.Equal("existing-session", saved.ClientSessionId);
    }

    [Fact]
    public void DeleteCurrentDatabase_RemovesRowsAndRecreatesEmptySqliteFile()
    {
        string databasePath = Path.Combine(_directory, "current.db");
        var picker = new FakeDatabaseFilePicker { ConfirmDelete = true };
        WindowsLocalDatabaseController controller = CreateController(databasePath, picker, out _, out SqliteFocusSessionRepository focusRepository);
        focusRepository.Save(Session("session-before-delete", "chrome.exe"));

        var result = controller.DeleteCurrentDatabase();

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(databasePath));
        Assert.Empty(focusRepository.QueryByRange(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 1, 0, 0, TimeSpan.Zero)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static WindowsLocalDatabaseController CreateController(
        string initialPath,
        IWindowsDatabaseFilePicker picker,
        out WindowsLocalDatabaseState state,
        out SqliteFocusSessionRepository focusRepository)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(initialPath)!);
        var databaseState = new WindowsLocalDatabaseState(initialPath);
        focusRepository = new SqliteFocusSessionRepository(() => databaseState.ConnectionString);
        var webRepository = new SqliteWebSessionRepository(() => databaseState.ConnectionString);
        var outboxRepository = new SqliteSyncOutboxRepository(() => databaseState.ConnectionString);
        focusRepository.Initialize();
        webRepository.Initialize();
        outboxRepository.Initialize();
        state = databaseState;

        return new WindowsLocalDatabaseController(databaseState, focusRepository, webRepository, outboxRepository, picker);
    }

    private static FocusSession Session(string clientSessionId, string platformAppKey)
        => FocusSession.FromUtc(
            clientSessionId,
            "windows-device",
            platformAppKey,
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 28, 0, 5, 0, TimeSpan.Zero),
            "Asia/Seoul",
            isIdle: false,
            source: "foreground_window");

    private sealed class FakeDatabaseFilePicker : IWindowsDatabaseFilePicker
    {
        public string? NewDatabasePath { get; init; }

        public string? ExistingDatabasePath { get; init; }

        public bool ConfirmDelete { get; init; }

        public string? PickNewDatabasePath(string currentDatabasePath)
            => NewDatabasePath;

        public string? PickExistingDatabasePath(string currentDatabasePath)
            => ExistingDatabasePath;

        public bool ConfirmDeleteDatabase(string currentDatabasePath)
            => ConfirmDelete;
    }
}
