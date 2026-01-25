using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MegaCrossbows
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MegaCrossbowsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.rikal.megacrossbows";
        public const string PluginName = "MegaCrossbows";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<float> DamageMultiplier;
        public static ConfigEntry<float> FireRate;
        public static ConfigEntry<float> ZoomMin;
        public static ConfigEntry<float> ZoomMax;
        public static ConfigEntry<float> Velocity;
        public static ConfigEntry<bool> NoGravity;
        public static ConfigEntry<int> MagazineCapacity;

        private Harmony _harmony;

        private void Awake()
        {
            ModEnabled = Config.Bind("General", "Enabled", true, "Enable or disable the mod");
            DamageMultiplier = Config.Bind("General", "DamageMultiplier", 100f, "Damage multiplier in percentage (100 = normal)");
            FireRate = Config.Bind("General", "FireRate", 5f, "Fire rate per second");
            ZoomMin = Config.Bind("Zoom", "ZoomMin", 2f, "Minimum zoom level");
            ZoomMax = Config.Bind("Zoom", "ZoomMax", 10f, "Maximum zoom level");
            Velocity = Config.Bind("Projectile", "Velocity", 100f, "Bolt velocity");
            NoGravity = Config.Bind("Projectile", "NoGravity", false, "Disable gravity for bolts");
            MagazineCapacity = Config.Bind("Magazine", "Capacity", 1000, "Magazine capacity");

            if (ModEnabled.Value)
            {
                _harmony = new Harmony(PluginGUID);
                _harmony.PatchAll();
                Logger.LogInfo($"{PluginName} v{PluginVersion} loaded!");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}

