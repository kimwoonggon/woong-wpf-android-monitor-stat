namespace Woong.MonitorStack.Server.Tests.Data;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("WOONG_MONITOR_RUN_POSTGRES_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Requires Docker daemon and WOONG_MONITOR_RUN_POSTGRES_TESTS=1. Use scripts/run-server-postgres-validation.ps1.";
        }
    }
}
