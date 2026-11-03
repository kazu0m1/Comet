# Native PDFium smoke: open, render, Unicode extraction, isolate process.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$exe = Get-ChildItem (Join-Path $root "src\Comet.PdfWorker\bin") -Filter "Comet.PdfWorker.exe" -File -Recurse |
    Where-Object { $_.FullName -match '[\\/]Release[\\/]' } | Select-Object -First 1
if (-not $exe) { throw "Worker binary was not built." }
$dir = Join-Path $env:RUNNER_TEMP ("comet-pdf-smoke-" + [Guid]::NewGuid().ToString("N"))
New-Item $dir -ItemType Directory | Out-Null
try {
    $nl = [char]10
    $content = "BT /F1 20 Tf 48 130 Td (Comet PDF smoke) Tj ET"
    $objects = @(
        "<< /Type /Catalog /Pages 2 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 240 240] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
        ("<< /Length " + $content.Length + " >>" + $nl + "stream" + $nl + $content + $nl + "endstream"),
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
    )
    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append("%PDF-1.4" + $nl)
    $offsets = @(0)
    for ($i = 0; $i -lt $objects.Count; $i++) {
        $offsets += $builder.Length
        [void]$builder.Append(($i+1).ToString() + " 0 obj" + $nl + $objects[$i] + $nl + "endobj" + $nl)
    }
    $xref = $builder.Length
    [void]$builder.Append("xref" + $nl + "0 " + ($objects.Count+1) + $nl + "0000000000 65535 f " + $nl)
    foreach ($offset in $offsets[1..($offsets.Count-1)]) {
        [void]$builder.Append(("{0:D10} 00000 n " -f [int]$offset) + $nl)
    }
    [void]$builder.Append("trailer" + $nl + "<< /Size " + ($objects.Count+1) + " /Root 1 0 R >>" + $nl +
        "startxref" + $nl + $xref + $nl + "%%EOF" + $nl)
    $pdf = Join-Path $dir "sample.pdf"
    [IO.File]::WriteAllText($pdf, $builder.ToString(), [Text.Encoding]::ASCII)
    $psi = [Diagnostics.ProcessStartInfo]::new($exe.FullName)
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $worker = [Diagnostics.Process]::Start($psi)
    try {
        function Ask-Worker([hashtable]$request) {
            $worker.StandardInput.WriteLine(($request | ConvertTo-Json -Compress))
            $worker.StandardInput.Flush()
            $line = $worker.StandardOutput.ReadLine()
            if (-not $line) { throw "Worker disconnected." }
            $reply = $line | ConvertFrom-Json
            if (-not $reply.ok) { throw "Worker error: $($reply.error), native=$($reply.pdfError)" }
            return $reply
        }
        $opened = Ask-Worker @{ command = "open"; path = $pdf }
        if ($opened.pageCount -ne 1) { throw "Expected one PDF page." }
        $render = Ask-Worker @{ command = "render"; index = 0; width = 512 }
        if (-not (Test-Path $render.imagePath)) { throw "PDF render image missing." }
        if ((Get-Item $render.imagePath).Length -lt 100000) { throw "Invalid raster output." }
        $pageText = Ask-Worker @{ command = "text"; index = 0 }
        if ($pageText.text -notlike "*Comet PDF smoke*") { throw "PDF text extraction failed." }
        $null = Ask-Worker @{ command = "outline" }
        $null = Ask-Worker @{ command = "close" }
        Write-Host "PDFium smoke: OK (render/text/outline)."
    }
    finally {
        if (-not $worker.HasExited) { $worker.Kill() }
        $worker.Dispose()
    }
}
finally { Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue }
