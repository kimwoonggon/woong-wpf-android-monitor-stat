namespace Woong.MonitorStack.Windows.Storage;

public sealed class BrowserRawEventRetentionService
{
    private readonly SqliteBrowserRawEventRepository _repository;
    private readonly BrowserRawEventRetentionPolicy _policy;

    public BrowserRawEventRetentionService(
        SqliteBrowserRawEventRepository repository,
        BrowserRawEventRetentionPolicy policy)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public int PruneExpired(DateTimeOffset utcNow)
        => _repository.DeleteOlderThan(_policy.CutoffFor(utcNow));
}
