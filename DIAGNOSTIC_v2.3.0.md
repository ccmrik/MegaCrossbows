# ?? DIAGNOSTIC VERSION - v2.3.0

## ? **EXTENSIVE LOGGING ADDED**

**Deployed: 17:52:19**

This version logs EVERYTHING:

### What Gets Logged:

1. **Mod Status** - Is it enabled? Every 5 seconds
2. **Weapon Detection** - What weapon? Is it a crossbow?
3. **Input Detection** - Left/Right mouse button states
4. **Zoom Status** - When zoom starts/stops, FOV values, scroll events
5. **Fire Events** - Every shot with magazine count
6. **FireBolt Function** - Every step: projectile creation, velocity, setup
7. **Reload Events** - Start/progress/complete
8. **HUD Updates** - What's displayed

---

## ?? **TEST NOW:**

```
1. Close Valheim completely
2. Reopen via r2modman
3. spawn Ripper 1
4. Equip Ripper
5. Try everything:
   - Hold left mouse for 5 seconds
   - Hold right mouse for 3 seconds
   - Scroll wheel while holding right mouse
6. Leave game running for 10 seconds
7. Check logs
```

---

## ?? **CHECK LOGS:**

```powershell
Get-Content "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\LogOutput.log" -Tail 200 | Select-String -Pattern "\[MC\]"
```

---

## ?? **What to Look For:**

### If Mod is Working:
```
[MC] Update: Active with $item_crossbow_ripper
[MC] Inputs: LMB=True, RMB=False
[MC] === FIRING === Mag: 999/1000
[MC] FireBolt: START
[MC] FireBolt: Projectile prefab: projectile_bolt_carapace
[MC] FireBolt: SUCCESS - Projectile launched
```

### If Zoom is Working:
```
[MC] Inputs: LMB=False, RMB=True
[MC] === ZOOM START === Normal FOV: 65
[MC] Zooming: FOV=32.5 (zoom 2.0x)
[MC] Zoom scroll: 2.0 -> 3.5
[MC] === ZOOM END === Restored FOV: 65
```

### If Something is Broken:
```
[MC] Update: Mod disabled in config  <- Config issue
[MC] Update: No weapon equipped     <- Weapon not detected
[MC] Update: Not a crossbow         <- Wrong weapon
[MC] FireBolt: ERROR - attack is NULL  <- Weapon has no attack
[MC] ERROR: GameCamera.instance is NULL!  <- Camera problem
```

---

## ?? **After Testing:**

Send me these log sections:

1. **First 20 lines with [MC]** - Shows startup
2. **Lines when holding left mouse** - Shows firing
3. **Lines when holding right mouse** - Shows zoom
4. **Any ERROR lines** - Shows problems

---

## ?? **The logs will tell us:**

- ? Is the Update patch running?
- ? Is the crossbow detected?
- ? Are inputs being read?
- ? Is FireBolt being called?
- ? Are projectiles being created?
- ? Is zoom changing FOV?
- ? Where exactly is it failing?

---

**Test now and send me the log output!** ??

This will show us EXACTLY what's happening (or not happening).
