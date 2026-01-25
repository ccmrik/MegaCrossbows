# Open Valheim Game Log
# Quickly view BepInEx logs for debugging

$logPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\LogOutput.log"

if (Test-Path $logPath) {
    Write-Host "Opening BepInEx log..." -ForegroundColor Cyan
    Write-Host "Log path: $logPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Last 50 lines:" -ForegroundColor Yellow
    Write-Host "================================" -ForegroundColor Gray
    Get-Content $logPath -Tail 50
    Write-Host "================================" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Full log opened in default text editor..." -ForegroundColor Cyan
    Start-Process $logPath
} else {
    Write-Host "? Log file not found at: $logPath" -ForegroundColor Red
    Write-Host "Make sure you've run Valheim with the mod at least once." -ForegroundColor Yellow
}
