using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace MegaCrossbows
{
    public class CrossbowData
    {
        public int currentMagazine;
        public float lastFireTime;
        public bool isReloading;
        public float reloadStartTime;
        public const float reloadDuration = 2f;

        public CrossbowData()
        {
            currentMagazine = MegaCrossbowsPlugin.MagazineCapacity.Value;
            lastFireTime = 0f;
            isReloading = false;
            reloadStartTime = 0f;
        }

        public float GetFireInterval()
        {
            return 1f / MegaCrossbowsPlugin.FireRate.Value;
        }
    }

    [HarmonyPatch(typeof(Player))]
    public class PlayerPatches
    {
        private static Dictionary<string, CrossbowData> playerCrossbowData = new Dictionary<string, CrossbowData>();
        private static bool isZooming = false;
        private static float currentZoom = 1f;
        private static float originalFov = 65f;
        private static bool vanillaZoomDisabled = false;

        private static CrossbowData GetPlayerData(Player player)
        {
            string playerId = player.GetPlayerID().ToString();
            if (!playerCrossbowData.TryGetValue(playerId, out var data))
            {
                data = new CrossbowData();
                playerCrossbowData[playerId] = data;
            }
            return data;
        }

        private static bool IsCrossbow(ItemDrop.ItemData weapon)
        {
            if (weapon == null) return false;
            string weaponName = weapon.m_shared.m_name.ToLower();
            return weaponName.Contains("crossbow") || 
                   weaponName.Contains("arbalest") ||
                   weaponName.Contains("ripper") ||
                   weapon.m_shared.m_skillType == Skills.SkillType.Crossbows;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(Player __instance)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
            if (Player.m_localPlayer == null || __instance != Player.m_localPlayer) return;
            if (__instance.IsDead() || __instance.IsStaggering() || __instance.InPlaceMode()) return;

            var currentWeapon = __instance.GetCurrentWeapon();
            
            // Handle zoom
            HandleZoom(__instance, currentWeapon);
            
            // Handle automatic fire
            if (currentWeapon != null && IsCrossbow(currentWeapon))
            {
                HandleAutomaticFire(__instance, currentWeapon);
            }
        }

        private static void HandleZoom(Player player, ItemDrop.ItemData weapon)
        {
            bool hasCrossbow = weapon != null && IsCrossbow(weapon);
            
            if (hasCrossbow && Input.GetMouseButton(1))
            {
                if (!isZooming)
                {
                    isZooming = true;
                    vanillaZoomDisabled = true;
                    if (GameCamera.instance != null)
                    {
                        originalFov = GameCamera.instance.m_fov;
                    }
                    currentZoom = MegaCrossbowsPlugin.ZoomMin.Value;
                }

                // Handle zoom adjustment with scroll wheel
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    currentZoom = Mathf.Clamp(
                        currentZoom + scroll * 5f,
                        MegaCrossbowsPlugin.ZoomMin.Value,
                        MegaCrossbowsPlugin.ZoomMax.Value
                    );
                }

                // Apply zoom
                if (GameCamera.instance != null)
                {
                    GameCamera.instance.m_fov = originalFov / currentZoom;
                }
            }
            else if (isZooming)
            {
                isZooming = false;
                vanillaZoomDisabled = false;
                if (GameCamera.instance != null)
                {
                    GameCamera.instance.m_fov = originalFov;
                }
            }
        }

        private static void HandleAutomaticFire(Player player, ItemDrop.ItemData weapon)
        {
            if (!Input.GetMouseButton(0)) return;

            var data = GetPlayerData(player);
            
            // Check if reloading
            if (data.isReloading)
            {
                if (Time.time - data.reloadStartTime >= CrossbowData.reloadDuration)
                {
                    data.isReloading = false;
                    data.currentMagazine = MegaCrossbowsPlugin.MagazineCapacity.Value;
                    player.Message(MessageHud.MessageType.Center, $"Reloaded! ({data.currentMagazine} rounds)");
                }
                return;
            }

            // Check magazine
            if (data.currentMagazine <= 0)
            {
                if (!data.isReloading)
                {
                    data.isReloading = true;
                    data.reloadStartTime = Time.time;
                    player.Message(MessageHud.MessageType.Center, "Reloading...");
                }
                return;
            }

            // Check fire rate
            float fireInterval = data.GetFireInterval();
            if (Time.time - data.lastFireTime < fireInterval)
            {
                return;
            }

            // Fire!
            data.lastFireTime = Time.time;
            data.currentMagazine--;
            
            // Show magazine count periodically
            if (data.currentMagazine % 100 == 0 || data.currentMagazine <= 10)
            {
                player.Message(MessageHud.MessageType.TopLeft, $"Ammo: {data.currentMagazine}");
            }

            // Directly fire the projectile
            FireProjectile(player, weapon);
        }

        private static void FireProjectile(Player player, ItemDrop.ItemData weapon)
        {
            // Get the attack component
            var currentAttack = player.GetCurrentWeapon()?.m_shared?.m_attack;
            if (currentAttack == null) return;

            // Spawn the projectile directly
            var attackOrigin = player.transform.position + player.transform.forward * 0.5f + Vector3.up * 1.5f;
            var attackDir = GameCamera.instance.transform.forward;

            // Get projectile prefab
            if (currentAttack.m_attackProjectile != null)
            {
                var projectileObj = Object.Instantiate(currentAttack.m_attackProjectile, attackOrigin, Quaternion.LookRotation(attackDir));
                var projectile = projectileObj.GetComponent<Projectile>();
                
                if (projectile != null)
                {
                    // Calculate velocity
                    float velocityMult = MegaCrossbowsPlugin.Velocity.Value / 100f;
                    Vector3 velocity = attackDir * currentAttack.m_projectileVel * velocityMult;
                    
                    // Setup projectile
                    HitData hitData = new HitData();
                    hitData.m_damage = weapon.GetDamage();
                    hitData.m_pushForce = 10f;
                    hitData.m_backstabBonus = 4f;
                    hitData.m_skill = weapon.m_shared.m_skillType;
                    
                    // Apply damage multiplier
                    float damageMult = MegaCrossbowsPlugin.DamageMultiplier.Value / 100f;
                    hitData.ApplyModifier(damageMult);
                    
                    // Setup projectile with ammo (use weapon as ammo for crossbow)
                    projectile.Setup(player, velocity, currentAttack.m_attackHitNoise, hitData, weapon, weapon);
                    
                    // Disable gravity if configured
                    if (MegaCrossbowsPlugin.NoGravity.Value)
                    {
                        var rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.useGravity = false;
                        }
                    }
                }
            }

            // Play attack animation using reflection to access protected field
            try
            {
                var zanimField = typeof(Character).GetField("m_zanim", BindingFlags.NonPublic | BindingFlags.Instance);
                if (zanimField != null)
                {
                    var zanim = zanimField.GetValue(player) as ZSyncAnimation;
                    if (zanim != null && weapon.m_shared.m_attack.m_attackAnimation != "")
                    {
                        zanim.SetTrigger(weapon.m_shared.m_attack.m_attackAnimation);
                    }
                }
            }
            catch { }
            
            // Reduce stamina slightly
            float staminaDrain = weapon.m_shared.m_attack.m_attackStamina * 0.1f;
            player.UseStamina(staminaDrain);
            
            // Add some camera shake for feedback
            if (GameCamera.instance != null)
            {
                GameCamera.instance.AddShake(player.transform.position, 2f, 0.1f, false);
            }
        }

        // Disable vanilla zoom when using crossbow zoom
        [HarmonyPatch("UpdatePlacementGhost")]
        [HarmonyPrefix]
        public static void UpdatePlacementGhost_Prefix(Player __instance)
        {
            if (vanillaZoomDisabled && Input.GetMouseButton(1))
            {
                // Block vanilla zoom
                Input.ResetInputAxes();
            }
        }
    }

    [HarmonyPatch(typeof(Projectile))]
    public class ProjectilePatches
    {
        private static bool IsCrossbow(ItemDrop.ItemData weapon)
        {
            if (weapon == null) return false;
            string weaponName = weapon.m_shared.m_name.ToLower();
            return weaponName.Contains("crossbow") || 
                   weaponName.Contains("arbalest") ||
                   weaponName.Contains("ripper") ||
                   weapon.m_shared.m_skillType == Skills.SkillType.Crossbows;
        }

        [HarmonyPatch(nameof(Projectile.Setup))]
        [HarmonyPostfix]
        public static void Setup_Postfix(Projectile __instance, Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
            if (item == null || !IsCrossbow(item)) return;

            var rb = __instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float velocityMult = MegaCrossbowsPlugin.Velocity.Value / 100f;
                rb.velocity = velocity.normalized * (velocity.magnitude * velocityMult);

                if (MegaCrossbowsPlugin.NoGravity.Value)
                {
                    rb.useGravity = false;
                }
            }

            if (hitData != null)
            {
                float damageMult = MegaCrossbowsPlugin.DamageMultiplier.Value / 100f;
                hitData.ApplyModifier(damageMult);
            }
        }
    }
}
