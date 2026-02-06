# Valheim API Reference - Verified Methods

## What We Know Works (v2.9.1)

### Player Class
**Verified working patches:**
- ? `Player.Update()` - Postfix patch works
- ? `Player.Awake()` - Postfix patch works (used to add MonoBehaviour component)
- ? `Player.GetCurrentWeapon()` - Returns ItemDrop.ItemData
- ? `Player.GetAmmoItem()` - Returns ItemDrop.ItemData for loaded ammo
- ? `Player.GetInventory()` - Returns Inventory
- ? `Player.UseStamina(float)` - Works (can be prefixed to block)
- ? `Player.Message(MessageHud.MessageType, string)` - Works
- ? `Player.GetPlayerID()` - Returns long
- ? `Player.m_localPlayer` - Static field for local player
- ? `Player.HaveStamina(float)` - Can be prefixed (returns bool)

**Verified NOT to exist or have AMBIGUOUS overloads:**
- ? `Player.StartAttack()` - Does NOT exist
- ? `Player.OnGUI()` - Does NOT exist
- ? `Player.GetAttackDrawPercentage()` - Does NOT exist (causes Harmony error)
- ? `Player.UpdateAttack()` - Does NOT exist
- ? `Player.AddNoise(float)` - Does NOT exist (confirmed v3.5.0 - "Undefined target method")
- ? `Character.ApplyPushback()` - Has MULTIPLE overloads (ambiguous, cannot patch without signature)
- ? `Character.AddStaggerDamage(float)` - Has MULTIPLE overloads (ambiguous, cannot patch without signature)

### Hud Class
**Verified working:**
- ? `Hud.Update()` - Postfix patch works
- ? `MessageHud.MessageType.Center` - Enum value exists
- ? `MessageHud.MessageType.TopLeft` - Exists but doesn't display properly

**Verified NOT to exist:**
- ? `Hud.OnGUI()` - Does NOT exist

### MonoBehaviour (Unity)
**Verified working:**
- ? `MonoBehaviour.OnGUI()` - Works when component attached to GameObject
- ? `AddComponent<T>()` - Works to attach custom components
- ? `GameObject.AddComponent<CrossbowHUD>()` - Works

### GameCamera
**Verified working:**
- ? `GameCamera.instance` - Static singleton
- ? `GameCamera.instance.m_fov` - Can read and write
- ? `GameCamera.instance.transform.position` - Accessible
- ? `GameCamera.instance.transform.forward` - Accessible

### ItemDrop.ItemData
**Verified working:**
- ? `item.m_shared` - SharedData property
- ? `item.m_shared.m_name` - String name
- ? `item.m_shared.m_attack` - Attack data
- ? `item.m_shared.m_attack.m_attackProjectile` - GameObject (can be null!)
- ? `item.m_shared.m_skillType` - Skills.SkillType enum
- ? `item.GetDamage()` - Returns HitData.DamageTypes
- ?? `item.GetMaxDurability()` - NEEDS VERIFICATION (wrapped in try-catch in mod)
- ?? `item.GetDurabilityPercentage()` - NEEDS VERIFICATION (wrapped in try-catch in mod)

### Projectile
**Verified working:**
- ? `Projectile.Setup(Character, Vector3, float, HitData, ItemDrop.ItemData, ItemDrop.ItemData)` - 6 parameters
- ? `GameObject.Instantiate(projectilePrefab, position, rotation)` - Works
- ? `projectile.GetComponent<Rigidbody>()` - Works
- ? `rigidbody.useGravity` - Can set to false
- ? `rigidbody.velocity` - Can set

**Needs verification (wrapped in try-catch):**
- ?? `projectile.m_aoe` - AOE radius field (may not exist)
- ?? `projectile.m_ttl` - Time to live field (may not exist)

### Attack Class
**Verified working:**
- ? `attack.m_attackProjectile` - GameObject (can be null for crossbows!)
- ? `attack.m_projectileVel` - float
- ? `attack.m_attackHitNoise` - float
- ? `attack.m_attackStamina` - float
- ? `attack.m_attackAnimation` - string
- ? `attack.m_hitEffect` - EffectList

