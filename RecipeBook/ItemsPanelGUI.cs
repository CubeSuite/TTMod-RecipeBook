using CasperEquinoxGUI;
using CasperEquinoxGUI.Controls;
using CasperEquinoxGUI.Layouts;
using CasperEquinoxGUI.Utilities;
using EquinoxsModUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolBuddy.ThirdParty.VectorGraphics;
using UnityEngine;
using UnityEngine.Rendering;

namespace RecipeBook
{
    public static class ItemsPanelGUI
    {
        public static Window itemsWindow;
        private static Textbox searchBar;
        private static WrapPanel itemsPanel;

        public static List<string> itemsToShow;
        public static bool isOpen = false;
        public static float sSinceOpen = 0;

        // Public Functions

        public static void CreateItemsPanel() {
            Grid grid = new Grid(1, 2, new string[] { "equal" }, new string[] { "60", "equal" });

            searchBar = new Textbox() { 
                Hint = "Search..." ,
                Margin = new Thickness(0, 0, 0, 10)
            };

            searchBar.TextChanged += OnSearchBarTextChanged;

            grid.AddControl(searchBar);

            itemsPanel = new WrapPanel() { RowIndex = 1 };
            DrawItemButtons();

            grid.AddControl(itemsPanel);

            itemsWindow = new Window() {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(20),
                Visible = false,
                Title = "Recipe Book",
                ShowShader = false,
                ShowCloseButton = false,
                RootLayout = grid,
                Width = RecipeBookPlugin.itemsPanelWidth.Value
            };

            CaspuinoxGUI.RegisterWindow(ref itemsWindow);
        }

        public static void Show() {
            InputHandler.instance.uiInputBlocked = true;
            sSinceOpen = 0;
            isOpen = true;
            itemsWindow?.Show();
        }

        public static void Hide() {
            if(InputHandler.instance != null) InputHandler.instance.uiInputBlocked = false;
            isOpen = false;
            itemsWindow?.Hide();
        }

        // Events

        private static void OnSearchBarTextChanged(object sender, EventArgs e) {
            itemsToShow = EMU.Names.Resources.SafeResources.Where(name => 
                name.ToLower().Contains(searchBar.Input.ToLower()) && 
                TechTreeState.instance.IsResourceKnown(EMU.Resources.GetResourceInfoByName(name))
            ).ToList();

            itemsPanel.ClearChildren();
            DrawItemButtons();
        }

        public static void OnResourceButtonLeftClicked(object sender, EventArgs e) {
            ResourceButton clickedButton = sender as ResourceButton;
            RecipesGUI.ShowRecipesForResource(clickedButton.Resource);
        }

        public static void OnResourceButtonRightClicked(object sender, EventArgs e) {
            ResourceButton clickedButton = sender as ResourceButton;
            RecipesGUI.ShowUsesForResource(clickedButton.Resource);
        }

        private static void DrawItemButtons() {
            foreach (string item in itemsToShow ?? EMU.Names.Resources.SafeResources) {
                ResourceButton itemButton = new ResourceButton(item) {
                    ImageWidth = RecipeBookPlugin.itemImageSize.Value,
                    ImageHeight = RecipeBookPlugin.itemImageSize.Value,
                    Margin = new Thickness(5),
                    Padding = new Thickness(5)
                };

                itemButton.LeftClicked += OnResourceButtonLeftClicked;
                itemButton.RightClicked += OnResourceButtonRightClicked;

                itemsPanel.AddControl(itemButton);
            }
        }
    }
}
