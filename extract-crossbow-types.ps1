# Extract Crossbow and Bolt Types from Valheim

try {
    Write-Host "Loading Valheim assembly..." -ForegroundColor Cyan
    $objectDBPath = "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll"
    
    # Use reflection to load without executing
    $bytes = [System.IO.File]::ReadAllBytes($objectDBPath)
    $assembly = [System.Reflection.Assembly]::Load($bytes)
    
    # Try to find ObjectDB or item definitions
    Write-Host "`nSearching for item-related types..." -ForegroundColor Yellow
    
    $types = $assembly.GetTypes() | Where-Object { 
        $_.Name -like "*Item*" -or 
        $_.Name -like "*Weapon*" -or 
        $_.Name -like "*ObjectDB*" -or
        $_.Name -like "*Prefab*"
    }
    
    Write-Host "`nFound types:" -ForegroundColor Green
    $types | Select-Object Name | Sort-Object Name | ForEach-Object { Write-Host "  - $($_.Name)" }
    
    # Look for enums
    Write-Host "`nSearching for Skills enum..." -ForegroundColor Yellow
    $skillsType = $assembly.GetType("Skills")
    if ($skillsType) {
        $skillEnum = $skillsType.GetNestedType("SkillType")
        if ($skillEnum) {
            Write-Host "Found Skills.SkillType enum values:" -ForegroundColor Green
            [Enum]::GetNames($skillEnum) | ForEach-Object { Write-Host "  - $_" }
        }
    }
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

# Alternative: Search game files for item names
Write-Host "`n=== SEARCHING GAME DATA FILES ===" -ForegroundColor Cyan
$dataPath = "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data"

Write-Host "`nNote: Actual item data is in Unity asset bundles." -ForegroundColor Yellow
Write-Host "We'll need to check what the mod detects in practice." -ForegroundColor Yellow

Write-Host "`n=== KNOWN CROSSBOW TYPES (from game knowledge) ===" -ForegroundColor Cyan
@(
    "ArbalestBronze",
    "ArbalestIron", 
    "Ripper",
    "CrossbowArbalest",
    "Crossbow"
) | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }

Write-Host "`n=== KNOWN BOLT TYPES ===" -ForegroundColor Cyan
@(
    "BoltBone",
    "BoltIron",
    "BoltCarapace",
    "BoltBlackmetal",
    "BoltWood"
) | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
