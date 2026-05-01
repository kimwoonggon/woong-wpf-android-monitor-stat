param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts\windows-msix",
    [switch]$SkipPublish,
    [switch]$Sign,
    [string]$CertificatePath = "",
    [string]$CertificatePassword = "",
    [string]$InstallCertificatePath = "",
    [switch]$CreateTestCertificate,
    [string]$CertificateSubject = "CN=WoongMonitorStack",
    [switch]$Install,
    [switch]$TrustCertificate
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\Woong.MonitorStack.Windows.App\Woong.MonitorStack.Windows.App.csproj"
$manifestTemplatePath = Join-Path $repoRoot "packaging\windows-msix\AppxManifest.xml"
$outputRootPath = Join-Path $repoRoot $OutputRoot
$publishDir = Join-Path $repoRoot "artifacts\windows-app"
$layoutDir = Join-Path $outputRootPath "layout"
$appLayoutDir = Join-Path $layoutDir "App"
$assetLayoutDir = Join-Path $layoutDir "Assets"
$certificateOutputDir = Join-Path $outputRootPath "certificates"
$msixPath = Join-Path $outputRootPath "WoongMonitorStack.Windows.msix"
$generatedCerPath = ""

function Assert-UnderArtifacts {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
    if (-not $fullPath.StartsWith($artifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to delete or reset path outside artifacts: $fullPath"
    }
}

function Reset-Directory {
    param([string]$Path)

    Assert-UnderArtifacts -Path $Path
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Find-WindowsSdkTool {
    param([string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $sdkRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    if (Test-Path -LiteralPath $sdkRoot) {
        $tool = Get-ChildItem -LiteralPath $sdkRoot -Recurse -Filter $Name -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($tool) {
            return $tool.FullName
        }
    }

    throw "Could not find $Name. Install the Windows SDK or run on windows-latest."
}

function Assert-NativeCommandSucceeded {
    param([string]$Operation)

    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

function NewPlaceholderPng {
    param(
        [string]$Path,
        [int]$Size
    )

    Add-Type -AssemblyName System.Drawing
    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(15, 107, 222))
        $fontSize = [Math]::Max(12, [int]($Size / 2.4))
        $font = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $rectangle = New-Object System.Drawing.RectangleF(0, 0, $Size, $Size)
        $graphics.DrawString("W", $font, $brush, $rectangle, $format)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function New-TestSigningCertificate {
    param(
        [string]$Subject,
        [string]$OutputDirectory,
        [string]$Password
    )

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

    $pfxPath = Join-Path $OutputDirectory "WoongMonitorStack.Windows.TestSigning.pfx"
    $cerPath = Join-Path $OutputDirectory "WoongMonitorStack.Windows.TestSigning.cer"
    $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
    $certificate = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $Subject `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

    try {
        Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
        Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null
    }
    finally {
        Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
    }

    return [pscustomobject]@{
        PfxPath = $pfxPath
        CerPath = $cerPath
        Password = $Password
    }
}

function Export-PublicCertificateFromPfx {
    param(
        [string]$PfxPath,
        [string]$Password,
        [string]$OutputDirectory
    )

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

    $cerPath = Join-Path $OutputDirectory "WoongMonitorStack.Windows.Signing.cer"
    $storageFlags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet
    if ([string]::IsNullOrEmpty($Password)) {
        $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($PfxPath)
    }
    else {
        $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
            $PfxPath,
            $Password,
            $storageFlags)
    }

    try {
        [System.IO.File]::WriteAllBytes(
            $cerPath,
            $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    }
    finally {
        $certificate.Dispose()
    }

    return $cerPath
}

function Write-InstallReadme {
    param(
        [string]$Path,
        [string]$CertificateFileName
    )

    $content = @"
# Woong Monitor Stack signed MSIX artifact

This artifact contains a signed MSIX package:

- `WoongMonitorStack.Windows.msix`
- `certificates\$CertificateFileName`
- `install-windows-msix.ps1`
- `Install-WoongMonitorStack.Windows.cmd`

Recommended install path:

1. Right-click `Install-WoongMonitorStack.Windows.cmd`.
2. Choose **Run as administrator**.
3. Accept the UAC prompt.

Manual install from an elevated PowerShell prompt:

~~~powershell
powershell -ExecutionPolicy Bypass -File .\install-windows-msix.ps1 -PackagePath .\WoongMonitorStack.Windows.msix -CertificatePath .\certificates\$CertificateFileName -TrustCertificate -TrustScope LocalMachine
~~~

Use the `.cer` shipped in the same artifact as the `.msix`.
The ephemeral test certificate changes on every CI run, so a certificate from a previous artifact will not trust this package.

Double-clicking `WoongMonitorStack.Windows.msix` before certificate trust is expected to fail with `0x800B010A`.
For a double-click MSIX install without a trust step, sign the release with Azure Artifact Signing, Microsoft Store signing, or a public trusted code-signing certificate.
"@

    Set-Content -Path $Path -Value $content -Encoding UTF8
}

function Write-InstallLauncher {
    param(
        [string]$Path,
        [string]$CertificateFileName
    )

    $content = @"
@echo off
setlocal
set SCRIPT_DIR=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process PowerShell -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%SCRIPT_DIR%install-windows-msix.ps1"" -PackagePath ""%SCRIPT_DIR%WoongMonitorStack.Windows.msix"" -CertificatePath ""%SCRIPT_DIR%certificates\$CertificateFileName"" -TrustCertificate -TrustScope LocalMachine'"
"@

    Set-Content -Path $Path -Value $content -Encoding ASCII
}

if (-not (Test-Path -LiteralPath $manifestTemplatePath)) {
    throw "Missing MSIX manifest template: $manifestTemplatePath"
}

New-Item -ItemType Directory -Path $outputRootPath -Force | Out-Null

if (-not $SkipPublish) {
    Write-Host "Publishing WPF app to $publishDir"
    dotnet publish $projectPath -c $Configuration --no-restore -o $publishDir
    Assert-NativeCommandSucceeded -Operation "dotnet publish"
}

if (-not (Test-Path -LiteralPath (Join-Path $publishDir "Woong.MonitorStack.Windows.App.exe"))) {
    throw "Published app executable not found. Run without -SkipPublish or publish to $publishDir first."
}

if ($CreateTestCertificate) {
    if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
        $CertificatePassword = [Guid]::NewGuid().ToString("N")
    }

    $generatedCertificate = New-TestSigningCertificate `
        -Subject $CertificateSubject `
        -OutputDirectory $certificateOutputDir `
        -Password $CertificatePassword
    $Sign = $true
    $CertificatePath = $generatedCertificate.PfxPath
    $generatedCerPath = $generatedCertificate.CerPath
    $InstallCertificatePath = $generatedCertificate.CerPath
}

Reset-Directory -Path $layoutDir
New-Item -ItemType Directory -Path $appLayoutDir -Force | Out-Null
New-Item -ItemType Directory -Path $assetLayoutDir -Force | Out-Null

Copy-Item -Path (Join-Path $publishDir "*") -Destination $appLayoutDir -Recurse -Force
Copy-Item -LiteralPath $manifestTemplatePath -Destination (Join-Path $layoutDir "AppxManifest.xml") -Force
NewPlaceholderPng -Path (Join-Path $assetLayoutDir "StoreLogo.png") -Size 50
NewPlaceholderPng -Path (Join-Path $assetLayoutDir "Square44x44Logo.png") -Size 44
NewPlaceholderPng -Path (Join-Path $assetLayoutDir "Square150x150Logo.png") -Size 150

$makeAppx = Find-WindowsSdkTool -Name "MakeAppx.exe"
if (Test-Path -LiteralPath $msixPath) {
    Remove-Item -LiteralPath $msixPath -Force
}

Write-Host "Packing MSIX with $makeAppx"
& $makeAppx pack /d $layoutDir /p $msixPath /o
Assert-NativeCommandSucceeded -Operation "MakeAppx.exe pack"

if ($Sign) {
    if ([string]::IsNullOrWhiteSpace($CertificatePath)) {
        throw "-Sign requires -CertificatePath."
    }

    $signTool = Find-WindowsSdkTool -Name "SignTool.exe"
    $signArgs = @("sign", "/fd", "SHA256", "/f", $CertificatePath)
    if (-not [string]::IsNullOrEmpty($CertificatePassword)) {
        $signArgs += @("/p", $CertificatePassword)
    }

    $signArgs += $msixPath
    Write-Host "Signing MSIX with $signTool"
    & $signTool @signArgs
    Assert-NativeCommandSucceeded -Operation "SignTool.exe sign"

    if ([string]::IsNullOrWhiteSpace($generatedCerPath) -and [string]::IsNullOrWhiteSpace($InstallCertificatePath)) {
        $generatedCerPath = Export-PublicCertificateFromPfx `
            -PfxPath $CertificatePath `
            -Password $CertificatePassword `
            -OutputDirectory $certificateOutputDir
        $InstallCertificatePath = $generatedCerPath
    }
}

if ($Install) {
    $installScript = Join-Path $repoRoot "scripts\install-windows-msix.ps1"
    $installArgs = @("-PackagePath", $msixPath)
    $certificateForInstall = $InstallCertificatePath
    if ([string]::IsNullOrWhiteSpace($certificateForInstall) -and -not [string]::IsNullOrWhiteSpace($generatedCerPath)) {
        $certificateForInstall = $generatedCerPath
    }

    if (-not [string]::IsNullOrWhiteSpace($certificateForInstall)) {
        $installArgs += @("-CertificatePath", $certificateForInstall)
    }

    if ($TrustCertificate) {
        if ([string]::IsNullOrWhiteSpace($certificateForInstall)) {
            throw "-TrustCertificate requires -InstallCertificatePath or -CreateTestCertificate."
        }

        $installArgs += "-TrustCertificate"
    }

    & $installScript @installArgs
}

Copy-Item -LiteralPath (Join-Path $repoRoot "scripts\install-windows-msix.ps1") `
    -Destination (Join-Path $outputRootPath "install-windows-msix.ps1") `
    -Force

if (-not [string]::IsNullOrWhiteSpace($generatedCerPath)) {
    $certificateFileName = Split-Path -Leaf $generatedCerPath
    Write-InstallLauncher `
        -Path (Join-Path $outputRootPath "Install-WoongMonitorStack.Windows.cmd") `
        -CertificateFileName $certificateFileName

    Write-InstallReadme `
        -Path (Join-Path $outputRootPath "README.md") `
        -CertificateFileName $certificateFileName
}

Write-Host "MSIX package: $msixPath"
if (-not [string]::IsNullOrWhiteSpace($generatedCerPath)) {
    Write-Host "MSIX public certificate: $generatedCerPath"
}
