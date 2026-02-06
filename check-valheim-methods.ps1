# Check Valheim Assembly Methods

try {
    $assembly = [System.Reflection.Assembly]::LoadFrom("C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll")
    
    Write-Host "=== PLAYER CLASS METHODS ===" -ForegroundColor Cyan
    $playerType = $assembly.GetType("Player")
    $methods = $playerType.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
    
    Write-Host "`nAttack-related methods:" -ForegroundColor Yellow
    $methods | Where-Object { $_.Name -like "*Attack*" } | Select-Object Name -Unique | Sort-Object Name | ForEach-Object { Write-Host "  - $($_.Name)" }
    
    Write-Host "`nStamina-related methods:" -ForegroundColor Yellow
    $methods | Where-Object { $_.Name -like "*Stamina*" } | Select-Object Name -Unique | Sort-Object Name | ForEach-Object { Write-Host "  - $($_.Name)" }
    
    Write-Host "`nDraw/Reload-related methods:" -ForegroundColor Yellow
    $methods | Where-Object { $_.Name -like "*Draw*" -or $_.Name -like "*Reload*" } | Select-Object Name -Unique | Sort-Object Name | ForEach-Object { Write-Host "  - $($_.Name)" }
    
    Write-Host "`n=== HUD CLASS METHODS ===" -ForegroundColor Cyan
    $hudType = $assembly.GetType("Hud")
    $hudMethods = $hudType.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance)
    
    Write-Host "`nGUI/Render-related:" -ForegroundColor Yellow
    $hudMethods | Where-Object { $_.Name -like "*GUI*" -or $_.Name -like "*Render*" -or $_.Name -like "*Draw*" } | Select-Object Name -Unique | Sort-Object Name | ForEach-Object { Write-Host "  - $($_.Name)" }
    
    Write-Host "`n=== MONOBEHAVIOUR BASE METHODS ===" -ForegroundColor Cyan
    $monoType = $assembly.GetType("UnityEngine.MonoBehaviour")
    if ($monoType) {
        Write-Host "MonoBehaviour available: Yes" -ForegroundColor Green
    }
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
