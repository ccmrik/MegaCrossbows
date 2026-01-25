# ?? MegaCrossbows - Quick Reference

## ? Common Commands

### Build & Deploy
```powershell
.\build-and-deploy.ps1
```

### Commit Changes
```powershell
.\quick-commit.ps1 "Your message here"
```

### Bump Version
```powershell
.\version-bump.ps1 patch "Bug fixes"
.\version-bump.ps1 minor "New features"
.\version-bump.ps1 major "Breaking changes"
```

### View Logs
```powershell
.\view-logs.ps1
```

### Check Environment
```powershell
.\check-env.ps1
```

---

## ?? Key Files

| File | Purpose |
|------|---------|
| `MegaCrossbows/Class1.cs` | Main plugin code |
| `MegaCrossbows/CrossbowPatches.cs` | Game patches |
| `README.md` | User documentation |
| `CHANGELOG.md` | Version history |
| `STATUS.md` | Project status |

---

## ?? Testing Checklist

- [ ] Build succeeds: `.\build-and-deploy.ps1`
- [ ] Launch Valheim via r2modman
- [ ] See "MegaCrossbows v1.0.0 loaded!" in console (F5)
- [ ] Equip crossbow
- [ ] Hold left mouse ? automatic fire works
- [ ] Right-click ? zoom activates
- [ ] Scroll wheel ? zoom adjusts
- [ ] Fire 1000 rounds ? reload triggers
- [ ] Check config file created
- [ ] Test different settings

---

## ?? Important Paths

**Config:**
```
%AppData%\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\config\com.rikal.megacrossbows.cfg
```

**Logs:**
```
%AppData%\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\LogOutput.log
```

**Plugin:**
```
%AppData%\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.dll
```

---

## ?? Git Quick Reference

```powershell
# Status
git status
git log --oneline --graph --all

# Branch
git checkout -b feature-name
git checkout main
git merge feature-name

# Undo
git reset --soft HEAD~1  # Undo last commit, keep changes
git checkout -- file.cs  # Discard changes

# Remote (after setup)
git push origin main
git push --tags
git pull origin main
```

---

## ?? Quick Fixes

**Build fails:**
```powershell
dotnet clean
dotnet restore
dotnet build
```

**Tool not found:**
```powershell
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
```

**Mod not loading:**
1. Check BepInEx console (F5)
2. Run `.\view-logs.ps1`
3. Verify DLL in plugins folder
4. Restart Valheim

---

## ?? Documentation Files

- **STATUS.md** - Current project status
- **TOOLS_INSTALLED.md** - Complete tool guide  
- **SETUP_COMPLETE.md** - Setup walkthrough
- **README.md** - Mod features & usage
- **CHANGELOG.md** - Version history
- **.github/copilot-instructions.md** - Coding standards

---

## ?? Current Version: 1.0.0

**Features:**
- Automatic fire
- Magazine system (1000 capacity)
- Zoom with scroll adjust
- Configurable damage, fire rate, velocity
- Gravity toggle

**Status:** ? Ready to test

---

*Keep this file open for quick reference!*
