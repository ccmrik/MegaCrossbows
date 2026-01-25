# Quick Git Commit Script
# Usage: .\quick-commit.ps1 "Your commit message"

param(
    [Parameter(Mandatory=$true)]
    [string]$message
)

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

Write-Host "Git Status:" -ForegroundColor Cyan
git status --short

Write-Host ""
Write-Host "Adding changes..." -ForegroundColor Yellow
git add .

Write-Host "Committing..." -ForegroundColor Yellow
git commit -m "$message"

Write-Host ""
Write-Host "Recent commits:" -ForegroundColor Cyan
git log --oneline --decorate -5

Write-Host ""
Write-Host "? Commit complete!" -ForegroundColor Green
