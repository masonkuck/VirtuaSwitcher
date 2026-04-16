$publishDir = Join-Path $PSScriptRoot "publish"

Write-Host "Publishing VirtuaSwitcher..." -ForegroundColor Cyan

dotnet publish "$PSScriptRoot\VirtuaSwitcher.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Published to: $publishDir" -ForegroundColor Green
Write-Host "Executable:   $publishDir\VirtuaSwitcher.exe" -ForegroundColor Green
