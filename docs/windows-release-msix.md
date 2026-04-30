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
- signed MSIX packaging with a per-run test certificate
- artifact upload

Artifacts:

- `woong-monitor-windows-app`
- `woong-monitor-windows-msix`

The `woong-monitor-windows-msix` artifact contains:

- `WoongMonitorStack.Windows.msix`
- `certificates\WoongMonitorStack.Windows.TestSigning.cer`
- `install-windows-msix.ps1`
- `README.md`

The CI artifact does not include the private `.pfx` key.

## Download From GitHub Actions

After the `Windows WPF CI` workflow finishes:

1. Open the workflow run in GitHub Actions.
2. Download the artifact named `woong-monitor-windows-msix`.
3. Extract the zip to a local folder.
4. Open PowerShell in the extracted folder.
5. Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-windows-msix.ps1 `
  -PackagePath .\WoongMonitorStack.Windows.msix `
  -CertificatePath .\certificates\WoongMonitorStack.Windows.TestSigning.cer `
  -TrustCertificate
```

The first install trusts the public test certificate for the current Windows
user only. Before the certificate is trusted, `Get-AuthenticodeSignature` can
report an untrusted-root status even though the MSIX has a signer certificate.

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

To create a signed local test MSIX plus public certificate:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1 -CreateTestCertificate
```

This writes:

```text
artifacts\windows-msix\WoongMonitorStack.Windows.msix
artifacts\windows-msix\certificates\WoongMonitorStack.Windows.TestSigning.cer
artifacts\windows-msix\install-windows-msix.ps1
artifacts\windows-msix\README.md
```

## Signed MSIX Install

Use a trusted development certificate, a release certificate, or the CI/local
test certificate. The install script imports a certificate only when
`-TrustCertificate` is explicitly passed, and only into the current user store:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install-windows-msix.ps1 `
  -PackagePath artifacts\windows-msix\WoongMonitorStack.Windows.msix `
  -CertificatePath artifacts\windows-msix\certificates\WoongMonitorStack.Windows.TestSigning.cer `
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
