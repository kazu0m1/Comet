param(
    [string]$Version = "1.0.7"
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

# Publish the isolated PDFium worker only into its own subdirectory.
# The executable and native DLL are never loaded on archive-only startup.
$workerProject = Join-Path $root "src\Comet.PdfWorker\Comet.PdfWorker.csproj"
$workerOutput = Join-Path $out "pdf-worker"
dotnet publish $workerProject `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishReadyToRun=true `
  -p:PublishTrimmed=false `
  -p:PublishSingleFile=false `
  -p:Version=$Version `
  -o $workerOutput
if ($LASTEXITCODE -ne 0) { throw "PDF worker publish failed." }

# createdump.exe is a .NET diagnostics helper. Comet does not enable
# automatic crash-dump generation, so omit it from end-user packages.
$createDump = Join-Path $out "createdump.exe"
if (Test-Path $createDump) { Remove-Item $createDump -Force }
$workerDump = Join-Path $workerOutput "createdump.exe"
if (Test-Path $workerDump) { Remove-Item $workerDump -Force }

Copy-Item (Join-Path $root "LICENSE") $out
Copy-Item (Join-Path $root "NOTICE.md") $out
Write-Host "Published to $out"
