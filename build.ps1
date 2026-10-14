# Build Script for Solution ERD Builder

Write-Host "`nBuilding Solution ERD Builder..." -ForegroundColor Cyan
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Green
dotnet restore SolutionErd.sln

Write-Host "`nBuilding project..." -ForegroundColor Green
dotnet build SolutionErd.sln --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful!" -ForegroundColor Green
    Write-Host "`nTo install the plugin:" -ForegroundColor Yellow
    Write-Host "  1. Run .\deploy.ps1 -Force" -ForegroundColor White
    Write-Host "  Or manually copy DLLs from src\SolutionErdXrmToolBoxPlugin\bin\Release\net48\ to your XRM ToolBox Plugins folder" -ForegroundColor White
}
else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit 1
}
