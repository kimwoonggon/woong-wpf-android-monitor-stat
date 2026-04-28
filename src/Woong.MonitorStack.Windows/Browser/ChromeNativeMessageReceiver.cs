using System.Text;

namespace Woong.MonitorStack.Windows.Browser;

public static class ChromeNativeMessageReceiver
{
    private const int LengthPrefixBytes = 4;
    private const int MaxMessageBytes = 1024 * 1024;

    public static async Task<ChromeTabChangedMessage?> ReadNextAsync(
        Stream input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var lengthBytes = new byte[LengthPrefixBytes];
        var prefixRead = await ReadAtMostAsync(input, lengthBytes, cancellationToken).ConfigureAwait(false);
        if (prefixRead == 0)
        {
            return null;
        }

        if (prefixRead != LengthPrefixBytes)
        {
            throw new InvalidDataException("Incomplete native message length prefix.");
        }

        var payloadLength = BitConverter.ToInt32(lengthBytes, 0);
        if (payloadLength <= 0 || payloadLength > MaxMessageBytes)
        {
            throw new InvalidDataException("Invalid native message payload length.");
        }

        var payload = new byte[payloadLength];
        var payloadRead = await ReadAtMostAsync(input, payload, cancellationToken).ConfigureAwait(false);
        if (payloadRead != payloadLength)
        {
            throw new InvalidDataException("Incomplete native message payload.");
        }

        var json = Encoding.UTF8.GetString(payload);
        return ChromeNativeMessageParser.ParseActiveTabChanged(json);
    }

    private static async Task<int> ReadAtMostAsync(
        Stream input,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await input
                .ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        return offset;
    }
}
