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
    Add-AppxPackage -Path $resolvedPackagePath
}
