# ?? Ripper Crossbow Test Guide

## ? Ripper Detection Added!

The mod now explicitly detects the **Ripper** crossbow!

---

## ?? How to Test with Ripper

### 1. Spawn the Ripper
Press **F5** in-game to open console, then:
```
spawn Ripper 1
```

Or if you need to craft it, make sure you have the required materials.

### 2. Equip the Ripper
- Put it in your hotbar
- Select it (number key)

### 3. Test Features

#### ? Automatic Fire
- **HOLD Left Mouse Button** ? Should rapid fire (5 shots/second default)
- Watch the ammo counter - should count down
- After 1000 shots ? "Reloading..." message ? 2 second wait ? "Reloaded!"

#### ?? Zoom
- **HOLD Right Mouse Button** ? Camera should zoom in
- **Scroll Mouse Wheel** (while holding right mouse) ? Zoom in/out
- **Release Right Mouse** ? Zoom back to normal

---

## ?? Config File Settings

After first launch, edit config at:
```
%AppData%\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\config\com.rikal.megacrossbows.cfg
```

**Recommended Settings for Testing:**
```ini
[General]
Enabled = true
DamageMultiplier = 100.0
FireRate = 10.0          # 10 shots/second - very obvious!

[Zoom]
ZoomMin = 2.0
ZoomMax = 10.0

[Projectile]
Velocity = 150.0         # 50% faster bolts
NoGravity = true         # No drop - laser straight!

[Magazine]
Capacity = 100           # Smaller mag for testing reload faster
```

---

## ?? Expected Behavior

### Normal Crossbow:
- Click ? Fire ? Reload animation ? Wait ? Fire again
- Slow, deliberate shots

### With MegaCrossbows:
- **HOLD mouse** ? RATATATATATAT! ??
- Super fast automatic fire
- No reload animation between shots
- Magazine runs out ? 2 second pause ? Back to firing

---

## ?? Supported Crossbows

The mod now detects:
- ? **Ripper** (Mistlands crossbow)
- ? **ArbalestBronze** (Bronze crossbow)
- ? **ArbalestIron** (Iron crossbow)
- ? Any weapon with "crossbow" in the name
- ? Any weapon using the Crossbows skill

---

## ?? Troubleshooting

### Not Firing Automatically?
1. **Restart Valheim** (MUST close completely and reopen)
2. Check console (F5): Should see `"MegaCrossbows v1.0.0 loaded!"`
3. Check config: `Enabled = true`
4. Try higher fire rate: `FireRate = 20` for super obvious effect

### Zoom Not Working?
1. Make sure you're holding right mouse button
2. Try scrolling while holding right mouse
3. Check FOV isn't locked by another mod

### Magazine Not Counting Down?
1. The magazine is 1000 by default - takes a while!
2. Set `Capacity = 10` in config for quick testing
3. Restart Valheim after config change

---

## ?? Quick Test Sequence

```
1. Open Valheim via r2modman
2. Press F5 ? Check for "MegaCrossbows v1.0.0 loaded!"
3. In console: spawn Ripper 1
4. Equip Ripper
5. HOLD left mouse ? Should go BRRRRRRR!
6. HOLD right mouse ? Should zoom
7. Success! ??
```

---

## ?? Fun Configs to Try

### Machine Gun Mode
```ini
FireRate = 30.0
Velocity = 200.0
NoGravity = true
Capacity = 5000
```

### Sniper Mode
```ini
FireRate = 1.0
DamageMultiplier = 500.0
Velocity = 300.0
NoGravity = true
ZoomMax = 20.0
```

### Shotgun Mode
```ini
FireRate = 2.0
DamageMultiplier = 200.0
Velocity = 80.0
Capacity = 8
```

---

## ? Current Version: v1.0.1

**Changes:**
- ? Added Ripper crossbow detection
- ? Fixed automatic fire not working
- ? Fixed zoom not working
- ? Improved detection for all crossbows

---

**Ready to shred with the Ripper! ??**

Let me know if it works or if you need any adjustments!
