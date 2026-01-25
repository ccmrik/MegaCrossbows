# Development Tools Installation Complete! ??

## ? Installed Tools

### Core Development Tools

1. **Git 2.52.0**
   - Purpose: Version control system
   - Location: Added to system PATH
   - Verify: `git --version`
   - Configuration:
     - Username: rikal
     - Email: rikal@megacrossbows.local
     - Default branch: main

2. **Node.js 25.4.0**
   - Purpose: JavaScript runtime (for build tools, documentation, etc.)
   - Includes: NPM (Node Package Manager)
   - Verify: `node --version`
   - NPM Verify: `npm --version` *(may need terminal restart)*

3. **PowerShell 7.5.4**
   - Purpose: Modern shell and scripting language
   - Verify: `pwsh --version`
   - Note: Original Windows PowerShell still available

4. **WinGet**
   - Purpose: Windows Package Manager (already installed)
   - Version: v1.12.460
   - Used for installing all other tools

## ?? Git Repository Status

? **Repository Initialized Successfully!**

- Initial commit: `v1.0.0 - Initial release`
- Branch: `main`
- Tag: `v1.0.0`
- Commit hash: `0897fe5`

### Files Tracked:
- ? MegaCrossbows/Class1.cs (Plugin main)
- ? MegaCrossbows/CrossbowPatches.cs (Harmony patches)
- ? MegaCrossbows/MegaCrossbows.csproj (Project config)
- ? .github/copilot-instructions.md (AI guidelines)
- ? README.md (User documentation)
- ? .gitignore (Git ignore rules)
- ? CHANGELOG.md (Version history)
- ? SETUP_COMPLETE.md (Setup guide)

## ??? Helper Scripts Created

### 1. **build-and-deploy.ps1**
Builds the mod and verifies deployment

```powershell
.\build-and-deploy.ps1
```

**What it does:**
- Compiles the mod in Release mode
- Verifies the DLL was copied to r2modman plugins folder
- Shows file size and timestamp
- Provides status messages

---

### 2. **quick-commit.ps1**
Quick Git commit with one command

```powershell
.\quick-commit.ps1 "Your commit message here"
```

**What it does:**
- Shows git status
- Stages all changes
- Commits with your message
- Shows recent commit history

**Example:**
```powershell
.\quick-commit.ps1 "Fixed fire rate bug"
```

---

### 3. **version-bump.ps1**
Automatically increment version and tag

```powershell
.\version-bump.ps1 patch "Bug fixes"
.\version-bump.ps1 minor "New zoom feature"
.\version-bump.ps1 major "Complete rewrite"
```

**What it does:**
- Reads current version from project files
- Increments version (patch, minor, or major)
- Updates MegaCrossbows.csproj
- Updates Class1.cs PluginVersion
- Creates Git commit and tag
- Reminds you to update CHANGELOG.md

**Examples:**
```powershell
# 1.0.0 ? 1.0.1
.\version-bump.ps1 patch "Fixed reload bug"

# 1.0.1 ? 1.1.0
.\version-bump.ps1 minor "Added burst fire mode"

# 1.1.0 ? 2.0.0
.\version-bump.ps1 major "Breaking API changes"
```

---

### 4. **view-logs.ps1**
View BepInEx logs for debugging

```powershell
.\view-logs.ps1
```

**What it does:**
- Shows last 50 lines of BepInEx log in console
- Opens full log in default text editor
- Useful for debugging in-game issues

---

### 5. **check-env.ps1**
Verify development environment setup

```powershell
.\check-env.ps1
```

**What it does:**
- Checks all installed tools (Git, Node, .NET, etc.)
- Verifies important paths (Valheim, plugin folder)
- Shows Git repository status
- Lists next steps

---

## ?? Quick Start Workflow

### First Time Setup
```powershell
# 1. Check everything is installed
.\check-env.ps1

# 2. Build and deploy
.\build-and-deploy.ps1

# 3. Launch Valheim through r2modman
# (Manually open r2modman and click "Start Modded")

# 4. Check logs after testing
.\view-logs.ps1
```

### Daily Development Workflow
```powershell
# 1. Make changes to code files

# 2. Build and test
.\build-and-deploy.ps1

# 3. Test in game

# 4. Commit changes
.\quick-commit.ps1 "Fixed crossbow zoom bug"

# 5. When ready for release
.\version-bump.ps1 patch "Bug fixes and improvements"
```

