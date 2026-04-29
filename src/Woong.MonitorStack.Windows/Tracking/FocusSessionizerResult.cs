using Woong.MonitorStack.Domain.Common;

namespace Woong.MonitorStack.Windows.Tracking;

public sealed record FocusSessionizerResult(
    FocusSession? ClosedSession,
    FocusSession CurrentSession,
    ForegroundWindowSnapshot? ForegroundWindow = null);
