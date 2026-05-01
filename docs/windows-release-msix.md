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
- signed MSIX packaging with a stable release signing certificate when
  repository secrets are configured
- signed MSIX packaging with an ephemeral test certificate when release signing
  secrets are not configured
- artifact upload

Artifacts:

- `woong-monitor-windows-app`
- `woong-monitor-windows-msix`

The `woong-monitor-windows-msix` artifact contains:

- `WoongMonitorStack.Windows.msix`
- `certificates\WoongMonitorStack.Windows.TestSigning.cer` for ephemeral CI test-certificate builds
- `certificates\WoongMonitorStack.Windows.Signing.cer` for stable release-secret builds
- `install-windows-msix.ps1`
- `Install-WoongMonitorStack.Windows.cmd`
- `README.md`

The CI artifact does not include the private `.pfx` key.

## Release Signing Secrets

For installable release artifacts, configure these GitHub repository secrets:

```text
WINDOWS_MSIX_CERTIFICATE_BASE64
WINDOWS_MSIX_CERTIFICATE_PASSWORD
```

`WINDOWS_MSIX_CERTIFICATE_BASE64` is a base64-encoded `.pfx` file. The workflow
decodes it only into `$env:RUNNER_TEMP\woong-monitor-msix-signing.pfx`, signs
the MSIX, exports only the public `.cer` into the artifact, and never uploads
the private `.pfx`.

If either secret is missing, CI falls back to an ephemeral test certificate so
pull requests and routine main builds still produce a locally testable signed
MSIX. That fallback certificate is not a stable release signing identity.

## Tag-Based Windows Releases

The release workflow is:

```text
.github/workflows/windows-wpf-release.yml
```

It runs for tags matching `v*` and can also be started manually. Unlike the
regular CI workflow, releases require `WINDOWS_MSIX_CERTIFICATE_BASE64` and
`WINDOWS_MSIX_CERTIFICATE_PASSWORD`; there is no ephemeral test-certificate
fallback for release publishing.

The workflow restores, builds, tests, publishes the WPF app, signs the MSIX with
the stable certificate, creates
`artifacts\woong-monitor-windows-msix-<tag>.zip`, uploads it as an Actions
artifact, and attaches it to the GitHub Release for that tag.

## Download From GitHub Actions

After the `Windows WPF CI` workflow finishes:

1. Open the workflow run in GitHub Actions.
2. Download the artifact named `woong-monitor-windows-msix`.
3. Extract the zip to a local folder.
4. Right-click `Install-WoongMonitorStack.Windows.cmd`.
5. Choose **Run as administrator** and accept the UAC prompt.

Manual install from an elevated PowerShell prompt:

```powershell
powershell -ExecutionPolicy Bypass -File .\install-windows-msix.ps1 `
  -PackagePath .\WoongMonitorStack.Windows.msix `
  -CertificatePath .\certificates\WoongMonitorStack.Windows.TestSigning.cer `
  -TrustCertificate `
  -TrustScope LocalMachine
```

Use the `.cer` shipped in the same artifact as the `.msix`. For stable
release-secret builds that file is normally
`certificates\WoongMonitorStack.Windows.Signing.cer`; for ephemeral CI
test-certificate builds it is normally
`certificates\WoongMonitorStack.Windows.TestSigning.cer`.

The ephemeral test certificate changes on every CI run, so a certificate from a
previous artifact will not trust a newly downloaded package.

The first install trusts the public test certificate in
`Cert:\LocalMachine\TrustedPeople`. This requires Administrator because Windows
App Installer validates MSIX signing certificates against the machine trust
store. Before the certificate is trusted, `Get-AuthenticodeSignature` can report
an untrusted-root status even though the MSIX has a signer certificate.

Double-clicking `WoongMonitorStack.Windows.msix` before certificate trust is expected to fail
for CI/local test-certificate artifacts. The MSIX package is signed, but Windows
does not trust a self-signed or private signing certificate until you explicitly
install the public `.cer` on the machine.

If you see `0x800B010A` or "publisher certificate could not be verified", the
certificate has not been trusted in the machine `TrustedPeople` store yet, or
you used a `.cer` from a different artifact. Re-run the install command above
from an elevated PowerShell prompt with the `.cer` included beside the `.msix`.

Remove local test certificate trust after verification:

```powershell
$thumbprint = (Get-AuthenticodeSignature .\WoongMonitorStack.Windows.msix).SignerCertificate.Thumbprint
certutil -delstore TrustedPeople $thumbprint
```

Run the removal command from an elevated PowerShell prompt if you trusted the
certificate with `-TrustScope LocalMachine`. If you deliberately used
`-TrustScope CurrentUser`, add `-user` to the `certutil` command.

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
`-TrustCertificate` is explicitly passed. For test certificates, use the
machine `TrustedPeople` store:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install-windows-msix.ps1 `
  -PackagePath artifacts\windows-msix\WoongMonitorStack.Windows.msix `
  -CertificatePath artifacts\windows-msix\certificates\WoongMonitorStack.Windows.TestSigning.cer `
  -TrustCertificate `
  -TrustScope LocalMachine
```

`-TrustScope LocalMachine` uses `Cert:\LocalMachine\TrustedPeople` and requires
Administrator. `-TrustScope CurrentUser` is still available for development
experiments, but it may not satisfy App Installer certificate validation.

To package and sign in one command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-windows-msix.ps1 `
  -Sign `
  -CertificatePath D:\path\to\woong-monitor.pfx `
  -CertificatePassword "<password>"
```

The script exports `artifacts\windows-msix\certificates\WoongMonitorStack.Windows.Signing.cer`
from a provided stable release signing certificate so the install artifact still
contains the public certificate required for sideload trust.

Do not commit private signing certificates or passwords.

For a double-click MSIX install without any certificate trust step, do not use
the ephemeral CI test certificate. Sign the MSIX with Azure Artifact Signing
(formerly Trusted Signing), Microsoft Store signing, or a public trusted
code-signing certificate whose chain is already trusted by Windows.

## Notes

- MSIX packaging requires Windows SDK tools: `MakeAppx.exe` and optionally
  `SignTool.exe`.
- The app remains a visible WPF desktop app. MSIX packaging does not change the
  privacy boundary: no keylogging, typed text capture, screen capture, page
  content capture, passwords, messages, forms, or clipboard capture.
