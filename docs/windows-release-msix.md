# Windows Release, CI, And MSIX

This page covers the Windows WPF app only.

## Release Build And Run

Run from the repository root:

```powershell
dotnet restore Woong.MonitorStack.sln --configfile NuGet.config
dotnet build Woong.MonitorStack.sln -c Release --no-restore -m:1 -v minimal
dotnet run --configuration Release --project src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj
```

The WPF app creates its SQLite database automatically. You do not need to start
SQLite. By default it uses:

```text
%LOCALAPPDATA%\WoongMonitorStack\windows-local.db
```

Clicking the window X minimizes Woong Monitor Stack to the taskbar so tracking
can continue. Use **Settings -> Exit app** for an explicit shutdown.

## GitHub CI/CD

The Windows workflow is:

```text
.github/workflows/windows-wpf-ci.yml
```

It runs on `windows-latest` and performs:

- restore
- Release build
- Release test
- WPF app publish
- unsigned MSIX packaging
- artifact upload

Artifacts:

- `woong-monitor-windows-app`
- `woong-monitor-windows-msix`

## Local MSIX Package

The packaging script builds a layout under ignored `artifacts\windows-msix` and
uses the Windows SDK `MakeAppx.exe`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1
```

This creates an unsigned MSIX by default:

```text
artifacts\windows-msix\WoongMonitorStack.Windows.msix
```

An unsigned MSIX is useful as a packaging artifact, but Windows normally requires
a signed MSIX for installation.

## Signed MSIX Install

Use a trusted development certificate or a release certificate. The install
script imports a certificate only when `-TrustCertificate` is explicitly passed,
and only into the current user store:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install-windows-msix.ps1 `
  -PackagePath artifacts\windows-msix\WoongMonitorStack.Windows.msix `
  -CertificatePath D:\path\to\woong-monitor.cer `
  -TrustCertificate
```

The script uses `Cert:\CurrentUser\TrustedPeople`, not `LocalMachine`.

To package and sign in one command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1 `
  -Sign `
  -CertificatePath D:\path\to\woong-monitor.pfx `
  -CertificatePassword "<password>"
```

Do not commit private signing certificates or passwords.

## Notes

- MSIX packaging requires Windows SDK tools: `MakeAppx.exe` and optionally
  `SignTool.exe`.
- The app remains a visible WPF desktop app. MSIX packaging does not change the
  privacy boundary: no keylogging, typed text capture, screen capture, page
  content capture, passwords, messages, forms, or clipboard capture.

