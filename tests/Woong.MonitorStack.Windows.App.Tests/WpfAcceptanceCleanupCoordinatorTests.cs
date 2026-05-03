using Woong.MonitorStack.Windows.AcceptanceCleanup;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class WpfAcceptanceCleanupCoordinatorTests
{
    [Fact]
    public void Cleanup_InvokesExplicitExitAndDoesNotKillWhenProcessExits()
    {
        var process = new FakeLaunchedWpfAppProcess { ExitOnWait = true };
        var explicitExitCallCount = 0;

        WpfAppCleanupEvidence result = WpfAppCleanupCoordinator.Cleanup(
            process,
            () =>
            {
                explicitExitCallCount++;
                return ExplicitExitRequestResult.Invoked("ExitApplicationButton");
            },
            TimeSpan.FromSeconds(5));

        Assert.Equal(1, explicitExitCallCount);
        Assert.Equal(1, process.WaitForExitCallCount);
        Assert.Equal(0, process.KillCallCount);
        Assert.False(result.WasKilled);
        Assert.True(result.ExplicitExitAttempted);
        Assert.Equal(WpfAppCleanupStatus.Pass, result.Status);
        Assert.Contains("ExitApplicationButton", result.Actual);
    }

    [Fact]
    public void Cleanup_DoesNotTreatStillRunningHiddenToTrayCloseAsExit()
    {
        var process = new FakeLaunchedWpfAppProcess { ExitOnWait = false, KillExits = true };

        WpfAppCleanupEvidence result = WpfAppCleanupCoordinator.Cleanup(
            process,
            () => ExplicitExitRequestResult.Invoked("ExitApplicationButton"),
            TimeSpan.FromMilliseconds(10));

        Assert.Equal(1, process.WaitForExitCallCount);
        Assert.Equal(1, process.KillCallCount);
        Assert.True(result.WasKilled);
        Assert.Equal(WpfAppCleanupStatus.Warn, result.Status);
        Assert.Contains("still running", result.Actual);
        Assert.Contains("hidden-to-tray", result.Actual);
        Assert.Contains("killed leftover process", result.Actual);
    }

    [Fact]
    public void Cleanup_KillsLeftoverProcessWhenExplicitExitControlIsUnavailable()
    {
        var process = new FakeLaunchedWpfAppProcess { KillExits = true };

        WpfAppCleanupEvidence result = WpfAppCleanupCoordinator.Cleanup(
            process,
            () => ExplicitExitRequestResult.Unavailable("ExitApplicationButton was not found"),
            TimeSpan.FromSeconds(5));

        Assert.Equal(0, process.WaitForExitCallCount);
        Assert.Equal(1, process.KillCallCount);
        Assert.True(result.WasKilled);
        Assert.False(result.ExplicitExitAttempted);
        Assert.Equal(WpfAppCleanupStatus.Warn, result.Status);
        Assert.Contains("explicit exit unavailable", result.Actual);
        Assert.Contains("ExitApplicationButton was not found", result.Actual);
        Assert.Contains("killed leftover process", result.Actual);
    }

    private sealed class FakeLaunchedWpfAppProcess : ILaunchedWpfAppProcess
    {
        private bool _hasExited;

        public bool ExitOnWait { get; init; }

        public bool KillExits { get; init; }

        public int WaitForExitCallCount { get; private set; }

        public int KillCallCount { get; private set; }

        public bool HasExited => _hasExited;

        public bool WaitForExit(TimeSpan timeout)
        {
            WaitForExitCallCount++;
            if (ExitOnWait)
            {
                _hasExited = true;
            }

            return _hasExited;
        }

        public void KillEntireProcessTree()
        {
            KillCallCount++;
            if (KillExits)
            {
                _hasExited = true;
            }
        }
    }
}
