using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using Woong.MonitorStack.Windows.Storage;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class SqliteDashboardDataSource : IDashboardDataSource
{
    private readonly SqliteFocusSessionRepository _focusSessionRepository;
    private readonly SqliteWebSessionRepository _webSessionRepository;

    public SqliteDashboardDataSource(
        SqliteFocusSessionRepository focusSessionRepository,
        SqliteWebSessionRepository webSessionRepository)
    {
        _focusSessionRepository = focusSessionRepository ?? throw new ArgumentNullException(nameof(focusSessionRepository));
        _webSessionRepository = webSessionRepository ?? throw new ArgumentNullException(nameof(webSessionRepository));
    }

    public IReadOnlyList<FocusSession> QueryFocusSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        => _focusSessionRepository.QueryByRange(startedAtUtc, endedAtUtc);

    public IReadOnlyList<WebSession> QueryWebSessions(DateTimeOffset startedAtUtc, DateTimeOffset endedAtUtc)
        => _focusSessionRepository
            .QueryByRange(startedAtUtc, endedAtUtc)
            .SelectMany(session => _webSessionRepository.QueryByFocusSessionId(session.ClientSessionId))
            .OrderBy(session => session.StartedAtUtc)
            .ToList();
}
