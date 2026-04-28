namespace Woong.MonitorStack.Domain.Common;

internal static class RequiredText
{
    public static string Ensure(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
