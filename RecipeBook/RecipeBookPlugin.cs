using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using FIMSpace.Generating.Planning.PlannerNodes.Math.Values;
using FluffyUnderware.Curvy.Examples;
using HarmonyLib;
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
        private const string MyGUID = "com.equinox.RecipeBook";
        private const string PluginName = "RecipeBook";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        // Config Entries

        public static ConfigEntry<bool> filterUnknown;

        // Textures
        private static Texture2D buttonBackground;
        private static Texture2D buttonHoverBackground;
        private static Texture2D textBoxNormal;
        private static Texture2D textBoxHover;
        private static Texture2D windowBackground;
        
        private static Texture2D checkboxUnticked;
        private static Texture2D checkboxTicked;

        private static Texture2D oneIngredient;
        private static Texture2D twoIngredients;
        private static Texture2D threeIngredients;
        private static Texture2D thresherRecipe;

        // Field Values
        Vector2 scrollPosition = Vector2.zero;
        Vector2 recipesScrollPosition = Vector2.zero;
        string searchTerm = "";
        bool useMk2 = false;

        // Variables
        float timeSinceOpen = 0f;
        bool trackTime = false;
        bool lastCheck = false;
        bool showRecipesGUI = false;
        bool showUsesGUI = false;
        string targetItemForRecipes = "";
        string targetItemForUses = "";

        // Recipes / Uses Window Settings
        public static float windowYOffset = 25;
        public static float windowWidth = 380;
        public static float windowHeight = 625;
        public static float windowX => (Screen.width - windowWidth) / 2.0f;
        public static float windowY => ((Screen.height - windowHeight) / 2.0f) - windowYOffset;

        // Unity Functions

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            filterUnknown = Config.Bind("General", "Filter Unknown", true, new ConfigDescription("Whether to hide items and recipes you have not discovered yet."));

            LoadImages();

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void OnGUI() {
            if (UIManager.instance == null) return;
            if (!HandleShouldShowGUI()) return;

            if (UnityInput.Current.GetKey(KeyCode.Escape)) {
                UIManager.instance.inventoryCraftingMenu.Close(true);
                return;
            }

            if(!showRecipesGUI && !showUsesGUI) {
                GUI.FocusControl("SearchBar");
            }

            InputHandler.instance.uiInputBlocked = true;

            HideHorizontalScrollBar();
            DrawSearchBar();

            // Items Scroller

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) {
                normal = { background = buttonBackground },
                hover = { background = buttonHoverBackground }
            };

            int maxHeight = ResourceNames.SafeResources.Count;
            while (maxHeight % 5 != 0) ++maxHeight;
            maxHeight *= 12;
            scrollPosition = GUI.BeginScrollView(new Rect(Screen.width - 320, 40, 320, Screen.height - 70), scrollPosition, new Rect(Screen.width - 320, 20, 300, maxHeight), false, false);
            List<string> searchResults = ResourceNames.SafeResources.Where(name => name.ToLower().Contains(searchTerm.ToLower())).ToList();
            if (filterUnknown.Value) {
                searchResults = searchResults.Where(name => TechTreeState.instance.IsResourceKnown(ModUtils.GetResourceIDByName(name))).ToList();
            }

            for (int i = 0; i < searchResults.Count; i++) {
                Rect buttonRect = GetButtonRect(i);
                if (GUI.Button(buttonRect, GetImageForResource(searchResults[i]), buttonStyle)) {
                    HandleResourceButtonClicked(searchResults[i]);
                }
            }
            
            GUI.EndScrollView();

            if(UnityInput.Current.GetKey(KeyCode.Escape) || InputHandler.instance.input.GetButtonDown(45)) {
                showRecipesGUI = false;
            }

            GUIStyle windowStyle = new GUIStyle(GUI.skin.window) {
                normal = { background = windowBackground },
                hover = { background = windowBackground },
                focused = { background = windowBackground },
                active = { background = windowBackground },

                onNormal = { background = windowBackground },
                onHover = { background = windowBackground },
                onFocused = { background = windowBackground },
                onActive = { background = windowBackground },
            };

            if (showRecipesGUI) GUI.Window(0, new Rect(windowX, windowY - windowYOffset, windowWidth, windowHeight), DrawRecipesWindowContent, "", windowStyle);
            else if (showUsesGUI) GUI.Window(1, new Rect(windowX, windowY - windowYOffset, windowWidth, windowHeight), DrawUsesWindowContent, "", windowStyle);
        }

        // Private Functions

        private void LoadImages() {
            LoadImage("RecipeBook.Images.ButtonBackground.png", ref buttonBackground);
            LoadImage("RecipeBook.Images.ButtonHoverBackground.png", ref buttonHoverBackground);
            LoadImage("RecipeBook.Images.TextBoxNormal.png", ref textBoxNormal);
            LoadImage("RecipeBook.Images.TextBoxHover.png", ref textBoxHover);
            LoadImage("RecipeBook.Images.WindowBackground.png", ref windowBackground);
            
            LoadImage("RecipeBook.Images.CheckboxUnTicked.png", ref checkboxUnticked);
            LoadImage("RecipeBook.Images.CheckboxTicked.png", ref checkboxTicked);

            LoadImage("RecipeBook.Images.OneIngredient.png", ref oneIngredient);
            LoadImage("RecipeBook.Images.TwoIngredients.png", ref twoIngredients);
            LoadImage("RecipeBook.Images.ThreeIngredients.png", ref threeIngredients);
            LoadImage("RecipeBook.Images.ThresherRecipe.png", ref thresherRecipe);
        }

        private void LoadImage(string path, ref Texture2D output) {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(path)) {
                if (stream == null) {
                    Debug.LogError($"Could not find button background image");
                    return;
                }

                using (MemoryStream memoryStream = new MemoryStream()) {
                    stream.CopyTo(memoryStream);
                    byte[] fileData = memoryStream.ToArray();

                    output = new Texture2D(2, 2);
                    output.LoadImage(fileData);
                }
            }
        }

        private bool HandleShouldShowGUI() {
            if (lastCheck == false && UIManager.instance.inventoryCraftingMenuOpen) {
                timeSinceOpen = 0f;
                trackTime = true;
                searchTerm = "";
            }

            if (trackTime) timeSinceOpen += Time.deltaTime;

            if (timeSinceOpen > 0.5f && (InputHandler.instance.input.GetButtonDown(45) || UnityInput.Current.GetKey(KeyCode.Escape))) {
                InputHandler.instance.uiInputBlocked = false;
                UIManager.instance.inventoryCraftingMenu.Close(true);
                trackTime = false;
                timeSinceOpen = 0f;
            }

            lastCheck = UIManager.instance.inventoryCraftingMenuOpen;

            if (!lastCheck) {
                showRecipesGUI = false;
                showUsesGUI = false;
            }

            return lastCheck;
        }

        private void HideHorizontalScrollBar() {
            GUIStyle scrollbarStyle = new GUIStyle(GUI.skin.horizontalScrollbar);
            scrollbarStyle.fixedHeight = scrollbarStyle.fixedWidth = 0;
        }

        private void DrawSearchBar() {
            GUIStyle searchBarStyle = new GUIStyle(GUI.skin.textField) {
                normal = { textColor = Color.yellow, background = textBoxNormal },
                hover = { textColor = Color.yellow, background = textBoxHover },
                active = { textColor = Color.yellow, background = textBoxNormal },
                focused = { textColor = Color.yellow, background = textBoxNormal },
            };

            GUIStyle hintLabelStyle = new GUIStyle(GUI.skin.label) {
                normal = { textColor = Color.gray }
            };
            
            if (searchTerm == "") GUI.Label(new Rect(Screen.width - 300, 10, 290, 20), "Search...", hintLabelStyle);
            GUI.SetNextControlName("SearchBar");
            searchTerm = GUI.TextField(new Rect(Screen.width - 310, 10, 300, 20), searchTerm, searchBarStyle);
        }

        // Resource Buttons

        private Rect GetButtonRect(int index) {
            float startX = Screen.width - 310;
            float startY = 20;

            const int rowWidth = 5;
            float xOffset = (index % rowWidth) * 60;
            float yOffset = (Mathf.FloorToInt((float)index / rowWidth)) * 60;

            return new Rect(startX + xOffset, startY + yOffset, 50, 50);
        }
        
        private Texture2D GetImageForResource(string name) {
            ResourceInfo info = ModUtils.GetResourceInfoByName(name);
            Sprite sprite = info.sprite;
            if (sprite.rect.width != sprite.texture.width) {
                Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                Color[] newColors = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                             (int)sprite.textureRect.y,
                                                             (int)sprite.textureRect.width,
                                                             (int)sprite.textureRect.height);
                newText.SetPixels(newColors);
                newText.Apply();
                return newText;
            }
            else {
                return sprite.texture;
            }
        }

        private void HandleResourceButtonClicked(string name) {
            GUI.FocusControl("WindowTitle");
            if (!UnityInput.Current.GetKey(KeyCode.LeftShift)) {
                showRecipesGUI = true;
                showUsesGUI = false;
                targetItemForRecipes = name;
                targetItemForUses = "";
            }
            else {
                showRecipesGUI = false;
                showUsesGUI = true;
                targetItemForRecipes = "";
                targetItemForUses = name;
            }
        }

        // Recipes Window

        private void DrawRecipesWindowContent(int windowID) {
            DrawWindow();

            ResourceInfo targetItem = ModUtils.GetResourceInfoByName(targetItemForRecipes);
            List<SchematicsRecipeData> recipes = GameDefines.instance.schematicsRecipeEntries.Where(recipe => recipe.outputTypes.Contains(targetItem)).ToList();
            if (filterUnknown.Value) {
                recipes = recipes.Where(recipe => TechTreeState.instance.IsRecipeKnown(recipe)).ToList();
            }

            DrawRecipes(recipes);
        }

        // Uses Window

        private void DrawUsesWindowContent(int windowID) {
            DrawWindow();

            ResourceInfo targetItem = ModUtils.GetResourceInfoByName(targetItemForUses);
            List<SchematicsRecipeData> recipes = GameDefines.instance.schematicsRecipeEntries.Where(recipe => recipe.ingTypes.Contains(targetItem)).ToList();
            if (filterUnknown.Value) {
                recipes = recipes.Where(recipe => TechTreeState.instance.IsRecipeKnown(recipe)).ToList();
            }

            DrawRecipes(recipes);
        }

        // Recipes / Uses Windows

        private void DrawWindow() {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.box) {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow, background = null }
            };
            string title = string.IsNullOrEmpty(targetItemForRecipes) ? $"Uses For {targetItemForUses}" : $"Recipes For {targetItemForRecipes}";
            GUI.Label(new Rect(0, 0, windowWidth, 40), title, titleStyle);
        }

        private void DrawRecipes(List<SchematicsRecipeData> recipes) {
            if (recipes.Count == 0) {
                GUIStyle titleStyle = new GUIStyle(GUI.skin.box) {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.yellow, background = null }
                };
                GUI.Label(new Rect(0, 40, windowWidth, 40), $"No recipes found...", titleStyle);
                return;
            }

            int nextHeight = 50;
            int maxHeight = 0;
            foreach (SchematicsRecipeData recipe in recipes) {
                Texture2D panel = GetPanelForRecipe(recipe);
                maxHeight += panel.height + 10;
            }

            GUIStyle checkboxStyle = new GUIStyle(GUI.skin.toggle) {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,

                normal = { background = useMk2 ? checkboxTicked : checkboxUnticked },
                hover = { background = useMk2 ? checkboxTicked : checkboxUnticked },
                active = { background = useMk2 ? checkboxTicked : checkboxUnticked },
                focused = { background = useMk2 ? checkboxTicked : checkboxUnticked },

                onNormal = { background = useMk2 ? checkboxTicked : checkboxUnticked },
                onHover = { background = useMk2 ? checkboxTicked : checkboxUnticked },
                onActive = { background = useMk2 ? checkboxTicked : checkboxUnticked },
                onFocused = { background = useMk2 ? checkboxTicked : checkboxUnticked }
            };

            useMk2 = GUI.Toggle(new Rect(10, 45, 40, 40), useMk2, "", checkboxStyle);
            GUI.Label(new Rect(60, 45, 316, 40), "Use MkII Machines", new GUIStyle() { fontSize = 16, alignment = TextAnchor.MiddleLeft, normal = { background = null, textColor = Color.yellow } });
            recipesScrollPosition = GUI.BeginScrollView(new Rect(0, 90, 366, 530), recipesScrollPosition, new Rect(0, 50, 340, maxHeight), false, true);

            foreach (SchematicsRecipeData recipe in recipes) {
                Texture2D panel = GetPanelForRecipe(recipe);
                int height = panel.height;
                DrawPanelForRecipe(panel, nextHeight, recipe);
                nextHeight += height + 10;
            }

            GUI.EndScrollView();
        }

        private Texture2D GetPanelForRecipe(SchematicsRecipeData recipe) {
            switch (recipe.craftingMethod) {
                case CraftingMethod.Smelter: return oneIngredient;
                case CraftingMethod.BlastSmelter: return oneIngredient;
                case CraftingMethod.Planter: return oneIngredient;
                
                case CraftingMethod.Thresher:
                    switch (recipe.outputTypes.Count()) {
                        case 1: return oneIngredient;
                        case 2: return thresherRecipe;
                    }
                    break;
                
                case CraftingMethod.Assembler:
                    switch (recipe.ingTypes.Count()) {
                        case 1: return oneIngredient;
                        case 2: return twoIngredients;
                        case 3: return threeIngredients;
                    }
                    break;
            }

            return null;
        }
        
        private void DrawPanelForRecipe(Texture2D panel, int height, SchematicsRecipeData recipe) {
            GUIStyle style = new GUIStyle(GUI.skin.box) { normal = { background = panel } };
            GUI.Box(new Rect(7, height, 340, panel.height), "", style);

            GUIStyle madeInStylye = new GUIStyle(GUI.skin.label) {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = null }
            };
            GUI.Label(new Rect(7, height, 340, 40), $"Made in: {Enum.GetName(typeof(CraftingMethod), recipe.craftingMethod)}", madeInStylye);
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) {
                normal = { background = buttonBackground },
                hover = { background = buttonHoverBackground },
                focused = { background = buttonHoverBackground },
                active = { background = buttonHoverBackground }
            };
            GUIStyle quantityStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight,
                normal = { background = null, textColor = Color.yellow }
            };
            GUIStyle rateStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 15,
                alignment = TextAnchor.LowerCenter,
                normal = { background = null, textColor = Color.yellow }
            };

            // Ingredient One
            if (GUI.Button(new Rect(12, height + 70, 50, 50), GetImageForResource(recipe.ingTypes[0].displayName), buttonStyle)) {
                HandleResourceButtonClicked(recipe.ingTypes[0].displayName);
            }
            GUI.Label(new Rect(10, height + 72, 50, 50), recipe.ingQuantities[0].ToString(), quantityStyle);
            GUI.Label(new Rect(-15, height + 20, 100, 50), GetRateStringForItem(recipe, 0, true), rateStyle);

            // Ingredient Two
            if(recipe.ingTypes.Count() > 1) {
                if (GUI.Button(new Rect(12, height + 140, 50, 50), GetImageForResource(recipe.ingTypes[1].displayName), buttonStyle)) {
                    HandleResourceButtonClicked(recipe.ingTypes[1].displayName);
                }
                GUI.Label(new Rect(10, height + 142, 50, 50), recipe.ingQuantities[1].ToString(), quantityStyle);
                GUI.Label(new Rect(-15, height + 90, 100, 50), GetRateStringForItem(recipe, 1, true), rateStyle);
            }

            // Ingredient Three
            if(recipe.ingTypes.Count() > 2) {
                if (GUI.Button(new Rect(12, height + 210, 50, 50), GetImageForResource(recipe.ingTypes[2].displayName), buttonStyle)) {
                    HandleResourceButtonClicked(recipe.ingTypes[2].displayName);
                }
                GUI.Label(new Rect(10, height + 212, 50, 50), recipe.ingQuantities[2].ToString(), quantityStyle);
                GUI.Label(new Rect(-15, height + 160, 100, 50), GetRateStringForItem(recipe, 2, true), rateStyle);
            }

            // Output One
            if (GUI.Button(new Rect(292, height + 70, 50, 50), GetImageForResource(recipe.outputTypes[0].displayName), buttonStyle)) {
                HandleResourceButtonClicked(recipe.outputTypes[0].displayName);
            }
            GUI.Label(new Rect(290, height + 72, 50, 50), recipe.outputQuantities[0].ToString(), quantityStyle);
            GUI.Label(new Rect(267, height + 20, 100, 50), GetRateStringForItem(recipe, 0, false), rateStyle);

            // Output Two
            if (recipe.outputTypes.Count() > 1) {
                if(GUI.Button(new Rect(292, height + 140, 50, 50), GetImageForResource(recipe.outputTypes[1].displayName), buttonStyle)) {
                    HandleResourceButtonClicked(recipe.outputTypes[1].displayName);
                }
                GUI.Label(new Rect(290, height + 142, 50, 50), recipe.outputQuantities[1].ToString(), quantityStyle);
                GUI.Label(new Rect(267, height + 90, 100, 50), GetRateStringForItem(recipe, 1, false), rateStyle);
            }
        }

        private string GetRateStringForItem(SchematicsRecipeData recipe, int index, bool isIng) {
            float craftingEfficiency = 1f;
            
            switch (recipe.craftingMethod) {
                case CraftingMethod.Assembler:
                    craftingEfficiency = useMk2 ? 0.5f : 0.25f;
                    if (isIng) {
                        return $"{recipe.ingQuantities[index] * AssemblerInstance.assemblerSpeedMultiplier * craftingEfficiency / recipe.duration * 60f}/m";
                    }
                    else {
                        return $"{recipe.outputQuantities[index] * AssemblerInstance.assemblerSpeedMultiplier * craftingEfficiency / recipe.duration * 120f}/m";
                    }

                case CraftingMethod.Smelter:
                    craftingEfficiency = useMk2 ? 8f : 1f;
                    if (isIng) {
                        return $"{recipe.ingQuantities[index] * SmelterInstance.smelterSpeedMultiplier * craftingEfficiency / recipe.duration * 60f}/m";
                    }
                    else {
                        return $"{recipe.outputQuantities[index] * SmelterInstance.smelterSpeedMultiplier * craftingEfficiency / recipe.duration * 60f}/m";
                    }

                case CraftingMethod.BlastSmelter:
                    float multiplier = 1;
                    if (TechTreeState.instance.IsUnlockActive(ModUtils.GetUnlockByName(UnlockNames.BSMMultiBlastII).uniqueId)) multiplier = 4f;
                    if (TechTreeState.instance.IsUnlockActive(ModUtils.GetUnlockByName(UnlockNames.BSMMultiBlastIII).uniqueId)) multiplier = 6f;
                    if (TechTreeState.instance.IsUnlockActive(ModUtils.GetUnlockByName(UnlockNames.BSMMultiBlastIV).uniqueId)) multiplier = 8f;
                    if (TechTreeState.instance.IsUnlockActive(ModUtils.GetUnlockByName(UnlockNames.BSMMultiBlastV).uniqueId)) multiplier = 10f;
                    if (isIng) {
                        return $"<={multiplier * recipe.ingQuantities[index] * 5}/m";
                    }
                    else {
                        return $"<={multiplier * recipe.outputQuantities[index] * 5}/m";
                    }

                case CraftingMethod.Thresher:
                    craftingEfficiency = useMk2 ? 2f : 1f;
                    if (isIng) {
                        return $"{recipe.ingQuantities[index] * ThresherInstance.thresherSpeedMultiplier * craftingEfficiency / recipe.duration * 60f}/m";
                    }
                    else {
                        return $"{recipe.outputQuantities[index] * ThresherInstance.thresherSpeedMultiplier * craftingEfficiency / recipe.duration * 60f}/m";
                    }
                    break;

                case CraftingMethod.Planter:
                    if (IsUsingPlanterCoreClusters()) {
                        return "2/m";
                    }
                    return "2/m";
            }

            return "?/m";
        }

        private bool IsUsingPlanterCoreClusters() {
            string pluginsFolder = AppDomain.CurrentDomain.BaseDirectory + "BepInEx/plugins";
            Debug.Log(pluginsFolder);
            string[] files = Directory.GetFiles(pluginsFolder);
            foreach (string file in files) {
                if (file.Contains("PlanterCoreClusters")) {
                    return true;
                }
            }

            return false;
        }
    }
}
