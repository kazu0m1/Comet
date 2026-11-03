param(
    [Parameter(Mandatory = $true)][string]$WorkerOutput
)

$ErrorActionPreference = "Stop"

# Match the binary version pinned in Comet.PdfWorker.csproj. The upstream
# asset is immutable and the digest is from the PDFium binary publisher's
# GitHub release chromium/8125. Fail closed if the asset changes.
$version = "158.0.8125"
$assetUrl = "https://github.com/bblanchon/pdfium-binaries/releases/download/chromium/8125/pdfium-win-x64.tgz"
$expectedSha256 = "9a964261760cff52570aabf038ab346d3a5034dd8b30d0d5fe8861235d356b8b"

$workerDll = Join-Path $WorkerOutput "pdfium.dll"
if (-not (Test-Path $workerDll)) {
    throw "The NuGet-published PDFium native DLL is missing: $workerDll"
}
$temp = Join-Path ([IO.Path]::GetTempPath()) ("Comet-Pdfium-License-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $temp -Force | Out-Null
try {
    $archive = Join-Path $temp "pdfium-win-x64.tgz"
    Invoke-WebRequest -Uri $assetUrl -OutFile $archive -MaximumRedirection 10
    $sha = (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sha -ne $expectedSha256) {
        throw "PDFium upstream release asset SHA256 mismatch: $sha"
    }
    $unpacked = Join-Path $temp "unpacked"
    New-Item -ItemType Directory -Path $unpacked | Out-Null
    & tar.exe -xzf $archive -C $unpacked
    if ($LASTEXITCODE -ne 0) { throw "Cannot extract pinned PDFium license bundle." }

    $upstreamDll = Get-ChildItem -Path $unpacked -Filter "pdfium.dll" -Recurse -File |
        Select-Object -First 1
    if ($null -eq $upstreamDll) { throw "Upstream PDFium asset has no native DLL." }
    $upstreamDllHash = (Get-FileHash $upstreamDll.FullName -Algorithm SHA256).Hash
    $publishedDllHash = (Get-FileHash $workerDll -Algorithm SHA256).Hash
    if ($upstreamDllHash -ne $publishedDllHash) {
        throw "The PDFium NuGet binary differs from the upstream pinned release; notices cannot be verified."
    }

    $licenseFile = Get-ChildItem -Path $unpacked -Filter "LICENSE" -Recurse -File |
        Where-Object { Test-Path (Join-Path $_.DirectoryName "licenses") } |
        Select-Object -First 1
    if ($null -eq $licenseFile) {
        throw "Pinned PDFium archive lacks its root LICENSE and licenses/ directory."
    }
    $thirdPartyDir = Join-Path $licenseFile.DirectoryName "licenses"
    $notices = @(Get-ChildItem $thirdPartyDir -Recurse -File)
    if ($notices.Count -lt 8) {
        throw "Unexpectedly incomplete PDFium third-party license bundle ($($notices.Count) files)."
    }

    $dest = Join-Path $WorkerOutput "licenses"
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    New-Item -ItemType Directory -Path $dest | Out-Null
    Copy-Item $licenseFile.FullName (Join-Path $dest "PDFium-LICENSE") -Force
    Copy-Item $thirdPartyDir (Join-Path $dest "third-party") -Recurse
    @(
        "Comet PDFium native distribution notices",
        "PDFium version: $version",
        "Binary origin: $assetUrl",
        "Upstream SHA256 (tgz): $expectedSha256",
        "The bundled pdfium.dll is byte-identical to this pinned upstream release.",
        "Main license: PDFium-LICENSE",
        "Bundled dependencies: third-party/",
        "The Comet application's separate MIT LICENSE remains in the application root."
    ) | Set-Content (Join-Path $dest "PROVENANCE.txt") -Encoding utf8

    if (-not (Test-Path (Join-Path $dest "PDFium-LICENSE")) -or
        -not (Test-Path (Join-Path $dest "third-party"))) {
        throw "Failed to stage PDFium license texts."
    }
    Write-Host "PDFium binary provenance and $($notices.Count) third-party license files verified and staged."
}
finally {
    Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
}
