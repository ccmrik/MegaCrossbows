# ?? MegaCrossbows - Complete Development Setup

## ? Installation Complete!

All development tools have been successfully installed and configured!

---

## ?? What's Been Installed

### Core Tools
| Tool | Version | Status | Purpose |
|------|---------|--------|---------|
| **Git** | 2.52.0 | ? Installed | Version control |
| **Node.js** | 25.4.0 | ? Installed | JavaScript runtime |
| **NPM** | Latest | ? Included | Package manager |
| **PowerShell** | 7.5.4 | ? Installed | Modern shell |
| **.NET SDK** | (existing) | ? Available | Build system |
| **WinGet** | 1.12.460 | ? Available | Package manager |

**Note:** Restart your terminal to ensure NPM is available in PATH.

---

## ??? Project Files

### Source Code
- ? `MegaCrossbows/Class1.cs` - Main plugin entry point
- ? `MegaCrossbows/CrossbowPatches.cs` - Harmony patches for gameplay
- ? `MegaCrossbows/MegaCrossbows.csproj` - Project configuration

### Documentation
- ? `README.md` - User-facing documentation
- ? `SETUP_COMPLETE.md` - Setup instructions
- ? `CHANGELOG.md` - Version history
- ? `TOOLS_INSTALLED.md` - Comprehensive tool guide
- ? `.github/copilot-instructions.md` - AI coding guidelines
- ? `STATUS.md` - This file!

### Helper Scripts
- ? `build-and-deploy.ps1` - Build & deploy automation
- ? `quick-commit.ps1` - Fast Git commits
- ? `version-bump.ps1` - Automated version management
- ? `view-logs.ps1` - View BepInEx logs
- ? `check-env.ps1` - Environment verification

### Configuration
- ? `.gitignore` - Git ignore rules
- ? `MegaCrossbows.slnx` - Solution file

---

## ?? Git Repository Status

```
Repository: Initialized ?
Branch: main
Commits: 2
Tags: v1.0.0

Commit History:
?? 33f2fd6 (HEAD -> main) Add development tools and helper scripts
?? 0897fe5 (tag: v1.0.0) v1.0.0 - Initial release
```

---

## ?? Mod Features (v1.0.0)

### Implemented ?
- ? **Automatic Fire** - Hold mouse button for rapid fire
- ? **Magazine System** - 1000 round capacity with auto-reload
- ? **Zoom Functionality** - Right-click zoom with scroll adjustment
- ? **Damage Multiplier** - Configurable damage (default 100%)
- ? **Fire Rate Control** - Adjustable shots per second (default 5)
- ? **Velocity Control** - Bolt speed multiplier
- ? **Gravity Toggle** - Optional projectile drop disable
- ? **Configuration File** - All settings user-configurable

### Build Status
- ? **Compiles Successfully** - No errors
- ? **Auto-Deploy** - PostBuild copies to r2modman
- ? **All Dependencies** - BepInEx, Harmony, Unity, Valheim assemblies

---

## ?? Quick Start Guide

### 1. Verify Environment
```powershell
.\check-env.ps1
```
This checks that all tools are installed and working.

### 2. Build the Mod
```powershell
.\build-and-deploy.ps1
```
Compiles the mod and deploys to r2modman plugins folder.

### 3. Test In-Game
1. Open **r2modman**
2. Select profile: **"Valheim Min Mods"**
3. Click **"Start Modded"**
4. Look for: `"MegaCrossbows v1.0.0 loaded!"` in console (F5)

### 4. Verify Functionality
- Equip any crossbow
- Hold left mouse = automatic fire
- Right-click = zoom in
- Mouse wheel = adjust zoom
- Fire 1000 rounds to test reload

### 5. Check Logs (if issues)
```powershell
.\view-logs.ps1
```

---

## ?? Development Workflow

### Making Changes

```powershell
# 1. Edit code files in Visual Studio

# 2. Build and test
.\build-and-deploy.ps1

# 3. Test in Valheim

# 4. Commit changes
.\quick-commit.ps1 "Fixed zoom bug"

# 5. (Optional) View git history
git log --oneline --graph --decorate
```

