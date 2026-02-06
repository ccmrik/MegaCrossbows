# View MegaCrossbows Log File

$logPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " MegaCrossbows Log Viewer" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $logPath) {
    $logInfo = Get-Item $logPath
    Write-Host "Log file: $logPath" -ForegroundColor Yellow
    Write-Host "Size: $($logInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "Modified: $($logInfo.LastWriteTime)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "=== LOG CONTENTS ===" -ForegroundColor Green
    Write-Host ""
    
    Get-Content $logPath
    
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "Lines: $((Get-Content $logPath).Count)" -ForegroundColor Gray
} else {
    Write-Host "? Log file not found!" -ForegroundColor Red
    Write-Host "Expected location: $logPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "The log file will be created when:" -ForegroundColor Yellow
    Write-Host "  1. You start Valheim" -ForegroundColor Gray
    Write-Host "  2. The mod loads" -ForegroundColor Gray
    Write-Host ""
}

Write-Host ""
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  Get-Content `"$logPath`" -Tail 50  # Last 50 lines" -ForegroundColor Gray
Write-Host "  Get-Content `"$logPath`" -Wait      # Live tail" -ForegroundColor Gray
