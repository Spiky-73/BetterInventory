using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using BetterInventory.ItemActions;

namespace BetterInventory.ItemSearch;

// BUG ??? DrawGuideCraftText when disabled
public sealed class GuideRequiredObjectsDisplay : ILoadable {

    public void Load(Mod mod) {
        On_Main.DrawGuideCraftText += HookGuideCraftText;
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookRequiredObjectBackground;
    }

    public void Unload() { }

    private static void HookGuideCraftText(On_Main.orig_DrawGuideCraftText orig, int adjY, Color craftingTipColor, out int inventoryX, out int inventoryY) {
        if (Configs.RecipeTooltip.Enabled) RequiredTooltipItem.OnDrawGuideCraftText();

        if (!Configs.BetterGuide.ConditionsDisplay) {
            orig(adjY, craftingTipColor, out inventoryX, out inventoryY);
            return;
        }
        // Main.guideItem's positions
        inventoryX = 73;
        inventoryY = 331 + adjY;
        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];

        // Update if needed
        if (Main.numAvailableRecipes == 0 || Guide.IsUnknown(recipe.RecipeIndex)) return;
        if (s_displayedRecipe != recipe.RecipeIndex) UpdateRequiredTiles(recipe);

        // Handles the position of the condition to displays
        float minX = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileSpacingRatio);
        Vector2 delta = new Vector2(TextureAssets.InventoryBack.Width() * (TileScale + TileSpacingRatio), -TextureAssets.InventoryBack.Height() * (TileScale + TileSpacingRatio)) * Main.inventoryScale;
        Vector2 position = new(minX, inventoryY - delta.Y);
        int number = 0;
        void MovePosition() {
            if (Configs.FixedUI.Wrapping && ++number % TilesPerLine == 0) {
                position.X = minX;
                position.Y += delta.Y;
                if (Configs.BetterGuide.GuideTile && number == TilesPerLine) MovePosition(); // Skip the position of guideTile if it is enabled
            } else position.X += delta.X;
        }

        Main.inventoryScale *= TileScale;

        void HandleObjectSlot(Item[] inv, int slot) {
            Item tile = inv[slot];

            // Draw the tile
            ItemSlot.Draw(Main.spriteBatch, inv, ContextID.CraftingMaterial, slot, position);

            // Handle mouse hover
            Rectangle hitbox = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
                ItemSlot.MouseHover(ref tile, ContextID.CraftingMaterial);
            }

            if (Configs.FixedUI.Wrapping && ++number % TilesPerLine == 0) {
                position.X = minX;
                position.Y += delta.Y;
                if (Configs.BetterGuide.GuideTile && number == TilesPerLine) position.X += delta.X; // Skip the position of guideTile if it is enabled
            } else position.X += delta.X;
        }

        // Display crafting stations
        for (int i = 0; i < _displayedRecipeTiles.Length; i++) HandleObjectSlot(_displayedRecipeTiles, i);
        for (int i = 0; i < _displayedRecipeConditions.Length; i++) HandleObjectSlot(_displayedRecipeConditions, i);

        Main.inventoryScale /= TileScale;
    }
    private static void UpdateRequiredTiles(Recipe recipe) {
        s_displayedRecipe = recipe.RecipeIndex;

        if (recipe.requiredTile.Count == 0) _displayedRecipeTiles = [PlaceholderItem.FromTile(PlaceholderItem.ByHandTile)];
        else _displayedRecipeTiles = recipe.requiredTile.TakeWhile(t => t != -1).Select(PlaceholderItem.FromTile).ToArray();
        _displayedRecipeConditions = recipe.Conditions.Select(PlaceholderItem.FromCondition).ToArray();
    }

    private void HookRequiredObjectBackground(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        Asset<Texture2D> texture;
        if (inv == _displayedRecipeTiles) texture = TextureAssets.InventoryBack3;
        else if (inv == _displayedRecipeConditions) texture = TextureAssets.InventoryBack12;
        else {
            orig(spriteBatch, inv, context, slot, position, lightColor);
            return;
        }
        (Asset<Texture2D> back, TextureAssets.InventoryBack4) = (TextureAssets.InventoryBack4, texture);
        orig(spriteBatch, inv, context, slot, position, lightColor);
        TextureAssets.InventoryBack4 = back;
    }

    private static int s_displayedRecipe = -1;
    internal static Item[] _displayedRecipeTiles = [];
    internal static Item[] _displayedRecipeConditions = [];

    public const int TilesPerLine = 7;
    public const float TileScale = 0.46f;
    public const float TileSpacingRatio = 0.08f;
}
