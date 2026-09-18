param(
    [string]$CometPath = (Join-Path $PSScriptRoot "..\artifacts\win-x64\Comet.exe"),
    [string]$BookPath,
    [switch]$KeepExistingLog
)

$ErrorActionPreference = "Stop"
$CometPath = [IO.Path]::GetFullPath($CometPath)
$logPath = Join-Path $env:LOCALAPPDATA "Comet\logs\performance.log"

if (-not (Test-Path $CometPath)) {
    Write-Error "Comet.exe not found: $CometPath"
}

if (-not $KeepExistingLog -and (Test-Path $logPath)) {
    Remove-Item $logPath -Force
}

$oldPerf = $env:COMET_PERF
$env:COMET_PERF = "1"
try {
    if ([string]::IsNullOrWhiteSpace($BookPath)) {
        $process = Start-Process -FilePath $CometPath -PassThru
    }
    else {
        $fullBookPath = [IO.Path]::GetFullPath($BookPath)
        $process = Start-Process -FilePath $CometPath -ArgumentList @("`"$fullBookPath`"") -PassThru
    }

    Write-Host "Comet performance session started (PID $($process.Id))."
    Write-Host "Use Comet normally, turn 20-30 pages, then exit Comet."
    Wait-Process -Id $process.Id
}
finally {
    $env:COMET_PERF = $oldPerf
}

& (Join-Path $PSScriptRoot "summarize-performance.ps1") -Path $logPath
