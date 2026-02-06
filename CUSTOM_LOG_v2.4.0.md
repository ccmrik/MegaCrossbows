# ?? CUSTOM LOG FILE - v2.4.0

## ? **NEW FEATURE: Dedicated Log File!**

**Deployed: Now**

The mod now writes to its own log file for easy debugging!

---

## ?? **Log File Location:**

```
C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log
```

---

## ?? **Test Instructions:**

```
1. Close Valheim completely
2. Reopen via r2modman
3. Game starts ? Log file is created
4. spawn Ripper 1
5. Equip it
6. Hold left mouse - 5 seconds
7. Hold right mouse - 3 seconds
8. Scroll wheel while zoomed
```

---

## ?? **View the Log:**

### **Easy Way:**
```powershell
.\view-mod-log.ps1
```

### **Manual Way:**
```powershell
Get-Content "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log"
```

### **Live Tail (watch in real-time):**
```powershell
Get-Content "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log" -Wait -Tail 20
```

---

## ?? **What's Logged:**

### **On Startup:**
```
=================================================
MegaCrossbows Mod - Log Started
Time: 01/25/2026 17:55:00
=================================================
=== Plugin Awake() called ===
Config Loaded:
  Enabled: True
  FireRate: 5
  MagazineCapacity: 1000
  Velocity: 100
  NoGravity: False
Log file location: C:\...\MegaCrossbows.log
=== MegaCrossbows v1.0.0 loaded! ===
Harmony patches applied successfully
```

### **During Gameplay:**
```
[17:55:10.123] Update: Active with $item_crossbow_ripper
[17:55:10.456] Inputs: LMB=True, RMB=False
[17:55:10.789] === FIRING === Mag: 999/1000
[17:55:10.790] FireBolt: START
[17:55:10.791] FireBolt: Projectile prefab: projectile_bolt_carapace
[17:55:10.795] FireBolt: SUCCESS - Projectile launched
[17:55:11.123] === ZOOM START === Normal FOV: 65
[17:55:11.456] Zooming: FOV=32.5 (zoom 2.0x)
```

---

## ?? **Benefits:**

? **Easy to find** - One specific file  
? **Timestamped** - Millisecond precision  
? **Cleared on startup** - Fresh log each time  
? **Thread-safe** - No corruption  
? **Also in Unity log** - Redundancy  
? **Readable format** - Plain text  

---

## ?? **After Testing:**

1. Run the game with crossbow
2. Try all features
3. Run:
```powershell
.\view-mod-log.ps1
```

4. **Copy the entire log output and send it to me!**

---

## ?? **Quick Commands:**

```powershell
# View entire log
.\view-mod-log.ps1

# Last 50 lines only
Get-Content "C:\...\MegaCrossbows.log" -Tail 50

# Watch live (shows new lines as they happen)
Get-Content "C:\...\MegaCrossbows.log" -Wait

# Search for errors
Get-Content "C:\...\MegaCrossbows.log" | Select-String "ERROR"

# Search for firing events
Get-Content "C:\...\MegaCrossbows.log" | Select-String "FIRING"

# Search for zoom events
Get-Content "C:\...\MegaCrossbows.log" | Select-String "ZOOM"
```

---

## ?? **Ready to Test!**

**The log file will show us EXACTLY what's happening, with timestamps!**

Test now and run `.\view-mod-log.ps1` to see the results! ??
