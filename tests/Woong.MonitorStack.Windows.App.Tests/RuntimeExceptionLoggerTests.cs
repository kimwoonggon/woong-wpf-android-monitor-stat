using System.IO;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class RuntimeExceptionLoggerTests
{
    [Theory]
    [InlineData(RuntimeExceptionSource.DispatcherUnhandledException, "DispatcherUnhandledException")]
    [InlineData(RuntimeExceptionSource.DomainUnhandledException, "UnhandledException")]
    [InlineData(RuntimeExceptionSource.UnobservedTaskException, "UnobservedTaskException")]
    public void LogException_WritesSourceAndExceptionToRuntimeLogSink(
        RuntimeExceptionSource source,
        string expectedOperation)
    {
        var sink = new RecordingRuntimeLogSink();
        var logger = new RuntimeExceptionLogger(sink);
        var exception = new InvalidOperationException("WPF startup failed.");

        logger.LogException(source, exception);

        (string operation, Exception recordedException) = Assert.Single(sink.Exceptions);
        Assert.Equal(expectedOperation, operation);
        Assert.Same(exception, recordedException);
    }

    [Fact]
    public void LogException_WhenRuntimeLogSinkThrows_DoesNotThrowAndWritesFallbackDiagnostic()
    {
        var fallbackMessages = new List<string>();
        var logger = new RuntimeExceptionLogger(
            new ThrowingRuntimeLogSink(),
            fallbackMessages.Add);
        var exception = new InvalidOperationException("Dispatcher crash.");

        Exception? thrown = Record.Exception(() =>
            logger.LogException(RuntimeExceptionSource.DispatcherUnhandledException, exception));

        Assert.Null(thrown);
        string fallbackMessage = Assert.Single(fallbackMessages);
        Assert.Contains("DispatcherUnhandledException", fallbackMessage, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", fallbackMessage, StringComparison.Ordinal);
        Assert.Contains("Runtime log sink failed", fallbackMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void LogDomainUnhandledException_WhenExceptionObjectIsNotException_WritesDiagnosticWithoutThrowing()
    {
        var fallbackMessages = new List<string>();
        var logger = new RuntimeExceptionLogger(new RecordingRuntimeLogSink(), fallbackMessages.Add);

        Exception? thrown = Record.Exception(() =>
            logger.LogDomainUnhandledException("non-exception crash object"));

        Assert.Null(thrown);
        string fallbackMessage = Assert.Single(fallbackMessages);
        Assert.Contains("UnhandledException", fallbackMessage, StringComparison.Ordinal);
        Assert.Contains("non-exception crash object", fallbackMessage, StringComparison.Ordinal);
    }

    private sealed class RecordingRuntimeLogSink : IDashboardRuntimeLogSink
    {
        public string LogPath { get; } = "D:\\logs\\windows-runtime.log";

        public List<(string Operation, Exception Exception)> Exceptions { get; } = [];

        public void WriteEvent(DashboardRuntimeLogEvent logEvent)
        {
        }

        public void WriteException(string operation, Exception exception)
            => Exceptions.Add((operation, exception));

        public DashboardRuntimeLogFolderOpenResult OpenLogFolder()
            => new(true, "D:\\logs", "Opened runtime log folder.");
    }

    private sealed class ThrowingRuntimeLogSink : IDashboardRuntimeLogSink
    {
        public string LogPath { get; } = "D:\\logs\\windows-runtime.log";

        public void WriteEvent(DashboardRuntimeLogEvent logEvent)
        {
        }

        public void WriteException(string operation, Exception exception)
            => throw new IOException("Runtime log is locked.");

        public DashboardRuntimeLogFolderOpenResult OpenLogFolder()
            => new(false, "D:\\logs", "Could not open runtime log folder.");
    }
}
