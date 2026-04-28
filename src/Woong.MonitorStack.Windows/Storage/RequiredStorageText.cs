namespace Woong.MonitorStack.Windows.Storage;

internal static class RequiredStorageText
{
    public static string Ensure(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
