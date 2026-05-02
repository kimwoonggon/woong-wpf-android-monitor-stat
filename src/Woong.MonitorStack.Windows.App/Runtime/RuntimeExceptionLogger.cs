using System.Diagnostics;
using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.App;

public enum RuntimeExceptionSource
{
    DispatcherUnhandledException,
    DomainUnhandledException,
    UnobservedTaskException
}

public sealed class RuntimeExceptionLogger
{
    private readonly IDashboardRuntimeLogSink? _runtimeLogSink;
    private readonly Action<string> _fallbackDiagnostic;

    public RuntimeExceptionLogger(
        IDashboardRuntimeLogSink? runtimeLogSink,
        Action<string>? fallbackDiagnostic = null)
    {
        _runtimeLogSink = runtimeLogSink;
        _fallbackDiagnostic = fallbackDiagnostic ?? (message => Trace.WriteLine(message));
    }

    public void LogException(RuntimeExceptionSource source, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string operation = FormatSource(source);
        try
        {
            if (_runtimeLogSink is null)
            {
                _fallbackDiagnostic($"{operation}: no runtime log sink is available for {exception.GetType().Name}: {exception.Message}");
                return;
            }

            _runtimeLogSink.WriteException(operation, exception);
        }
        catch (Exception sinkException)
        {
            _fallbackDiagnostic(
                $"{operation}: Runtime log sink failed with {sinkException.GetType().Name}: {sinkException.Message}. Original exception {exception.GetType().Name}: {exception.Message}");
        }
    }

    public void LogDomainUnhandledException(object? exceptionObject)
    {
        if (exceptionObject is Exception exception)
        {
            LogException(RuntimeExceptionSource.DomainUnhandledException, exception);
            return;
        }

        _fallbackDiagnostic(
            $"{FormatSource(RuntimeExceptionSource.DomainUnhandledException)}: unhandled exception object was not an Exception: {exceptionObject}");
    }

    private static string FormatSource(RuntimeExceptionSource source)
        => source switch
        {
            RuntimeExceptionSource.DispatcherUnhandledException => "DispatcherUnhandledException",
            RuntimeExceptionSource.DomainUnhandledException => "UnhandledException",
            RuntimeExceptionSource.UnobservedTaskException => "UnobservedTaskException",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unsupported runtime exception source.")
        };
}
