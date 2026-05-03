using LiveChartsCore.SkiaSharpView.WPF;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Woong.MonitorStack.Domain.Common;
using Woong.MonitorStack.Windows.App.Controls;
using Woong.MonitorStack.Windows.App.Views;
using Woong.MonitorStack.Windows.Presentation.Dashboard;
using static Woong.MonitorStack.Windows.App.Tests.WpfTestHelpers;

namespace Woong.MonitorStack.Windows.App.Tests;

public sealed partial class MainWindowUiExpectationTests
{

    [Fact]
    public void SettingsPanel_PreservesPrivacyControlsAndSafeDefaults()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                CheckBox collectionVisible = FindByAutomationId<CheckBox>(panel, "CollectionVisibleCheckBox");
                CheckBox windowTitleVisible = FindByAutomationId<CheckBox>(panel, "WindowTitleVisibleCheckBox");
                CheckBox pageTitleCapture = FindByAutomationId<CheckBox>(panel, "PageTitleCaptureCheckBox");
                CheckBox fullUrlCapture = FindByAutomationId<CheckBox>(panel, "FullUrlCaptureCheckBox");
                CheckBox domainOnlyStorage = FindByAutomationId<CheckBox>(panel, "DomainOnlyBrowserStorageCheckBox");
                TextBlock browserUrlPrivacy = FindByAutomationId<TextBlock>(panel, "BrowserUrlPrivacyText");

