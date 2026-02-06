# MegaCrossbows Copilot Instructions

## ?? MANDATORY: EVERY ITERATION

1. **Update these instructions** if any features, configs, patches, or behavior changed
2. **Push to git** after every iteration: `git add -A && git commit -m "description" && git push`
3. **Only read logs from the latest timestamp** — skip old data to save time

## ?? MANDATORY: CHECK LOGS FIRST ON EVERY PROMPT

**BEFORE making ANY code changes, ALWAYS:**

1. **Run** `Get-Content "C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log" -Tail 100`
2. **Only read from the latest timestamp** — ignore old sessions
3. **Reference the logs in your response** — quote specific log lines
4. **Check VALHEIM_API_VERIFIED.md** for verified methods
5. **Review relevant code files** before making changes

### Log Analysis Checklist:
- Are patches loading? Look for "Plugin loaded" messages
- Are methods being called? Look for function entry logs
- Are there errors? Look for "ERROR" or exception messages
- Check `=== BOLT DAMAGE DIAGNOSTIC ===` for damage values
- Check `=== PROJECTILE STATS ===` for velocity/TTL/range
- Check `=== DESTROY HIT CHECK ===` for object destruction flow
- Check `=== STATUS EFFECT DoT DIAGNOSTIC ===` for elemental DoT

---

## ?? CRITICAL: ALWAYS Verify Game Assembly Methods

**BEFORE attempting ANY Harmony patch:**

1. **CHECK VALHEIM_API_VERIFIED.md FIRST** - Contains verified working/non-working methods
2. **If method not listed**, assume it DOESN'T EXIST until verified
3. **NEVER guess method names** - Guessing causes Harmony errors that crash mod loading
4. **Document failures** - Add non-existent methods to VALHEIM_API_VERIFIED.md

---

## Project Overview

BepInEx Harmony mod for Valheim that transforms crossbows into rapid-fire weapons with full config control. All settings auto-reload on file save (no restart needed).

