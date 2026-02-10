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
        public static ConfigEntry<string> ConfigProfile;
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
        
        // Damage
        public static ConfigEntry<float> DamageMultiplier;
        public static ConfigEntry<bool> DamagePierce;
        public static ConfigEntry<bool> DamageBlunt;
        public static ConfigEntry<bool> DamageSlash;
        public static ConfigEntry<float> Stagger;
        
        // Damage - Elemental
        public static ConfigEntry<bool> DamageFire;
        public static ConfigEntry<bool> DamageFrost;
        public static ConfigEntry<bool> DamageLightning;
        public static ConfigEntry<bool> DamagePoison;
        public static ConfigEntry<bool> DamageSpirit;
        public static ConfigEntry<float> ElementalDoT;
        
        // AOE
        public static ConfigEntry<float> AoeRadius;

        // Building Damage
        public static ConfigEntry<float> BuildingDamage;
        public static ConfigEntry<float> BuildingFireDamage;
        public static ConfigEntry<float> BuildingFireDuration;

        // HouseFire
        public static ConfigEntry<float> HouseFireDamage;
        public static ConfigEntry<float> HouseFireRadius;
        public static ConfigEntry<float> HouseFireTickInterval;
        public static ConfigEntry<int> HouseFireSpread;
        public static ConfigEntry<float> HouseFireSmokeDieChance;
        public static ConfigEntry<float> HouseFireMaxSmoke;

        private Harmony _harmony;
        private FileSystemWatcher _configWatcher;

        private void Awake()
        {
            // Profile (top of config file)
            ConfigProfile = Config.Bind("0. Profile", "ConfigProfile", "Default",
                new ConfigDescription(
                    "Quick config preset.\n" +
                    "Default = normal play settings.\n" +
                    "Development = enables DestroyObjects + AOE radius 10m for testing.",
                    new AcceptableValueList<string>("Default", "Development")));

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
            
            // Damage - Split system: base pierce is divided evenly across all enabled types
            // BaseMultiplier scales the total. e.g. Charred bolt (82 pierce) with all 8 types enabled:
            //   82 / 8 = 10.25 per type. With BaseMultiplier=2: 164 / 8 = 20.5 per type.
            DamageMultiplier = Config.Bind("4. Damage", "BaseMultiplier", 1f, 
                new ConfigDescription("Overall damage multiplier (1 = normal, 2 = double total damage, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            DamagePierce = Config.Bind("4. Damage", "Pierce", true,
                "Enable pierce damage (always counts toward the split)");
            DamageBlunt = Config.Bind("4. Damage", "Blunt", true,
                "Enable blunt damage (splits total damage across more types)");
            DamageSlash = Config.Bind("4. Damage", "Slash", true,
                "Enable slash damage (splits total damage across more types)");
            Stagger = Config.Bind("4. Damage", "Stagger", 1f, 
                new ConfigDescription("Stagger/knockback multiplier (0 = none, 1 = normal, 10 = 10x)", new AcceptableValueRange<float>(0f, 10f)));
            
            // Elemental types also split from the same pool
            DamageFire = Config.Bind("5. Damage - Elemental", "Fire", true,
                "Enable fire damage (splits total damage across more types)");
            DamageFrost = Config.Bind("5. Damage - Elemental", "Frost", true,
                "Enable frost damage (splits total damage across more types)");
            DamageLightning = Config.Bind("5. Damage - Elemental", "Lightning", true,
                "Enable lightning damage (splits total damage across more types)");
            DamagePoison = Config.Bind("5. Damage - Elemental", "Poison", true,
                "Enable poison damage (splits total damage across more types)");
            DamageSpirit = Config.Bind("5. Damage - Elemental", "Spirit", true,
                "Enable spirit damage (splits total damage across more types)");
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

            // HouseFire (ALT-mode fire spawned on impact)
            HouseFireDamage = Config.Bind("8. HouseFire", "FireDamage", 10f,
                new ConfigDescription("Fire damage per tick to buildings/creatures in radius (default: 10)", new AcceptableValueRange<float>(1f, 100f)));
            HouseFireRadius = Config.Bind("8. HouseFire", "DotRadius", 1f,
                new ConfigDescription("Radius of the fire's damage sphere (default: 1m)", new AcceptableValueRange<float>(1f, 10f)));
            HouseFireTickInterval = Config.Bind("8. HouseFire", "TickInterval", 1f,
                new ConfigDescription("Seconds between damage ticks (lower = faster burn, default: 1)", new AcceptableValueRange<float>(0.1f, 5f)));
            HouseFireSpread = Config.Bind("8. HouseFire", "Spread", 4,
                new ConfigDescription("Max fires allowed nearby — higher = more spread (default: 4)", new AcceptableValueRange<int>(1, 20)));
            HouseFireSmokeDieChance = Config.Bind("8. HouseFire", "SmokeDieChance", 0.5f,
                new ConfigDescription("Chance fire dies when suffocated by smoke (0 = immortal fire, 0.5 = default, 1 = always dies)", new AcceptableValueRange<float>(0f, 1f)));
            HouseFireMaxSmoke = Config.Bind("8. HouseFire", "MaxSmoke", 3f,
                new ConfigDescription("Smoke tolerance before fire can die (higher = survives longer in enclosed spaces, default: 3)", new AcceptableValueRange<float>(1f, 50f)));

            // Apply profile overrides
            ApplyProfileOverrides();

            // Watch config file for live reload on save
            SetupConfigWatcher();

            if (ModEnabled.Value)
            {
                _harmony = new Harmony(PluginGUID);
                _harmony.PatchAll();
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
            }
            catch { }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                Config.Reload();
                ApplyProfileOverrides();
            }
            catch { }
        }

        private static void ApplyProfileOverrides()
        {
            if (ConfigProfile.Value == "Development")
            {
                DestroyObjects.Value = true;
                AoeRadius.Value = 10f;
            }
        }
    }
}


