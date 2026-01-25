# Test Helper - Crossbow Detection

Write-Host "==================================" -ForegroundColor Cyan
Write-Host " MegaCrossbows - Test Guide" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "What Changed:" -ForegroundColor Yellow
Write-Host "? Rewrote automatic fire system" -ForegroundColor Green
Write-Host "? Fixed zoom functionality" -ForegroundColor Green
Write-Host "? Improved crossbow detection" -ForegroundColor Green
Write-Host "? Added proper Player.Update patch" -ForegroundColor Green
Write-Host ""

Write-Host "How to Test:" -ForegroundColor Yellow
Write-Host "1. Restart Valheim (important!)" -ForegroundColor White
Write-Host "2. Press F5 to open console" -ForegroundColor White
Write-Host "3. Look for: 'MegaCrossbows v1.0.0 loaded!'" -ForegroundColor White
Write-Host "4. Craft or spawn a crossbow:" -ForegroundColor White
Write-Host "   spawn ArbalestBronze 1" -ForegroundColor Gray
Write-Host "5. Equip the crossbow" -ForegroundColor White
Write-Host "6. Hold LEFT MOUSE BUTTON - should rapid fire" -ForegroundColor White
Write-Host "7. Hold RIGHT MOUSE BUTTON - should zoom" -ForegroundColor White
Write-Host "8. Scroll wheel while zoomed - adjust zoom" -ForegroundColor White
Write-Host ""

Write-Host "Troubleshooting:" -ForegroundColor Yellow
Write-Host "• If not working, check config file exists:" -ForegroundColor White
Write-Host "  %AppData%\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\config\" -ForegroundColor Gray
Write-Host "  Look for: com.rikal.megacrossbows.cfg" -ForegroundColor Gray
Write-Host ""
Write-Host "• Check if 'Enabled = true' in config" -ForegroundColor White
Write-Host "• Try setting FireRate higher (like 10) for more obvious effect" -ForegroundColor White
Write-Host "• Make sure you're using an actual crossbow (not bow)" -ForegroundColor White
Write-Host ""

Write-Host "Common Issues:" -ForegroundColor Yellow
Write-Host "• MUST restart Valheim after deploying new DLL" -ForegroundColor Red
Write-Host "• Config only creates AFTER first launch" -ForegroundColor White
Write-Host "• Crossbow name must contain 'crossbow' or use Crossbows skill" -ForegroundColor White
Write-Host ""

Write-Host "Valheim Crossbow Names:" -ForegroundColor Cyan
Write-Host "• ArbalestBronze" -ForegroundColor Gray
Write-Host "• ArbalestIron" -ForegroundColor Gray
Write-Host "• Crossbow (if any mod adds it)" -ForegroundColor Gray
Write-Host ""
