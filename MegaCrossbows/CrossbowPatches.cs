using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace MegaCrossbows
{
    [HarmonyPatch(typeof(Attack))]
    public class AttackPatches
    {
        private static Dictionary<Attack, CrossbowData> crossbowDataCache = new Dictionary<Attack, CrossbowData>();

        public class CrossbowData
        {
            public int currentMagazine;
            public float lastFireTime;
            public float fireInterval;
            public bool isReloading;
            public float reloadStartTime;
            public const float reloadDuration = 2f;

            public CrossbowData()
            {
                currentMagazine = MegaCrossbowsPlugin.MagazineCapacity.Value;
                lastFireTime = 0f;
                fireInterval = 1f / MegaCrossbowsPlugin.FireRate.Value;
                isReloading = false;
                reloadStartTime = 0f;
            }
        }

        private static CrossbowData GetCrossbowData(Attack attack)
        {
            if (!crossbowDataCache.TryGetValue(attack, out var data))
            {
                data = new CrossbowData();
                crossbowDataCache[attack] = data;
            }
            return data;
        }

        [HarmonyPatch(nameof(Attack.Start))]
        [HarmonyPrefix]
        public static bool Start_Prefix(Attack __instance, Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
            if (weapon == null || !IsCrossbow(weapon)) return true;

            var data = GetCrossbowData(__instance);
            
            if (data.isReloading)
            {
                if (Time.time - data.reloadStartTime < CrossbowData.reloadDuration)
                {
                    __result = false;
                    return false;
                }
                else
                {
                    data.isReloading = false;
                    data.currentMagazine = MegaCrossbowsPlugin.MagazineCapacity.Value;
                    character.Message(MessageHud.MessageType.Center, "Reloaded!");
                }
            }

            if (data.currentMagazine <= 0)
            {
                data.isReloading = true;
                data.reloadStartTime = Time.time;
                character.Message(MessageHud.MessageType.Center, "Reloading...");
                __result = false;
                return false;
            }

            if (Time.time - data.lastFireTime < data.fireInterval)
            {
                __result = false;
                return false;
            }

            data.lastFireTime = Time.time;
            data.currentMagazine--;

            return true;
        }

        private static bool IsCrossbow(ItemDrop.ItemData weapon)
        {
            return weapon.m_shared.m_name.ToLower().Contains("crossbow") || 
                   weapon.m_shared.m_skillType == Skills.SkillType.Crossbows;
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    public class HumanoidPatches
    {
        private static bool isZooming = false;
        private static float currentZoom = 1f;
        private static GameObject crosshairObject = null;

        [HarmonyPostfix]
        public static void Update_Postfix(Player __instance)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
            if (Player.m_localPlayer == null || __instance != Player.m_localPlayer) return;

            var currentWeapon = __instance.GetCurrentWeapon();
            if (currentWeapon == null || !IsCrossbow(currentWeapon))
            {
                if (isZooming)
                {
                    DisableZoom();
                }
                return;
            }

            if (Input.GetMouseButton(1))
            {
                if (!isZooming)
                {
                    EnableZoom();
                }

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0f)
                {
                    currentZoom = Mathf.Clamp(
                        currentZoom - scroll * 2f,
                        MegaCrossbowsPlugin.ZoomMin.Value,
                        MegaCrossbowsPlugin.ZoomMax.Value
                    );
                    ApplyZoom(currentZoom);
                }
            }
            else if (isZooming)
            {
                DisableZoom();
            }

            if (Input.GetMouseButton(0) && currentWeapon != null)
            {
                __instance.StartAttack(null, false);
            }
        }

        private static void EnableZoom()
        {
            isZooming = true;
            currentZoom = MegaCrossbowsPlugin.ZoomMin.Value;
            ApplyZoom(currentZoom);
            ShowCrosshair();
        }

        private static void DisableZoom()
        {
            isZooming = false;
            currentZoom = 1f;
            ApplyZoom(1f);
            HideCrosshair();
        }

        private static void ApplyZoom(float zoom)
        {
            if (GameCamera.instance != null)
            {
                GameCamera.instance.m_minDistance = 6f / zoom;
                GameCamera.instance.m_maxDistance = 6f / zoom;
            }
        }

        private static void ShowCrosshair()
        {
            if (Hud.instance != null)
            {
                var crosshair = Hud.instance.m_crosshair;
                if (crosshair != null)
                {
                    crosshair.gameObject.SetActive(true);
                }
            }
        }

        private static void HideCrosshair()
        {
            
        }

        private static bool IsCrossbow(ItemDrop.ItemData weapon)
        {
            return weapon.m_shared.m_name.ToLower().Contains("crossbow") || 
                   weapon.m_shared.m_skillType == Skills.SkillType.Crossbows;
        }
    }

    [HarmonyPatch(typeof(Projectile))]
    public class ProjectilePatches
    {
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

        private static bool IsCrossbow(ItemDrop.ItemData weapon)
        {
            return weapon.m_shared.m_name.ToLower().Contains("crossbow") || 
                   weapon.m_shared.m_skillType == Skills.SkillType.Crossbows;
        }
    }
}