                Assert.Equal("Collection visible", collectionVisible.Content);
                Assert.True(collectionVisible.IsChecked);
                Assert.Equal("Capture window title", windowTitleVisible.Content);
                Assert.False(windowTitleVisible.IsChecked);
                Assert.Equal("Capture page title", pageTitleCapture.Content);
                Assert.False(pageTitleCapture.IsChecked);
                Assert.Equal("Full URL capture (off)", fullUrlCapture.Content);
                Assert.False(fullUrlCapture.IsEnabled);
                Assert.False(fullUrlCapture.IsChecked);
                Assert.Equal("Domain-only browser storage", domainOnlyStorage.Content);
                Assert.False(domainOnlyStorage.IsEnabled);
                Assert.True(domainOnlyStorage.IsChecked);
                Assert.Equal(
                    "Browser URL storage is domain-only by default. Full URLs require explicit future opt-in.",
                    browserUrlPrivacy.Text);
                Assert.True(dashboard.ViewModel.Settings.IsCollectionVisible);
                Assert.False(dashboard.ViewModel.Settings.IsWindowTitleVisible);
                Assert.False(dashboard.ViewModel.Settings.IsPageTitleCaptureEnabled);
                Assert.False(dashboard.ViewModel.Settings.IsFullUrlCaptureEnabled);
                Assert.True(dashboard.ViewModel.Settings.IsDomainOnlyBrowserStorageEnabled);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedSectionHeadingTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<Style>(panel.FindResource("SettingsSectionTitleTextStyle"));
                AssertSettingsSectionTitleStyle(FindTextBlock(panel, "Privacy"));
                AssertSettingsSectionTitleStyle(FindTextBlock(panel, "Sync"));
                AssertSettingsSectionTitleStyle(FindTextBlock(panel, "Runtime"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedMutedTextTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<Style>(panel.FindResource("SettingsMutedTextStyle"));
                AssertSettingsMutedTextStyle(FindByAutomationId<TextBlock>(panel, "BrowserUrlPrivacyText"));
                AssertSettingsMutedTextStyle(FindByAutomationId<TextBlock>(panel, "SyncModeLabel"));
                AssertSettingsMutedTextStyle(FindTextBlock(panel, "Poll interval: 1 second"));
                AssertSettingsMutedTextStyle(FindTextBlock(panel, "Idle threshold: 5 minutes"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedWarningTextTypography()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<SolidColorBrush>(panel.FindResource("WarningTextBrush"));
                Assert.IsType<Style>(panel.FindResource("SettingsWarningTextStyle"));
                AssertSettingsWarningTextStyle(FindByAutomationId<TextBlock>(panel, "SyncStatusLabel"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedInputStyle()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);
                TextBox syncEndpoint = FindByAutomationId<TextBox>(panel, "SyncEndpointTextBox");

                Assert.IsType<Style>(panel.FindResource("SettingsInputTextBoxStyle"));
                Style style = Assert.IsType<Style>(syncEndpoint.Style);
                AssertStyleSetter(style, Control.FontSizeProperty, 13.0);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_UsesSharedCheckBoxStyle()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                Assert.IsType<Style>(panel.FindResource("SettingsCheckBoxStyle"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "CollectionVisibleCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "WindowTitleVisibleCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "PageTitleCaptureCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "FullUrlCaptureCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "DomainOnlyBrowserStorageCheckBox"));
                AssertSettingsCheckBoxStyle(FindByAutomationId<CheckBox>(panel, "SyncEnabledCheckBox"));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_PreservesSyncControlsAndTwoWayBinding()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);

                CheckBox syncEnabled = FindByAutomationId<CheckBox>(panel, "SyncEnabledCheckBox");
                TextBlock syncMode = FindByAutomationId<TextBlock>(panel, "SyncModeLabel");
                TextBlock syncStatus = FindByAutomationId<TextBlock>(panel, "SyncStatusLabel");
                TextBox syncEndpoint = FindByAutomationId<TextBox>(panel, "SyncEndpointTextBox");
                Button registerRepairDevice = FindByAutomationId<Button>(panel, "RegisterRepairDeviceButton");
                TextBlock registrationStatus = FindByAutomationId<TextBlock>(panel, "SyncDeviceRegistrationStatusText");

                Assert.Equal("Sync enabled", syncEnabled.Content);
                Assert.False(syncEnabled.IsChecked);
                Assert.Equal("Local only", syncMode.Text);
                Assert.Equal("Sync is off. Data stays on this Windows device.", syncStatus.Text);
                Assert.Equal("No sync endpoint configured", syncEndpoint.Text);
                Assert.False(syncEndpoint.IsEnabled);
                Assert.Equal("Register / repair device", registerRepairDevice.Content);
                Assert.Same(dashboard.ViewModel.RegisterRepairDeviceCommand, registerRepairDevice.Command);
                Assert.Equal(
                    "Device not registered. Register / repair is available after sync is turned on.",
                    registrationStatus.Text);
                AssertReadableButton(registerRepairDevice);

                syncEnabled.IsChecked = true;
                window.UpdateLayout();

                Assert.True(dashboard.ViewModel.Settings.IsSyncEnabled);
                Assert.Equal("Sync enabled", syncMode.Text);
                Assert.True(syncEndpoint.IsEnabled);
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_ShowsRegisterRepairDeviceButtonWithoutTokenInput()
        => RunOnStaThread(() =>
        {
            TestDashboard dashboard = CreateDashboard();
            var window = new MainWindow(dashboard.ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);
                Button registerRepairDevice = FindByAutomationId<Button>(panel, "RegisterRepairDeviceButton");
                TextBlock registrationStatus = FindByAutomationId<TextBlock>(panel, "SyncDeviceRegistrationStatusText");
                IReadOnlyList<TextBox> textBoxes = FindVisualDescendants<TextBox>(panel);
                IReadOnlyList<PasswordBox> passwordBoxes = FindVisualDescendants<PasswordBox>(panel);
                IReadOnlySet<string> visibleText = CollectText(panel);

                Assert.Equal("Register / repair device", registerRepairDevice.Content);
                Assert.Same(dashboard.ViewModel.RegisterRepairDeviceCommand, registerRepairDevice.Command);
                Assert.Equal(
                    "Device not registered. Register / repair is available after sync is turned on.",
                    registrationStatus.Text);
                Assert.Empty(passwordBoxes);
                Assert.DoesNotContain(textBoxes, textBox =>
                    AutomationProperties.GetAutomationId(textBox).Contains("Token", StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(visibleText, text =>
                    text.Contains("device token", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public void SettingsPanel_PreservesRuntimeAndStorageActions()
        => RunOnStaThread(() =>
        {
            var window = new MainWindow(CreateDashboard().ViewModel);

            try
            {
                SettingsPanel panel = ShowSettingsPanel(window);
                IReadOnlySet<string> text = CollectText(panel);

                Assert.Contains("Poll interval: 1 second", text);
                Assert.Contains("Idle threshold: 5 minutes", text);
                Assert.Contains("Runtime log", text);
                Assert.Contains("Local SQLite DB", text);
                Assert.Contains("App lifetime", text);
                TextBlock runtimeLogPath = FindByAutomationId<TextBlock>(panel, "RuntimeLogPathText");
                TextBlock currentDatabasePath = FindByAutomationId<TextBlock>(panel, "CurrentDatabasePathText");
                TextBlock databaseStatus = FindByAutomationId<TextBlock>(panel, "DatabaseStatusLabel");
                Button createDatabase = FindByAutomationId<Button>(panel, "CreateLocalDatabaseButton");
                Button loadDatabase = FindByAutomationId<Button>(panel, "LoadExistingLocalDatabaseButton");
                Button deleteDatabase = FindByAutomationId<Button>(panel, "DeleteLocalDatabaseButton");
                Button exitApplication = FindByAutomationId<Button>(panel, "ExitApplicationButton");
                Assert.Equal("runtime log disabled", runtimeLogPath.Text);
                Assert.Equal("No local database configured", currentDatabasePath.Text);
                Assert.Equal("Local database ready.", databaseStatus.Text);
                Assert.Equal("Create / switch DB", createDatabase.Content);
                Assert.Equal("Load existing DB", loadDatabase.Content);
                Assert.Equal("Delete local DB", deleteDatabase.Content);
                Assert.Equal("Exit app", exitApplication.Content);
                Assert.False(deleteDatabase.IsEnabled);
                AssertReadableButton(createDatabase);
                AssertReadableButton(loadDatabase);
                AssertReadableButton(deleteDatabase);
                AssertReadableButton(exitApplication);
                Assert.Same(((DashboardViewModel)window.DataContext).ExitApplicationCommand, exitApplication.Command);
            }
            finally
            {
                window.Close();
            }
        });
}
