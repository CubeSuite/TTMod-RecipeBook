using CasperEquinoxGUI;
using CasperEquinoxGUI.Controls;
using CasperEquinoxGUI.Layouts;
using CasperEquinoxGUI.Utilities;
using EquinoxsModUtils;
using RecipeBook.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using ToolBuddy.ThirdParty.VectorGraphics;
using UnityEngine;

namespace RecipeBook
{
    public static class RecipesGUI
    {
        // Members
        public static Window recipesWindow;
        public static StackPanel recipesPanel;

        private static RadioButton mk1RadioButton;
        private static RadioButton mk2RadioButton;
        private static RadioButton mk3RadioButton;

        private static ResourceInfo currentResource;
        private static bool showingRecipes;

        // Properties

        public static bool UseMk1Machines => mk1RadioButton.IsChecked;
        public static bool UseMk2Machines => mk2RadioButton.IsChecked;
        public static bool UseMk3Machines => mk3RadioButton.IsChecked;

        // Public Functions

        public static void CreateRecipesWindow() {
            Grid grid = new Grid(1, 2, "equal", new string[] { "60", "equal" });

            grid.AddControl(CreateMachineMkGUI());

            recipesPanel = new StackPanel() { 
                Orientation = Orientation.Vertical,
                Margin = new Thickness(40, 0, 20 , 0),
                RowIndex = 1
            }; 

            grid.AddControl(recipesPanel);

            recipesWindow = new Window() {
                RootLayout = grid,
                Visible = false,
                Width = RecipeBookPlugin.recipesPanelWidth.Value,
                Height = RecipeBookPlugin.recipesPanelHeight.Value,
                Margin = new Thickness(0, RecipeBookPlugin.recipesPanelTopMargin.Value, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            CaspuinoxGUI.RegisterWindow(ref recipesWindow);
        }

        public static void ShowRecipesForResource(ResourceInfo resource) {
            currentResource = resource;
            showingRecipes = true;
            recipesPanel.ClearChildren();
            
            recipesWindow.Title = $"Recipes For {resource.displayName}";
            recipesWindow.Visible = true;

            List<SchematicsRecipeData> recipes = GameDefines.instance.schematicsRecipeEntries.Where(recipe => recipe.outputTypes.Contains(resource)).ToList();
            if (RecipeBookPlugin.filterUnknown.Value) {
                recipes = recipes.Where(recipe => TechTreeState.instance.IsRecipeKnown(recipe)).ToList();
            }

            foreach(SchematicsRecipeData recipe in recipes) {
                recipesPanel.AddControl(new RecipePanel(recipe) { Margin = new Thickness(10, 5) });
            }
        }

        public static void ShowUsesForResource(ResourceInfo resource) {
            currentResource = resource;
            showingRecipes = false;
            recipesPanel.ClearChildren();

            recipesWindow.Title = $"Uses For {resource.displayName}";
            recipesWindow.Visible = true;

            List<SchematicsRecipeData> uses = GameDefines.instance.schematicsRecipeEntries.Where(recipe => recipe.ingTypes.Contains(resource)).ToList();
            if (RecipeBookPlugin.filterUnknown.Value) {
                uses = uses.Where(recipe => TechTreeState.instance.IsRecipeKnown(recipe)).ToList();
            }

            foreach(SchematicsRecipeData use in uses) {
                recipesPanel.AddControl(new RecipePanel(use) { Margin = new Thickness(10, 5) });
            }
        }

        public static void Hide() {
            if (recipesWindow == null) return;
            recipesWindow.Visible = false;
        }

        public static string GetRateStringForItem(SchematicsRecipeData recipe, int index, bool isIng) {
            switch (recipe.craftingMethod) {
                case CraftingMethod.Assembler: return GetRateStringForAssembler(recipe, index, isIng);
                case CraftingMethod.Smelter: return GetRateStringForSmelter(recipe, index, isIng);
                case CraftingMethod.BlastSmelter: return GetRateStringForBlastSmelter(recipe, index, isIng);
                case CraftingMethod.Thresher: return GetRateStringForThresher(recipe, index, isIng);
                case CraftingMethod.Planter: return GetRateStringForPlanter(recipe, index, isIng);
                case CraftingMethod.Crusher: return GetRateStringForCrusher(recipe, index, isIng);
            }

            return "?/m";
        }

        public static string GetMadeInString(SchematicsRecipeData recipe) {
            switch (recipe.craftingMethod) {
                case CraftingMethod.BlastSmelter: return "Made in: Blast Smelter";
                default: return $"Made in: {recipe.craftingMethod}";
            }
        }

        // Events

        public static void OnMachineMkChanged(object sender, EventArgs e) {
            if (showingRecipes) ShowRecipesForResource(currentResource);
            else ShowUsesForResource(currentResource);
        }

        // Private Functions

        private static Grid CreateMachineMkGUI() {
            Grid grid = new Grid(4, 1, "equal", "equal");

            grid.AddControl(new TextBlock() {
                Text = "Machine Mk:",
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            mk1RadioButton = new RadioButton(RecipeBookPlugin.MyGUID, "MachineMk") { 
                Label = "Mk 1", 
                VerticalAlignment = VerticalAlignment.Center, 
                ColumnIndex = 1, 
                IsChecked = RecipeBookPlugin.defaultMachineMk.Value == 1 
            };

            mk2RadioButton = new RadioButton(RecipeBookPlugin.MyGUID, "MachineMk") { 
                Label = "Mk 2", 
                VerticalAlignment = VerticalAlignment.Center, 
                ColumnIndex = 2,
                IsChecked = RecipeBookPlugin.defaultMachineMk.Value == 2
            };
            
            mk3RadioButton = new RadioButton(RecipeBookPlugin.MyGUID, "MachineMk") { 
                Label = "Mk 3", 
                VerticalAlignment = VerticalAlignment.Center, 
                ColumnIndex = 3,
                IsChecked = RecipeBookPlugin.defaultMachineMk.Value == 3
            };

            mk1RadioButton.IsCheckedChanged += OnMachineMkChanged;
            mk2RadioButton.IsCheckedChanged += OnMachineMkChanged;
            mk3RadioButton.IsCheckedChanged += OnMachineMkChanged;

            grid.AddControl(mk1RadioButton);
            grid.AddControl(mk2RadioButton);
            grid.AddControl(mk3RadioButton);

            return grid;
        }

        private static string GetRateStringForAssembler(SchematicsRecipeData recipe, int index, bool isIng) {
            float craftingEfficiency = ((AssemblerDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.Assembler)).runtimeSettings.craftingEfficiency;
            if(UseMk2Machines || UseMk3Machines) craftingEfficiency = ((AssemblerDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.AssemblerMKII)).runtimeSettings.craftingEfficiency;

            float quantity = isIng ? recipe.runtimeIngQuantities[index] : recipe.outputQuantities[index];
            if (!isIng) quantity *= 2;
            if (!isIng && (UseMk2Machines || UseMk3Machines)) quantity *= 2;

            float rate = quantity * AssemblerInstance.assemblerSpeedMultiplier * craftingEfficiency / recipe.duration * 60f;
            return $"{rate:0.##}/m";
        }

        private static string GetRateStringForSmelter(SchematicsRecipeData recipe, int index, bool isIng) {
            float craftingEfficiency = ((SmelterDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.Smelter)).runtimeSettings.craftingEfficiency;
            if (UseMk2Machines) craftingEfficiency = ((SmelterDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.SmelterMKII)).runtimeSettings.craftingEfficiency;
            if (UseMk3Machines) craftingEfficiency = craftingEfficiency = ((SmelterDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.SmelterMKIII)).runtimeSettings.craftingEfficiency;

            float quantity = isIng ? recipe.runtimeIngQuantities[index] : recipe.outputQuantities[index];
            float rate = quantity * SmelterInstance.smelterSpeedMultiplier * craftingEfficiency / recipe.duration * 60f;

            return $"{rate:0.##}/m";
        }

        private static string GetRateStringForBlastSmelter(SchematicsRecipeData recipe, int index, bool isIng) {
            float multiplier = 1;
            if (TechTreeState.instance.IsUnlockActive(EMU.Unlocks.GetUnlockByName(EMU.Names.Unlocks.BSMMultiBlastII).uniqueId)) multiplier = 4f;
            if (TechTreeState.instance.IsUnlockActive(EMU.Unlocks.GetUnlockByName(EMU.Names.Unlocks.BSMMultiBlastIII).uniqueId)) multiplier = 6f;
            if (TechTreeState.instance.IsUnlockActive(EMU.Unlocks.GetUnlockByName(EMU.Names.Unlocks.BSMMultiBlastIV).uniqueId)) multiplier = 8f;
            if (TechTreeState.instance.IsUnlockActive(EMU.Unlocks.GetUnlockByName(EMU.Names.Unlocks.BSMMultiBlastV).uniqueId)) multiplier = 10f;
            
            float quantity = isIng ? recipe.runtimeIngQuantities[index] : recipe.outputQuantities[index];
            float rate = multiplier * quantity * 5;
            return $"<={rate}/m";
        }

        private static string GetRateStringForThresher(SchematicsRecipeData recipe, int index, bool isIng) {
            float craftingEfficiency = ((ThresherDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.Thresher)).runtimeSettings.craftingEfficiency;
            if(UseMk2Machines || UseMk3Machines) craftingEfficiency = ((ThresherDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.ThresherMKII)).runtimeSettings.craftingEfficiency;

            float quantity = isIng ? recipe.runtimeIngQuantities[index] : recipe.outputQuantities[index];
            float rate = quantity * ThresherInstance.thresherSpeedMultiplier * craftingEfficiency / recipe.duration * 60f;
            return $"{rate:0.##/m}";
        }

        private static string GetRateStringForPlanter(SchematicsRecipeData recipe, int index, bool isIng) {
            float craftingEfficiency = ((PlanterDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.Planter)).runtimeSettings.craftingEfficiency;
            if(UseMk2Machines || UseMk3Machines) craftingEfficiency = ((PlanterDefinition)EMU.Resources.GetResourceInfoByName(EMU.Names.Resources.PlanterMKII)).runtimeSettings.craftingEfficiency;
            float rate = 240 * craftingEfficiency / recipe.duration;
            return $"{rate:0.##}/m";
        }

        private static string GetRateStringForCrusher(SchematicsRecipeData recipe, int index, bool isIng) {
            float quantity = isIng ? recipe.runtimeIngQuantities[index] : recipe.outputQuantities[index];
            float rate = quantity / recipe.duration * 60f;
            return $"{rate:0.##/m}";
        }
    }
}
