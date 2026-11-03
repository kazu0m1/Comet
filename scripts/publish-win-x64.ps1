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
if ($LASTEXITCODE -ne 0) { throw "Comet application publish failed." }

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

# The NuGet package does not itself copy the upstream PDFium/third-party
# notices to the published directory. Verify the native DLL against its
# pinned upstream release and stage the complete license bundle.
& (Join-Path $PSScriptRoot "collect-pdfium-licenses.ps1") -WorkerOutput $workerOutput


# createdump.exe is a .NET diagnostics helper. Comet does not enable
# automatic crash-dump generation, so omit it from end-user packages.
$createDump = Join-Path $out "createdump.exe"
if (Test-Path $createDump) { Remove-Item $createDump -Force }
$workerDump = Join-Path $workerOutput "createdump.exe"
if (Test-Path $workerDump) { Remove-Item $workerDump -Force }

Copy-Item (Join-Path $root "LICENSE") $out
Copy-Item (Join-Path $root "NOTICE.md") $out
if ($Version.Contains("-dev")) {
    $previewScript = Join-Path $out "Start-Comet-PDF-Preview.cmd"
    @'
@echo off
set "COMET_PROFILE_DIR=%~dp0Preview-Profile"
start "" "%~dp0Comet.exe" %*
'@ | Set-Content $previewScript -Encoding ascii
}
Write-Host "Published to $out (including verified PDFium redistribution notices)"
