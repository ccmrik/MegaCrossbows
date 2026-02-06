# Complete Diagnostic Check

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " MegaCrossbows - Complete Diagnostic" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Check 1: DLL exists and version
$dllPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.dll"
Write-Host "1. Checking DLL..." -ForegroundColor Yellow
if (Test-Path $dllPath) {
    $dll = Get-Item $dllPath
    Write-Host "  ? DLL exists" -ForegroundColor Green
    Write-Host "    Size: $($dll.Length) bytes" -ForegroundColor Gray
    Write-Host "    Modified: $($dll.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "  ? DLL NOT FOUND!" -ForegroundColor Red
}
Write-Host ""

# Check 2: Config file
$configPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\config\com.rikal.megacrossbows.cfg"
Write-Host "2. Checking Config..." -ForegroundColor Yellow
if (Test-Path $configPath) {
    Write-Host "  ? Config exists" -ForegroundColor Green
    $config = Get-Content $configPath
    Write-Host "    Key settings:" -ForegroundColor Gray
    $config | Select-String "Enabled|FireRate|Capacity" | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
} else {
    Write-Host "  ? Config NOT FOUND (will be created on first run)" -ForegroundColor Yellow
}
Write-Host ""

# Check 3: Log file
$logPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log"
Write-Host "3. Checking Custom Log..." -ForegroundColor Yellow
if (Test-Path $logPath) {
    $log = Get-Item $logPath
    Write-Host "  ? Log file exists!" -ForegroundColor Green
    Write-Host "    Size: $($log.Length) bytes" -ForegroundColor Gray
    Write-Host "    Modified: $($log.LastWriteTime)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  === LAST 30 LINES ===" -ForegroundColor Cyan
    Get-Content $logPath -Tail 30 | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
} else {
    Write-Host "  ? Log file NOT FOUND" -ForegroundColor Red
    Write-Host "    This means the mod hasn't run yet OR logger failed to initialize" -ForegroundColor Yellow
}
Write-Host ""

# Check 4: BepInEx main log
$bepinexLog = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\LogOutput.log"
Write-Host "4. Checking BepInEx Log for MegaCrossbows..." -ForegroundColor Yellow
if (Test-Path $bepinexLog) {
    $mcLines = Get-Content $bepinexLog | Select-String "MegaCrossbows|MC\]" | Select-Object -Last 20
    if ($mcLines) {
        Write-Host "  ? Found MegaCrossbows entries" -ForegroundColor Green
        $mcLines | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
    } else {
        Write-Host "  ? NO MegaCrossbows entries found!" -ForegroundColor Red
        Write-Host "    Mod may not be loading at all" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ? BepInEx log not found" -ForegroundColor Red
}
Write-Host ""

# Check 5: Harmony errors
Write-Host "5. Checking for Harmony errors..." -ForegroundColor Yellow
if (Test-Path $bepinexLog) {
    $errors = Get-Content $bepinexLog | Select-String "Error.*MegaCrossbows|Exception.*MegaCrossbows|HarmonyException" | Select-Object -Last 10
    if ($errors) {
        Write-Host "  ? Found errors:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    } else {
        Write-Host "  ? No errors found" -ForegroundColor Green
    }
} else {
    Write-Host "  ? Cannot check - log not found" -ForegroundColor Red
}
Write-Host ""

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $logPath) {
    Write-Host "CUSTOM LOG EXISTS - Mod is at least attempting to run" -ForegroundColor Green
    Write-Host "Run: .\view-mod-log.ps1  to see full log" -ForegroundColor Yellow
} else {
    Write-Host "NO CUSTOM LOG - Mod may not be loading!" -ForegroundColor Red
    Write-Host "Possible issues:" -ForegroundColor Yellow
    Write-Host "  1. BepInEx not working" -ForegroundColor Gray
    Write-Host "  2. Harmony patches failing" -ForegroundColor Gray
    Write-Host "  3. Config has Enabled = false" -ForegroundColor Gray
}
Write-Host ""

Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. If log exists: Review it for clues" -ForegroundColor Gray
Write-Host "  2. If no log: Check BepInEx is working (other mods load?)" -ForegroundColor Gray
Write-Host "  3. Try with devcommands: spawn Ripper 1" -ForegroundColor Gray
Write-Host "  4. Hold left mouse and check if anything happens" -ForegroundColor Gray
