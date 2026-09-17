param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet --info
    dotnet restore .\Comet.sln
    dotnet build .\Comet.sln -c $Configuration --no-restore
    dotnet run --project .\tests\Comet.SmokeTests\Comet.SmokeTests.csproj -c $Configuration --no-build
    Write-Host "Verification passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
