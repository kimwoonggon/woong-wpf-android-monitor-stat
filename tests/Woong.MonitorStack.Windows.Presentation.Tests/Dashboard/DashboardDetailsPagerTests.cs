using Woong.MonitorStack.Windows.Presentation.Dashboard;

namespace Woong.MonitorStack.Windows.Presentation.Tests.Dashboard;

public sealed class DashboardDetailsPagerTests
{
    [Fact]
    public void BuildPage_UsesSelectedTabCountForTotalPagesAndClampsCurrentPage()
    {
        var pager = new DashboardDetailsPager();
        IReadOnlyList<DashboardSessionRow> appRows = Enumerable.Range(0, 12)
            .Select(index => SessionRow($"app-{index}"))
            .ToList();
        IReadOnlyList<DashboardWebSessionRow> webRows = [WebRow("example.com")];

        DashboardDetailsPage page = pager.BuildPage(
            DetailsTab.WebSessions,
            currentPage: 2,
            rowsPerPage: 10,
            appRows,
            webRows,
            liveEventRows: []);

        Assert.Equal(1, page.CurrentPage);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal("1 / 1", page.PageText);
        Assert.Equal(10, page.VisibleAppSessionRows.Count);
        DashboardWebSessionRow webRow = Assert.Single(page.VisibleWebSessionRows);
        Assert.Equal("example.com", webRow.Domain);
    }

    [Fact]
    public void BuildPage_PaginatesSelectedRowsWithRequestedPageSize()
    {
        var pager = new DashboardDetailsPager();
        IReadOnlyList<DashboardSessionRow> appRows = Enumerable.Range(0, 26)
            .Select(index => SessionRow($"app-{index}"))
            .ToList();

        DashboardDetailsPage page = pager.BuildPage(
            DetailsTab.AppSessions,
            currentPage: 3,
            rowsPerPage: 10,
            appRows,
            webSessionRows: [],
            liveEventRows: []);

        Assert.Equal(3, page.CurrentPage);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal("app-20", page.VisibleAppSessionRows[0].AppName);
        Assert.Equal(6, page.VisibleAppSessionRows.Count);
        Assert.True(page.CanGoToPreviousPage);
        Assert.False(page.CanGoToNextPage);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(25, true)]
    [InlineData(50, true)]
    [InlineData(11, false)]
    public void IsSupportedRowsPerPage_OnlyAllowsDashboardOptions(int rowsPerPage, bool expected)
    {
        var pager = new DashboardDetailsPager();

        Assert.Equal(expected, pager.IsSupportedRowsPerPage(rowsPerPage));
    }

    private static DashboardSessionRow SessionRow(string appName)
        => new(
            appName,
            ProcessName: appName,
            StartedAtLocal: "09:00",
            EndedAtLocal: "09:01",
            Duration: "1m",
            State: "Active",
            WindowTitle: "Hidden by privacy setting",
            Source: "foreground_window",
            IsIdle: false);

    private static DashboardWebSessionRow WebRow(string domain)
        => new(
            domain,
            PageTitle: "Page title hidden by privacy settings",
            UrlMode: "Domain only",
            StartedAtLocal: "09:00",
            EndedAtLocal: "09:01",
            Duration: "1m",
            Browser: "Chrome",
            Confidence: "High");
}
