param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "artifacts\windows-msix",
    [switch]$SkipPublish,
    [switch]$Sign,
    [string]$CertificatePath = "",
    [string]$CertificatePassword = "",
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
$msixPath = Join-Path $outputRootPath "WoongMonitorStack.Windows.msix"

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

Write-Host "Packing unsigned MSIX with $makeAppx"
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
}

if ($Install) {
    $installScript = Join-Path $repoRoot "scripts\install-windows-msix.ps1"
    $installArgs = @("-PackagePath", $msixPath)
    if (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
        $installArgs += @("-CertificatePath", $CertificatePath)
    }

    if ($TrustCertificate) {
        $installArgs += "-TrustCertificate"
    }

    & $installScript @installArgs
}

Write-Host "MSIX package: $msixPath"
