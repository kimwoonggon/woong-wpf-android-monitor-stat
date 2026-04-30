using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Woong.MonitorStack.Windows.App.Converters;

public sealed class ProcessIconVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasExecutablePath = value is string processPath
                                 && !string.IsNullOrWhiteSpace(processPath)
                                 && File.Exists(processPath);
        bool isVisible = Invert ? !hasExecutablePath : hasExecutablePath;

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
