using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.IO;

namespace MegaCrossbows
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MegaCrossbowsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.rikal.megacrossbows";
        public const string PluginName = "MegaCrossbows";
        public const string PluginVersion = "2.0.0";

        // General
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> DestroyObjects;
        public static ConfigEntry<KeyCode> DestroyObjectsKey;
        public static ConfigEntry<float> FireRate;
        public static ConfigEntry<int> MagazineCapacity;
        
        // Zoom
        public static ConfigEntry<float> ZoomMin;
        public static ConfigEntry<float> ZoomMax;
        
        // Projectile
        public static ConfigEntry<float> Velocity;
        public static ConfigEntry<bool> NoGravity;
        public static ConfigEntry<float> Distance;
        
        // Damage - Base
        public static ConfigEntry<float> DamageMultiplier;
        public static ConfigEntry<float> DamageBlunt;
        public static ConfigEntry<float> DamageSlash;
        public static ConfigEntry<float> DamagePierce;
        public static ConfigEntry<float> Stagger;
        
        // Damage - Elemental
        public static ConfigEntry<float> DamageFire;
        public static ConfigEntry<float> DamageFrost;
        public static ConfigEntry<float> DamageLightning;
        public static ConfigEntry<float> DamagePoison;
        public static ConfigEntry<float> DamageSpirit;
        public static ConfigEntry<float> ElementalDoT;
        
        // AOE
        public static ConfigEntry<float> AoeRadius;

        // Building Damage
        public static ConfigEntry<float> BuildingDamage;
        public static ConfigEntry<float> BuildingFireDamage;
        public static ConfigEntry<float> BuildingFireDuration;

        private Harmony _harmony;
        private FileSystemWatcher _configWatcher;

        private void Awake()
        {
            Logger.LogInfo("=== MegaCrossbows Awake() starting ===");
            ModLogger.Initialize();
            ModLogger.Log("=== Plugin Awake() called ===");
            
            // General
            ModEnabled = Config.Bind("1. General", "Enabled", true, "Enable or disable the mod");
            DestroyObjects = Config.Bind("1. General", "DestroyObjects", false,
                "Bolts instantly destroy resource objects: trees, logs, rocks, copper, tin, silver, obsidian, flametal, and other mineable/choppable objects (must hold modifier key while firing)");
            DestroyObjectsKey = Config.Bind("1. General", "DestroyObjectsKey", KeyCode.LeftAlt,
                "Hold this key while firing to destroy objects (only when DestroyObjects is enabled)");
            FireRate = Config.Bind("1. General", "FireRate", 10f,
                new ConfigDescription("Fire rate per second (1-10)", new AcceptableValueRange<float>(1f, 10f)));
            MagazineCapacity = Config.Bind("1. General", "MagazineCapacity", 1000, "Magazine capacity before reload");
            
            // Zoom
            ZoomMin = Config.Bind("2. Zoom", "ZoomMin", 2f, "Minimum zoom level");
            ZoomMax = Config.Bind("2. Zoom", "ZoomMax", 10f, "Maximum zoom level");
            
            // Projectile - M4A1/M16 muzzle velocity is ~940 m/s
            // Base Valheim crossbow is ~200 m/s, so 470% = ~940 m/s
            Velocity = Config.Bind("3. Projectile", "Velocity", 470f, "Bolt velocity multiplier (470 = ~940 m/s, like an M4A1/M16)");
            NoGravity = Config.Bind("3. Projectile", "NoGravity", true, "Disable gravity for bolts (default: true for accuracy)");
            Distance = Config.Bind("3. Projectile", "Distance", 1f,
                new ConfigDescription("Bolt travel distance multiplier (1 = normal, 10 = 10x distance)", new AcceptableValueRange<float>(1f, 10f)));
            
            // Damage - Base
            DamageMultiplier = Config.Bind("4. Damage - Base", "BaseMultiplier", 1f, 
                new ConfigDescription("Overall damage multiplier (0 = none, 0.1 = 10%, 1 = normal, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            DamagePierce = Config.Bind("4. Damage - Base", "Pierce", 1f, 
                new ConfigDescription("Pierce damage multiplier (0 = none, 0.1 = 10%, 1 = normal, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            DamageBlunt = Config.Bind("4. Damage - Base", "Blunt", 0f, 
                new ConfigDescription("Blunt damage multiplier based on bolt's base pierce (0 = none, 0.1 = 10%, 1 = equal to pierce, 10 = 10x pierce)", new AcceptableValueRange<float>(0f, 10f)));
            DamageSlash = Config.Bind("4. Damage - Base", "Slash", 0f, 
                new ConfigDescription("Slash damage multiplier based on bolt's base pierce (0 = none, 0.1 = 10%, 1 = equal to pierce, 10 = 10x pierce)", new AcceptableValueRange<float>(0f, 10f)));
            Stagger = Config.Bind("4. Damage - Base", "Stagger", 1f, 
                new ConfigDescription("Stagger/knockback multiplier (0 = none, 0.1 = 10%, 1 = normal, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            
            // Damage - Elemental (multiplier of bolt's base pierce: 0=none, 0.1=10%, 1=equal to pierce, 10=10x)
            DamageFire = Config.Bind("5. Damage - Elemental", "Fire", 0f, 
                new ConfigDescription("Fire damage multiplier based on bolt's base pierce (0 = none, 0.1 = 10%, 1 = equal to pierce, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            DamageFrost = Config.Bind("5. Damage - Elemental", "Frost", 0f, 
                new ConfigDescription("Frost damage multiplier based on bolt's base pierce (0 = none, 0.1 = 10%, 1 = equal to pierce, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            DamageLightning = Config.Bind("5. Damage - Elemental", "Lightning", 0f, 
                new ConfigDescription("Lightning damage multiplier based on bolt's base pierce (0 = none, 0.1 = 10%, 1 = equal to pierce, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            DamagePoison = Config.Bind("5. Damage - Elemental", "Poison", 0f, 
                new ConfigDescription("Poison damage multiplier based on bolt's base pierce (0 = none, 0.1 = 10%, 1 = equal to pierce, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            DamageSpirit = Config.Bind("5. Damage - Elemental", "Spirit", 0f, 
                new ConfigDescription("Spirit damage multiplier based on bolt's base pierce (0 = none, 0.1 = 10%, 1 = equal to pierce, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            ElementalDoT = Config.Bind("5. Damage - Elemental", "ElementalDoT", 0f, 
                new ConfigDescription("Elemental damage over time multiplier (0 = none, 1 = normal, 10 = 10x stronger DoT)", new AcceptableValueRange<float>(0f, 10f)));
            
            // AOE
            AoeRadius = Config.Bind("6. AOE", "Radius", 1f, 
                new ConfigDescription("Area of Effect radius (0 = disabled, 1 = default)", new AcceptableValueRange<float>(0f, 10f)));
            
            // Building Damage
            BuildingDamage = Config.Bind("7. Building Damage", "BuildingDamageMultiplier", 1f, 
                new ConfigDescription("Building damage multiplier (1 = normal, 10 = 10x)", new AcceptableValueRange<float>(1f, 10f)));
            BuildingFireDamage = Config.Bind("7. Building Damage", "BuildingFireDamage", 0f, 
                new ConfigDescription("Fire damage to buildings - Ashlands fire behavior (0 = none, 1 = normal, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            BuildingFireDuration = Config.Bind("7. Building Damage", "BuildingFireDuration", 1f, 
                new ConfigDescription("How long buildings burn (1 = normal Ashlands duration, 10 = 10x duration)", new AcceptableValueRange<float>(1f, 10f)));

            ModLogger.Log($"Config Loaded - FireRate: {FireRate.Value}, Velocity: {Velocity.Value}");

            // Watch config file for live reload on save
            SetupConfigWatcher();

            if (ModEnabled.Value)
            {
                _harmony = new Harmony(PluginGUID);
                _harmony.PatchAll();
                Logger.LogInfo($"{PluginName} v{PluginVersion} loaded!");
                ModLogger.Log($"=== {PluginName} v{PluginVersion} loaded! ===");
            }
            else
            {
                ModLogger.Log("Mod is DISABLED in config");
                Logger.LogWarning($"{PluginName} is DISABLED in config");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _configWatcher?.Dispose();
        }

        private void SetupConfigWatcher()
        {
            try
            {
                var configFile = Config.ConfigFilePath;
                var configDir = Path.GetDirectoryName(configFile);
                var configFileName = Path.GetFileName(configFile);

                _configWatcher = new FileSystemWatcher(configDir, configFileName);
                _configWatcher.Changed += OnConfigFileChanged;
                _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
                _configWatcher.EnableRaisingEvents = true;

                ModLogger.Log($"Config watcher active: {configFile}");
            }
            catch (System.Exception ex)
            {
                ModLogger.LogError($"Failed to setup config watcher: {ex.Message}");
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Config.Reload();
                ModLogger.Log("Config reloaded from file");
            }
            catch (System.Exception ex)
            {
                ModLogger.LogError($"Config reload failed: {ex.Message}");
            }
        }
    }
}


