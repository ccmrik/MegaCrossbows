# MegaCrossbows Build and Deploy Script
# This script builds the mod and copies it to the plugin folder

Write-Host "==================================" -ForegroundColor Cyan
Write-Host " MegaCrossbows Build & Deploy" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build MegaCrossbows\MegaCrossbows.csproj --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build successful!" -ForegroundColor Green
    
    # Check if DLL was copied
    $pluginPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.dll"
    
    if (Test-Path $pluginPath) {
        $fileInfo = Get-Item $pluginPath
        Write-Host "? Plugin deployed: $pluginPath" -ForegroundColor Green
        Write-Host "  File size: $($fileInfo.Length) bytes" -ForegroundColor Gray
        Write-Host "  Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
    } else {
        Write-Host "? Warning: DLL not found in plugin folder" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Ready to test in game! Launch Valheim through r2modman." -ForegroundColor Cyan
} else {
    Write-Host "? Build failed! Check errors above." -ForegroundColor Red
    exit 1
}

Write-Host ""
