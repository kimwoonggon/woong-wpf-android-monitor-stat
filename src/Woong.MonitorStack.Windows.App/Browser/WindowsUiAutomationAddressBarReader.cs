using System.Windows.Automation;
using Woong.MonitorStack.Windows.Tracking;

namespace Woong.MonitorStack.Windows.App.Browser;

public sealed class WindowsUiAutomationAddressBarReader : IBrowserAddressBarReader
{
    private static readonly string[] AddressBarNameMarkers =
    [
        "address and search bar",
        "address bar",
        "url bar",
        "urlbar",
        "location bar",
        "주소 및 검색창",
        "주소 표시줄",
        "주소창"
    ];

    public string? TryReadAddress(ForegroundWindowSnapshot foregroundWindow)
    {
        ArgumentNullException.ThrowIfNull(foregroundWindow);

        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            AutomationElement root = AutomationElement.FromHandle(foregroundWindow.Hwnd);
            AutomationElement? addressBar = FindAddressBar(root);
            if (addressBar is null)
            {
                return null;
            }

            return TryReadValue(addressBar);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static AutomationElement? FindAddressBar(AutomationElement root)
    {
        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
        AutomationElementCollection edits = root.FindAll(TreeScope.Descendants, condition);
        foreach (AutomationElement edit in edits)
        {
            if (LooksLikeAddressBar(edit))
            {
                return edit;
            }
        }

        return null;
    }

    private static bool LooksLikeAddressBar(AutomationElement element)
    {
        string automationId = ReadProperty(element, AutomationElement.AutomationIdProperty);
        string name = ReadProperty(element, AutomationElement.NameProperty);
        string helpText = ReadProperty(element, AutomationElement.HelpTextProperty);
        string candidate = $"{automationId} {name} {helpText}".ToLowerInvariant();

        return AddressBarNameMarkers.Any(marker => candidate.Contains(marker, StringComparison.Ordinal));
    }

    private static string? TryReadValue(AutomationElement element)
    {
        return element.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern) &&
               pattern is ValuePattern valuePattern
            ? valuePattern.Current.Value
            : null;
    }

    private static string ReadProperty(AutomationElement element, AutomationProperty property)
    {
        object value = element.GetCurrentPropertyValue(property, ignoreDefaultValue: true);
        return value is string text ? text : string.Empty;
    }
}
