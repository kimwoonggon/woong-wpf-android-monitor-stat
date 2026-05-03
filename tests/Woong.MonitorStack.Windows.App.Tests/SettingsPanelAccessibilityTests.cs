using System.Windows.Controls;
using Woong.MonitorStack.Windows.App.Views;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed class SettingsPanelAccessibilityTests
{
    [Fact]
    public void SettingsPanel_PrimarySettingsControlsExposeReadableAutomationNames()
        => RunContentWindowTest(
            () => new SettingsPanel(),
            panel =>
            {
                AssertAutomationName<CheckBox>(panel, "CollectionVisibleCheckBox", "Collection visible");
                AssertAutomationName<CheckBox>(panel, "WindowTitleVisibleCheckBox", "Capture window title");
                AssertAutomationName<CheckBox>(panel, "PageTitleCaptureCheckBox", "Capture page title");
                AssertAutomationName<CheckBox>(panel, "FullUrlCaptureCheckBox", "Full URL capture");
                AssertAutomationName<CheckBox>(
                    panel,
                    "DomainOnlyBrowserStorageCheckBox",
                    "Domain-only browser storage");
                AssertAutomationName<CheckBox>(panel, "SyncEnabledCheckBox", "Sync enabled");
                AssertAutomationName<TextBox>(panel, "SyncEndpointTextBox", "Sync endpoint");
                AssertAutomationName<Button>(
                    panel,
                    "RegisterRepairDeviceButton",
                    "Register or repair sync device");
                AssertAutomationName<Button>(
                    panel,
                    "DisconnectSyncDeviceButton",
                    "Disconnect sync device");
            });

}
