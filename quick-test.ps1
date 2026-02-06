# Quick Test - Is Mod Loading?

Write-Host "==================================" -ForegroundColor Cyan
Write-Host " Quick Mod Loading Test" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

$logPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log"
$bepinexLog = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\LogOutput.log"

Write-Host "Test 1: Custom log file exists?" -ForegroundColor Yellow
if (Test-Path $logPath) {
    Write-Host "  ? YES - Mod is at least trying to run" -ForegroundColor Green
    Write-Host ""
    Write-Host "Last log entry:" -ForegroundColor Cyan
    Get-Content $logPath -Tail 1
    Write-Host ""
} else {
    Write-Host "  ? NO - Mod is NOT loading!" -ForegroundColor Red
    Write-Host ""
}

Write-Host "Test 2: BepInEx loaded MegaCrossbows?" -ForegroundColor Yellow
if (Test-Path $bepinexLog) {
    $loaded = Get-Content $bepinexLog | Select-String "MegaCrossbows.*loaded" | Select-Object -Last 1
    if ($loaded) {
        Write-Host "  ? YES" -ForegroundColor Green
        Write-Host "  $loaded" -ForegroundColor Gray
    } else {
        Write-Host "  ? NO - Not found in BepInEx log" -ForegroundColor Red
    }
} else {
    Write-Host "  ? Cannot check - BepInEx log not found" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "Test 3: Any Harmony errors?" -ForegroundColor Yellow
if (Test-Path $bepinexLog) {
    $errors = Get-Content $bepinexLog | Select-String "Error.*MegaCrossbows|HarmonyException|MegaCrossbows.*Exception" | Select-Object -Last 3
    if ($errors) {
        Write-Host "  ? YES - Found errors:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    } else {
        Write-Host "  ? NO - No errors found" -ForegroundColor Green
    }
} else {
    Write-Host "  ? Cannot check" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "VERDICT:" -ForegroundColor Cyan

if (Test-Path $logPath) {
    Write-Host "Mod IS loading and creating log file" -ForegroundColor Green
    Write-Host "For full analysis, run: .\diagnose.ps1" -ForegroundColor Yellow
} else {
    Write-Host "Mod is NOT loading!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible reasons:" -ForegroundColor Yellow
    Write-Host "  1. Config has Enabled = false" -ForegroundColor Gray
    Write-Host "  2. BepInEx not working" -ForegroundColor Gray
    Write-Host "  3. Harmony patches failing" -ForegroundColor Gray
    Write-Host "  4. DLL not in correct location" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Run: .\diagnose.ps1  for detailed check" -ForegroundColor Cyan
}
