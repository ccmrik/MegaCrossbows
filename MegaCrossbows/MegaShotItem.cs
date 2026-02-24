using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MegaCrossbows
{
    /// <summary>
    /// Custom MegaShot crossbow — cloned from CrossbowRipper with 8 quality levels,
    /// per-level damage, per-level upgrade recipes with different ingredients.
    /// </summary>
    public static class MegaShotItem
    {
        public const string ItemName = "MegaShot";
        public const string PrefabName = "MegaShot";
        public const string Description = "A legendary rapid-fire crossbow. Forged for destruction.";

        private static GameObject megaShotPrefab;
        private static Recipe megaShotRecipe;
        private static int currentRecipeLevel = -1;

        // Per-level pierce damage (index 0 = level 1, index 7 = level 8)
        public static readonly float[] PierceDamagePerLevel = { 31f, 41f, 31f, 51f, 61f, 71f, 81f, 91f };

        // Per-level recipe ingredient prefab names (all 5 each)
        private static readonly string[][] IngredientNames = new string[][]
        {
            new[] { "Wood", "DeerHide", "Resin" },                     // Level 1
            new[] { "RoundLog", "BearHide", "Tin" },                   // Level 2
            new[] { "ElderBark", "Bloodbag", "Iron" },                 // Level 3
            new[] { "FineWood", "FenrisHair", "Silver" },              // Level 4
            new[] { "FineWood", "VileRibcage", "BlackMetal" },         // Level 5
            new[] { "YggdrasilWood", "Carapace", "BlackMarble" },      // Level 6
            new[] { "Ashwood", "AsksvinHide", "Flametal" },            // Level 7
            new[] { "SurtlingCore", "BlackCore", "MoltenCore" },       // Level 8
        };
        private const int IngredientAmount = 5;

        // Per-level crafting station: (stationKeyword, minStationLevel)
        // Keyword is matched against recipe crafting station names in ObjectDB
        private static readonly string[] StationKeywords = new string[]
        {
            "workbench",   // Level 1
            "workbench",   // Level 2
            "forge",       // Level 3
            "forge",       // Level 4
            "forge",       // Level 5
            "blackforge",  // Level 6
            "blackforge",  // Level 7
            "blackforge",  // Level 8
        };
        private static readonly int[] StationLevels = new int[]
        {
            1, 2, 1, 2, 3, 1, 2, 3
        };

        // Cached crafting station references (found from existing recipes)
        private static CraftingStation cachedWorkbench;
        private static CraftingStation cachedForge;
        private static CraftingStation cachedBlackForge;
        private static bool stationsCached = false;

        public static bool IsMegaShot(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            return item.m_shared.m_name == ItemName;
        }

        public static float GetPierceDamage(int quality)
        {
            int idx = Mathf.Clamp(quality - 1, 0, PierceDamagePerLevel.Length - 1);
            return PierceDamagePerLevel[idx];
        }

        public static void Register(ObjectDB objectDB)
        {
            if (objectDB == null || objectDB.m_items == null) return;

            // Check if already registered in this ObjectDB instance
            foreach (var item in objectDB.m_items)
            {
                if (item != null && item.name == PrefabName) return;
            }

            try
            {
                // Find CrossbowRipper prefab
                GameObject ripperPrefab = null;
                foreach (var prefab in objectDB.m_items)
                {
                    if (prefab == null) continue;
                    if (prefab.name == "CrossbowRipper")
                    {
                        ripperPrefab = prefab;
                        break;
                    }
                }
                if (ripperPrefab == null) return;

                // Clone: deactivate source first to prevent ZNetView.Awake() from
                // registering a live ZDO on the clone (causes NullRef in ZNetScene.RemoveObjects)
                bool wasActive = ripperPrefab.activeSelf;
                ripperPrefab.SetActive(false);
                megaShotPrefab = UnityEngine.Object.Instantiate(ripperPrefab);
                ripperPrefab.SetActive(wasActive);
                megaShotPrefab.name = PrefabName;
                UnityEngine.Object.DontDestroyOnLoad(megaShotPrefab);

                // Modify item properties
                var itemDrop = megaShotPrefab.GetComponent<ItemDrop>();
                if (itemDrop == null) return;

                var shared = itemDrop.m_itemData.m_shared;
                shared.m_name = ItemName;
                shared.m_description = Description;
                shared.m_maxQuality = 8;
                shared.m_backstabBonus = 3f;

                // Clear DLC flag if any
                try { shared.m_dlc = ""; } catch { }

                // Base damage (level 1) — per-level handled by GetDamage patch + FireBolt override
                var baseDmg = new HitData.DamageTypes();
                baseDmg.m_pierce = PierceDamagePerLevel[0];
                shared.m_damages = baseDmg;
                shared.m_damagesPerLevel = new HitData.DamageTypes();

                // Register in ObjectDB
                objectDB.m_items.Add(megaShotPrefab);

                // Update ObjectDB's internal hash lookup
                try
                {
                    var field = typeof(ObjectDB).GetField("m_itemByHash",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        var dict = field.GetValue(objectDB) as Dictionary<int, GameObject>;
                        if (dict != null)
                            dict[megaShotPrefab.name.GetStableHashCode()] = megaShotPrefab;
                    }
                }
                catch { }

                // Register in ZNetScene's hash lookup ONLY (not m_prefabs list).
                // Adding to m_prefabs causes NullRef in ZNetScene.RemoveObjects.
                try
                {
                    if (ZNetScene.instance != null)
                    {
                        var hash = megaShotPrefab.name.GetStableHashCode();
                        var namedField = typeof(ZNetScene).GetField("m_namedPrefabs",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (namedField != null)
                        {
                            var dict = namedField.GetValue(ZNetScene.instance) as Dictionary<int, GameObject>;
                            if (dict != null && !dict.ContainsKey(hash))
                            {
                                dict[hash] = megaShotPrefab;
                            }
                        }
                    }
                }
                catch { }

                // Cache crafting stations from existing recipes
                CacheStations(objectDB);

                // Create recipe
                CreateRecipe(objectDB);
            }
            catch { }
        }

        /// <summary>
        /// Scans existing recipes in ObjectDB to find crafting station references
        /// (Workbench, Forge, Black Forge) by matching station GameObject names.
        /// </summary>
        private static void CacheStations(ObjectDB objectDB)
        {
            if (stationsCached) return;
            stationsCached = true;

            try
            {
                if (objectDB.m_recipes == null) return;

                foreach (var r in objectDB.m_recipes)
                {
                    if (r == null || r.m_craftingStation == null) continue;
                    string stationName = r.m_craftingStation.gameObject.name.ToLower();

                    if (cachedWorkbench == null && stationName.Contains("workbench"))
                        cachedWorkbench = r.m_craftingStation;
                    if (cachedForge == null && stationName == "forge")
                        cachedForge = r.m_craftingStation;
                    if (cachedBlackForge == null && stationName.Contains("blackforge"))
                        cachedBlackForge = r.m_craftingStation;

                    if (cachedWorkbench != null && cachedForge != null && cachedBlackForge != null)
                        break;
                }
            }
            catch { }
        }

        private static CraftingStation GetStationForLevel(int level)
        {
            if (level < 1 || level > 8) return cachedWorkbench;
            string keyword = StationKeywords[level - 1];
            if (keyword == "blackforge" && cachedBlackForge != null) return cachedBlackForge;
            if (keyword == "forge" && cachedForge != null) return cachedForge;
            return cachedWorkbench;
        }

        private static void CreateRecipe(ObjectDB objectDB)
        {
            try
            {
                megaShotRecipe = ScriptableObject.CreateInstance<Recipe>();
                megaShotRecipe.name = "Recipe_MegaShot";
                megaShotRecipe.m_item = megaShotPrefab.GetComponent<ItemDrop>();
                megaShotRecipe.m_amount = 1;
                megaShotRecipe.m_enabled = true;
                megaShotRecipe.m_craftingStation = GetStationForLevel(1);
                megaShotRecipe.m_repairStation = cachedWorkbench;
                megaShotRecipe.m_minStationLevel = StationLevels[0];

                // Set initial resources for level 1
                SetRecipeResources(objectDB, 1);

                objectDB.m_recipes.Add(megaShotRecipe);
            }
            catch { }
        }

        private static void SetRecipeResources(ObjectDB objectDB, int level)
        {
            if (objectDB == null || megaShotRecipe == null) return;
            if (level < 1 || level > 8) return;
            if (level == currentRecipeLevel) return;

            try
            {
                var names = IngredientNames[level - 1];
                var reqs = new List<Piece.Requirement>();

                foreach (var ingredientName in names)
                {
                    GameObject prefab = FindItemPrefab(objectDB, ingredientName);
                    if (prefab == null) continue;
                    var itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop == null) continue;

                    var req = new Piece.Requirement();
                    req.m_resItem = itemDrop;
                    req.m_amount = IngredientAmount;
                    req.m_amountPerLevel = IngredientAmount;
                    req.m_recover = true;
                    reqs.Add(req);
                }

                megaShotRecipe.m_resources = reqs.ToArray();

                // Swap crafting station and min level for the target quality
                megaShotRecipe.m_craftingStation = GetStationForLevel(level);
                megaShotRecipe.m_minStationLevel = StationLevels[level - 1];

                currentRecipeLevel = level;
            }
            catch { }
        }

        private static GameObject FindItemPrefab(ObjectDB objectDB, string name)
        {
            // Try ObjectDB.GetItemPrefab(string) via reflection (may or may not exist)
            try
            {
                var method = typeof(ObjectDB).GetMethod("GetItemPrefab",
                    new Type[] { typeof(string) });
                if (method != null)
                {
                    var result = method.Invoke(objectDB, new object[] { name }) as GameObject;
                    if (result != null) return result;
                }
            }
            catch { }

            // Fallback: search m_items by name
            try
            {
                foreach (var item in objectDB.m_items)
                {
                    if (item != null && item.name == name)
                        return item;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Called from Player.Update to keep the recipe resources in sync with the
        /// player's current MegaShot quality (so upgrades show correct ingredients).
        /// Dynamically swaps recipe ingredients based on target upgrade level.
        /// </summary>
        public static void UpdateRecipeForPlayer(Player player)
        {
            if (megaShotRecipe == null) return;

            try
            {
                bool craftingOpen = false;
                try { craftingOpen = InventoryGui.IsVisible(); } catch { }
                if (!craftingOpen)
                {
                    // Reset to level 1 when GUI is closed
                    if (currentRecipeLevel != 1 && ObjectDB.instance != null)
                        SetRecipeResources(ObjectDB.instance, 1);
                    return;
                }

                // Find the player's MegaShot and determine target quality
                int targetLevel = 1;
                var inv = player.GetInventory();
                if (inv != null)
                {
                    foreach (var item in inv.GetAllItems())
                    {
                        if (IsMegaShot(item))
                        {
                            targetLevel = item.m_quality + 1;
                            break;
                        }
                    }
                }

                if (targetLevel > 8) return;

                if (ObjectDB.instance != null)
                    SetRecipeResources(ObjectDB.instance, targetLevel);
            }
            catch { }
        }
    }

    // Register MegaShot item in ObjectDB.Awake
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class PatchRegisterMegaShot
    {
        [HarmonyPriority(Priority.High)]
        public static void Postfix(ObjectDB __instance)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
                MegaShotItem.Register(__instance);
            }
            catch { }
        }
    }

    // Manually patched in Class1.cs (not attribute-based) — safe if GetDamage doesn't exist
    public static class PatchMegaShotDamage
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            try
            {
                if (!MegaCrossbowsPlugin.ModEnabled.Value) return;
                if (!MegaShotItem.IsMegaShot(__instance)) return;
                __result.m_pierce = MegaShotItem.GetPierceDamage(__instance.m_quality);
            }
            catch { }
        }
    }
}
