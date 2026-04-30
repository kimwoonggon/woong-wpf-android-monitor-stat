using System.Globalization;
using System.Windows.Media;
using Woong.MonitorStack.Windows.App.Converters;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class ProcessIconImageSourceConverterTests
{
    [Fact]
    public void Convert_WithMissingExecutablePath_ReturnsNull()
    {
        var converter = new ProcessIconImageSourceConverter();

        object? result = converter.Convert(
            @"C:\missing\not-real.exe",
            typeof(ImageSource),
            null,
            CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_WithCurrentProcessExecutablePath_ReturnsFrozenImageSource()
    {
        var converter = new ProcessIconImageSourceConverter();

        object? result = converter.Convert(
            Environment.ProcessPath,
            typeof(ImageSource),
            null,
            CultureInfo.InvariantCulture);

        var imageSource = Assert.IsAssignableFrom<ImageSource>(result);
        Assert.True(imageSource.IsFrozen);
    }
}
