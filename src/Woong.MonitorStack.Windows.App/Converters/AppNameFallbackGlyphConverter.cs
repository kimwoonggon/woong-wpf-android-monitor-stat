using System.Globalization;
using System.Windows.Data;

namespace Woong.MonitorStack.Windows.App.Converters;

public sealed class AppNameFallbackGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string text = value as string ?? "";
        char glyph = text.FirstOrDefault(char.IsLetterOrDigit);

        return glyph == default ? "?" : char.ToUpperInvariant(glyph).ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
