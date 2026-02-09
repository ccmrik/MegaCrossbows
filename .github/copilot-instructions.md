# MegaCrossbows Copilot Instructions

## MANDATORY: EVERY ITERATION

1. **Update these instructions** if any features, configs, patches, or behavior changed
2. **Push to git** after every iteration: `git add -A && git commit -m "description" && git push`

## MANDATORY: BEFORE MAKING CHANGES

1. **Check VALHEIM_API_VERIFIED.md** for verified methods
2. **Review relevant code files** before making changes
3. The mod has **NO logging** - do not look for log files or add any debug output

---

## ?? CRITICAL: ALWAYS Verify Game Assembly Methods

**BEFORE attempting ANY Harmony patch:**

1. **CHECK VALHEIM_API_VERIFIED.md FIRST** - Contains verified working/non-working methods
2. **If method not listed**, assume it DOESN'T EXIST until verified
3. **NEVER guess method names** - Guessing causes Harmony errors that crash mod loading
4. **Document failures** - Add non-existent methods to VALHEIM_API_VERIFIED.md

---

## Project Overview

BepInEx Harmony mod for Valheim that transforms crossbows into rapid-fire weapons with full config control. All settings auto-reload on file save (no restart needed). No custom logging — the mod is silent.

**Plugin GUID**: `com.rikal.megacrossbows`
**Target Framework**: .NET Framework 4.6.2 (Valheim requirement)
**Unity Version**: 2020.3 (Valheim's engine)

---

## Project Structure

| File | Purpose |
|---|---|
| `MegaCrossbows/Class1.cs` | Main plugin entry, all BepInEx config definitions, FileSystemWatcher for live config reload |
| `MegaCrossbows/CrossbowPatches.cs` | All Harmony patches, firing logic, HUD, zoom, sound, animation, building damage, object destruction, DoT, bolt stacks |
| `MegaCrossbows/MegaCrossbows.csproj` | Project file with all assembly references |
| `VALHEIM_API_VERIFIED.md` | Verified working/broken Valheim API methods — **check before any patch** |
| `build-and-deploy.ps1` | Build and deploy DLL to plugin folder |
| `quick-commit.ps1` | Git add, commit, push shortcut |
| `version-bump.ps1` | Semantic version bump (patch/minor/major) |

---

## Coding Standards

### General Rules
- Target: .NET Framework 4.6.2
- Always check `MegaCrossbowsPlugin.ModEnabled.Value` before applying any patch logic
- Always check `__instance == Player.m_localPlayer` for player-specific patches
- Use `try-catch` around ALL reflection and unverified API calls
- **No logging** — do not add `ModLogger`, `Logger.Log`, `Debug.Log`, or any logging calls
- Never cache config values — read `.Value` at point of use for live reload

### Harmony Patching Rules
- Wrap ALL patch bodies in try-catch to prevent crashing other mods
- **Check VALHEIM_API_VERIFIED.md** before attempting any new Harmony patch
- If a method is not listed, assume it doesn't exist until verified
- Never guess method names — guessing causes Harmony errors that crash mod loading
- Use `[HarmonyPrefix]` returning `false` to block original method
- Use `[HarmonyPostfix]` to add behavior after
- Document any new verified/broken methods in VALHEIM_API_VERIFIED.md

### Performance Rules
- Cache `GetComponentInChildren<T>()` results in static fields
- Cap AOE spread to prevent performance issues
- Use `Physics.OverlapSphere` with layer masks, not `FindObjectsOfType`

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
- `BaseMultiplier` scales `m_damage` (weapon base damage), not pierce-based
- `Stagger` scales weapon `m_attackForce` and `m_staggerMultiplier`
- **Chop/Pickaxe** passed through from weapon base stats (unless Destroy Objects active)

### 3. Elemental DoT System
Two-layer approach for reliable DoT:
1. **In FireBolt**: Elemental damage on HitData scaled by DoT multiplier
2. **`PatchCharacterDamageDoT` on `Character.Damage` Postfix**: Scales status effect TTL and damage pool. Handles `DamageTypes` struct fields (not just float).
- `DoT=0`: No modification (default Valheim behavior)
- `DoT=1+`: Multiply burn/poison duration AND per-tick damage

### 4. Object Destruction (`DestroyObjects` + modifier key)
- Tags bolt with `m_chop=999999, m_pickaxe=999999` at fire time
- Destroy patches detect tagged bolts by checking `m_chop >= 999000 || m_pickaxe >= 999000`
- AOE destruction via `Physics.OverlapSphere` with configured AOE radius
- Recursion guard: `isApplyingAOE` flag prevents infinite recursion
- Patched types: `TreeBase`, `TreeLog`, `Destructible`, `MineRock`, `MineRock5`
- **Buildings (`WearNTear`) are EXCLUDED** from destroy mode — they are never instant-destroyed
- Ashlands cliffs (`cliff_ashlands*` on `static_solid` layer) are static terrain — NOT destroyable

### 5. HouseFire (`HouseFireHelper`)
- When ALT-mode bolt hits a building piece (`WearNTear`), spawns Valheim's native `Fire` at impact point
- Also spawns fire on ALL ALT-mode hits: trees, logs, rocks, destructibles, mine deposits
- Also spawns fire on buildings within AOE radius of destroy-mode hits
- Forces `m_burnable = true` on nearby `WearNTear` pieces so stone, black marble, and grausten burn
- Prefab found at runtime: tries known names, then searches `Cinder.m_houseFirePrefab`, then any prefab with `Fire` component
- Cached after first lookup

### 6. Bolt Stack Size (`PatchBoltStackSize` on `ObjectDB.Awake`)
- Sets `m_maxStackSize = 1000` for all bolt items

### 7. Projectile Physics
- Bolt spawns at player chest height, aimed at crosshair raycast point
- Velocity multiplied by config (470 default = ~940 m/s)
- Optional no-gravity (both `Projectile.m_gravity` and `Rigidbody.useGravity`)
- **CCD** (`CollisionDetectionMode.ContinuousDynamic`) always enabled to prevent tunneling
- AOE radius configurable (shared between combat and object destruction)

### 8. Zoom System (`HandleZoom` / `ResetZoom`)
- **Right mouse hold** = zoom in, scroll wheel adjusts level
- Modifies `GameCamera.instance.m_fov`
- FOV restored on release or when UI opens

### 9. Magazine / Reload
- Magazine counts down per shot
- At zero ? 2-second reload with HUD message

### 10. HUD (`CrossbowHUD` MonoBehaviour)
- `OnGUI()` renders ammo count, zoom level, distance to target
- HUD throttled to 10 updates/sec for performance

### 11. Building Damage (`PatchBuildingDamage` on `WearNTear.Damage`)
- Building damage multiplier, fire damage injection, fire spread, Ashlands ignite

### 12. Crossbow Detection (`CrossbowHelper`)
- Checks `m_skillType == Skills.SkillType.Crossbows`
- Fallback: name contains "crossbow", "arbalest", or "ripper"
- Fallback: ammo type contains "bolt"

### 13. Durability
- Crossbows set to effectively indestructible

### 14. Live Config Reload
- `FileSystemWatcher` monitors the `.cfg` file
- All `ConfigEntry.Value` properties read at point of use (not cached)

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
| `PatchSuppressDamageText` | `DamageText.AddInworldText` | Prefix | Suppress damage number spam |
| `PatchPlayerUpdate` | `Player.Update` | Postfix | Main logic (fire, zoom, HUD) |
| `PatchBuildingDamage` | `WearNTear.Damage` | Prefix+Postfix | Building damage/fire, HouseFire on ALT-mode |
| `PatchCrossbowAOE` | `Character.Damage` | Postfix | AOE splash from impact point |
| `PatchCharacterDamageDoT` | `Character.Damage` | Postfix | Elemental DoT TTL+damage scaling |
| `PatchBoltStackSize` | `ObjectDB.Awake` | Postfix | Bolt stack size ? 1000 |
| `PatchDestroyTree` | `TreeBase.Damage` | Prefix+Postfix | Destroy trees + AOE |
| `PatchDestroyLog` | `TreeLog.Damage` | Prefix+Postfix | Destroy logs + AOE |
| `PatchDestroyDestructible` | `Destructible.Damage` | Prefix+Postfix | Destroy small objects + AOE |
| `PatchDestroyMineRock` | `MineRock.Damage` | Prefix+Postfix | Destroy tin/obsidian/flametal + AOE |
| `PatchDestroyMineRock5` | `MineRock5.Damage` | Prefix+Postfix | Destroy copper/silver + AOE |

---

## File Paths

| Path | Purpose |
|---|---|
| `C:\Program Files (x86)\Steam\steamapps\common\Valheim` | Valheim install (client) |
| `...\Valheim\valheim_Data\Managed\` | Game assemblies + Unity assemblies |
| `C:\Users\Rik\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins\MegaCrossbows\` | Deployed plugin (client, via r2modman) |
| `C:\Users\Rik\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Valheim Min Mods\BepInEx\plugins` | Mods folder (client) |
| `...\BepInEx\config\com.rikal.megacrossbows.cfg` | Config file |
| `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server` | Dedicated server install |
| `C:\Users\Rik\AppData\LocalLow\IronGate\Valheim` | Server save directory |
| `C:\Users\Rik\OneDrive\Valheim\logs` | Server logs (WinSW) |

---

## Build & Deploy

```powershell
.\build-and-deploy.ps1
```

- PostBuild event in `.csproj` auto-copies DLL to plugin folder
- All references are `Private=false` (no DLL bloat)
- If DLL deployment fails, log out of Valheim and retry

---

## Dedicated Server

Valheim dedicated server runs as a Windows service via **WinSW** (`Win-SW-x64`).

- **Service ID**: `ValheimServer`
- **Service Name**: Valheim Kvastur Server
- **World**: Kvastur
- **Port**: 2456
- **Save dir**: `C:\Users\Rik\AppData\LocalLow\IronGate\Valheim`
- **Logs**: `C:\Users\Rik\OneDrive\Valheim\logs`
- **Save interval**: 600s, **Backups**: 4 (short: 7200s, long: 43200s)

Manage via:
```powershell
# Start/stop/restart
sc start ValheimServer
sc stop ValheimServer
sc query ValheimServer
```

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

