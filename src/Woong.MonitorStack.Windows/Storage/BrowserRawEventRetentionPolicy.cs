namespace Woong.MonitorStack.Windows.Storage;

public sealed class BrowserRawEventRetentionPolicy
{
    public static BrowserRawEventRetentionPolicy Default { get; } = new(TimeSpan.FromDays(30));

    public BrowserRawEventRetentionPolicy(TimeSpan retentionPeriod)
    {
        if (retentionPeriod <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionPeriod), retentionPeriod, "Retention period must be positive.");
        }

        RetentionPeriod = retentionPeriod;
    }

    public TimeSpan RetentionPeriod { get; }

    public DateTimeOffset CutoffFor(DateTimeOffset utcNow)
        => utcNow.ToUniversalTime().Subtract(RetentionPeriod);
}
