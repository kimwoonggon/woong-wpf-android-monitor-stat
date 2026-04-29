namespace Woong.MonitorStack.Windows.Browser;

public sealed class ChromeNativeMessageHostRunner
{
    private readonly ChromeNativeMessageIngestionFlow _ingestionFlow;

    public ChromeNativeMessageHostRunner(ChromeNativeMessageIngestionFlow ingestionFlow)
    {
        _ingestionFlow = ingestionFlow ?? throw new ArgumentNullException(nameof(ingestionFlow));
    }

    public async Task RunUntilEndAsync(Stream nativeMessageStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nativeMessageStream);

        while (!cancellationToken.IsCancellationRequested)
        {
            bool processed = await _ingestionFlow
                .TryIngestNextAsync(nativeMessageStream, cancellationToken)
                .ConfigureAwait(false);
            if (!processed)
            {
                return;
            }
        }
    }
}
