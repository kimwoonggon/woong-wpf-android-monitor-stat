using System.Diagnostics;

namespace Woong.MonitorStack.Windows.AcceptanceCleanup;

internal interface ILaunchedWpfAppProcess
{
    bool HasExited { get; }

    bool WaitForExit(TimeSpan timeout);

    void KillEntireProcessTree();
}

internal sealed class LaunchedWpfAppProcess : ILaunchedWpfAppProcess
{
    private readonly Process _process;

    public LaunchedWpfAppProcess(Process process)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
    }

    public bool HasExited => _process.HasExited;

    public bool WaitForExit(TimeSpan timeout)
        => _process.WaitForExit(ToMilliseconds(timeout));

    public void KillEntireProcessTree()
    {
        _process.Kill(entireProcessTree: true);
        _process.WaitForExit(ToMilliseconds(TimeSpan.FromSeconds(5)));
    }

    private static int ToMilliseconds(TimeSpan timeout)
        => timeout.TotalMilliseconds >= int.MaxValue
            ? int.MaxValue
            : Math.Max(0, (int)Math.Ceiling(timeout.TotalMilliseconds));
}

internal enum ExplicitExitRequestStatus
{
    Invoked = 0,
    Unavailable = 1,
    Failed = 2
}

internal sealed record ExplicitExitRequestResult(
    ExplicitExitRequestStatus Status,
    string Detail)
{
    public static ExplicitExitRequestResult Invoked(string detail)
        => new(ExplicitExitRequestStatus.Invoked, detail);

    public static ExplicitExitRequestResult Unavailable(string detail)
        => new(ExplicitExitRequestStatus.Unavailable, detail);

    public static ExplicitExitRequestResult Failed(string detail)
        => new(ExplicitExitRequestStatus.Failed, detail);
}

internal enum WpfAppCleanupStatus
{
    Pass = 0,
    Warn = 1,
    Fail = 2
}

internal sealed record WpfAppCleanupEvidence(
    string Claim,
    string Expected,
    string Actual,
    WpfAppCleanupStatus Status,
    bool ExplicitExitAttempted,
    bool WasKilled);

internal static class WpfAppCleanupCoordinator
{
    private const string Claim = "WPF app cleanup";
    private const string Expected = "Use explicit Exit app when available; do not treat X-close-to-tray or a still-running process as app exit.";

    public static WpfAppCleanupEvidence Cleanup(
        ILaunchedWpfAppProcess? process,
        Func<ExplicitExitRequestResult> requestExplicitExit,
        TimeSpan explicitExitTimeout)
    {
        ArgumentNullException.ThrowIfNull(requestExplicitExit);
        if (explicitExitTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(explicitExitTimeout), "Cleanup timeout must be positive.");
        }

        if (process is null)
        {
            return Pass("No launched WPF app process was tracked for cleanup.", explicitExitAttempted: false);
        }

        if (process.HasExited)
        {
            return Pass("The launched WPF app process had already exited before cleanup.", explicitExitAttempted: false);
        }

        ExplicitExitRequestResult explicitExit = RequestExplicitExit(requestExplicitExit);
        if (explicitExit.Status == ExplicitExitRequestStatus.Invoked)
        {
            if (process.WaitForExit(explicitExitTimeout))
            {
                return Pass(
                    $"Explicit exit path exited the WPF app: {explicitExit.Detail}.",
                    explicitExitAttempted: true);
            }

            return KillLeftover(
                process,
                explicitExitAttempted: true,
                $"Explicit exit path was invoked ({explicitExit.Detail}), but the process was still running after {explicitExitTimeout.TotalSeconds:N0}s. The cleanup treated this as hidden-to-tray or stuck shutdown, not as app exit, and killed leftover process.");
        }

        if (explicitExit.Status == ExplicitExitRequestStatus.Unavailable)
        {
            return KillLeftover(
                process,
                explicitExitAttempted: false,
                $"explicit exit unavailable: {explicitExit.Detail}; killed leftover process.");
        }

        return KillLeftover(
            process,
            explicitExitAttempted: true,
            $"explicit exit failed: {explicitExit.Detail}; killed leftover process.");
    }

    private static ExplicitExitRequestResult RequestExplicitExit(Func<ExplicitExitRequestResult> requestExplicitExit)
    {
        try
        {
            return requestExplicitExit();
        }
        catch (Exception exception)
        {
            return ExplicitExitRequestResult.Failed(
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static WpfAppCleanupEvidence KillLeftover(
        ILaunchedWpfAppProcess process,
        bool explicitExitAttempted,
        string actual)
    {
        try
        {
            process.KillEntireProcessTree();
        }
        catch (Exception exception)
        {
            return new WpfAppCleanupEvidence(
                Claim,
                Expected,
                $"{actual} Kill failed with {exception.GetType().Name}: {exception.Message}",
                WpfAppCleanupStatus.Fail,
                explicitExitAttempted,
                WasKilled: false);
        }

        return process.HasExited
            ? new WpfAppCleanupEvidence(
                Claim,
                Expected,
                actual,
                WpfAppCleanupStatus.Warn,
                explicitExitAttempted,
                WasKilled: true)
            : new WpfAppCleanupEvidence(
                Claim,
                Expected,
                $"{actual} Kill was requested, but the process was still running.",
                WpfAppCleanupStatus.Fail,
                explicitExitAttempted,
                WasKilled: true);
    }

    private static WpfAppCleanupEvidence Pass(string actual, bool explicitExitAttempted)
        => new(
            Claim,
            Expected,
            actual,
            WpfAppCleanupStatus.Pass,
            explicitExitAttempted,
            WasKilled: false);
}
