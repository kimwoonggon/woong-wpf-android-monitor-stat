namespace Woong.MonitorStack.Windows.Browser;

public interface IBrowserProcessClassifier
{
    BrowserProcessClassification Classify(string? processName);
}