**Needs verification (wrapped in try-catch):**
- ?? `attack.m_startEffect` - EffectList for firing effects (may not exist or differ from m_hitEffect)

### Humanoid Class (base class for Player)
**Needs verification (wrapped in try-catch):**
- ?? `Humanoid.StartAttack()` - May not exist, Player.StartAttack() confirmed NOT to exist
- ?? `Humanoid.BlockAttack()` - Used to prevent blocking stance
- ?? `Humanoid.IsBlocking()` - Returns bool, used to check block state
- ? `Humanoid.GetCurrentWeapon()` - Inherited, works same as Player

### Character (base class for Player)
**Verified working:**
- ? `character.m_zanim` - ZSyncAnimation (private field, needs reflection)
- ? Can use reflection to access: `typeof(Character).GetField("m_zanim", BindingFlags.NonPublic | BindingFlags.Instance)`

### ZSyncAnimation
**Verified working:**
- ? `zanim.SetTrigger(string)` - Triggers animation

### Input (Unity)
**Verified working:**
- ? `Input.GetMouseButton(0)` - Left mouse
- ? `Input.GetMouseButton(1)` - Right mouse
- ? `Input.GetAxis("Mouse ScrollWheel")` - Scroll value

### Physics (Unity)
**Verified working:**
- ? `Physics.Raycast(Ray, out RaycastHit, float)` - Returns bool

### HitData.DamageTypes
**Verified working:**
- ? `m_damage` - Base damage
- ? `m_blunt` - Blunt damage
- ? `m_slash` - Slash damage
- ? `m_pierce` - Pierce damage
- ? `m_fire` - Fire damage
- ? `m_frost` - Frost damage
- ? `m_lightning` - Lightning damage
- ? `m_poison` - Poison damage
- ? `m_spirit` - Spirit damage
- ? `m_chop` - Chop damage (for trees)
- ? `m_pickaxe` - Pickaxe damage (for rocks)

---

## Working Patterns

### Custom GUI Rendering
```csharp
// Create MonoBehaviour component
public class CustomHUD : MonoBehaviour
{
    void OnGUI() 
    {
        GUI.Label(new Rect(x, y, width, height), text, style);
    }
}

// Attach to player
[HarmonyPatch(typeof(Player), "Awake")]
[HarmonyPostfix]
public static void AttachComponent(Player __instance)
{
    __instance.gameObject.AddComponent<CustomHUD>();
}
```

### Blocking Normal Attacks
```csharp
// Block stamina check
[HarmonyPrefix]
[HarmonyPatch(typeof(Player), "HaveStamina")]
public static bool HaveStamina_Prefix(ref bool __result)
{
    __result = true;
    return false; // Skip original
}

// Block attack draw
[HarmonyPrefix]
[HarmonyPatch(typeof(Player), "GetAttackDrawPercentage")]
public static bool GetAttackDrawPercentage_Prefix(ref float __result)
{
    __result = 1f; // Always fully drawn
    return false;
}
```

### Getting Projectile from Ammo
```csharp
var ammoItem = player.GetAmmoItem();
if (ammoItem != null && ammoItem.m_shared.m_attack.m_attackProjectile != null)
{
    GameObject projectilePrefab = ammoItem.m_shared.m_attack.m_attackProjectile;
    // Use ammo projectile, not weapon!
}
```

---

## Common Pitfalls

### ? Don't Do This:
- Patch non-existent methods (StartAttack, OnGUI on Hud, etc.)
- Use TopLeft message type (doesn't display)
- Get projectile from weapon.m_shared.m_attack.m_attackProjectile (null for crossbows!)
- Assume all Unity methods are available (some modules not referenced)

### ? Do This Instead:
- Check method exists first or use MonoBehaviour components
- Use Center message type or custom GUI
- Get projectile from ammo using player.GetAmmoItem()
- Add required Unity module references to .csproj

---

## Required Assembly References

```xml
<Reference Include="UnityEngine.CoreModule" />
<Reference Include="UnityEngine.UI" />
<Reference Include="UnityEngine.PhysicsModule" />
<Reference Include="UnityEngine.IMGUIModule" />
<Reference Include="UnityEngine.TextRenderingModule" />
```

---

This document is based on actual testing and verified working code from MegaCrossbows v2.9.1.
