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
                    if (GameCamera.instance != null)
                    {
                        originalFov = GameCamera.instance.m_fov;
                    }
                    currentZoom = MegaCrossbowsPlugin.ZoomMin.Value;
                }

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0f)
                {
                    currentZoom = Mathf.Clamp(
                        currentZoom - scroll * 2f,
                        MegaCrossbowsPlugin.ZoomMin.Value,
                        MegaCrossbowsPlugin.ZoomMax.Value
                    );
                }

                if (GameCamera.instance != null)
                {
                    GameCamera.instance.m_fov = originalFov / currentZoom;
                }
            }
            else if (isZooming)
            {
                isZooming = false;
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
                    player.Message(MessageHud.MessageType.Center, "Reloaded!");
                }
                else
                {
                    return;
                }
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
            
            // Trigger attack
            player.StartAttack(null, false);
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
