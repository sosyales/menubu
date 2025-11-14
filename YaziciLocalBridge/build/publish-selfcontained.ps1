$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Split-Path -Parent $projectRoot
$publishDir = Join-Path $solutionRoot "publish\win-x64"

Write-Host ">> Temizlik yapılıyor..."
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host ">> Self-contained build alınıyor..."
dotnet publish "$solutionRoot\MenuBuPrinterAgent.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $publishDir

Write-Host ">> Build tamamlandı. Çıktı dizini: $publishDir"
