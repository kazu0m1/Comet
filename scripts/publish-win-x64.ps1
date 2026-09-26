param(
    [string]$Version = "1.0.3"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts\win-x64"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

$project = Join-Path $root "src\Comet.App\Comet.App.csproj"
dotnet publish $project `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishReadyToRun=true `
  -p:PublishTrimmed=false `
  -p:PublishSingleFile=false `
  -p:Version=$Version `
  -o $out

# createdump.exe is a .NET diagnostics helper. Comet does not enable
# automatic crash-dump generation, so omit it from end-user packages.
$createDump = Join-Path $out "createdump.exe"
if (Test-Path $createDump) { Remove-Item $createDump -Force }

Copy-Item (Join-Path $root "LICENSE") $out
Copy-Item (Join-Path $root "NOTICE.md") $out
Write-Host "Published to $out"
