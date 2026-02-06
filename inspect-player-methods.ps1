# Properly check Player class methods

$valheimDll = "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll"

Write-Host "Loading assembly..." -ForegroundColor Cyan

Add-Type -Path $valheimDll

$playerType = [Player]

Write-Host "`n=== ALL PLAYER METHODS ===" -ForegroundColor Green
$playerType.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly) | 
    Where-Object { $_.DeclaringType.Name -eq "Player" } |
    Select-Object Name, ReturnType |
    Sort-Object Name -Unique |
    Format-Table -AutoSize

Write-Host "`n=== ATTACK-RELATED METHODS ===" -ForegroundColor Yellow
$playerType.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly) | 
    Where-Object { $_.Name -match "Attack|attack|Fire|Reload|Draw" -and $_.DeclaringType.Name -eq "Player" } |
    Select-Object Name, ReturnType |
    Sort-Object Name -Unique |
    Format-Table -AutoSize

Write-Host "`n=== INPUT/UPDATE METHODS ===" -ForegroundColor Yellow
$playerType.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly) | 
    Where-Object { $_.Name -match "Update|Input|Button" -and $_.DeclaringType.Name -eq "Player" } |
    Select-Object Name, ReturnType |
    Sort-Object Name -Unique |
    Format-Table -AutoSize

Write-Host "`n=== CHARACTER (BASE CLASS) ATTACK METHODS ===" -ForegroundColor Cyan
$characterType = [Character]
$characterType.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly) | 
    Where-Object { $_.Name -match "Attack|attack" -and $_.DeclaringType.Name -eq "Character" } |
    Select-Object Name, ReturnType |
    Sort-Object Name -Unique |
    Format-Table -AutoSize
