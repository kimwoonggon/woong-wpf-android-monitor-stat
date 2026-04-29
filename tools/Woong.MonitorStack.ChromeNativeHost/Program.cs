using Woong.MonitorStack.Windows.Browser;
using Woong.MonitorStack.Windows.Storage;

return await ChromeNativeHostProgram.RunAsync(args, Console.OpenStandardInput(), CancellationToken.None)
    .ConfigureAwait(false);

internal static class ChromeNativeHostProgram
{
    private const string LocalDbEnvironmentVariable = "WOONG_MONITOR_LOCAL_DB";
    private const string DeviceIdEnvironmentVariable = "WOONG_MONITOR_DEVICE_ID";
    private const string FocusSessionIdEnvironmentVariable = "WOONG_MONITOR_NATIVE_HOST_FOCUS_SESSION_ID";
    private const string RequireExplicitDbEnvironmentVariable = "WOONG_MONITOR_REQUIRE_EXPLICIT_DB";

    public static async Task<int> RunAsync(string[] args, Stream input, CancellationToken cancellationToken)
    {
        try
        {
            string dbPath = ResolveDatabasePath(args);
            string? dbDirectory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            string connectionString = $"Data Source={dbPath};Pooling=False";
            var rawEvents = new SqliteBrowserRawEventRepository(connectionString);
            var webSessions = new SqliteWebSessionRepository(connectionString);
            var outbox = new SqliteSyncOutboxRepository(connectionString);
            rawEvents.Initialize();
            webSessions.Initialize();
            outbox.Initialize();

            var ingestion = new ChromeNativeMessageIngestionFlow(
                rawEvents,
                webSessions,
                outbox,
                ResolveDeviceId(),
                new BrowserWebSessionizer(ResolveFocusSessionId()),
                new BrowserUrlSanitizer(),
                BrowserUrlStoragePolicy.DomainOnly,
                new BrowserRawEventRetentionService(rawEvents, BrowserRawEventRetentionPolicy.Default));
            var runner = new ChromeNativeMessageHostRunner(ingestion);

            await runner.RunUntilEndAsync(input, cancellationToken).ConfigureAwait(false);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Woong Chrome native host failed: {exception.GetType().Name}");
            return 1;
        }
    }

    private static string ResolveDatabasePath(string[] args)
    {
        string? argumentPath = TryReadOption(args, "--db");
        if (!string.IsNullOrWhiteSpace(argumentPath))
        {
            return argumentPath;
        }

        string? environmentPath = Environment.GetEnvironmentVariable(LocalDbEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentPath))
        {
            return environmentPath;
        }

        if (string.Equals(
            Environment.GetEnvironmentVariable(RequireExplicitDbEnvironmentVariable),
            "1",
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Chrome native host acceptance requires an explicit --db argument or WOONG_MONITOR_LOCAL_DB.");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WoongMonitorStack",
            "windows-local.db");
    }

    private static string ResolveDeviceId()
    {
        string? deviceId = Environment.GetEnvironmentVariable(DeviceIdEnvironmentVariable);
        return string.IsNullOrWhiteSpace(deviceId)
            ? $"windows-{Environment.MachineName}"
            : deviceId;
    }

    private static string ResolveFocusSessionId()
    {
        string? focusSessionId = Environment.GetEnvironmentVariable(FocusSessionIdEnvironmentVariable);
        return string.IsNullOrWhiteSpace(focusSessionId)
            ? "native-messaging-unlinked"
            : focusSessionId;
    }

    private static string? TryReadOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
