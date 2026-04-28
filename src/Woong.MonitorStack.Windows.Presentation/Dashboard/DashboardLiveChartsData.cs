using LiveChartsCore;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardLiveChartsData(
    IReadOnlyList<ISeries> Series,
    IReadOnlyList<string> Labels);