### Creating a Release

```powershell
# Update CHANGELOG.md with changes

# Bump version and tag
.\version-bump.ps1 patch "Bug fixes and improvements"
# or
.\version-bump.ps1 minor "New feature: burst fire mode"
# or
.\version-bump.ps1 major "Breaking changes"

# Build release version
.\build-and-deploy.ps1

# Test thoroughly

# Push to GitHub (when set up)
git push origin main --tags
```

---

## ?? Configuration File Location

After first launch, config file is created at:
```
C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\config\com.rikal.megacrossbows.cfg
```

### Default Settings
```ini
[General]
Enabled = true
DamageMultiplier = 100.0
FireRate = 5.0

[Zoom]
ZoomMin = 2.0
ZoomMax = 10.0

[Projectile]
Velocity = 100.0
NoGravity = false

[Magazine]
Capacity = 1000
```

---

## ?? Next Steps (Optional)

### Create GitHub Repository
```powershell
# Install GitHub CLI
winget install --id GitHub.cli

# Authenticate
gh auth login

# Create repository
gh repo create MegaCrossbows --public --source=. --remote=origin

# Push code
git push -u origin main
git push --tags
```

### Publish to Thunderstore
1. Build release version
2. Create icon (256x256 PNG)
3. Package with manifest.json
4. Upload to [Thunderstore](https://valheim.thunderstore.io/)

### Share on Nexus Mods
1. Create mod page on [Nexus Mods](https://www.nexusmods.com/valheim)
2. Upload DLL in zip file
3. Add screenshots and description

---

## ?? Important Paths

### Valheim Game
```
C:\Program Files (x86)\Steam\steamapps\common\Valheim
```

### r2modman Profile
```
C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods
```

### Plugin Deployment
```
C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\
```

### BepInEx Logs
```
C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\LogOutput.log
```

---

## ?? Troubleshooting

### Issue: Git/NPM commands not found
**Solution:** Restart terminal or refresh PATH:
```powershell
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
```

### Issue: Build fails
**Solution:** Clean and restore:
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Issue: Mod not loading in game
**Solution:**
1. Check BepInEx console (F5 in game)
2. View logs: `.\view-logs.ps1`
3. Verify DLL exists in plugins folder
4. Ensure BepInEx is installed in r2modman

### Issue: Can't run PowerShell scripts
**Solution:** Enable script execution:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

## ?? Resources

### Documentation
- [TOOLS_INSTALLED.md](./TOOLS_INSTALLED.md) - Complete tool guide
- [SETUP_COMPLETE.md](./SETUP_COMPLETE.md) - Setup walkthrough
- [README.md](./README.md) - Mod documentation
- [.github/copilot-instructions.md](./.github/copilot-instructions.md) - Coding guidelines

### External Resources
- [Valheim Modding Wiki](https://github.com/Valheim-Modding/Wiki)
- [BepInEx Documentation](https://docs.bepinex.dev/)
- [Harmony Patching Guide](https://harmony.pardeike.net/)
- [Git Documentation](https://git-scm.com/doc)

---

## ?? Summary

? **Project Created** - MegaCrossbows Valheim mod  
? **Tools Installed** - Git, Node.js, PowerShell  
? **Repository Initialized** - Git with v1.0.0 tag  
? **Build System** - Automated build & deploy  
? **Helper Scripts** - 5 PowerShell automation scripts  
? **Documentation** - Complete guides and references  
? **Compiles Successfully** - No errors, ready to test  

---

## ?? You're Ready to Go!

Everything is set up and ready for development. Your next steps:

1. ? **Build the mod**: `.\build-and-deploy.ps1`
2. ? **Launch Valheim** through r2modman
3. ? **Test the features** in-game
4. ? **Make improvements** as needed
5. ? **Share your mod** with the community!

**Happy Modding! ??????**

---

*Last Updated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")*  
*Version: 1.0.0*  
*Status: ? Ready for Development*
