using System.IO;
using Woong.MonitorStack.Windows.App.Dashboard;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class FileDashboardRuntimeLogSinkTests : IDisposable
{
    private readonly string _logPath = Path.Combine(
        Path.GetTempPath(),
        "woong-monitor-tests",
        $"{Guid.NewGuid():N}",
        "windows-runtime.log");

    [Fact]
    public void WriteEvent_AppendsRuntimeEventToLocalLogFile()
    {
        var sink = new FileDashboardRuntimeLogSink(_logPath);

        sink.WriteEvent(new DashboardRuntimeLogEvent(
            new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            "FocusSession persisted",
            "chrome.exe",
            "github.com",
            "FocusSession persisted for chrome.exe."));

        string logText = File.ReadAllText(_logPath);
        Assert.Contains("EVENT", logText, StringComparison.Ordinal);
        Assert.Contains("FocusSession persisted", logText, StringComparison.Ordinal);
        Assert.Contains("chrome.exe", logText, StringComparison.Ordinal);
        Assert.Contains("github.com", logText, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteException_AppendsOperationAndExceptionToLocalLogFile()
    {
        var sink = new FileDashboardRuntimeLogSink(_logPath);

        sink.WriteException("PollTracking", new InvalidOperationException("SQLite write failed."));

        string logText = File.ReadAllText(_logPath);
        Assert.Contains("ERROR", logText, StringComparison.Ordinal);
        Assert.Contains("PollTracking", logText, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", logText, StringComparison.Ordinal);
        Assert.Contains("SQLite write failed.", logText, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenLogFolder_CreatesLogDirectoryAndLaunchesIt()
    {
        var openedFolders = new List<string>();
        var sink = new FileDashboardRuntimeLogSink(_logPath, openedFolders.Add);

        DashboardRuntimeLogFolderOpenResult result = sink.OpenLogFolder();

        string expectedDirectory = Path.GetDirectoryName(_logPath)!;
        Assert.True(result.Succeeded);
        Assert.Equal(expectedDirectory, result.FolderPath);
        Assert.Equal([expectedDirectory], openedFolders);
        Assert.True(Directory.Exists(expectedDirectory));
        Assert.Contains("Opened runtime log folder", result.StatusMessage, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        string? directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
