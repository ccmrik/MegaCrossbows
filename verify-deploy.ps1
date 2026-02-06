# Verify Deployment

Write-Host "==================================" -ForegroundColor Cyan
Write-Host " Deployment Verification" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

$sourcePath = "MegaCrossbows\bin\Release\net462\MegaCrossbows.dll"
$targetPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.dll"

if (Test-Path $sourcePath) {
    $source = Get-Item $sourcePath
    Write-Host "Source DLL:" -ForegroundColor Yellow
    Write-Host "  Path: $sourcePath" -ForegroundColor Gray
    Write-Host "  Size: $($source.Length) bytes" -ForegroundColor Gray
    Write-Host "  Modified: $($source.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "? Source DLL not found!" -ForegroundColor Red
}

Write-Host ""

if (Test-Path $targetPath) {
    $target = Get-Item $targetPath
    Write-Host "Target DLL (Deployed):" -ForegroundColor Yellow
    Write-Host "  Path: $targetPath" -ForegroundColor Gray
    Write-Host "  Size: $($target.Length) bytes" -ForegroundColor Gray
    Write-Host "  Modified: $($target.LastWriteTime)" -ForegroundColor Gray
    
    Write-Host ""
    
    if ($source.Length -eq $target.Length -and $source.LastWriteTime -eq $target.LastWriteTime) {
        Write-Host "? Files match! Deployment successful!" -ForegroundColor Green
    } else {
        Write-Host "??  Files don't match - may need to copy" -ForegroundColor Yellow
        
        if ($source.LastWriteTime -gt $target.LastWriteTime) {
            Write-Host "   Source is NEWER - deploying..." -ForegroundColor Yellow
            Copy-Item $sourcePath $targetPath -Force
            Write-Host "? Copied!" -ForegroundColor Green
        }
    }
} else {
    Write-Host "? Target path not found!" -ForegroundColor Red
    Write-Host "   Creating directory and copying..." -ForegroundColor Yellow
    $targetDir = Split-Path $targetPath
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Copy-Item $sourcePath $targetPath -Force
    Write-Host "? Deployed!" -ForegroundColor Green
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Next: Restart Valheim completely!" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
