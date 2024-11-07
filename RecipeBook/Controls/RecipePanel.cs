using CasperEquinoxGUI;
using CasperEquinoxGUI.Controls;
using CasperEquinoxGUI.Layouts;
using CasperEquinoxGUI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RecipeBook.Controls
{
    public class RecipePanel : Panel
    {
        // Members
        private SchematicsRecipeData recipe;
        private ModImage background;
        private Grid grid;

        // Constructors

        public RecipePanel(SchematicsRecipeData recipe) {
            ShowBackground = false;
            HorizontalAlignment = HorizontalAlignment.Center;
            this.recipe = recipe;

            GetBackgroundAndGrid();

            for (int i = 0; i < recipe.ingTypes.Length; i++) DrawIngredient(i);
            for (int i = 0; i < recipe.outputTypes.Length; i++) DrawOutput(i);

            grid.AddControl(new TextBlock() {
                Text = RecipesGUI.GetMadeInString(recipe),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ColumnIndex = 2,
                Margin = new Thickness(0, 10, 0, 0)
            });

            Layout = grid;
        }

        // Private Functions

        private void GetBackgroundAndGrid() {
            string[] columns = new string[] { "10", "50", "equal", "50", "10" };
            string[] oneRows = new string[] { "10", "26", "26", "50", "10" };
            string[] twoRows = new string[] { "10", "26", "26", "50", "26", "50", "10" };
            string[] threeRows = new string[] { "10", "26", "26", "50", "26", "50", "26", "50", "10" };

            switch (recipe.ingTypes.Length) {
                case 1:
                    switch (recipe.outputTypes.Length) {
                        case 1:
                            background = Images.oneToOne;
                            grid = new Grid(5, 5, columns, oneRows);
                            break;
                        case 2:
                            background = Images.oneToTwo;
                            grid = new Grid(5, 7, columns, twoRows);
                            break;
                        case 3:
                            background = Images.oneToThree;
                            grid = new Grid(5, 9, columns, threeRows);
                            break;
                    }
                    break;
                case 2:
                    switch (recipe.outputTypes.Length) {
                        case 1:
                            background = Images.twoToOne;
                            grid = new Grid(5, 7, columns, twoRows);
                            break;
                        case 2:
                            background = Images.twoToTwo;
                            grid = new Grid(5, 7, columns, twoRows);
                            break;
                        case 3:
                            background = Images.twoToThree;
                            grid = new Grid(5, 9, columns, threeRows);
                            break;
                    }
                    break;
                case 3:
                    switch (recipe.outputTypes.Length) {
                        case 1:
                            background = Images.threeToOne;
                            grid = new Grid(5, 9, columns, threeRows);
                            break;
                        case 2:
                            background = Images.threeToTwo;
                            grid = new Grid(5, 9, columns, threeRows);
                            break;
                        case 3:
                            background = Images.threeToThree;
                            grid = new Grid(5, 9, columns, threeRows);
                            break;
                    }
                    break;
            }

            //UpdateRects(); 
        }

        private void DrawIngredient(int index) {
            ResourceButton button = new ResourceButton(recipe.ingTypes[index].displayName) {
                Padding = new Thickness(5),
                ImageWidth = 40,
                ImageHeight = 40,
                ColumnIndex = 1,
                RowIndex = (uint)((2 * index) + 3)
            };

            button.LeftClicked += ItemsPanelGUI.OnResourceButtonLeftClicked;
            button.RightClicked += ItemsPanelGUI.OnResourceButtonRightClicked;

            grid.AddControl(button);

            TextBlock rateLabel = new TextBlock() {
                Text = RecipesGUI.GetRateStringForItem(recipe, index, true),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ColumnIndex = 1,
                RowIndex = (uint)((2 * index) + 2)
            };

            grid.AddControl(rateLabel);

        }

        private void DrawOutput(int index) {
            ResourceButton button = new ResourceButton(recipe.outputTypes[index].displayName) {
                Padding = new Thickness(5),
                ImageWidth = 40,
                ImageHeight = 40,
                ColumnIndex = 3,
                RowIndex = (uint)((2 * index) + 3)
            };

            button.LeftClicked += ItemsPanelGUI.OnResourceButtonLeftClicked;
            button.RightClicked += ItemsPanelGUI.OnResourceButtonRightClicked;

            grid.AddControl(button);

            TextBlock rateLabel = new TextBlock() {
                Text = RecipesGUI.GetRateStringForItem(recipe, index, false),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ColumnIndex = 3,
                RowIndex = (uint)((2 * index) + 2)
            };

            grid.AddControl(rateLabel);
        }

        // Overrides
         
        public override void Draw() {
            background.Draw(ContentRectFloat.x, ContentRectFloat.y);
            base.Draw();
        }

        protected override int CalculateContentWidth() {
            if (background == null) return 366;
            return (int)background.width;
        }

        protected override int CalculateContentHeight() {
            if (background == null) return 10;
            return (int)background.height;
        }
    }
}
