using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RecipeBook.Patches
{
    internal class InventoryAndCraftingUIPatch
    {
        [HarmonyPatch(typeof(InventoryAndCraftingUI), nameof(InventoryAndCraftingUI.OnOpen))]
        [HarmonyPrefix]
        static void ShowItemsPanel() {
            ItemsPanelGUI.Show();
        }

        [HarmonyPatch(typeof(InventoryAndCraftingUI), nameof(InventoryAndCraftingUI.OnClose))]
        [HarmonyPrefix]
        internal static void HideItemsPanel() {
            ItemsPanelGUI.Hide();
            RecipesGUI.Hide();
        }
    }
}