**Plugin GUID**: `com.rikal.megacrossbows`
**Target Framework**: .NET Framework 4.6.2 (Valheim requirement)
**Unity Version**: 2020.3 (Valheim's engine)

---

## Project Structure

| File | Purpose |
|---|---|
| `MegaCrossbows/Class1.cs` | Main plugin entry, all BepInEx config definitions, FileSystemWatcher for live config reload |
| `MegaCrossbows/CrossbowPatches.cs` | All Harmony patches, firing logic, HUD, zoom, sound, animation, building damage, object destruction, DoT, bolt stacks |
| `MegaCrossbows/ModLogger.cs` | Custom file logger to `MegaCrossbows.log` in plugin folder |
| `MegaCrossbows/MegaCrossbows.csproj` | Project file with all assembly references |
| `VALHEIM_API_VERIFIED.md` | Verified working/broken Valheim API methods — **check before any patch** |

---

## All Features & How They Work

### 1. Rapid Fire System (`PatchPlayerUpdate` ? `FireBolt`)
- **Left mouse hold** = auto-fire at configured rate
- Fire timing uses **additive timing** (`lastFireTime += interval`) to prevent drift
- Vanilla attack is **blocked** (`PatchBlockVanillaAttack` on `Humanoid.StartAttack`)
- Blocking/shield stance **blocked** while holding crossbow (`PatchBlockBlocking`)
- Stamina drain **blocked** for crossbows (`PatchBlockStamina`)

### 2. Damage System
All damage types are **multipliers of the bolt's base pierce damage**:
- `0` = none, `0.1` = 10% of base pierce, `1` = equal to base pierce, `10` = 10x base pierce
- Example: Black metal bolt (62 pierce) with Blunt=0.1 ? 6.2 blunt damage
- Example: Charred bolt (82 pierce) with Fire=10 ? 820 fire damage
- `BaseMultiplier` scales `m_damage` (weapon base damage), not pierce-based
- `Stagger` scales weapon `m_attackForce` and `m_staggerMultiplier`
- **Chop/Pickaxe** passed through from weapon base stats (unless Destroy Objects active)

### 3. Elemental DoT System
Two-layer approach for reliable DoT:
1. **In FireBolt**: Elemental damage on HitData is multiplied by DoT multiplier (if DoT > 0). This scales the damage fed into `SE_Burning`/`SE_Poison`.
2. **`PatchCharacterDamageDoT` on `Character.Damage` Postfix**: After the full damage pipeline runs, finds elemental status effects on the target and scales their TTL and damage pool by DoT multiplier. Handles `DamageTypes` struct fields (not just float).
- `DoT=0`: No modification (default Valheim behavior)
- `DoT=1`: 1x (same as default)
- `DoT=10`: 10x damage AND 10x duration

### 4. Object Destruction (`DestroyObjects` + modifier key)
- **Config**: `DestroyObjects` (bool, default false) + `DestroyObjectsKey` (KeyCode, default LeftAlt)
- **At fire time**: If enabled AND modifier key held ? tags bolt with `m_chop=999999, m_pickaxe=999999` on HitData (reliably stored in `Projectile.m_damage`). Also sets `projectile.m_toolTier=9999` via reflection.
- **At hit time**: Destroy patches detect tagged bolts by checking `m_chop >= 999000 || m_pickaxe >= 999000`. Boosts ALL damage types to 999999 and sets `hit.m_toolTier=9999`.
- **AOE Destruction**: After direct hit, `TryAOEDestroy()` uses `Physics.OverlapSphere` with configured AOE radius to find and destroy nearby resource objects.
- **Recursion guard**: `isApplyingAOE` flag prevents infinite recursion from AOE hits.
- **Patched types**: `TreeBase`, `TreeLog`, `Destructible`, `MineRock`, `MineRock5` (covers all Valheim destructible world objects)
- **Note**: Ashlands cliffs (`cliff_ashlands*` on `static_solid` layer) are static terrain — NOT destroyable.

### 5. Bolt Stack Size (`PatchBoltStackSize` on `ObjectDB.Awake`)
- Sets `m_maxStackSize = 1000` for all items detected as bolts by `CrossbowHelper.IsBolt()`

### 6. Projectile Physics
- Bolt spawns at player chest height, aimed at crosshair raycast point
- Velocity multiplied by config (470 default = ~940 m/s)
- Optional no-gravity (both `Projectile.m_gravity` and `Rigidbody.useGravity`)
- **CCD** (`CollisionDetectionMode.ContinuousDynamic`) always enabled to prevent tunneling at high speed
- TTL = `max(base, 60s) × Distance` multiplier for travel range
- AOE radius configurable (shared between combat and object destruction)

### 7. Zoom System (`HandleZoom` / `ResetZoom`)
- **Right mouse hold** = zoom in
- Scroll wheel adjusts zoom level (min ? max)
- Modifies `GameCamera.instance.m_fov`
- FOV restored on release or when UI opens

### 8. Magazine / Reload
- Magazine counts down per shot
- At zero ? 2-second reload with HUD message
- Magazine capacity configurable

### 9. HUD (`CrossbowHUD` MonoBehaviour)
- `OnGUI()` renders ammo count, zoom level, distance to target
- Distance uses same raycast as firing (crosshair-accurate)
- HUD throttled to 10 updates/sec for performance

### 10. Building Damage (`PatchBuildingDamage` on `WearNTear.Damage`)
- Identifies crossbow bolts via `hit.m_skill == Skills.SkillType.Crossbows`
- Building damage multiplier, fire damage injection, fire spread, Ashlands ignite

### 11. Crossbow Detection (`CrossbowHelper`)
- Checks `m_skillType == Skills.SkillType.Crossbows`
- Fallback: name contains "crossbow", "arbalest", or "ripper"
- Fallback: ammo type contains "bolt"

### 12. Durability
- Crossbows set to effectively indestructible

### 13. Live Config Reload
- `FileSystemWatcher` monitors the `.cfg` file
- All `ConfigEntry.Value` properties read at point of use (not cached)
- No game restart needed

---

## Complete Config Reference

All configs are in BepInEx config file: `com.rikal.megacrossbows.cfg`
Config auto-reloads on save (FileSystemWatcher).

### 1. General
| Key | Type | Default | Range | Description |
|---|---|---|---|---|
| `Enabled` | bool | `true` | — | Master on/off |
| `DestroyObjects` | bool | `false` | — | Bolts instantly destroy trees, rocks, deposits (must hold modifier key) |
| `DestroyObjectsKey` | KeyCode | `LeftAlt` | — | Modifier key to hold while firing for object destruction |
| `FireRate` | float | `10` | 1-10 | Shots per second |
| `MagazineCapacity` | int | `1000` | — | Rounds before reload |

### 2. Zoom
| Key | Type | Default | Range | Description |
|---|---|---|---|---|
| `ZoomMin` | float | `2` | — | Minimum zoom multiplier |
| `ZoomMax` | float | `10` | — | Maximum zoom multiplier |

### 3. Projectile
| Key | Type | Default | Range | Description |
|---|---|---|---|---|
| `Velocity` | float | `470` | — | Bolt velocity % (470 = ~940 m/s) |
| `NoGravity` | bool | `true` | — | Disable bolt gravity |
| `Distance` | float | `1` | 1-10 | Bolt travel distance multiplier (scales TTL) |

### 4. Damage - Base (multiplier of bolt's base pierce damage)
| Key | Type | Default | Range | Description |
|---|---|---|---|---|
| `BaseMultiplier` | float | `1` | 0-10 | Overall damage multiplier (scales weapon m_damage) |
| `Pierce` | float | `1` | 0-10 | Pierce damage multiplier |
| `Blunt` | float | `0` | 0-10 | Blunt damage (0=none, 1=equal to pierce, 10=10x) |
| `Slash` | float | `0` | 0-10 | Slash damage (0=none, 1=equal to pierce, 10=10x) |
| `Stagger` | float | `1` | 0-10 | Stagger/knockback multiplier |

### 5. Damage - Elemental (multiplier of bolt's base pierce damage)
| Key | Type | Default | Range | Description |
|---|---|---|---|---|
| `Fire` | float | `0` | 0-10 | Fire damage multiplier |
| `Frost` | float | `0` | 0-10 | Frost damage multiplier |
| `Lightning` | float | `0` | 0-10 | Lightning damage multiplier |
| `Poison` | float | `0` | 0-10 | Poison damage multiplier |
| `Spirit` | float | `0` | 0-10 | Spirit damage multiplier |
| `ElementalDoT` | float | `0` | 0-10 | DoT multiplier (0=default Valheim, 10=10x duration+damage) |

### 6. AOE
| Key | Type | Default | Range | Description |
|---|---|---|---|---|
| `Radius` | float | `1` | 0-10 | AOE radius (shared: combat + object destruction) |

### 7. Building Damage
| Key | Type | Default | Range | Description |
|---|---|---|---|---|
| `BuildingDamageMultiplier` | float | `1` | 1-10 | Building damage multiplier |
| `BuildingFireDamage` | float | `0` | 0-10 | Ashlands fire behavior |
| `BuildingFireDuration` | float | `1` | 1-10 | Burn duration multiplier |

---

## Harmony Patches (all in CrossbowPatches.cs)

| Patch Class | Target | Type | Purpose |
|---|---|---|---|
| `PatchBlockStamina` | `Player.UseStamina` | Prefix | Block stamina drain |
| `PatchBlockVanillaAttack` | `Humanoid.StartAttack` | Prefix | Block vanilla attack |
| `PatchBlockBlocking` | `Humanoid.BlockAttack` / `IsBlocking` | Prefix | Right-click = zoom |
| `PatchDurability` | `ItemDrop.ItemData.GetMaxDurability` / `GetDurabilityPercentage` | Prefix | Indestructible crossbows |
| `PatchPlayerUpdate` | `Player.Update` | Postfix | Main logic (fire, zoom, HUD) |
| `PatchBuildingDamage` | `WearNTear.Damage` | Prefix+Postfix | Building damage/fire |
| `PatchCharacterDamageDoT` | `Character.Damage` | Postfix | Elemental DoT TTL+damage scaling |
| `PatchBoltStackSize` | `ObjectDB.Awake` | Postfix | Bolt stack size ? 1000 |
| `PatchDestroyTree` | `TreeBase.Damage` | Prefix+Postfix | Destroy trees + AOE |
| `PatchDestroyLog` | `TreeLog.Damage` | Prefix+Postfix | Destroy logs + AOE |
| `PatchDestroyDestructible` | `Destructible.Damage` | Prefix+Postfix | Destroy small objects + AOE |
| `PatchDestroyMineRock` | `MineRock.Damage` | Prefix+Postfix | Destroy tin/obsidian/flametal + AOE |
| `PatchDestroyMineRock5` | `MineRock5.Damage` | Prefix+Postfix | Destroy copper/silver + AOE |

---

## Coding Standards

### General Rules
- Target: .NET Framework 4.6.2
- Always check `MegaCrossbowsPlugin.ModEnabled.Value` before applying any patch logic
- Always check `__instance == Player.m_localPlayer` for player-specific patches
- Use `try-catch` around ALL reflection and unverified API calls
- Use `ModLogger.Log()` for significant events, `ModLogger.LogError()` for errors
- Never cache config values — read `.Value` at point of use for live reload

### Harmony Patching Rules
- Wrap ALL patch bodies in try-catch to prevent crashing other mods
- Test every new patch against VALHEIM_API_VERIFIED.md
- Use `[HarmonyPrefix]` returning `false` to block original method
- Use `[HarmonyPostfix]` to add behavior after

### Performance Rules
- Cache `GetComponentInChildren<T>()` results in static fields
- Throttle logging (every 2 seconds for diagnostics)
- Cap AOE spread to prevent performance issues
- Use `Physics.OverlapSphere` with layer masks, not `FindObjectsOfType`

---

## File Paths

| Path | Purpose |
|---|---|
| `C:\Program Files (x86)\Steam\steamapps\common\Valheim` | Valheim install |
| `...\Valheim\valheim_Data\Managed\` | Game assemblies |
| `...\Valheim\unstripped_corlib\` | Unity assemblies |
| `C:\Users\rikal\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\` | Deployed plugin |
| `...\BepInEx\config\com.rikal.megacrossbows.cfg` | Config file |
| `...\BepInEx\plugins\MegaCrossbows\MegaCrossbows.log` | Custom mod log |

---

## Build & Deploy

### Build Process
1. PostBuild event in `.csproj` auto-copies DLL to plugin folder
2. All references are `Private=false` (no DLL bloat)

### After Every Iteration
1. ? Build successful (no compilation errors)
2. ? DLL deployed to plugin folder
3. ? Update this instructions file
4. ? Git commit and push: `git add -A && git commit -m "description" && git push`

---

## Common Pitfalls
- **Fire rate > 10 doesn't work** — Valheim's animation system can't keep up
- **Don't cache config values** — they must be read via `.Value` for live reload
- **Ashlands cliffs are NOT destroyable** — `cliff_ashlands*` on `static_solid` layer is terrain
- **Projectile tunneling at high speed** — CCD (`ContinuousDynamic`) prevents this
- **`m_toolTier` not preserved by Projectile** — use `m_damage` fields (chop/pickaxe) as reliable tags
- **DoT timing window broken** — patch `Character.Damage` Postfix, not `SEMan.AddStatusEffect`
- **SE_Burning.m_damage is DamageTypes struct**, not float — must handle via reflection correctly
- **Animator speed affects ALL animations** — must reset to 1.0 when not firing

---

## Git Workflow
- Commit after each feature completion
- Use semantic versioning in commit messages
- Tag releases with version number
- Include changelog in version commits