---

## ?? Useful NPM Packages (Optional)

Once your terminal is restarted and NPM is available, you can install these useful packages:

### Documentation Tools
```powershell
npm install -g markdown-toc    # Generate table of contents for markdown
npm install -g markdownlint-cli # Lint markdown files
```

### Development Tools
```powershell
npm install -g prettier        # Code formatter
npm install -g http-server     # Simple local web server for docs
```

### Asset Management (Future)
```powershell
npm install -g sharp-cli       # Image optimization
npm install -g svgo            # SVG optimization
```

---

## ?? Additional Useful Tools (Optional)

### dnSpy (Decompiler)
For exploring Valheim's code:
```powershell
winget install --id dnSpy.dnSpy
```
**Usage:** Open `assembly_valheim.dll` to explore game code

### ILSpy
Alternative .NET decompiler:
```powershell
winget install --id icsharpcode.ILSpy
```

### Visual Studio Code
Lightweight code editor:
```powershell
winget install --id Microsoft.VisualStudioCode
```

### GitHub CLI
Manage GitHub from terminal:
```powershell
winget install --id GitHub.cli
```

**Usage:**
```powershell
gh auth login           # Authenticate
gh repo create          # Create repository
gh repo view            # View repository
```

### Notepad++
Advanced text editor:
```powershell
winget install --id Notepad++.Notepad++
```

---

## ?? Git Basics Reference

### Common Commands
```powershell
# Check status
git status

# View history
git log --oneline --graph --decorate --all

# Create branch
git branch feature-name
git checkout feature-name
# Or in one command:
git checkout -b feature-name

# Merge branch
git checkout main
git merge feature-name

# View tags
git tag

# Delete tag
git tag -d v1.0.0

# Show changes
git diff

# Undo unstaged changes
git checkout -- filename

# Undo last commit (keep changes)
git reset --soft HEAD~1
```

### Remote Repository (When Ready)
```powershell
# Add remote
git remote add origin https://github.com/yourusername/MegaCrossbows.git

# Push to remote
git push -u origin main
git push --tags

# Pull from remote
git pull origin main

# Clone repository
git clone https://github.com/yourusername/MegaCrossbows.git
```

---

## ?? Troubleshooting

### Git/Node/NPM Not Found After Installation
**Solution:** Restart your terminal or VS Studio
```powershell
# Or manually refresh PATH in current session:
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
```

### Build Errors
```powershell
# Clean and rebuild
dotnet clean
dotnet build

# Restore packages
dotnet restore
```

### Git Line Ending Warnings
**Solution:** Configure Git for Windows:
```powershell
git config --global core.autocrlf true
```

### Can't Run PowerShell Scripts
**Solution:** Enable script execution:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

## ?? Learning Resources

### Git
- [Git Documentation](https://git-scm.com/doc)
- [GitHub Git Cheat Sheet](https://education.github.com/git-cheat-sheet-education.pdf)
- [Atlassian Git Tutorials](https://www.atlassian.com/git/tutorials)

### Valheim Modding
- [Valheim Modding Wiki](https://github.com/Valheim-Modding/Wiki)
- [BepInEx Documentation](https://docs.bepinex.dev/)
- [Harmony Documentation](https://harmony.pardeike.net/)

### C# & .NET
- [Microsoft C# Documentation](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [Unity Scripting Reference](https://docs.unity3d.com/ScriptReference/)

---

## ?? Next Steps

1. ? **Restart your terminal/IDE** to ensure all PATH changes take effect
2. ? **Run check-env.ps1** to verify everything is working
3. ? **Build the mod** with build-and-deploy.ps1
4. ? **Test in Valheim** through r2modman
5. ? **Make improvements** and use quick-commit.ps1 to save changes
6. ? **Create GitHub repository** (optional) to backup your work
7. ? **Share your mod** on Thunderstore or Nexus Mods when ready!

---

## ?? You're All Set!

Your development environment is fully configured with:
- ? Version control (Git)
- ? Build automation (helper scripts)
- ? Modern shell (PowerShell 7)
- ? Node.js ecosystem (for future tools)
- ? Proper project structure
- ? Documentation and guides

**Happy Modding! ????**
