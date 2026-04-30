using System.Globalization;
using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class FileDashboardRuntimeLogSink : IDashboardRuntimeLogSink
{
    private readonly object _gate = new();

    public FileDashboardRuntimeLogSink(string logPath)
    {
        LogPath = string.IsNullOrWhiteSpace(logPath)
            ? throw new ArgumentException("Runtime log path must not be empty.", nameof(logPath))
            : logPath;
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
}
