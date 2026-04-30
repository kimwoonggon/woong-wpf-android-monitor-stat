using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Woong.MonitorStack.Windows.App.Converters;

public sealed class ProcessIconImageSourceConverter : IValueConverter
{
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string processPath || string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
        {
            return null;
        }

        if (Cache.TryGetValue(processPath, out ImageSource? cached))
        {
            return cached;
        }

        ImageSource? source = LoadIcon(processPath);
        Cache[processPath] = source;

        return source;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    private static ImageSource? LoadIcon(string processPath)
    {
        try
        {
            using Icon? icon = Icon.ExtractAssociatedIcon(processPath);
            if (icon is null)
            {
                return null;
            }

            BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(20, 20));
            source.Freeze();

            return source;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
