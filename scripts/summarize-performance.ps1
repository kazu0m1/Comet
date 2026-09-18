param(
    [string]$Path = (Join-Path $env:LOCALAPPDATA "Comet\logs\performance.log")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Path)) {
    Write-Error "Performance log not found: $Path. Run Comet with COMET_PERF=1 first."
}

$rows = foreach ($line in Get-Content $Path) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split "`t", 4
    if ($parts.Count -lt 3) { continue }

    $msText = $parts[2] -replace " ms$", ""
    [pscustomobject]@{
        Time = $parts[0]
        Operation = $parts[1]
        Milliseconds = if ($msText -eq "-") { $null } else { [double]::Parse($msText, [Globalization.CultureInfo]::InvariantCulture) }
        Detail = if ($parts.Count -ge 4) { $parts[3] } else { "" }
    }
}

if (-not $rows) {
    Write-Host "No performance records found."
    exit 0
}

function Get-Percentile([double[]]$Values, [double]$P) {
    if (-not $Values -or $Values.Count -eq 0) { return $null }
    $sorted = $Values | Sort-Object
    $index = [Math]::Ceiling(($P / 100.0) * $sorted.Count) - 1
    $index = [Math]::Max(0, [Math]::Min($sorted.Count - 1, $index))
    return $sorted[$index]
}

Write-Host ""
Write-Host "Comet performance summary"
Write-Host "Log: $Path"
Write-Host ""

$timed = $rows | Where-Object { $null -ne $_.Milliseconds }
$groups = $timed | Group-Object Operation | Sort-Object Name

$result = foreach ($group in $groups) {
    [double[]]$values = @($group.Group | ForEach-Object { $_.Milliseconds })
    [pscustomobject]@{
        Operation = $group.Name
        Count = $values.Count
        AvgMs = [Math]::Round(($values | Measure-Object -Average).Average, 2)
        P50Ms = [Math]::Round((Get-Percentile $values 50), 2)
        P95Ms = [Math]::Round((Get-Percentile $values 95), 2)
        MaxMs = [Math]::Round(($values | Measure-Object -Maximum).Maximum, 2)
    }
}

$result | Format-Table -AutoSize

$cacheRows = @($rows | Where-Object { $_.Operation -match '^(?<role>.+)\.cache\.(?<result>hit|miss)
 })
if ($cacheRows.Count -gt 0) {
    Write-Host ""
    foreach ($roleGroup in ($cacheRows | Group-Object { ($_.Operation -split '\.cache\.')[0] } | Sort-Object Name)) {
        $hits = @($roleGroup.Group | Where-Object Operation -like "*.cache.hit").Count
        $misses = @($roleGroup.Group | Where-Object Operation -like "*.cache.miss").Count
        $total = $hits + $misses
        $rate = if ($total -gt 0) { [Math]::Round(100.0 * $hits / $total, 1) } else { 0 }
        Write-Host "$($roleGroup.Name) cache hit rate: $rate% ($hits hits / $misses misses)"
    }
}
else {
    $hits = @($rows | Where-Object Operation -eq "cache.hit").Count
    $misses = @($rows | Where-Object Operation -eq "cache.miss").Count
    $total = $hits + $misses
    if ($total -gt 0) {
        $rate = [Math]::Round(100.0 * $hits / $total, 1)
        Write-Host ""
        Write-Host "Cache hit rate: $rate% ($hits hits / $misses misses)"
    }
}
