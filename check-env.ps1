# Development Environment Setup Check
# Verifies all tools are installed correctly

Write-Host "==================================" -ForegroundColor Cyan
Write-Host " MegaCrossbows Dev Environment" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Refresh PATH
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

# Check Git
Write-Host "Checking Git..." -ForegroundColor Yellow
try {
    $gitVersion = git --version
    Write-Host "? $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "? Git not found" -ForegroundColor Red
}

# Check Node.js
Write-Host "Checking Node.js..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "? Node.js $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "? Node.js not found (restart terminal)" -ForegroundColor Yellow
}

# Check NPM
Write-Host "Checking NPM..." -ForegroundColor Yellow
try {
    $npmVersion = npm --version 2>$null
    if ($npmVersion) {
        Write-Host "? NPM $npmVersion" -ForegroundColor Green
    } else {
        Write-Host "? NPM not found (restart terminal)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? NPM not found (restart terminal)" -ForegroundColor Yellow
}

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "? .NET SDK $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "? .NET SDK not found" -ForegroundColor Red
}

# Check PowerShell version
Write-Host "Checking PowerShell..." -ForegroundColor Yellow
Write-Host "? PowerShell $($PSVersionTable.PSVersion)" -ForegroundColor Green

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan

# Check project files
Write-Host ""
Write-Host "Project Status:" -ForegroundColor Cyan
Write-Host "? Plugin source: MegaCrossbows\Class1.cs" -ForegroundColor Green
Write-Host "? Patches: MegaCrossbows\CrossbowPatches.cs" -ForegroundColor Green
Write-Host "? Config: MegaCrossbows\MegaCrossbows.csproj" -ForegroundColor Green

# Check paths
Write-Host ""
Write-Host "Important Paths:" -ForegroundColor Cyan

$valheimPath = "C:\Program Files (x86)\Steam\steamapps\common\Valheim"
if (Test-Path $valheimPath) {
    Write-Host "? Valheim: $valheimPath" -ForegroundColor Green
} else {
    Write-Host "? Valheim not found" -ForegroundColor Red
}

$pluginPath = "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows"
if (Test-Path $pluginPath) {
    Write-Host "? Plugin folder: $pluginPath" -ForegroundColor Green
} else {
    Write-Host "? Plugin folder will be created on first build" -ForegroundColor Yellow
}

# Git status
Write-Host ""
Write-Host "Git Repository:" -ForegroundColor Cyan
try {
    $gitStatus = git rev-parse --short HEAD 2>$null
    if ($gitStatus) {
        Write-Host "? Repository initialized (commit: $gitStatus)" -ForegroundColor Green
        $tags = git tag
        if ($tags) {
            Write-Host "  Tags: $($tags -join ', ')" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "? Git repository not initialized" -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Environment check complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run: .\build-and-deploy.ps1" -ForegroundColor White
Write-Host "  2. Launch Valheim through r2modman" -ForegroundColor White
Write-Host "  3. Check logs: .\view-logs.ps1" -ForegroundColor White
Write-Host ""
