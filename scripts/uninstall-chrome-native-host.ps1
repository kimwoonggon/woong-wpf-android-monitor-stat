[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Chrome", "Edge", "Brave", "Firefox")]
    [string]$Browser = "Chrome",

    [string]$HostName = "com.woong.monitorstack.chrome_test",
    [switch]$HadPreviousValue,
    [string]$PreviousDefaultValue = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Assert-ScopedNativeHostName {
    param([string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name) -or $Name -notmatch '^[a-z0-9_]+(\.[a-z0-9_]+)+$') {
        throw "HostName must be a scoped native messaging host name such as com.woong.monitorstack.chrome_test."
    }
}

function Get-NativeMessagingRegistryRoot {
    param([string]$BrowserName)

    switch ($BrowserName) {
        "Chrome" { "HKCU:\Software\Google\Chrome\NativeMessagingHosts" }
        "Edge" { "HKCU:\Software\Microsoft\Edge\NativeMessagingHosts" }
        "Brave" { "HKCU:\Software\BraveSoftware\Brave-Browser\NativeMessagingHosts" }
        "Firefox" { "HKCU:\Software\Mozilla\NativeMessagingHosts" }
    }
}

Assert-ScopedNativeHostName $HostName

$registryRoot = Get-NativeMessagingRegistryRoot $Browser
$registryPath = "$registryRoot\$HostName"
$displayRegistryPath = $registryPath.Replace("HKCU:", "HKCU")

Write-Host "Native host cleanup key: $displayRegistryPath"

if ($DryRun) {
    Write-Host "DRY RUN: no HKCU registry values were changed."
    if ($HadPreviousValue) {
        Write-Host "Restored previous value would be: $PreviousDefaultValue"
    } else {
        Write-Host "Removed key would be: $displayRegistryPath"
    }
    return
}

try {
    if ($HadPreviousValue) {
        if ($PSCmdlet.ShouldProcess($registryPath, "Restore previous native messaging host value")) {
            New-Item -Path $registryPath -Force | Out-Null
            Set-Item -Path $registryPath -Value $PreviousDefaultValue
        }

        Write-Host "Restored $Browser native messaging host value:"
        Write-Host "  Host: $HostName"
        Write-Host "  Registry: $displayRegistryPath"
        Write-Host "  Manifest: $PreviousDefaultValue"
        return
    }

    if (Test-Path $registryPath) {
        if ($PSCmdlet.ShouldProcess($registryPath, "Remove scoped native messaging host key")) {
            Remove-Item -Path $registryPath -Recurse -Force
        }
    }

    Write-Host "Removed $Browser native messaging host key:"
    Write-Host "  Host: $HostName"
    Write-Host "  Registry: $displayRegistryPath"
}
catch {
    Write-Warning "Native host cleanup failed for ${displayRegistryPath}: $($_.Exception.Message)"
    Write-Warning "Manual cleanup command: Remove-Item -Path '$registryPath' -Recurse -Force"
    throw
}
