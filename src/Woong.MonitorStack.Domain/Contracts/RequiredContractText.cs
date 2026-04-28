namespace Woong.MonitorStack.Domain.Contracts;

internal static class RequiredContractText
{
    public static string Ensure(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be empty.", parameterName)
            : value;
}
