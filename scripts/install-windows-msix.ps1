param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,
    [string]$CertificatePath = "",
    [switch]$TrustCertificate,
    [ValidateSet("LocalMachine", "CurrentUser")]
    [string]$TrustScope = "LocalMachine",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-CertificateMatchesPackageSigner {
    param(
        [string]$PackagePath,
        [string]$CertificatePath
    )

    $signature = Get-AuthenticodeSignature -FilePath $PackagePath
    if ($null -eq $signature.SignerCertificate) {
        throw "MSIX package does not expose a signer certificate: $PackagePath"
    }

    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($CertificatePath)
    try {
        if (-not [string]::Equals(
                $signature.SignerCertificate.Thumbprint,
                $certificate.Thumbprint,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "The provided certificate does not match the MSIX signer. Use the .cer shipped in the same GitHub Actions artifact as this .msix. Package signer thumbprint: $($signature.SignerCertificate.Thumbprint). Provided certificate thumbprint: $($certificate.Thumbprint)."
        }
    }
    finally {
        $certificate.Dispose()
    }
}

$resolvedPackagePath = [System.IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $resolvedPackagePath)) {
    throw "MSIX package not found: $resolvedPackagePath"
}

if ($TrustCertificate) {
    if ([string]::IsNullOrWhiteSpace($CertificatePath)) {
        throw "-TrustCertificate requires -CertificatePath for a public .cer certificate."
    }

    $resolvedCertificatePath = [System.IO.Path]::GetFullPath($CertificatePath)
    if (-not (Test-Path -LiteralPath $resolvedCertificatePath)) {
        throw "Certificate file not found: $resolvedCertificatePath"
    }

    Assert-CertificateMatchesPackageSigner -PackagePath $resolvedPackagePath -CertificatePath $resolvedCertificatePath

    $certificateStoreLocation = if ($TrustScope -eq "LocalMachine") {
        "Cert:\LocalMachine\TrustedPeople"
    }
    else {
        "Cert:\CurrentUser\TrustedPeople"
    }

    if ($TrustScope -eq "LocalMachine" -and -not $WhatIf -and -not (Test-IsAdministrator)) {
        throw "TrustScope LocalMachine requires PowerShell as Administrator. Re-run from an elevated PowerShell prompt, or pass -TrustScope CurrentUser for a less reliable per-user trust attempt."
    }

    Write-Host "Importing package certificate into $certificateStoreLocation"
    if (-not $WhatIf) {
        Import-Certificate -FilePath $resolvedCertificatePath -CertStoreLocation $certificateStoreLocation | Out-Null
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
    Write-Warning "CertificatePath was provided but -TrustCertificate was not set. The certificate will not be imported."
}

Write-Host "Installing MSIX package with Add-AppxPackage: $resolvedPackagePath"
if (-not $WhatIf) {
    try {
        Add-AppxPackage -Path $resolvedPackagePath
    }
    catch {
        $message = $_.Exception.Message
        if ($message.Contains("0x800B010A", [System.StringComparison]::OrdinalIgnoreCase) -or
            $message.Contains("publisher certificate", [System.StringComparison]::OrdinalIgnoreCase) -or
            $message.Contains("root certificate", [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "MSIX install failed because Windows does not trust the signing certificate chain yet. Run this script from elevated PowerShell with -TrustCertificate -TrustScope LocalMachine and the .cer shipped in the same artifact as this .msix, or sign releases with Azure Artifact Signing / a public trusted code-signing certificate. Original error: $message"
        }

        throw
    }
}
