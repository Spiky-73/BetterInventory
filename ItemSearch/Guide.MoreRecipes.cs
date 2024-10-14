using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SpikysLib;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;


public sealed partial class Guide : ModSystem {

    private static void DrawGuideTile(int inventoryX, int inventoryY) {
        float x = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileSpacingRatio);
        float y = inventoryY;
        Main.inventoryScale *= TileScale;
        Item[] items = GuideItems;

        // Handle Mouse hover
        Rectangle hitbox = new((int)x, (int)y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
        if (!s_visibilityHover && hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.player[Main.myPlayer].mouseInterface = true;
            Main.craftingHide = true;
            ItemSlot.OverrideHover(items, ContextID.GuideItem, 1);
            ItemSlot.LeftClick(items, ContextID.GuideItem, 1);
            GuideItems = items; // Update items if changed
            if (Main.mouseLeftRelease && Main.mouseLeft) Recipe.FindRecipes();
            ItemSlot.RightClick(items, ContextID.GuideItem, 1);
            ItemSlot.MouseHover(items, ContextID.GuideItem, 1);
            GuideItems = items; // Update items if changed
        }
        ItemSlot.Draw(Main.spriteBatch, items, ContextID.GuideItem, 1, hitbox.TopLeft());
        Main.inventoryScale /= TileScale;
    }

    private static void ILMoreGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);

        Utility.GotoRecipeDisabled(cursor, out ILLabel endLoop, out _, out int recipe);

        //     ++ if(<extraRecipe>) {
        //     ++     <addRecipe>
        //     ++     continue;
        //     ++ }
        cursor.EmitLdloc(recipe);
        cursor.EmitDelegate((Recipe recipe) => {
            bool add = false;

            if (Configs.BetterGuide.AvailableRecipes) {
                // Removes non available recipes
                if (!ShowAllRecipes() && !IsAvailable(recipe.RecipeIndex) && GetFavoriteState(recipe.RecipeIndex) != FavoriteState.Favorited) return true;

                // Add all items if no guide item and available recipes
                if (Main.guideItem.IsAir) add = true;
            }

            // Check GuideTile
            if (Configs.BetterGuide.GuideTile) {
                if (!CheckGuideTileFilter(recipe)) return true;
                if (Main.guideItem.IsAir) add = true;
            }

            // Add extra recipes
            if (Configs.BetterGuide.MoreRecipes && recipe.HasResult(Main.guideItem.type)) add = true;

            if (add) Reflection.Recipe.AddToAvailableRecipes.Invoke(recipe.RecipeIndex);
            return add;
        });
        cursor.EmitBrtrue(endLoop);
    }
    private static bool CheckGuideTileFilter(Recipe recipe) {
        return guideTile.IsAir || GetPlaceholderType(guideTile) switch {
            PlaceholderType.ByHand => recipe.requiredTile.Count == 0,
            PlaceholderType.Tile => recipe.requiredTile.Contains(guideTile.createTile),
            PlaceholderType.Condition => recipe.Conditions.Exists(c => c.Description.Key == guideTile.BestiaryNotes[ConditionMark.Length..]),
            _ => guideTile.createTile != -1 ? // Real Item
                recipe.requiredTile.Contains(guideTile.createTile) :
                recipe.Conditions.Exists(c => ConditionItems.TryGetValue(c.Description.Key, out int type) && type == guideTile.type),
        };
    }


    private static int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (!Configs.BetterGuide.Enabled || context != ContextID.GuideItem) return orig(inv, context, slot, checkItem);
        return slot switch {
            0 when (GetPlaceholderType(Main.mouseItem) == PlaceholderType.None) && (Configs.BetterGuide.MoreRecipes || orig(inv, context, slot, checkItem) != -1) => 0,
            1 when Configs.BetterGuide.GuideTile && (checkItem.IsAir || FitsCraftingTile(Main.mouseItem)) => 0,
            _ => -1,
        };
    }

    public static void dropGuideTileCheck(Player self) {
        if (Main.InGuideCraftMenu || guideTile.IsAir) return;
        if (GetPlaceholderType(guideTile) != PlaceholderType.None) guideTile.TurnToAir();
        else self.GetDropItem(ref guideTile);
    }


    public static Item guideTile = new();
}
