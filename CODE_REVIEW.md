# ?? CODE REVIEW - Why Nothing Works

## Current Status:
- ? No auto firing
- ? No HUD
- ? No zoom (presumably)
- ? No visible effects at all

---

## ?? What We've Built:

### v2.4.0 Code Structure:
```
MegaCrossbowsPlugin (Class1.cs)
?? Awake() - Loads config, initializes logger, applies Harmony patches
?? ModLogger - Custom log file writer
?? PlayerPatch (CrossbowPatches.cs)
    ?? Update() - Main loop, checks every frame
    ?? HandleZoom() - REMOVED, integrated into Update
    ?? FireBolt() - Spawns projectiles directly
    ?? UpdateHUD() - Shows magazine/ammo/range
```

### What SHOULD Happen:
1. Player equips Ripper
2. Update() detects crossbow every frame
3. Holding left mouse ? FireBolt() spawns projectiles
4. HUD updates every 0.2 seconds
5. Extensive logging to MegaCrossbows.log

---

## ?? Potential Issues:

### Issue #1: Mod Not Loading At All
**Symptoms:** No log file created  
**Causes:**
- BepInEx not working
- Harmony patches failing
- Config has `Enabled = false`
- Wrong .NET Framework version

**Check:**
```powershell
.\diagnose.ps1
```

---

### Issue #2: Crossbow Not Detected
**Symptoms:** Log shows "Not a crossbow"  
**Causes:**
- IsCrossbow() not matching weapon name
- Ripper doesn't contain "crossbow", "ripper", or "arbalest"
- Weapon.m_shared.m_name is different than expected

**Current Detection:**
```csharp
string name = item.m_shared.m_name.ToLower();
return name.Contains("crossbow") || name.Contains("ripper") || name.Contains("arbalest");
```

---

### Issue #3: Input Not Being Read
**Symptoms:** Log shows crossbow active but "LMB=False"  
**Causes:**
- Another mod intercepting input
- Game not in focus
- Input.GetMouseButton(0) not working in Valheim

---

### Issue #4: Projectile Spawning Fails
**Symptoms:** Log shows "FIRING" but no projectiles  
**Causes:**
- attack.m_attackProjectile is null
- Instantiate() failing
- Projectile.Setup() failing
- Projectiles spawning but invisible/despawning instantly

---

### Issue #5: Game's Normal Attack Interfering
**Symptoms:** Fires once, then reloads normally  
**Causes:**
- Our projectile spawns
- Game's attack system ALSO fires
- Game's reload animation blocks everything

**We tried to fix this** but StartAttack patch failed (method doesn't exist)

---

## ?? Diagnostic Steps:

### Step 1: Run Diagnostic
```powershell
.\diagnose.ps1
```

This will show:
- ? Is DLL deployed?
- ? Does config exist?
- ? Is log file created?
- ? What does BepInEx log say?
- ? Are there Harmony errors?

---

### Step 2: Check Custom Log
```powershell
.\view-mod-log.ps1
```

Look for:
- Plugin startup messages
- Config values
- "Update: Active with..." messages
- Input states
- Firing events
- Errors

---

### Step 3: Check Weapon Name
In game console:
```
debugmode
spawn Ripper 1
[Check what the actual internal name is]
```

---

### Step 4: Test with Debug Mode
Try enabling Valheim's debug mode to see if projectiles are spawning but invisible.

---

## ?? What the Log Should Show:

### On Startup:
```
=== Plugin Awake() called ===
Config Loaded:
  Enabled: True
  FireRate: 5
=== MegaCrossbows v1.0.0 loaded! ===
Harmony patches applied successfully
```

### When Equipping Ripper:
```
Created new state for player X, mag: 1000
IsCrossbow: $item_crossbow_ripper = True
Update: Active with $item_crossbow_ripper
```

### When Holding Left Mouse:
```
Inputs: LMB=True, RMB=False
Fire check: interval=0.200, timeSince=0.250, mag=1000
=== FIRING === Mag: 999/1000
FireBolt: START
FireBolt: Projectile prefab: projectile_bolt_carapace
FireBolt: GameObject created
FireBolt: SUCCESS - Projectile launched
UpdateHUD: MAG 999/1000 | AMMO 0 | 45m
```

### If Something is Wrong:
```
Update: Mod disabled in config             <- Config issue
Update: Not a crossbow: $some_other_name   <- Detection issue
FireBolt: ERROR - attack is NULL           <- Weapon data issue
ERROR: GameCamera.instance is NULL!        <- Camera issue
```

---

## ?? Most Likely Issues:

### Theory #1: Valheim's Input System
Valheim might use a custom input system that blocks `Input.GetMouseButton()` during certain states.

### Theory #2: Attack Component
The weapon's `m_shared.m_attack` might not be set up the way we expect.

### Theory #3: Harmony Not Patching
The `Player.Update` patch might not be applying correctly.

### Theory #4: Wrong Timing
We might be checking inputs before the game is ready.

---

## ?? Immediate Actions:

1. **Run diagnostic:**
   ```powershell
   .\diagnose.ps1
   ```

2. **If log file exists, review it:**
   ```powershell
   .\view-mod-log.ps1
   ```

3. **Share the output** - I need to see:
   - Does the mod load?
   - Does it detect the crossbow?
   - Does it see input?
   - Where does it fail?

4. **Try alternative approach** - If current method fails, we may need to:
   - Patch a different method
   - Use a different input system
   - Hook into Valheim's attack system differently

---

## ?? Code Review Findings:

### What's Good:
? Clean code structure  
? Extensive logging  
? Config system working  
? Multiple detection methods  
? Thread-safe logger  

### What Might Be Wrong:
? Assumption that Input.GetMouseButton works in Valheim  
? Assumption that Player.Update is the right place  
? Assumption about weapon data structure  
? No verification that patches actually apply  

---

## ?? Next Steps:

**PLEASE RUN:**
```powershell
.\diagnose.ps1
```

Then share the output so I can see:
1. Is the mod loading?
2. What do the logs say?
3. Where is it failing?

Without the log output, I'm working blind. The diagnostic will show us exactly what's happening (or not happening).
