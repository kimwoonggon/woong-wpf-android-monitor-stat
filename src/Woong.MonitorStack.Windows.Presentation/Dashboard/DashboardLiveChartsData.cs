using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed record DashboardLiveChartsData(
    IReadOnlyList<ISeries> Series,
    IReadOnlyList<string> Labels,
    IReadOnlyList<Axis> XAxes,
    IReadOnlyList<Axis> YAxes,
    string EmptyStateText);
