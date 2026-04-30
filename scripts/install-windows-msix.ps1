param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,
    [string]$CertificatePath = "",
    [switch]$TrustCertificate,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

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

    Write-Host "Importing package certificate into Cert:\CurrentUser\TrustedPeople"
    if (-not $WhatIf) {
        Import-Certificate -FilePath $resolvedCertificatePath -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" | Out-Null
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
    Write-Warning "CertificatePath was provided but -TrustCertificate was not set. The certificate will not be imported."
}

Write-Host "Installing MSIX package with Add-AppxPackage: $resolvedPackagePath"
if (-not $WhatIf) {
    Add-AppxPackage -Path $resolvedPackagePath
}

