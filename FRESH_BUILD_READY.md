# ?? FRESH BUILD DEPLOYED - TEST NOW!

## ? **VERIFIED:**

**NEW DLL deployed at 5:39:53 PM**
- Location: `C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.dll`
- Size: 11,776 bytes
- Timestamp: 25/01/2026 5:39:53 PM

**Previous DLL was from 4:48 PM** - That's why you saw no change!

---

## ?? **WHAT'S IN THIS VERSION:**

### Direct Projectile Spawning
```csharp
FireBolt() {
    // Spawns projectile GameObject directly
    // No StartAttack(), no game cooldowns
    // Raycast for perfect aim
}
```

### Debug Logging
```csharp
Debug.Log("[MegaCrossbows] Active with Ripper");
Debug.Log("[MegaCrossbows] FIRING! Mag: 999");
```

### Features
- ? Rapid fire (spawns projectiles directly)
- ? Zoom (FOV manipulation)
- ? Magazine system (only reloads at 0)
- ? HUD (mag/ammo/range every 0.3s)
- ? No recoil (zero camera shake)

---

## ?? **TEST STEPS:**

### 1. COMPLETE RESTART (CRITICAL!)
```
1. Close Valheim if running
2. Close r2modman
3. Open r2modman
4. Click "Start Modded"
```

### 2. Check Console
```
Press F5 in game
Look for:
[Info   : MegaCrossbows] MegaCrossbows v1.0.0 loaded!
```

### 3. Spawn Weapon
```
Still in console (F5):
Type: spawn Ripper 1
Press Enter
```

### 4. Equip and Test
```
1. Equip Ripper
2. Wait 1-2 seconds
3. Press F5 - should see:
   [MegaCrossbows] Active with $item_crossbow_ripper
4. HOLD left mouse button
5. Press F5 - should see:
   [MegaCrossbows] FIRING! Mag: 999
   [MegaCrossbows] FIRING! Mag: 998
   [MegaCrossbows] FIRING! Mag: 997
```

### 5. Test Zoom
```
1. HOLD right mouse button
2. Screen should zoom in
3. Scroll wheel up/down
4. Release right mouse
```

---

## ?? **WHAT TO LOOK FOR:**

### Console Messages (F5):
```
? MegaCrossbows v1.0.0 loaded!
? [MegaCrossbows] Active with $item_crossbow_ripper
? [MegaCrossbows] FIRING! Mag: XXX
```

### In-Game (Top-Left Corner):
```
MAG: 1000/1000 | AMMO: 0 | RNG: 45m
```

### Visual:
- Bolts flying out rapidly when holding left mouse
- Screen zooms when holding right mouse
- No camera shake

### Audio:
- Rapid fire sounds
- Continuous whoosh of bolts

---

## ?? **IF YOU SEE:**

### "MegaCrossbows v1.0.0 loaded!" but nothing else:
- Mod is loaded but not detecting weapon
- Try: spawn ArbalestBronze 1
- Check weapon name contains "crossbow" or "ripper"

### "FIRING!" logs but no projectiles:
- FireBolt() is being called but projectiles not spawning
- Check attack.m_attackProjectile is not null
- Try different weapon

### No logs at all:
- Mod didn't load
- Check BepInEx console for errors
- Run: `.\view-logs.ps1`

### Fires once then stops:
- Magazine empty or reload triggered
- Check config Capacity value
- Look for "Reloading..." message

---

## ?? **CONFIG FILE:**

Location: `%AppData%\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\config\com.rikal.megacrossbows.cfg`

**For OBVIOUS testing:**
```ini
[General]
Enabled = true
DamageMultiplier = 100.0
FireRate = 15.0              # 15 shots per second!

[Zoom]
ZoomMin = 2.0
ZoomMax = 10.0

[Projectile]
Velocity = 300.0             # 3x speed
NoGravity = true             # Laser straight

[Magazine]
Capacity = 30                # Small for quick reload test
```

---

## ?? **CODE VERIFICATION:**

Let me show you the actual code that's deployed:

**FireBolt() spawns projectile:**
```csharp
GameObject proj = Object.Instantiate(attack.m_attackProjectile, spawnPos, Quaternion.LookRotation(aimDir));
Projectile projectile = proj.GetComponent<Projectile>();
projectile.Setup(player, velocity, attack.m_attackHitNoise, hitData, weapon, weapon);
```

**Zoom is simple:**
```csharp
if (Input.GetMouseButton(1)) {
    GameCamera.instance.m_fov = normalFOV / zoomLevel;
}
```

**Firing logic:**
```csharp
if (Input.GetMouseButton(0) && magazineAmmo > 0) {
    if (time since last shot > fire interval) {
        magazineAmmo--;
        Debug.Log("FIRING!");
        FireBolt();
    }
}
```

---

## ? **WHAT SHOULD HAPPEN:**

```
1. Load game ? See "loaded" in console
2. Equip Ripper ? See "Active with" every second
3. Hold left mouse ? See "FIRING!" logs rapidly
4. Look at screen ? See bolts flying
5. Top-left ? See magazine counting down
6. Hold right mouse ? Zoom activates
7. Empty magazine ? "Reloading..." ? 2 sec ? "Reloaded!"
```

---

## ?? **CURRENT STATUS:**

- ? **New DLL built:** 5:39:53 PM
- ? **Deployed to plugins folder:** Verified
- ? **Debug logging added:** Yes
- ? **File size changed:** 11,776 bytes (was 10,752)
- ? **Code verified:** FireBolt() present with debug logs

**Everything is ready. Close Valheim, close r2modman, reopen both, and test!**

---

## ?? **THE KEY DIFFERENCE:**

**Why you saw no change before:**
- Build was creating Debug build (5:37 PM)
- deploy.ps1 uses Release build (4:48 PM old)
- **So old code was still running!**

**Now:**
- Fresh Release build (5:39 PM)
- Deployed correctly
- New code is in place

---

## ?? **TEST IMMEDIATELY:**

```
1. Close Valheim NOW
2. Close r2modman NOW
3. Reopen r2modman
4. Start Valheim
5. spawn Ripper 1
6. Equip
7. HOLD left mouse
8. Press F5 ? See FIRING logs!
```

---

**The DLL has been updated. The code is simple and direct. Test it now!** ??
