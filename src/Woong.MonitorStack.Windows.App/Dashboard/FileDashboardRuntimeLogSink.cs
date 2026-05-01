using System.Diagnostics;
using System.Globalization;
using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class FileDashboardRuntimeLogSink : IDashboardRuntimeLogSink
{
    private readonly object _gate = new();
    private readonly Action<string> _openFolder;

    public FileDashboardRuntimeLogSink(string logPath, Action<string>? openFolder = null)
    {
        LogPath = string.IsNullOrWhiteSpace(logPath)
            ? throw new ArgumentException("Runtime log path must not be empty.", nameof(logPath))
            : logPath;
        _openFolder = openFolder ?? OpenFolderWithShell;
    }

    public string LogPath { get; }

    public void WriteEvent(DashboardRuntimeLogEvent logEvent)
    {
        string line = string.Create(
            CultureInfo.InvariantCulture,
            $"{logEvent.OccurredAtUtc:O}\tEVENT\t{logEvent.EventType}\t{logEvent.AppName}\t{logEvent.Domain}\t{logEvent.Message}");
        AppendLine(line);
    }

    public void WriteException(string operation, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string line = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:O}\tERROR\t{operation}\t{exception.GetType().Name}\t{exception.Message}\t{exception}");
        AppendLine(line);
    }

    public DashboardRuntimeLogFolderOpenResult OpenLogFolder()
    {
        string folderPath = "";
        try
        {
            folderPath = Path.GetDirectoryName(Path.GetFullPath(LogPath)) ?? "";
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return new(false, folderPath, "Runtime log folder is unavailable.");
            }

            Directory.CreateDirectory(folderPath);
            _openFolder(folderPath);

            return new(true, folderPath, $"Opened runtime log folder: {folderPath}");
        }
        catch (Exception exception)
        {
            return new(false, folderPath, $"Could not open runtime log folder: {exception.Message}");
        }
    }

    private void AppendLine(string line)
    {
        try
        {
            string? directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            lock (_gate)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch (Exception)
        {
        }
    }

    private static void OpenFolderWithShell(string folderPath)
    {
        Process? process = Process.Start(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException($"Windows did not open folder: {folderPath}");
        }
    }
}
