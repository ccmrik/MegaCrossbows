using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MegaCrossbows
{
    // =========================================================================
    // CROSSBOW DETECTION
    // =========================================================================
    public static class CrossbowHelper
    {
        public static bool IsCrossbow(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            if (item.m_shared.m_skillType == Skills.SkillType.Crossbows) return true;
            string name = item.m_shared.m_name.ToLower();
            if (name.Contains("crossbow") || name.Contains("arbalest") || name.Contains("ripper")) return true;
            string ammo = item.m_shared.m_ammoType?.ToLower() ?? "";
            if (ammo.Contains("bolt")) return true;
            return false;
        }

        public static bool IsBolt(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            return item.m_shared.m_name.ToLower().Contains("bolt");
        }
    }

    // =========================================================================
    // PER-PLAYER STATE
    // =========================================================================
    public class CrossbowState
    {
        public int magazineAmmo;
        public bool isReloading = false;
        public float reloadStartTime = 0f;
    }

    // =========================================================================
    // HUD (MonoBehaviour.OnGUI - VERIFIED WORKING)
    // =========================================================================
    public class CrossbowHUD : MonoBehaviour
    {
        public static bool showHUD = false;
        public static string ammoText = "";
        public static string distanceText = "";

        private GUIStyle ammoStyle;
        private GUIStyle distanceStyle;

        void OnGUI()
        {
            if (!showHUD) return;

            if (ammoStyle == null)
            {
                ammoStyle = new GUIStyle();
                ammoStyle.fontSize = 18;
                ammoStyle.fontStyle = FontStyle.Bold;
                ammoStyle.normal.textColor = Color.white;
                ammoStyle.alignment = TextAnchor.MiddleRight;

                distanceStyle = new GUIStyle();
                distanceStyle.fontSize = 16;
                distanceStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                distanceStyle.alignment = TextAnchor.MiddleCenter;
            }

            float w = Screen.width;
            float h = Screen.height;

            // Ammo counter - bottom right
            GUI.Label(new Rect(w - 250, h - 100, 240, 40), ammoText, ammoStyle);

            // Distance - below crosshair
            if (!string.IsNullOrEmpty(distanceText))
            {
                GUI.Label(new Rect(w / 2f - 100, h / 2f + 40, 200, 30), distanceText, distanceStyle);
            }
        }
    }

    // =========================================================================
    // HARMONY PATCHES - ONLY verified methods (see VALHEIM_API_VERIFIED.md)
    // =========================================================================

    // Block stamina drain for crossbows (VERIFIED: Player.UseStamina)
    [HarmonyPatch(typeof(Player), "UseStamina")]
    public static class PatchBlockStamina
    {
        public static bool Prefix(Player __instance, float v)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
            if (__instance != Player.m_localPlayer) return true;
            var weapon = __instance.GetCurrentWeapon();
            if (weapon != null && CrossbowHelper.IsCrossbow(weapon) && v > 0f)
                return false;
            return true;
        }
    }

    // Block vanilla attack - we handle firing ourselves (Humanoid.StartAttack - try-catch)
    [HarmonyPatch(typeof(Humanoid), "StartAttack")]
    public static class PatchBlockVanillaAttack
    {
        public static bool Prefix(Humanoid __instance)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
                if (__instance != Player.m_localPlayer) return true;
                var weapon = __instance.GetCurrentWeapon();
                if (weapon != null && CrossbowHelper.IsCrossbow(weapon))
                    return false;
            }
            catch { }
            return true;
        }
    }

    // Block blocking stance - right-click is zoom, not block (try-catch)
    [HarmonyPatch(typeof(Humanoid))]
    public static class PatchBlockBlocking
    {
        [HarmonyPrefix]
        [HarmonyPatch("BlockAttack")]
        public static bool BlockAttack_Prefix(Humanoid __instance, ref bool __result)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
                if (__instance != Player.m_localPlayer) return true;
                var weapon = __instance.GetCurrentWeapon();
                if (weapon != null && CrossbowHelper.IsCrossbow(weapon))
                {
                    __result = false;
                    return false;
                }
            }
            catch { }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IsBlocking")]
        public static bool IsBlocking_Prefix(Humanoid __instance, ref bool __result)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
                if (__instance != Player.m_localPlayer) return true;
                var weapon = __instance.GetCurrentWeapon();
                if (weapon != null && CrossbowHelper.IsCrossbow(weapon))
                {
                    __result = false;
                    return false;
                }
            }
            catch { }
            return true;
        }
    }

    // Make crossbows indestructible (NEEDS VERIFICATION - try-catch)
    [HarmonyPatch(typeof(ItemDrop.ItemData))]
    public static class PatchDurability
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetMaxDurability", new Type[] { })]
        public static bool MaxDur_Prefix(ItemDrop.ItemData __instance, ref float __result)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
                if (CrossbowHelper.IsCrossbow(__instance))
                {
                    __result = 9999999f;
                    return false;
                }
            }
            catch { }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetDurabilityPercentage")]
        public static bool DurPct_Prefix(ItemDrop.ItemData __instance, ref float __result)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
                if (CrossbowHelper.IsCrossbow(__instance))
                {
                    __result = 1f;
                    return false;
                }
            }
            catch { }
            return true;
        }
    }

    // =========================================================================
    // MAIN MOD LOGIC - Player.Update postfix (VERIFIED)
    // =========================================================================
    [HarmonyPatch(typeof(Player), "Update")]
    public static class PatchPlayerUpdate
    {
        // State
        private static Dictionary<long, CrossbowState> states = new Dictionary<long, CrossbowState>();
        private static CrossbowHUD hudComponent;

        // Zoom
        private static bool zooming = false;
        private static float zoomLevel = 2f;
        private static float savedFOV = 65f;

        // Fire timing
        private static float lastFireTime = 0f;

        // Cached reflection for animation (VERIFIED: m_zanim + SetTrigger)
        private static FieldInfo zanimField;
        private static bool zanimFieldCached = false;

        // Cached animator for speed control
        private static Animator cachedAnimator;
        private static bool effectsDiagLogged = false;

        // Cached audio for reliable sound per shot
        private static AudioSource cachedAudioSource;
        private static AudioClip cachedFireClip;
        private static bool fireClipSearched = false;

        // Damage diagnostic throttle
        private static float lastDamageDiagTime = 0f;

        // HUD throttle
        private static float lastHudUpdate = 0f;

        private static CrossbowState GetState(Player player)
        {
            long id = player.GetPlayerID();
            if (!states.ContainsKey(id))
            {
                var s = new CrossbowState();
                s.magazineAmmo = MegaCrossbowsPlugin.MagazineCapacity.Value;
                states[id] = s;
            }
            return states[id];
        }

        // ---- Shared aim helpers (used by BOTH FireBolt and HUD) ----

        private static bool GetAimRay(out Camera cam, out Ray aimRay)
        {
            cam = null;
            aimRay = default(Ray);
            if (GameCamera.instance == null) return false;
            cam = GameCamera.instance.GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;
            aimRay = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            return true;
        }

        /// <summary>
        /// Raycast along the crosshair direction, skipping the local player's own colliders
        /// and non-solid layers (water volumes, triggers, UI).
        /// Uses RaycastAll to find all hits, then filters out invalid targets.
        /// </summary>
        private static float lastRaycastLogTime = 0f;
        private static bool RaycastCrosshair(Ray aimRay, Player player, out RaycastHit hit, out Vector3 targetPoint)
        {
            // Exclude non-solid/invisible layers that should never block aiming
            int layerMask = ~(LayerMask.GetMask("UI", "character_trigger", "viewblock", "WaterVolume", "Water", "smoke"));
            bool shouldLog = Time.time - lastRaycastLogTime > 2f;

            // Get ALL hits along the ray
            RaycastHit[] hits = Physics.RaycastAll(aimRay.origin, aimRay.direction, 1000f, layerMask);

            if (shouldLog)
            {
                lastRaycastLogTime = Time.time;
                ModLogger.Log($"=== RAYCAST DEBUG === {hits.Length} hits from camera");
                for (int i = 0; i < Mathf.Min(hits.Length, 8); i++)
                {
                    var h = hits[i];
                    string root = h.collider.transform.root?.name ?? "null";
                    ModLogger.Log($"  Hit[{i}]: dist={h.distance:F1}m obj={h.collider.name} layer={LayerMask.LayerToName(h.collider.gameObject.layer)} root={root}");
                }
            }

            // Sort by distance (RaycastAll doesn't guarantee order)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Find first hit that is NOT the local player and NOT a trigger collider
            Transform playerRoot = player.transform.root;
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitRoot = hits[i].collider.transform.root;
                if (hitRoot == playerRoot) continue;
                if (hits[i].collider.isTrigger) continue;

                hit = hits[i];
                targetPoint = hit.point;

                if (shouldLog)
                {
                    ModLogger.Log($"  SELECTED: dist={hit.distance:F1}m obj={hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                }
                return true;
            }

            // Nothing hit past player
            hit = default(RaycastHit);
            targetPoint = aimRay.origin + aimRay.direction * 500f;
            if (shouldLog) ModLogger.Log("  SELECTED: nothing hit, using 500m fallback");
            return false;
        }

        // ---- Main Update ----

        public static void Postfix(Player __instance)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
            if (__instance != Player.m_localPlayer) return;

            // Ensure HUD component
            if (hudComponent == null)
            {
                hudComponent = __instance.gameObject.GetComponent<CrossbowHUD>();
                if (hudComponent == null)
                {
                    hudComponent = __instance.gameObject.AddComponent<CrossbowHUD>();
                    ModLogger.Log("CrossbowHUD attached");
                }
            }

            var weapon = __instance.GetCurrentWeapon();
            if (weapon == null || !CrossbowHelper.IsCrossbow(weapon))
            {
                CrossbowHUD.showHUD = false;
                if (zooming) ResetZoom();
                // Reset audio cache when leaving crossbow
                fireClipSearched = false;
                cachedFireClip = null;
                effectsDiagLogged = false;
                return;
            }

            var state = GetState(__instance);

            // === Block all input when UI is open (menu, inventory, chat, map, console, etc.) ===
            bool uiOpen = false;
            try
            {
                uiOpen = InventoryGui.IsVisible()
                    || Menu.IsVisible()
                    || TextInput.IsVisible()
                    || Minimap.IsOpen()
                    || Console.IsVisible()
                    || StoreGui.IsVisible();
            }
            catch { }

            if (uiOpen)
            {
                if (zooming) ResetZoom();
                UpdateHUD(__instance, state);
                return;
            }

            // === ZOOM (Right Mouse) ===
            HandleZoom();

            // === RELOAD ===
            if (state.isReloading)
            {
                if (Time.time - state.reloadStartTime >= 2f)
                {
                    state.isReloading = false;
                    state.magazineAmmo = MegaCrossbowsPlugin.MagazineCapacity.Value;
                    __instance.Message(MessageHud.MessageType.Center, "<color=green>RELOADED</color>");
                    ModLogger.Log($"Reloaded: {state.magazineAmmo} rounds");
                }
                UpdateHUD(__instance, state);
                return;
            }

            // === AUTO FIRE (Left Mouse Hold) ===
            if (Input.GetMouseButton(0))
            {
                if (state.magazineAmmo <= 0)
                {
                    state.isReloading = true;
                    state.reloadStartTime = Time.time;
                    __instance.Message(MessageHud.MessageType.Center, "<color=yellow>RELOADING</color>");
                    ModLogger.Log("Magazine empty - reloading");
                }
                else
                {
                    float interval = 1f / MegaCrossbowsPlugin.FireRate.Value;
                    if (Time.time - lastFireTime >= interval)
                    {
                        // Additive timing to prevent drift, cap to prevent burst after pause
                        lastFireTime = Mathf.Max(lastFireTime + interval, Time.time - interval);
                        state.magazineAmmo--;
                        FireBolt(__instance, weapon);
                    }
                }
            }
            else
            {
                // Reset animator speed when not firing
                try
                {
                    if (cachedAnimator == null)
                        cachedAnimator = __instance.GetComponentInChildren<Animator>();
                    if (cachedAnimator != null && cachedAnimator.speed > 1f)
                        cachedAnimator.speed = 1f;
                }
                catch { }
            }

            UpdateHUD(__instance, state);
        }

        // ---- Zoom ----

        private static void HandleZoom()
        {
            if (Input.GetMouseButton(1))
            {
                if (!zooming && GameCamera.instance != null)
                {
                    savedFOV = GameCamera.instance.m_fov;
                    zooming = true;
                    zoomLevel = MegaCrossbowsPlugin.ZoomMin.Value;
                    ModLogger.Log($"Zoom ON: FOV {savedFOV} -> {savedFOV / zoomLevel}");
                }

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    zoomLevel = Mathf.Clamp(
                        zoomLevel + scroll * 3f,
                        MegaCrossbowsPlugin.ZoomMin.Value,
                        MegaCrossbowsPlugin.ZoomMax.Value
                    );
                }

                if (GameCamera.instance != null)
                    GameCamera.instance.m_fov = savedFOV / zoomLevel;
            }
            else if (zooming)
            {
                ResetZoom();
            }
        }

        private static void ResetZoom()
        {
            zooming = false;
            if (GameCamera.instance != null)
                GameCamera.instance.m_fov = savedFOV;
            ModLogger.Log($"Zoom OFF: restored FOV {savedFOV}");
        }

        // ---- Fire ----

        private static void FireBolt(Player player, ItemDrop.ItemData weapon)
        {
            // 1. Find projectile prefab: ammo -> weapon primary -> weapon secondary
            GameObject prefab = null;
            var ammoItem = player.GetAmmoItem();

            if (ammoItem != null && ammoItem.m_shared?.m_attack?.m_attackProjectile != null)
                prefab = ammoItem.m_shared.m_attack.m_attackProjectile;
            else if (weapon.m_shared?.m_attack?.m_attackProjectile != null)
                prefab = weapon.m_shared.m_attack.m_attackProjectile;
            else if (weapon.m_shared?.m_secondaryAttack?.m_attackProjectile != null)
                prefab = weapon.m_shared.m_secondaryAttack.m_attackProjectile;

            if (prefab == null)
            {
                ModLogger.LogError("FireBolt: No projectile prefab found");
                return;
            }

            // 2. Aim: raycast from CAMERA (crosshair origin) to find target point,
            //    then aim bolt from player chest to that exact point.
            //    This ensures bolt hits exactly where the crosshair points.
            Camera cam;
            Ray aimRay;
            if (!GetAimRay(out cam, out aimRay)) return;

            Vector3 spawnPos = player.transform.position + Vector3.up * 1.5f;
            Vector3 targetPoint;
            RaycastHit hit;
            RaycastCrosshair(aimRay, player, out hit, out targetPoint);

            Vector3 aimDir = (targetPoint - spawnPos).normalized;

            // 3. Spawn projectile
            GameObject proj = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.LookRotation(aimDir));
            if (proj == null) return;

            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile == null)
            {
                UnityEngine.Object.Destroy(proj);
                ModLogger.LogError("FireBolt: No Projectile component on prefab");
                return;
            }

            // 4. Velocity
            var attack = weapon.m_shared.m_attack;
            float speed = attack.m_projectileVel * (MegaCrossbowsPlugin.Velocity.Value / 100f);
            Vector3 velocity = aimDir * speed;

            // 5. Damage
            // All damage types are multipliers of the bolt's base pierce damage:
            //   0 = none, 0.1 = 10% of pierce, 1 = equal to pierce, 10 = 10x pierce
            //   e.g. black metal bolt (62 pierce): mult 0.1 = 6.2, mult 1 = 62, mult 10 = 620
            // Stagger: direct multiplier on weapon attack force (0=none, 1=normal, 10=10x)
            HitData hitData = new HitData();
            HitData.DamageTypes baseDmg = weapon.GetDamage();
            float basePierce = baseDmg.m_pierce;

            // Physical damage
            hitData.m_damage.m_damage = baseDmg.m_damage * MegaCrossbowsPlugin.DamageMultiplier.Value;
            hitData.m_damage.m_pierce = basePierce * MegaCrossbowsPlugin.DamagePierce.Value;
            hitData.m_damage.m_blunt = basePierce * MegaCrossbowsPlugin.DamageBlunt.Value;
            hitData.m_damage.m_slash = basePierce * MegaCrossbowsPlugin.DamageSlash.Value;

            // Elemental damage (multiplier of base pierce)
            // DoT multiplier additionally scales elemental values fed into SE_Burning/SE_Poison
            // DoT=0: no modification, DoT=1: 1x (same), DoT=10: 10x damage AND 10x duration
            float dotMult = MegaCrossbowsPlugin.ElementalDoT.Value;
            float elemDoT = (dotMult > 0f) ? dotMult : 1f;
            hitData.m_damage.m_fire = basePierce * MegaCrossbowsPlugin.DamageFire.Value * elemDoT;
            hitData.m_damage.m_frost = basePierce * MegaCrossbowsPlugin.DamageFrost.Value * elemDoT;
            hitData.m_damage.m_lightning = basePierce * MegaCrossbowsPlugin.DamageLightning.Value * elemDoT;
            hitData.m_damage.m_poison = basePierce * MegaCrossbowsPlugin.DamagePoison.Value * elemDoT;
            hitData.m_damage.m_spirit = basePierce * MegaCrossbowsPlugin.DamageSpirit.Value * elemDoT;

            hitData.m_damage.m_chop = baseDmg.m_chop;
            hitData.m_damage.m_pickaxe = baseDmg.m_pickaxe;
            hitData.m_skill = weapon.m_shared.m_skillType;

            // Tag bolt for object destruction if modifier key is held
            // Sets chop/pickaxe to 999999 on HitData - these are reliably stored
            // in Projectile.m_damage and carried to hit time
            bool destroyMode = MegaCrossbowsPlugin.DestroyObjects.Value &&
                Input.GetKey(MegaCrossbowsPlugin.DestroyObjectsKey.Value);
            if (destroyMode)
            {
                hitData.m_damage.m_chop = 999999f;
                hitData.m_damage.m_pickaxe = 999999f;
            }

            // Stagger / knockback
            float staggerMult = MegaCrossbowsPlugin.Stagger.Value;
            try { hitData.m_pushForce = weapon.m_shared.m_attackForce * staggerMult; } catch { }
            try { hitData.m_staggerMultiplier = staggerMult; } catch { }

            // Diagnostic logging (throttled - once per 2 seconds)
            if (Time.time - lastDamageDiagTime > 2f)
            {
                lastDamageDiagTime = Time.time;
                ModLogger.Log($"=== BOLT DAMAGE DIAGNOSTIC ===");
                ModLogger.Log($"  Weapon base: dmg={baseDmg.m_damage:F0} blunt={baseDmg.m_blunt:F0} slash={baseDmg.m_slash:F0} pierce={baseDmg.m_pierce:F0}");
                ModLogger.Log($"  Weapon base elemental: fire={baseDmg.m_fire:F0} frost={baseDmg.m_frost:F0} light={baseDmg.m_lightning:F0} poison={baseDmg.m_poison:F0} spirit={baseDmg.m_spirit:F0}");
                ModLogger.Log($"  Base pierce for scaling: {basePierce:F0}");
                ModLogger.Log($"  Config: mult={MegaCrossbowsPlugin.DamageMultiplier.Value} pierce={MegaCrossbowsPlugin.DamagePierce.Value} blunt={MegaCrossbowsPlugin.DamageBlunt.Value}x slash={MegaCrossbowsPlugin.DamageSlash.Value}x stagger={staggerMult}");
                ModLogger.Log($"  Config elem: fire={MegaCrossbowsPlugin.DamageFire.Value}x frost={MegaCrossbowsPlugin.DamageFrost.Value}x light={MegaCrossbowsPlugin.DamageLightning.Value}x poison={MegaCrossbowsPlugin.DamagePoison.Value}x spirit={MegaCrossbowsPlugin.DamageSpirit.Value}x DoT={dotMult}x (effective={elemDoT}x)");
                ModLogger.Log($"  FINAL phys: dmg={hitData.m_damage.m_damage:F1} pierce={hitData.m_damage.m_pierce:F1} blunt={hitData.m_damage.m_blunt:F1} slash={hitData.m_damage.m_slash:F1}");
                ModLogger.Log($"  FINAL elem: fire={hitData.m_damage.m_fire:F1} frost={hitData.m_damage.m_frost:F1} light={hitData.m_damage.m_lightning:F1} poison={hitData.m_damage.m_poison:F1} spirit={hitData.m_damage.m_spirit:F1}");
                try { ModLogger.Log($"  FINAL stagger: pushForce={hitData.m_pushForce:F1} staggerMult={hitData.m_staggerMultiplier:F1}"); } catch { }
                ModLogger.Log($"  DESTROY MODE: {destroyMode} (config={MegaCrossbowsPlugin.DestroyObjects.Value}, key={MegaCrossbowsPlugin.DestroyObjectsKey.Value}, keyHeld={Input.GetKey(MegaCrossbowsPlugin.DestroyObjectsKey.Value)})");
                ModLogger.Log($"  Chop={hitData.m_damage.m_chop:F0} Pickaxe={hitData.m_damage.m_pickaxe:F0}");
            }

            // 6. Setup projectile (VERIFIED: 6-parameter overload)
            var itemForSetup = ammoItem ?? weapon;
            projectile.Setup(player, velocity, 0f, hitData, weapon, itemForSetup);

            // Set tool tier AFTER Setup via reflection (Setup may overwrite from item data)
            if (destroyMode)
            {
                try
                {
                    var tierField = typeof(Projectile).GetField("m_toolTier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (tierField != null) tierField.SetValue(projectile, (short)9999);
                }
                catch { }
            }

            // 7. AOE
            try { if (MegaCrossbowsPlugin.AoeRadius.Value > 0) projectile.m_aoe = MegaCrossbowsPlugin.AoeRadius.Value; } catch { }

            // 8. TTL - controls bolt travel distance
            try
            {
                float baseTTL = Mathf.Max(projectile.m_ttl, 60f);
                projectile.m_ttl = baseTTL * MegaCrossbowsPlugin.Distance.Value;
            }
            catch { }

            // Projectile stats diagnostic (throttled with damage diag)
            if (Time.time - lastDamageDiagTime < 0.1f)
            {
                ModLogger.Log($"=== PROJECTILE STATS ===");
                ModLogger.Log($"  Base m_projectileVel={attack.m_projectileVel:F0} VelocityConfig={MegaCrossbowsPlugin.Velocity.Value} FinalSpeed={speed:F0} m/s");
                ModLogger.Log($"  TTL={projectile.m_ttl:F1}s (Distance config={MegaCrossbowsPlugin.Distance.Value}x)");
                ModLogger.Log($"  Theoretical max range={speed * projectile.m_ttl:F0}m");
                ModLogger.Log($"  AOE radius={projectile.m_aoe:F1} NoGravity={MegaCrossbowsPlugin.NoGravity.Value}");
            }

            // 9. Physics - gravity and collision detection
            if (MegaCrossbowsPlugin.NoGravity.Value)
            {
                // Projectile component gravity (Valheim's custom gravity system)
                try { projectile.m_gravity = 0f; } catch { }
            }

            // Always set CCD and Rigidbody for high-speed bolts
            try
            {
                Rigidbody rb = proj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (MegaCrossbowsPlugin.NoGravity.Value)
                    {
                        rb.useGravity = false;
                        rb.drag = 0f;
                    }
                    // CCD prevents fast bolts from tunneling through thin colliders
                    // At 940 m/s the bolt moves ~15m per frame - without CCD it phases through objects
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
            }
            catch { }

            // 10. Animation - force restart at fire-rate speed so every shot shows
            try
            {
                if (!zanimFieldCached)
                {
                    zanimField = typeof(Character).GetField("m_zanim", BindingFlags.NonPublic | BindingFlags.Instance);
                    zanimFieldCached = true;
                }

                if (cachedAnimator == null)
                    cachedAnimator = player.GetComponentInChildren<Animator>();

                float fireRate = MegaCrossbowsPlugin.FireRate.Value;

                if (cachedAnimator != null)
                {
                    // Scale animator speed so full attack animation fits within one fire interval
                    // A typical crossbow attack anim is ~1s, so at rate 10 we need 10x speed
                    cachedAnimator.speed = Mathf.Max(1f, fireRate);

                    // Force restart the current animation state from the beginning.
                    // This is critical: SetTrigger alone won't replay if we're already
                    // in the attack state. Play(hash, 0, 0f) restarts it every shot.
                    AnimatorStateInfo stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
                    cachedAnimator.Play(stateInfo.fullPathHash, 0, 0f);
                }

                // Also fire the trigger via ZSyncAnimation for network sync
                if (zanimField != null)
                {
                    var zanim = zanimField.GetValue(player) as ZSyncAnimation;
                    if (zanim != null && !string.IsNullOrEmpty(attack.m_attackAnimation))
                        zanim.SetTrigger(attack.m_attackAnimation);
                }
            }
            catch (Exception ex) { ModLogger.LogError($"Animation error: {ex.Message}"); }

            // 11. Sound effect - play firing sound every shot matching fire rate
            try
            {
                // One-time: log all available effect fields for diagnostics
                if (!effectsDiagLogged)
                {
                    effectsDiagLogged = true;
                    LogWeaponEffects(weapon, attack, ammoItem);
                }

                bool soundPlayed = false;

                // --- Attempt 1: EffectList.Create (Valheim's native effect system) ---
                if (!soundPlayed) soundPlayed = TryPlayEffect(attack, "m_triggerEffect", spawnPos, aimDir, player.transform);
                if (!soundPlayed) soundPlayed = TryPlayEffect(attack, "m_startEffect", spawnPos, aimDir, player.transform);
                if (!soundPlayed) soundPlayed = TryPlayEffect(attack, "m_hitEffect", spawnPos, aimDir, player.transform);
                if (!soundPlayed && ammoItem?.m_shared?.m_attack != null)
                {
                    var ammoAtk = ammoItem.m_shared.m_attack;
                    if (!soundPlayed) soundPlayed = TryPlayEffect(ammoAtk, "m_triggerEffect", spawnPos, aimDir, player.transform);
                    if (!soundPlayed) soundPlayed = TryPlayEffect(ammoAtk, "m_startEffect", spawnPos, aimDir, player.transform);
                }

                // --- Attempt 2: Direct AudioSource.PlayOneShot (guaranteed reliable) ---
                if (!soundPlayed)
                {
                    // Find and cache the fire clip on first attempt
                    if (!fireClipSearched)
                    {
                        fireClipSearched = true;
                        cachedFireClip = FindFireClip(attack, weapon, ammoItem);
                        if (cachedFireClip != null)
                            ModLogger.Log($"Cached fire AudioClip: {cachedFireClip.name}");
                        else
                            ModLogger.LogWarning("No AudioClip found in any effect source");
                    }

                    if (cachedFireClip != null)
                    {
                        // Ensure we have a persistent AudioSource on the player
                        if (cachedAudioSource == null)
                        {
                            cachedAudioSource = player.gameObject.GetComponent<AudioSource>();
                            if (cachedAudioSource == null)
                            {
                                cachedAudioSource = player.gameObject.AddComponent<AudioSource>();
                                cachedAudioSource.spatialBlend = 1f;
                                cachedAudioSource.maxDistance = 50f;
                                cachedAudioSource.rolloffMode = AudioRolloffMode.Linear;
                                ModLogger.Log("Created AudioSource on player for firing sounds");
                            }
                        }
                        cachedAudioSource.PlayOneShot(cachedFireClip);
                        soundPlayed = true;
                    }
                }

                // --- Attempt 3: Instantiate effect prefabs directly ---
                if (!soundPlayed)
                {
                    soundPlayed = TryInstantiateEffectPrefabs(attack, spawnPos, aimDir);
                }

                if (!soundPlayed)
                    ModLogger.LogWarning("No firing sound could be played");
            }
            catch (Exception ex) { ModLogger.LogError($"Sound error: {ex.Message}"); }
        }

        // ---- Sound Helpers ----

        private static bool TryPlayEffect(object source, string fieldName, Vector3 pos, Vector3 dir, Transform parent)
        {
            try
            {
                var field = source.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field == null) return false;
                var el = field.GetValue(source) as EffectList;
                if (el?.m_effectPrefabs == null || el.m_effectPrefabs.Length == 0) return false;
                bool hasValid = false;
                foreach (var ep in el.m_effectPrefabs)
                    if (ep.m_prefab != null) { hasValid = true; break; }
                if (!hasValid) return false;
                var created = el.Create(pos, Quaternion.LookRotation(dir), parent, 1f);
                if (created != null && created.Length > 0)
                {
                    ModLogger.Log($"Sound played from {source.GetType().Name}.{fieldName} ({created.Length} objects)");
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// Searches all EffectList fields on Attack, SharedData, and ammo for AudioClips.
        /// Checks ZSFX (Valheim custom audio) and AudioSource components.
        /// </summary>
        private static AudioClip FindFireClip(Attack attack, ItemDrop.ItemData weapon, ItemDrop.ItemData ammoItem)
        {
            // Sources to search: attack, weapon shared data, ammo attack
            object[] sources = ammoItem?.m_shared?.m_attack != null
                ? new object[] { attack, weapon.m_shared, ammoItem.m_shared.m_attack }
                : new object[] { attack, weapon.m_shared };

            foreach (var source in sources)
            {
                try
                {
                    foreach (var f in source.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (f.FieldType != typeof(EffectList)) continue;
                        var el = f.GetValue(source) as EffectList;
                        if (el?.m_effectPrefabs == null) continue;
                        foreach (var ep in el.m_effectPrefabs)
                        {
                            if (ep.m_prefab == null) continue;

                            // Try ZSFX (Valheim's randomized audio component)
                            try
                            {
                                var zsfx = ep.m_prefab.GetComponent<ZSFX>();
                                if (zsfx != null)
                                {
                                    // ZSFX stores clips in m_audioClips
                                    var clipsField = typeof(ZSFX).GetField("m_audioClips", BindingFlags.Public | BindingFlags.Instance);
                                    if (clipsField != null)
                                    {
                                        var clips = clipsField.GetValue(zsfx) as AudioClip[];
                                        if (clips != null && clips.Length > 0 && clips[0] != null)
                                        {
                                            ModLogger.Log($"Found ZSFX clip in {source.GetType().Name}.{f.Name}: {clips[0].name}");
                                            return clips[0];
                                        }
                                    }
                                }
                            }
                            catch { }

                            // Try regular AudioSource
                            try
                            {
                                var audioSrc = ep.m_prefab.GetComponentInChildren<AudioSource>();
                                if (audioSrc != null && audioSrc.clip != null)
                                {
                                    ModLogger.Log($"Found AudioSource clip in {source.GetType().Name}.{f.Name}: {audioSrc.clip.name}");
                                    return audioSrc.clip;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Last resort: directly instantiate effect prefabs (they may auto-play sounds via ZSFX).
        /// </summary>
        private static bool TryInstantiateEffectPrefabs(Attack attack, Vector3 pos, Vector3 dir)
        {
            try
            {
                foreach (var f in typeof(Attack).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType != typeof(EffectList)) continue;
                    var el = f.GetValue(attack) as EffectList;
                    if (el?.m_effectPrefabs == null) continue;
                    foreach (var ep in el.m_effectPrefabs)
                    {
                        if (ep.m_prefab == null) continue;
                        var go = UnityEngine.Object.Instantiate(ep.m_prefab, pos, Quaternion.LookRotation(dir));
                        if (go != null)
                        {
                            UnityEngine.Object.Destroy(go, 3f);
                            ModLogger.Log($"Sound: instantiated prefab {ep.m_prefab.name}");
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static void LogWeaponEffects(ItemDrop.ItemData weapon, Attack attack, ItemDrop.ItemData ammoItem)
        {
            ModLogger.Log("=== WEAPON EFFECTS DIAGNOSTIC ===");
            ModLogger.Log($"Weapon: {weapon.m_shared.m_name}");
            try
            {
                foreach (var f in typeof(Attack).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType != typeof(EffectList)) continue;
                    var el = f.GetValue(attack) as EffectList;
                    int cnt = el?.m_effectPrefabs?.Length ?? 0;
                    if (cnt > 0)
                    {
                        string names = "";
                        foreach (var ep in el.m_effectPrefabs)
                            names += (ep.m_prefab?.name ?? "null") + " ";
                        ModLogger.Log($"  Attack.{f.Name}: {cnt} [{names.Trim()}]");
                    }
                    else
                    {
                        ModLogger.Log($"  Attack.{f.Name}: EMPTY");
                    }
                }
            }
            catch (Exception ex) { ModLogger.Log($"  Attack scan error: {ex.Message}"); }
            try
            {
                foreach (var f in typeof(ItemDrop.ItemData.SharedData).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType != typeof(EffectList)) continue;
                    var el = f.GetValue(weapon.m_shared) as EffectList;
                    int cnt = el?.m_effectPrefabs?.Length ?? 0;
                    if (cnt > 0)
                    {
                        string names = "";
                        foreach (var ep in el.m_effectPrefabs)
                            names += (ep.m_prefab?.name ?? "null") + " ";
                        ModLogger.Log($"  SharedData.{f.Name}: {cnt} [{names.Trim()}]");
                    }
                }
            }
            catch (Exception ex) { ModLogger.Log($"  SharedData scan error: {ex.Message}"); }
            if (ammoItem != null)
            {
                ModLogger.Log($"Ammo: {ammoItem.m_shared.m_name}");
                try
                {
                    var ammoAtk = ammoItem.m_shared.m_attack;
                    if (ammoAtk != null)
                    {
                        foreach (var f in typeof(Attack).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (f.FieldType != typeof(EffectList)) continue;
                            var el = f.GetValue(ammoAtk) as EffectList;
                            int cnt = el?.m_effectPrefabs?.Length ?? 0;
                            if (cnt > 0)
                            {
                                string names = "";
                                foreach (var ep in el.m_effectPrefabs)
                                    names += (ep.m_prefab?.name ?? "null") + " ";
                                ModLogger.Log($"  AmmoAttack.{f.Name}: {cnt} [{names.Trim()}]");
                            }
                        }
                    }
                }
                catch { }
            }
            ModLogger.Log("=== END EFFECTS DIAGNOSTIC ===");
        }

        // ---- HUD ----

        private static void UpdateHUD(Player player, CrossbowState state)
        {
            if (Time.time - lastHudUpdate < 0.1f)
            {
                CrossbowHUD.showHUD = true;
                return;
            }
            lastHudUpdate = Time.time;

            // Bolt count in inventory
            int totalBolts = 0;
            var inv = player.GetInventory();
            if (inv != null)
            {
                foreach (var item in inv.GetAllItems())
                {
                    if (CrossbowHelper.IsBolt(item))
                        totalBolts += item.m_stack;
                }
            }

            // Distance: raycast from CAMERA (crosshair), measure from player to hit point.
            // Uses the SAME shared raycast as FireBolt so the numbers always match.
            float range = -1f;
            Camera cam;
            Ray aimRay;
            if (GetAimRay(out cam, out aimRay))
            {
                RaycastHit hit;
                Vector3 targetPoint;
                if (RaycastCrosshair(aimRay, player, out hit, out targetPoint))
                {
                    Vector3 playerPos = player.transform.position + Vector3.up * 1.5f;
                    range = Vector3.Distance(playerPos, targetPoint);
                }
            }

            // Format HUD text
            string zoomStr = zooming ? $" | {zoomLevel:F1}x" : "";
            if (state.isReloading)
            {
                CrossbowHUD.ammoText = "RELOADING...";
                CrossbowHUD.distanceText = "";
            }
            else
            {
                CrossbowHUD.ammoText = $"{state.magazineAmmo}/{MegaCrossbowsPlugin.MagazineCapacity.Value} | {totalBolts}{zoomStr}";
                CrossbowHUD.distanceText = range > 0 ? $"{range:F0}m" : "";
            }
            CrossbowHUD.showHUD = true;
        }
    }

    // =========================================================================
    // BUILDING DAMAGE - WearNTear patch for crossbow bolts
    // =========================================================================
    [HarmonyPatch(typeof(WearNTear), "Damage")]
    public static class PatchBuildingDamage
    {
        private static bool buildingDiagLogged = false;
        private static bool isApplyingSpread = false;

        public static void Prefix(WearNTear __instance, HitData hit)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
                if (hit == null) return;
                if (hit.m_skill != Skills.SkillType.Crossbows) return;

                // --- Building damage multiplier ---
                float buildMult = MegaCrossbowsPlugin.BuildingDamage.Value;
                if (buildMult > 1f)
                {
                    hit.m_damage.m_damage *= buildMult;
                    hit.m_damage.m_blunt *= buildMult;
                    hit.m_damage.m_slash *= buildMult;
                    hit.m_damage.m_pierce *= buildMult;
                    hit.m_damage.m_chop *= buildMult;
                    hit.m_damage.m_pickaxe *= buildMult;
                    hit.m_damage.m_fire *= buildMult;
                    hit.m_damage.m_frost *= buildMult;
                    hit.m_damage.m_lightning *= buildMult;
                    hit.m_damage.m_poison *= buildMult;
                    hit.m_damage.m_spirit *= buildMult;
                }

                // --- Fire damage to buildings (Ashlands fire behavior) ---
                float fireMult = MegaCrossbowsPlugin.BuildingFireDamage.Value;
                if (fireMult > 0f)
                {
                    // Base fire damage of 10 at level 1, scaling to 100 at level 10
                    float fireDmg = 10f * fireMult;
                    hit.m_damage.m_fire = Mathf.Max(hit.m_damage.m_fire, fireDmg);
                }
            }
            catch (Exception ex) { ModLogger.LogError($"BuildingDamage Prefix: {ex.Message}"); }
        }

        public static void Postfix(WearNTear __instance, HitData hit)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
                if (hit == null) return;
                if (hit.m_skill != Skills.SkillType.Crossbows) return;
                if (isApplyingSpread) return;

                float fireMult = MegaCrossbowsPlugin.BuildingFireDamage.Value;
                if (fireMult <= 0f) return;

                // One-time: log WearNTear fire-related fields for diagnostics
                if (!buildingDiagLogged)
                {
                    buildingDiagLogged = true;
                    LogBuildingFireFields(__instance);
                }

                // Try to trigger/extend Ashlands fire behavior via reflection
                TryApplyAshlandsFire(__instance);

                // Fire spread to nearby building pieces
                if (fireMult >= 2f)
                {
                    float spreadRadius = fireMult; // 2-10m radius based on fire level
                    ApplyFireSpread(__instance, spreadRadius, fireMult);
                }
            }
            catch (Exception ex) { ModLogger.LogError($"BuildingDamage Postfix: {ex.Message}"); }
        }

        /// <summary>
        /// Try to trigger Ashlands fire behavior on a WearNTear piece via reflection.
        /// Ashlands added fire fields to WearNTear - we try to find and activate them.
        /// </summary>
        private static void TryApplyAshlandsFire(WearNTear wnt)
        {
            try
            {
                float durationMult = MegaCrossbowsPlugin.BuildingFireDuration.Value;

                // Try to find and call fire-related methods on WearNTear
                // Ashlands uses methods like "UpdateFire", "SetFire", or fields like "m_burning"
                var wntType = typeof(WearNTear);

                // Try to find a method to ignite the piece
                string[] igniteMethods = { "Ignite", "SetFire", "StartFire", "RPC_Ignite" };
                foreach (var methodName in igniteMethods)
                {
                    try
                    {
                        var method = wntType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null)
                        {
                            var parms = method.GetParameters();
                            if (parms.Length == 0)
                                method.Invoke(wnt, null);
                            else if (parms.Length == 1 && parms[0].ParameterType == typeof(bool))
                                method.Invoke(wnt, new object[] { true });
                            ModLogger.Log($"Called WearNTear.{methodName}()");
                            break;
                        }
                    }
                    catch { }
                }

                // Try to set fire duration fields
                string[] durationFields = { "m_burnTime", "m_fireDuration", "m_burnDuration", "m_ashDamageTimer" };
                foreach (var fieldName in durationFields)
                {
                    try
                    {
                        var field = wntType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null && field.FieldType == typeof(float))
                        {
                            float current = (float)field.GetValue(wnt);
                            field.SetValue(wnt, current * durationMult);
                            ModLogger.Log($"Set WearNTear.{fieldName}: {current} -> {current * durationMult}");
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// Spread fire damage to nearby building pieces within radius.
        /// </summary>
        private static void ApplyFireSpread(WearNTear source, float radius, float fireMult)
        {
            try
            {
                isApplyingSpread = true;

                int pieceMask = LayerMask.GetMask("piece", "piece_nonsolid");
                Collider[] nearby = Physics.OverlapSphere(source.transform.position, radius, pieceMask);

                float spreadFireDmg = 5f * fireMult; // Spread does less than direct hit
                int spreadCount = 0;

                foreach (var col in nearby)
                {
                    if (col == null) continue;
                    var wnt = col.GetComponentInParent<WearNTear>();
                    if (wnt == null || wnt == source) continue;

                    // Apply fire damage to nearby piece
                    HitData fireHit = new HitData();
                    fireHit.m_damage.m_fire = spreadFireDmg;
                    wnt.Damage(fireHit);

                    TryApplyAshlandsFire(wnt);
                    spreadCount++;

                    // Cap spread per shot to avoid performance issues
                    if (spreadCount >= 10) break;
                }

                if (spreadCount > 0)
                    ModLogger.Log($"Fire spread to {spreadCount} nearby pieces (radius {radius:F1}m)");
            }
            catch (Exception ex) { ModLogger.LogError($"FireSpread: {ex.Message}"); }
            finally
            {
                isApplyingSpread = false;
            }
        }

        /// <summary>
        /// One-time diagnostic: log all fire-related fields on WearNTear.
        /// </summary>
        private static void LogBuildingFireFields(WearNTear wnt)
        {
            ModLogger.Log("=== WEARNTEAR FIRE DIAGNOSTIC ===");
            try
            {
                var fields = typeof(WearNTear).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var f in fields)
                {
                    string name = f.Name.ToLower();
                    if (name.Contains("fire") || name.Contains("burn") || name.Contains("ash") || name.Contains("flame") || name.Contains("ignite"))
                    {
                        try
                        {
                            object val = f.GetValue(wnt);
                            ModLogger.Log($"  {f.Name} ({f.FieldType.Name}): {val}");
                        }
                        catch
                        {
                            ModLogger.Log($"  {f.Name} ({f.FieldType.Name}): <read error>");
                        }
                    }
                }

                var methods = typeof(WearNTear).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var m in methods)
                {
                    string name = m.Name.ToLower();
                    if (name.Contains("fire") || name.Contains("burn") || name.Contains("ash") || name.Contains("flame") || name.Contains("ignite"))
                    {
                        ModLogger.Log($"  Method: {m.Name}({string.Join(", ", System.Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name))})");
                    }
                }
            }
            catch (Exception ex) { ModLogger.Log($"  Scan error: {ex.Message}"); }
            ModLogger.Log("=== END WEARNTEAR DIAGNOSTIC ===");
        }
    }

    // =========================================================================
    // ELEMENTAL DoT - Patch Character.Damage to scale status effect duration
    // when a crossbow bolt hits. This runs AFTER the full damage pipeline
    // (including AddFireDamage/AddPoisonDamage) so TTL and damage are reliable.
    // DoT=0: no modification (default Valheim behavior)
    // DoT=1+: multiply burn/poison duration AND per-tick damage
    // The elemental damage on HitData is already scaled by DoT in FireBolt,
    // so SE_Burning's damage pool is pre-scaled. This patch handles TTL.
    // =========================================================================
    [HarmonyPatch(typeof(Character), "Damage")]
    public static class PatchCharacterDamageDoT
    {
        private static bool diagLogged = false;

        private static bool IsElementalEffect(StatusEffect se)
        {
            if (se == null) return false;
            string name = (se.m_name ?? "").ToLower();
            return name.Contains("burn") || name.Contains("fire") ||
                   name.Contains("poison") || name.Contains("frost") ||
                   name.Contains("lightning") || name.Contains("spirit");
        }

        public static void Postfix(Character __instance, HitData hit)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
                if (hit == null) return;
                if (hit.m_skill != Skills.SkillType.Crossbows) return;

                float dotMult = MegaCrossbowsPlugin.ElementalDoT.Value;
                if (dotMult <= 0f) return;

                // Check if there's any elemental damage on this hit
                bool hasElemental = hit.m_damage.m_fire > 0 || hit.m_damage.m_frost > 0 ||
                                    hit.m_damage.m_lightning > 0 || hit.m_damage.m_poison > 0 ||
                                    hit.m_damage.m_spirit > 0;
                if (!hasElemental) return;

                var seman = __instance.GetSEMan();
                if (seman == null) return;

                var effects = seman.GetStatusEffects();
                if (effects == null) return;

                foreach (var se in effects)
                {
                    if (!IsElementalEffect(se)) continue;

                    // One-time diagnostic: log status effect fields
                    if (!diagLogged)
                    {
                        diagLogged = true;
                        ModLogger.Log($"=== STATUS EFFECT DoT DIAGNOSTIC ===");
                        ModLogger.Log($"  Effect: {se.m_name} type={se.GetType().Name} TTL={se.m_ttl:F1}");
                        try
                        {
                            foreach (var f in se.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                string fname = f.Name.ToLower();
                                if (fname.Contains("ttl") || fname.Contains("time") || fname.Contains("duration") ||
                                    fname.Contains("damage") || fname.Contains("tick") || fname.Contains("interval"))
                                {
                                    try { ModLogger.Log($"  {f.Name} ({f.FieldType.Name}): {f.GetValue(se)}"); }
                                    catch { ModLogger.Log($"  {f.Name} ({f.FieldType.Name}): <read error>"); }
                                }
                            }
                        }
                        catch { }
                        ModLogger.Log($"=== END STATUS EFFECT DIAGNOSTIC ===");
                    }

                    // Scale TTL (duration)
                    float originalTTL = se.m_ttl;
                    se.m_ttl *= dotMult;

                    // Scale damage pool (SE_Burning.m_damage is HitData.DamageTypes, not float)
                    try
                    {
                        var dmgField = se.GetType().GetField("m_damage", BindingFlags.Public | BindingFlags.Instance);
                        if (dmgField != null && dmgField.FieldType == typeof(HitData.DamageTypes))
                        {
                            var dmg = (HitData.DamageTypes)dmgField.GetValue(se);
                            dmg.m_fire *= dotMult;
                            dmg.m_frost *= dotMult;
                            dmg.m_lightning *= dotMult;
                            dmg.m_poison *= dotMult;
                            dmg.m_spirit *= dotMult;
                            dmgField.SetValue(se, dmg);
                        }
                    }
                    catch { }

                    // Also try float damage fields (other SE types)
                    string[] floatDmgFields = { "m_damagePerHit", "m_damagePerTick", "m_fireDamage", "m_poisonDamage" };
                    foreach (var fieldName in floatDmgFields)
                    {
                        try
                        {
                            var field = se.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null && field.FieldType == typeof(float))
                            {
                                float val = (float)field.GetValue(se);
                                if (val > 0)
                                    field.SetValue(se, val * dotMult);
                            }
                        }
                        catch { }
                    }

                    ModLogger.Log($"DoT: {se.m_name} TTL {originalTTL:F1}s -> {se.m_ttl:F1}s ({dotMult}x)");
                }
            }
            catch (Exception ex) { ModLogger.LogError($"CharacterDamage DoT: {ex.Message}"); }
        }
    }

    // =========================================================================
    // BOLT STACK SIZE - Set all bolt items to stack up to 1000
    // =========================================================================
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class PatchBoltStackSize
    {
        public static void Postfix(ObjectDB __instance)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
                if (__instance.m_items == null) return;

                foreach (var prefab in __instance.m_items)
                {
                    if (prefab == null) continue;
                    var itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop == null) continue;
                    if (CrossbowHelper.IsBolt(itemDrop.m_itemData))
                    {
                        int oldStack = itemDrop.m_itemData.m_shared.m_maxStackSize;
                        itemDrop.m_itemData.m_shared.m_maxStackSize = 1000;
                        ModLogger.Log($"Bolt stack: {itemDrop.m_itemData.m_shared.m_name} {oldStack} -> 1000");
                    }
                }
            }
            catch (Exception ex) { ModLogger.LogError($"BoltStackSize: {ex.Message}"); }
        }
    }

    // =========================================================================
    // DESTROY OBJECTS - Crossbow bolts instantly destroy resource objects
    // Covers: trees, logs, rocks, copper, tin, silver, obsidian, flametal,
    //         and any other mineable/choppable world objects.
    // Requires DestroyObjects=true AND modifier key held when bolt was fired.
    // Bolts are tagged at fire time with chop/pickaxe=999999 on HitData.m_damage
    // (reliably preserved by Projectile) and projectile.m_toolTier=9999.
    // Also applies AOE destruction using the configured AOE radius.
    // =========================================================================
    public static class DestroyObjectsHelper
    {
        private static bool isApplyingAOE = false;
        private static float lastDestroyLogTime = 0f;

        /// <summary>
        /// Detects destroy-tagged bolts by their massive chop/pickaxe damage values
        /// (set in FireBolt, carried by Projectile.m_damage). Boosts all damage types
        /// to 999999 and sets toolTier to pass all tier checks.
        /// </summary>
        public static bool TryApplyDestroyDamage(HitData hit)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return false;
            if (!MegaCrossbowsPlugin.DestroyObjects.Value) return false;
            if (hit == null) return false;

            // Log every hit's chop/pickaxe values for diagnosis (throttled)
            bool shouldLog = Time.time - lastDestroyLogTime > 1f;
            if (shouldLog)
            {
                lastDestroyLogTime = Time.time;
                ModLogger.Log($"=== DESTROY HIT CHECK === chop={hit.m_damage.m_chop:F0} pickaxe={hit.m_damage.m_pickaxe:F0} point={hit.m_point} isAOE={isApplyingAOE}");
            }

            // Detect destroy-tagged bolts by their chop/pickaxe values
            // (these are reliably preserved in Projectile.m_damage)
            if (hit.m_damage.m_chop < 999000f && hit.m_damage.m_pickaxe < 999000f)
            {
                if (shouldLog)
                    ModLogger.Log($"  SKIPPED: chop/pickaxe below 999000 threshold");
                return false;
            }

            if (shouldLog)
                ModLogger.Log($"  APPLYING destroy damage (all types -> 999999, toolTier -> 9999)");

            hit.m_damage.m_damage = 999999f;
            hit.m_damage.m_blunt = 999999f;
            hit.m_damage.m_slash = 999999f;
            hit.m_damage.m_pierce = 999999f;
            hit.m_damage.m_chop = 999999f;
            hit.m_damage.m_pickaxe = 999999f;
            hit.m_damage.m_fire = 999999f;
            hit.m_damage.m_frost = 999999f;
            hit.m_damage.m_lightning = 999999f;
            hit.m_damage.m_poison = 999999f;
            hit.m_damage.m_spirit = 999999f;
            hit.m_toolTier = 9999;
            return true;
        }

        /// <summary>
        /// Checks if a HitData is tagged for object destruction.
        /// </summary>
        public static bool IsDestroyTagged(HitData hit)
        {
            if (hit == null) return false;
            return hit.m_damage.m_chop >= 999000f || hit.m_damage.m_pickaxe >= 999000f;
        }

        /// <summary>
        /// Force-destroys an object that survived our 999999 damage due to immunity/resistance.
        /// Bypasses all damage checks by directly destroying via ZNetView or setting health to 0.
        /// </summary>
        public static void ForceDestroyIfNeeded(Component target, HitData hit, string typeName)
        {
            if (!IsDestroyTagged(hit)) return;
            if (target == null || target.gameObject == null) return;

            try
            {
                // Try setting health fields to 0 via reflection (many types use m_health)
                var healthField = target.GetType().GetField("m_health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (healthField != null && healthField.FieldType == typeof(float))
                {
                    float hp = (float)healthField.GetValue(target);
                    if (hp > 0)
                    {
                        healthField.SetValue(target, 0f);
                        ModLogger.Log($"Force-destroy: Set {typeName}.m_health from {hp:F0} to 0");
                    }
                }
            }
            catch { }

            try
            {
                // Force-destroy via ZNetView (network-safe destruction)
                var nview = target.GetComponent<ZNetView>();
                if (nview == null) nview = target.GetComponentInParent<ZNetView>();
                if (nview != null && nview.IsValid())
                {
                    // Claim ownership so we have authority to destroy
                    if (!nview.IsOwner())
                    {
                        nview.ClaimOwnership();
                    }
                    nview.Destroy();
                    ModLogger.Log($"Force-destroy: ZNetView.Destroy() on {typeName} ({target.gameObject.name})");
                }
            }
            catch (Exception ex) { ModLogger.LogError($"Force-destroy {typeName}: {ex.Message}"); }
        }

        /// <summary>
        /// Creates a destroy-level HitData for AOE spread hits.
        /// </summary>
        private static HitData CreateDestroyHitData(Vector3 hitPoint)
        {
            HitData aoeHit = new HitData();
            aoeHit.m_point = hitPoint;
            aoeHit.m_damage.m_damage = 999999f;
            aoeHit.m_damage.m_blunt = 999999f;
            aoeHit.m_damage.m_slash = 999999f;
            aoeHit.m_damage.m_pierce = 999999f;
            aoeHit.m_damage.m_chop = 999999f;
            aoeHit.m_damage.m_pickaxe = 999999f;
            aoeHit.m_damage.m_fire = 999999f;
            aoeHit.m_damage.m_frost = 999999f;
            aoeHit.m_damage.m_lightning = 999999f;
            aoeHit.m_damage.m_poison = 999999f;
            aoeHit.m_damage.m_spirit = 999999f;
            aoeHit.m_toolTier = 9999;
            return aoeHit;
        }

        /// <summary>
        /// Destroys all resource objects within the AOE radius around the hit point.
        /// Uses the same radius as the configured AOE for elemental/combat damage.
        /// </summary>
        public static void TryAOEDestroy(HitData hit)
        {
            if (isApplyingAOE) return;
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
            if (!MegaCrossbowsPlugin.DestroyObjects.Value) return;
            if (hit == null) return;
            if (hit.m_damage.m_chop < 999000f && hit.m_damage.m_pickaxe < 999000f) return;

            float radius = MegaCrossbowsPlugin.AoeRadius.Value;
            if (radius <= 0f)
            {
                ModLogger.Log($"AOE Destroy: SKIPPED - radius={radius:F1} (disabled)");
                return;
            }

            Vector3 hitPoint = hit.m_point;
            if (hitPoint == Vector3.zero)
            {
                ModLogger.Log($"AOE Destroy: SKIPPED - hit.m_point is Vector3.zero");
                return;
            }

            ModLogger.Log($"AOE Destroy: scanning radius={radius:F1}m at point={hitPoint}");

            try
            {
                isApplyingAOE = true;
                int destroyCount = 0;

                Collider[] nearby = Physics.OverlapSphere(hitPoint, radius);
                foreach (var col in nearby)
                {
                    if (col == null) continue;
                    GameObject go = col.gameObject;
                    if (go == null) continue;

                    HitData aoeHit = CreateDestroyHitData(go.transform.position);

                    // Try each destructible type — damage first, then force-destroy if survived
                    try
                    {
                        var tree = go.GetComponentInParent<TreeBase>();
                        if (tree != null) { tree.Damage(aoeHit); ForceDestroyIfNeeded(tree, aoeHit, "TreeBase(AOE)"); destroyCount++; continue; }
                    }
                    catch { }
                    try
                    {
                        var log = go.GetComponentInParent<TreeLog>();
                        if (log != null) { log.Damage(aoeHit); ForceDestroyIfNeeded(log, aoeHit, "TreeLog(AOE)"); destroyCount++; continue; }
                    }
                    catch { }
                    try
                    {
                        var dest = go.GetComponentInParent<Destructible>();
                        if (dest != null) { dest.Damage(aoeHit); ForceDestroyIfNeeded(dest, aoeHit, "Destructible(AOE)"); destroyCount++; continue; }
                    }
                    catch { }
                    try
                    {
                        var rock = go.GetComponentInParent<MineRock>();
                        if (rock != null) { rock.Damage(aoeHit); ForceDestroyIfNeeded(rock, aoeHit, "MineRock(AOE)"); destroyCount++; continue; }
                    }
                    catch { }
                    try
                    {
                        var rock5 = go.GetComponentInParent<MineRock5>();
                        if (rock5 != null) { rock5.Damage(aoeHit); ForceDestroyIfNeeded(rock5, aoeHit, "MineRock5(AOE)"); destroyCount++; continue; }
                    }
                    catch { }
                }

                if (destroyCount > 0)
                    ModLogger.Log($"AOE Destroy: {destroyCount} objects within {radius:F1}m radius");
            }
            catch (Exception ex) { ModLogger.LogError($"AOE Destroy: {ex.Message}"); }
            finally
            {
                isApplyingAOE = false;
            }
        }
    }

    // Trees (standing)
    [HarmonyPatch(typeof(TreeBase), "Damage")]
    public static class PatchDestroyTree
    {
        public static void Prefix(HitData hit)
        {
            try { DestroyObjectsHelper.TryApplyDestroyDamage(hit); }
            catch { }
        }
        public static void Postfix(TreeBase __instance, HitData hit)
        {
            try { DestroyObjectsHelper.ForceDestroyIfNeeded(__instance, hit, "TreeBase"); }
            catch { }
            try { DestroyObjectsHelper.TryAOEDestroy(hit); }
            catch { }
        }
    }

    // Logs (fallen trees)
    [HarmonyPatch(typeof(TreeLog), "Damage")]
    public static class PatchDestroyLog
    {
        public static void Prefix(HitData hit)
        {
            try { DestroyObjectsHelper.TryApplyDestroyDamage(hit); }
            catch { }
        }
        public static void Postfix(TreeLog __instance, HitData hit)
        {
            try { DestroyObjectsHelper.ForceDestroyIfNeeded(__instance, hit, "TreeLog"); }
            catch { }
            try { DestroyObjectsHelper.TryAOEDestroy(hit); }
            catch { }
        }
    }

    // Generic destructibles (small rocks, stumps, etc.)
    [HarmonyPatch(typeof(Destructible), "Damage")]
    public static class PatchDestroyDestructible
    {
        public static void Prefix(HitData hit)
        {
            try { DestroyObjectsHelper.TryApplyDestroyDamage(hit); }
            catch { }
        }
        public static void Postfix(Destructible __instance, HitData hit)
        {
            try { DestroyObjectsHelper.ForceDestroyIfNeeded(__instance, hit, "Destructible"); }
            catch { }
            try { DestroyObjectsHelper.TryAOEDestroy(hit); }
            catch { }
        }
    }

    // Small mineral deposits (tin, obsidian, flametal, etc.)
    [HarmonyPatch(typeof(MineRock), "Damage")]
    public static class PatchDestroyMineRock
    {
        public static void Prefix(HitData hit)
        {
            try { DestroyObjectsHelper.TryApplyDestroyDamage(hit); }
            catch { }
        }
        public static void Postfix(MineRock __instance, HitData hit)
        {
            try { DestroyObjectsHelper.ForceDestroyIfNeeded(__instance, hit, "MineRock"); }
            catch { }
            try { DestroyObjectsHelper.TryAOEDestroy(hit); }
            catch { }
        }
    }

    // Large mineral deposits (copper, silver, etc. - multi-area)
    [HarmonyPatch(typeof(MineRock5), "Damage")]
    public static class PatchDestroyMineRock5
    {
        public static void Prefix(HitData hit)
        {
            try { DestroyObjectsHelper.TryApplyDestroyDamage(hit); }
            catch { }
        }
        public static void Postfix(MineRock5 __instance, HitData hit)
        {
            try { DestroyObjectsHelper.ForceDestroyIfNeeded(__instance, hit, "MineRock5"); }
            catch { }
            try { DestroyObjectsHelper.TryAOEDestroy(hit); }
            catch { }
        }
    }
}
