$runtimes = @("win-x64", "win-x86")
$failed = @()

foreach ($rid in $runtimes) {
    $publishDir = Join-Path $PSScriptRoot "publish\$rid"

    Write-Host "Publishing $rid..." -ForegroundColor Cyan

    dotnet publish "$PSScriptRoot\VirtuaSwitcher.csproj" `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Failed." -ForegroundColor Red
        $failed += $rid
    } else {
        Write-Host "  -> $publishDir\VirtuaSwitcher.exe" -ForegroundColor Green
    }

    Write-Host ""
}

if ($failed.Count -gt 0) {
    Write-Host "Failed builds: $($failed -join ', ')" -ForegroundColor Red
    exit 1
} else {
    Write-Host "All builds succeeded." -ForegroundColor Green
}
