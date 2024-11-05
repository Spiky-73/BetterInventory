using BetterInventory.ItemSearch;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib.UI.Elements;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.Crafting.UI;

public sealed class RecipeFiltersCanvas : UIState {
    public UIFlexGrid filterList = null!;

    public sealed override void OnInitialize() {
        filterList = new() {
            ListPadding = 6,
            ItemWidth = 14
        };
        for (int i = 1; i < 11; i++) {
            UIImageFramed testI = new(RecipeFiltering.recipeFilters, RecipeFiltering.recipeFilters.Frame(horizontalFrames: 11, frameX: i, sizeOffsetX: -2));
            filterList.Add(testI);
        }

        Append(filterList);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);
        if (filterList.IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
    }

    public void RebuildRecipeGrid() {
        static void OnFilterChanges() {
            if (Configs.BetterGuide.AvailableRecipes) Guide.FindGuideRecipes();
            else Recipe.FindRecipes();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        filterList.Clear();
        filterList.Height.Pixels = filterList.GetTotalHeight();
        filterList.Width.Pixels = filterList.GetTotalHeight();


        EntryFilterer<Item, IRecipeFilter> filters = RecipeFiltering.LocalFilters.Filterer;
        for (int i = 0; i < filters.AvailableFilters.Count; i++) {
            IRecipeFilter filter = filters.AvailableFilters[i];
            bool active = filters.IsFilterActive(i);
            int recipeCount = RecipeFiltering.LocalFilters.RecipeInFilter[i];
            bool available = recipeCount > 0;

            string text = Language.GetTextValue(filter.GetDisplayNameKey());
            if (available || active) {
                text = Language.GetTextValue($"{Localization.Keys.UI}.Filter", text, recipeCount);
            } else if (Configs.RecipeFilters.Value.hideUnavailable) continue;
            if (!available) text = $"[c/{Colors.RarityTrash.Hex3()}:{text}]";

            HoverImageFramed item = new(available ? filter.GetSource() : filter.GetSourceGray(), filter.GetSourceFrame(), text);
            if (!active) item.Color = item.Color.MultiplyRGBA(new(80, 80, 80, 70));
            item.OnLeftClick += (_, _) => {
                bool keepOn = !active || filters.ActiveFilters.Count > 1;
                filters.ActiveFilters.Clear();
                if (keepOn) filters.ActiveFilters.Add(filter);
                OnFilterChanges();
            };
            int number = i;
            item.OnRightClick += (_, _) => {
                filters.ToggleFilter(number);
                OnFilterChanges();
            };
            filterList.Add(item);
        }
        Recalculate();
    }

}