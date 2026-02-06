# Complete rewrite with proper attack blocking

$content = @'
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace MegaCrossbows
{
    // Centralized crossbow detection
    public static class CrossbowHelper
    {
        public static bool IsCrossbow(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            if (item.m_shared.m_skillType == Skills.SkillType.Crossbows) return true;
            string itemName = item.m_shared.m_name.ToLower();
            if (itemName.Contains("crossbow") || itemName.Contains("arbalest") || itemName.Contains("ripper")) return true;
            string ammoType = item.m_shared.m_ammoType?.ToLower() ?? "";
            if (ammoType.Contains("bolt")) return true;
            return false;
        }

        public static bool IsBolt(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            string itemName = item.m_shared.m_name.ToLower();
            return itemName.Contains("bolt");
        }
    }

    public class CrossbowState
    {
        public int magazineAmmo = 1000;
        public float lastShotTime = 0f;
        public bool isReloading = false;
        public float reloadStartTime = 0f;
        public float hudUpdateTime = 0f;
        public string ammoText = "";
        public string distanceText = "";
        public bool showHUD = false;
    }

    // HUD Component
    public class CrossbowHUD : MonoBehaviour
    {
        private GUIStyle ammoStyle;
        private GUIStyle distanceStyle;
        public static CrossbowState currentState;
        public static bool showHUD = false;

        void OnGUI()
        {
            if (!showHUD || currentState == null) return;

            if (ammoStyle == null)
            {
                ammoStyle = new GUIStyle();
                ammoStyle.fontSize = 18;
                ammoStyle.fontStyle = FontStyle.Bold;
                ammoStyle.normal.textColor = Color.white;
                ammoStyle.alignment = TextAnchor.MiddleRight;

                distanceStyle = new GUIStyle();
                distanceStyle.fontSize = 16;
                distanceStyle.fontStyle = FontStyle.Normal;
                distanceStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                distanceStyle.alignment = TextAnchor.MiddleCenter;
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Ammo bottom-right
            float ammoX = screenWidth - 250;
            float ammoY = screenHeight - 100;
            GUI.Label(new Rect(ammoX, ammoY, 240, 40), currentState.ammoText, ammoStyle);

            // Distance below crosshair
            if (!string.IsNullOrEmpty(currentState.distanceText))
            {
                float distX = (screenWidth / 2) - 100;
                float distY = (screenHeight / 2) + 50;
                GUI.Label(new Rect(distX, distY, 200, 30), currentState.distanceText, distanceStyle);
            }
        }
    }

    // BLOCK ALL ATTACK INPUTS FOR CROSSBOWS
    [HarmonyPatch(typeof(Player))]
    public static class BlockCrossbowAttacks
    {
        // Block attack button input completely
        [HarmonyPrefix]
        [HarmonyPatch("UpdateAttack")]
        public static bool UpdateAttack_Prefix(Player __instance)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return true;
            var weapon = __instance.GetCurrentWeapon();
            if (weapon != null && CrossbowHelper.IsCrossbow(weapon) && __instance == Player.m_localPlayer)
            {
                // Block the entire attack update - no recoil, no reload animation
                return false;
            }
            return true;
        }
    }

    // Main mod logic
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch
    {
        private static Dictionary<long, CrossbowState> states = new Dictionary<long, CrossbowState>();
        private static CrossbowHUD hudComponent;
        private static bool zooming = false;
        private static float zoomLevel = 2f;
        private static float normalFOV = 65f;

        private static CrossbowState GetState(Player player)
        {
            long id = player.GetPlayerID();
            if (!states.ContainsKey(id))
            {
                states[id] = new CrossbowState();
                states[id].magazineAmmo = MegaCrossbowsPlugin.MagazineCapacity.Value;
            }
            return states[id];
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Update_Postfix(Player __instance)
        {
            if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
            if (__instance != Player.m_localPlayer) return;

            // Ensure HUD component exists
            if (hudComponent == null)
            {
                hudComponent = __instance.gameObject.GetComponent<CrossbowHUD>();
                if (hudComponent == null)
                {
                    hudComponent = __instance.gameObject.AddComponent<CrossbowHUD>();
                    ModLogger.Log("CrossbowHUD component added");
                }
            }

            var weapon = __instance.GetCurrentWeapon();
            
            if (weapon == null || !CrossbowHelper.IsCrossbow(weapon))
            {
                CrossbowHUD.showHUD = false;
                if (zooming && GameCamera.instance != null)
                {
                    GameCamera.instance.m_fov = normalFOV;
                    zooming = false;
                }
                return;
            }

            var state = GetState(__instance);

            // ZOOM
            if (Input.GetMouseButton(1))
            {
                if (!zooming && GameCamera.instance != null)
                {
                    normalFOV = GameCamera.instance.m_fov;
                    zooming = true;
                    zoomLevel = MegaCrossbowsPlugin.ZoomMin.Value;
                }

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    // Scroll DOWN to zoom IN, UP to zoom OUT
                    zoomLevel = Mathf.Clamp(
                        zoomLevel + scroll * 3f,
                        MegaCrossbowsPlugin.ZoomMin.Value,
                        MegaCrossbowsPlugin.ZoomMax.Value
                    );
                }

                if (GameCamera.instance != null)
                {
                    GameCamera.instance.m_fov = normalFOV / zoomLevel;
                }
            }
            else if (zooming)
            {
                zooming = false;
                if (GameCamera.instance != null)
                {
                    GameCamera.instance.m_fov = normalFOV;
                }
            }

            // RELOAD
            if (state.isReloading)
            {
                if (Time.time - state.reloadStartTime >= 2f)
                {
                    state.isReloading = false;
                    state.magazineAmmo = MegaCrossbowsPlugin.MagazineCapacity.Value;
                    __instance.Message(MessageHud.MessageType.Center, $"<color=green>RELOADED {state.magazineAmmo}</color>");
                }
                UpdateHUD(__instance, state);
                return;
            }

            // AUTO FIRE - only reload when magazine is 0
            if (Input.GetMouseButton(0))
            {
                if (state.magazineAmmo <= 0)
                {
                    state.isReloading = true;
                    state.reloadStartTime = Time.time;
                    __instance.Message(MessageHud.MessageType.Center, "<color=yellow>RELOADING</color>");
                    return;
                }

                float interval = 1f / MegaCrossbowsPlugin.FireRate.Value;
                if (Time.time - state.lastShotTime >= interval)
                {
                    state.lastShotTime = Time.time;
                    state.magazineAmmo--;
                    FireBolt(__instance, weapon);
                }
            }

            UpdateHUD(__instance, state);
        }

        private static void FireBolt(Player player, ItemDrop.ItemData weapon)
        {
            var ammoItem = player.GetAmmoItem();
            GameObject projectilePrefab = null;
            
            if (ammoItem != null && ammoItem.m_shared.m_attack.m_attackProjectile != null)
            {
                projectilePrefab = ammoItem.m_shared.m_attack.m_attackProjectile;
            }
            
            if (projectilePrefab == null) return;

            Vector3 spawnPos = player.transform.position + Vector3.up * 1.6f + player.transform.forward * 0.5f;
            Vector3 aimDir = GameCamera.instance != null ? GameCamera.instance.transform.forward : player.transform.forward;

            GameObject proj = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(aimDir));
            Projectile projectile = proj.GetComponent<Projectile>();

            if (projectile != null)
            {
                var attack = weapon.m_shared.m_attack;
                float speed = attack.m_projectileVel * (MegaCrossbowsPlugin.Velocity.Value / 100f);
                Vector3 velocity = aimDir * speed;

                HitData hitData = new HitData();
                hitData.m_damage = weapon.GetDamage();
                hitData.m_skill = weapon.m_shared.m_skillType;
                hitData.ApplyModifier(MegaCrossbowsPlugin.DamageMultiplier.Value / 100f);

                projectile.Setup(player, velocity, attack.m_attackHitNoise, hitData, weapon, ammoItem ?? weapon);

                if (MegaCrossbowsPlugin.NoGravity.Value)
                {
                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    if (rb != null) rb.useGravity = false;
                }

                // Trigger animation
                try
                {
                    var zanimField = typeof(Character).GetField("m_zanim", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (zanimField != null)
                    {
                        var zanim = zanimField.GetValue(player) as ZSyncAnimation;
                        if (zanim != null && !string.IsNullOrEmpty(attack.m_attackAnimation))
                        {
                            zanim.SetTrigger(attack.m_attackAnimation);
                        }
                    }
                }
                catch { }

                player.UseStamina(attack.m_attackStamina * 0.05f);
            }
        }

        private static void UpdateHUD(Player player, CrossbowState state)
        {
            if (Time.time - state.hudUpdateTime < 0.2f) return;
            state.hudUpdateTime = Time.time;

            int invAmmo = 0;
            var inv = player.GetInventory();
            if (inv != null)
            {
                foreach (var item in inv.GetAllItems())
                {
                    if (CrossbowHelper.IsBolt(item))
                        invAmmo += item.m_stack;
                }
            }

            float range = -1f;
            if (GameCamera.instance != null)
            {
                Ray ray = new Ray(GameCamera.instance.transform.position, GameCamera.instance.transform.forward);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 500f))
                    range = hit.distance;
            }

            string zoomStr = zooming ? $" | {zoomLevel:F1}x" : "";
            
            if (state.isReloading)
            {
                state.ammoText = "RELOADING...";
                state.distanceText = "";
            }
            else
            {
                state.ammoText = $"MAG {state.magazineAmmo}/{MegaCrossbowsPlugin.MagazineCapacity.Value} | AMMO {invAmmo}{zoomStr}";
                state.distanceText = range > 0 ? $"{range:F0}m" : "";
            }
            
            state.showHUD = true;
            CrossbowHUD.currentState = state;
            CrossbowHUD.showHUD = true;
        }
    }
}
'@

Set-Content -Path "MegaCrossbows\CrossbowPatches_FIXED.cs" -Value $content
Write-Host "Created fixed version with UpdateAttack blocking" -ForegroundColor Green
