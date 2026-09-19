param(
    [string]$Version = "0.3.0-dev",
    [switch]$BuildInstaller
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot "publish-win-x64.ps1") -Version $Version

$release = Join-Path $root "artifacts\release"
if (Test-Path $release) { Remove-Item $release -Recurse -Force }
New-Item -ItemType Directory -Path $release | Out-Null

$publish = Join-Path $root "artifacts\win-x64"
$portableName = "Comet-v$Version-win-x64.zip"
$portablePath = Join-Path $release $portableName
Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $portablePath -CompressionLevel Optimal

if ($BuildInstaller) {
    $iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        $fallback = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        if (Test-Path $fallback) { $iscc = Get-Item $fallback }
    }
    if (-not $iscc) { throw "Inno Setup (ISCC.exe) was not found." }

    $installerScript = Join-Path $root "installer\Comet.iss"
    & $iscc.Source "/DMyAppVersion=$Version" $installerScript
    $setup = Join-Path $root "artifacts\installer\Comet-v$Version-win-x64-Setup.exe"
    if (-not (Test-Path $setup)) { throw "Installer output was not created: $setup" }
    Copy-Item $setup $release
}

$checksums = Get-ChildItem $release -File | Sort-Object Name | ForEach-Object {
    $hash = Get-FileHash $_.FullName -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $_.Name
}
$checksums | Set-Content (Join-Path $release "SHA256SUMS.txt") -Encoding ascii
Write-Host "Release assets are in $release"
