using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CasperEquinoxGUI;
using EquinoxsModUtils;
using FIMSpace.Generating.Planning.PlannerNodes.Math.Values;
using HarmonyLib;
using RecipeBook.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Windows;
using static Rewired.Demos.GamepadTemplateUI.GamepadTemplateUI;

namespace RecipeBook
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class RecipeBookPlugin : BaseUnityPlugin
    {
        internal const string MyGUID = "com.equinox.RecipeBook";
        private const string PluginName = "RecipeBook";
        private const string VersionString = "2.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        // Variables

        public static string selectedItem = "";

        #region Config Entries

        // Items Panel
        public static ConfigEntry<bool> filterUnknown;
        public static ConfigEntry<int> itemsPanelWidth;
        public static ConfigEntry<int> itemImageSize;

        // Recipes Panel
        public static ConfigEntry<int> defaultMachineMk;
        public static ConfigEntry<int> recipesPanelWidth;
        public static ConfigEntry<int> recipesPanelHeight;
        public static ConfigEntry<int> recipesPanelTopMargin;

        #endregion

        // Unity Functions

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            BindEvents();
            CreateConfigEntries();
            ApplyPatches();

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void OnGUI() {
            if (!Images.initialised) Images.InitialiseStyles();

            ItemsPanelGUI.sSinceOpen += Time.deltaTime;

            if(ItemsPanelGUI.isOpen && ItemsPanelGUI.sSinceOpen > 0.3f && (UnityInput.Current.GetKey(KeyCode.Escape) || UnityInput.Current.GetKey(KeyCode.Tab))) {
                InventoryAndCraftingUIPatch.HideItemsPanel();
            }
        }

        // Events

        private void OnReadyForGUI() {
            ItemsPanelGUI.CreateItemsPanel();
            RecipesGUI.CreateRecipesWindow();
        }

        // Private Functions

        private void BindEvents() {
            CaspuinoxGUI.ReadyForGUI += OnReadyForGUI;
        }

        private void CreateConfigEntries() {
            filterUnknown = Config.Bind("General", "Filter Unknown", true, new ConfigDescription("Whether to hide items and recipes you have not discovered yet."));
            
            itemsPanelWidth = Config.Bind("Items Panel", "Items Panel Width", 460, new ConfigDescription("The width of the Items Panel", new AcceptableValueRange<int>(100, 10000)));
            itemImageSize = Config.Bind("Items Panel", "Item Image Size", 50, new ConfigDescription("The width and height of each item's image in the Items Panel", new AcceptableValueRange<int>(10, 100)));

            defaultMachineMk = Config.Bind("Recipes Panel", "Default Machine Mk", 1, new ConfigDescription("The default 'Machine Mk' to use", new AcceptableValueRange<int>(1, 3)));
            recipesPanelWidth = Config.Bind("Recipes Panel", "Recipes Panel Width", 500, new ConfigDescription("The width of the Recipes Panel", new AcceptableValueRange<int>(100, 1000)));
            recipesPanelHeight = Config.Bind("Recipes Panel", "Recipes Panel Height", 825, new ConfigDescription("The height of the Recipes Panel", new AcceptableValueRange<int>(100, 2000)));
            recipesPanelTopMargin = Config.Bind("Recipes Panel", "Recipes Panel Top Margin", 245, new ConfigDescription("Distance between the top of the screen and the top of the Recipes Panel", new AcceptableValueRange<int>(0, 1000)));
        }

        private void ApplyPatches() {
            Harmony.CreateAndPatchAll(typeof(InventoryAndCraftingUIPatch));
        }
    }
}
