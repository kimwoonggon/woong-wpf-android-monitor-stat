using System.Xml.Linq;
using System.Text.Json;

namespace Woong.MonitorStack.Architecture.Tests;

public sealed class PrivacyBoundaryRulesTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AndroidManifest_DoesNotDeclareInvasiveMonitoringPermissionsOrServices()
    {
        XDocument manifest = XDocument.Load(Path.Combine(
            RepositoryRoot,
            "android",
            "app",
            "src",
            "main",
            "AndroidManifest.xml"));
        XNamespace android = "http://schemas.android.com/apk/res/android";

        string[] declaredPermissions = manifest
            .Descendants("uses-permission")
            .Select(permission => permission.Attribute(android + "name")?.Value)
            .OfType<string>()
            .ToArray();
        string[] prohibitedPermissions =
        [
            "android.permission.BIND_ACCESSIBILITY_SERVICE",
            "android.permission.SYSTEM_ALERT_WINDOW",
            "android.permission.RECORD_AUDIO",
            "android.permission.CAMERA",
            "android.permission.READ_SMS",
            "android.permission.RECEIVE_SMS",
            "android.permission.SEND_SMS",
            "android.permission.READ_CONTACTS",
            "android.permission.READ_CALL_LOG",
            "android.permission.READ_PHONE_STATE",
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.WRITE_EXTERNAL_STORAGE",
            "android.permission.MANAGE_EXTERNAL_STORAGE",
            "android.permission.READ_MEDIA_IMAGES",
            "android.permission.READ_MEDIA_VIDEO",
            "android.permission.READ_MEDIA_AUDIO",
            "android.permission.QUERY_ALL_PACKAGES",
            "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE",
            "android.permission.BIND_INPUT_METHOD"
        ];

        string[] violations = declaredPermissions
            .Intersect(prohibitedPermissions, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Empty(violations);
        Assert.DoesNotContain(
            manifest.Descendants("service"),
            service => string.Equals(
                service.Attribute(android + "permission")?.Value,
                "android.permission.BIND_ACCESSIBILITY_SERVICE",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ChromeExtension_DoesNotRequestPageContentScreenCaptureClipboardOrHistoryAccess()
    {
        string manifestText = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "extensions",
            "chrome",
            "manifest.json"));
        string backgroundScript = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "extensions",
            "chrome",
            "background.js"));

        string[] forbiddenManifestTokens =
        [
            "\"content_scripts\"",
            "\"debugger\"",
            "\"cookies\"",
            "\"history\"",
            "\"downloads\"",
            "\"clipboardRead\"",
            "\"clipboardWrite\"",
            "\"desktopCapture\"",
            "\"tabCapture\""
        ];
        string[] forbiddenScriptTokens =
        [
            "chrome.scripting",
            "chrome.debugger",
            "chrome.cookies",
            "chrome.history",
            "chrome.downloads",
            "chrome.tabs.captureVisibleTab",
            "chrome.desktopCapture",
            "chrome.tabCapture",
            "navigator.clipboard",
            "document.body",
            "document.querySelector"
        ];

        Assert.Empty(FindTokenViolations("extensions/chrome/manifest.json", manifestText, forbiddenManifestTokens));
        Assert.Empty(FindTokenViolations("extensions/chrome/background.js", backgroundScript, forbiddenScriptTokens));

        using JsonDocument manifest = JsonDocument.Parse(manifestText);
        Assert.True(manifest.RootElement.TryGetProperty("manifest_version", out JsonElement manifestVersion));
        Assert.Equal(3, manifestVersion.GetInt32());
    }

    [Fact]
    public void ProductSource_DoesNotUseForbiddenInputScreenClipboardOrAccessibilityApis()
    {
        string[] roots =
        [
            Path.Combine(RepositoryRoot, "src"),
            Path.Combine(RepositoryRoot, "android", "app", "src", "main"),
            Path.Combine(RepositoryRoot, "extensions", "chrome")
        ];
        string[] extensions = [".cs", ".kt", ".kts", ".xml", ".js", ".json"];
        string[] forbiddenTokens =
        [
            "SetWindowsHookEx",
            "WH_KEYBOARD",
            "WH_KEYBOARD_LL",
            "LowLevelKeyboardProc",
            "GetAsyncKeyState",
            "GetKeyState",
            "GetKeyboardState",
            "ToUnicode",
            "RegisterRawInputDevices",
            "System.Windows.Clipboard",
            "Clipboard.Get",
            "Clipboard.Set",
            "BitBlt",
            "PrintWindow",
            "Graphics.CopyFromScreen",
            "IDXGIOutputDuplication",
            "Windows.Graphics.Capture",
            "MediaProjection",
            "AccessibilityService",
            "BIND_ACCESSIBILITY_SERVICE",
            "dispatchGesture",
            "TYPE_VIEW_TEXT_CHANGED",
            "InputConnection",
            "ClipboardManager",
            "chrome.tabs.captureVisibleTab",
            "chrome.desktopCapture",
            "chrome.tabCapture",
            "chrome.scripting",
            "chrome.cookies",
            "chrome.history",
            "navigator.clipboard",
            "document.querySelector"
        ];

        string[] violations = EnumerateProductSourceFiles(roots, extensions)
            .SelectMany(path => FindTokenViolations(
                Path.GetRelativePath(RepositoryRoot, path).Replace('\\', '/'),
                File.ReadAllText(path),
                forbiddenTokens))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void UiSnapshotTool_CapturesOnlyTargetAppWindowOrElements()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "tools",
            "Woong.MonitorStack.Windows.UiSnapshots",
            "Program.cs"));

        Assert.Contains("window.CaptureToFile", source, StringComparison.Ordinal);
        Assert.Contains("element.CaptureToFile", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AutomationElement.RootElement", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Desktop.Capture", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Graphics.CopyFromScreen", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PrintWindow", source, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateProductSourceFiles(string[] roots, string[] extensions)
    {
        foreach (string root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (IsIgnoredPath(path) || !extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return path;
            }
        }
    }

    private static bool IsIgnoredPath(string path)
    {
        string normalized = path.Replace('\\', '/');

        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/build/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/.gradle/", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] FindTokenViolations(string relativePath, string source, string[] forbiddenTokens)
        => forbiddenTokens
            .Where(token => source.Contains(token, StringComparison.OrdinalIgnoreCase))
            .Select(token => $"{relativePath}: forbidden token `{token}`")
            .ToArray();

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Woong.MonitorStack.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
