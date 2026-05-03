namespace Woong.MonitorStack.Windows.Presentation.Dashboard;

public sealed class DashboardDetailsPager
{
    public const int DefaultRowsPerPage = 10;

    public static IReadOnlyList<int> RowsPerPageOptions { get; } = [10, 25, 50];

    public bool IsSupportedRowsPerPage(int rowsPerPage)
        => RowsPerPageOptions.Contains(rowsPerPage);

    public int CalculateTotalPages(
        DetailsTab selectedTab,
        int appSessionRowCount,
        int webSessionRowCount,
        int liveEventRowCount,
        int rowsPerPage)
    {
        int selectedRowCount = selectedTab switch
        {
            DetailsTab.AppSessions => appSessionRowCount,
            DetailsTab.WebSessions => webSessionRowCount,
            DetailsTab.LiveEventLog => liveEventRowCount,
            DetailsTab.Settings => 0,
            _ => 0
        };

        return Math.Max(1, (int)Math.Ceiling(selectedRowCount / (double)Math.Max(1, rowsPerPage)));
    }

    public DashboardDetailsPage BuildPage(
        DetailsTab selectedTab,
        int currentPage,
        int rowsPerPage,
        IReadOnlyList<DashboardSessionRow> appSessionRows,
        IReadOnlyList<DashboardWebSessionRow> webSessionRows,
        IReadOnlyList<DashboardEventLogRow> liveEventRows)
    {
        int totalPages = CalculateTotalPages(
            selectedTab,
            appSessionRows.Count,
            webSessionRows.Count,
            liveEventRows.Count,
            rowsPerPage);
        int resolvedCurrentPage = currentPage > totalPages ? totalPages : currentPage;
        int safeRowsPerPage = Math.Max(1, rowsPerPage);
        int skip = (resolvedCurrentPage - 1) * safeRowsPerPage;

        return new DashboardDetailsPage(
            resolvedCurrentPage,
            totalPages,
            appSessionRows.Skip(skip).Take(safeRowsPerPage).ToList(),
            webSessionRows.Skip(skip).Take(safeRowsPerPage).ToList(),
            liveEventRows.Skip(skip).Take(safeRowsPerPage).ToList());
    }
}

public sealed record DashboardDetailsPage(
    int CurrentPage,
    int TotalPages,
    IReadOnlyList<DashboardSessionRow> VisibleAppSessionRows,
    IReadOnlyList<DashboardWebSessionRow> VisibleWebSessionRows,
    IReadOnlyList<DashboardEventLogRow> VisibleLiveEventRows)
{
    public string PageText => $"{CurrentPage} / {TotalPages}";

    public bool CanGoToPreviousPage => CurrentPage > 1;

    public bool CanGoToNextPage => CurrentPage < TotalPages;
}
