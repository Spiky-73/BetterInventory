using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;

// BUG ??? DrawGuideCraftText when disabled
public sealed partial class Guide : ModSystem {
    private static void DrawRequiredTiles(int inventoryX, int inventoryY) {
        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];

        // Update if needed
        if (s_displayedRecipe != (Main.numAvailableRecipes == 0 ? -1 : recipe.RecipeIndex)) UpdateRequiredTiles(recipe);

        // Handles the position of the condition to displays
        float minX = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + TileSpacingRatio);
        Vector2 delta = new Vector2(TextureAssets.InventoryBack.Width() * (TileScale + TileSpacingRatio), -TextureAssets.InventoryBack.Height() * (TileScale + TileSpacingRatio)) * Main.inventoryScale;
        Vector2 position = new(minX, inventoryY - delta.Y);
        int slot = 0;
        void MovePosition() {
            if (Configs.FixedUI.Wrapping && ++slot % TilesPerLine == 0) {
                position.X = minX;
                position.Y += delta.Y;
                if (Configs.BetterGuide.GuideTile && slot == TilesPerLine) MovePosition(); // Skip the position of guideTile if it is enabled
            } else position.X += delta.X;
        }

        Main.inventoryScale *= TileScale;

        // Display crafting stations
        for (int i = 0; i < displayedRecipeTiles.Count; i++) {
            Item tile = displayedRecipeTiles[i];

            // Draw the tile
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(s_tileTextures, false, !Configs.BetterGuide.AvailableRecipes || GetPlaceholderType(tile) == PlaceholderType.ByHand || Main.LocalPlayer.adjTile[tile.createTile]);
            ItemSlot.Draw(Main.spriteBatch, ref tile, ContextID.CraftingMaterial, position);
            TextureAssets.InventoryBack4 = s_inventoryBack4;
            Main.inventoryBack = inventoryBack;

            // Handle mouse hover
            Rectangle hitbox = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
                ItemSlot.MouseHover(ref tile, ContextID.CraftingMaterial);
            }

            MovePosition();
        }

        // Display conditions
        for (int i = 0; i < displayedRecipeConditions.Count; i++) {
            (Item item, Condition condition) = displayedRecipeConditions[i];

            // Draw the condition
            Color inventoryBack = Main.inventoryBack;
            OverrideRecipeTexture(s_conditionTextures, false, condition.Predicate());
            ItemSlot.Draw(Main.spriteBatch, ref item, ContextID.CraftingMaterial, position);
            Main.inventoryBack = inventoryBack;
            TextureAssets.InventoryBack4 = s_inventoryBack4;

            // Handle mouse hover
            Rectangle hitbox = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
                forcedTooltip = condition.Description;
                ItemSlot.MouseHover(ref item, ContextID.CraftingMaterial);
            }

            MovePosition();
        }

        Main.inventoryScale /= TileScale;
    }
    private static void UpdateRequiredTiles(Recipe recipe) {
        displayedRecipeTiles.Clear();
        displayedRecipeConditions.Clear();

        // Skip if there is no recipe to display
        if (Main.numAvailableRecipes == 0 || IsUnknown(Main.availableRecipe[Main.focusRecipe])) {
            s_displayedRecipe = -1;
            return;
        }
        s_displayedRecipe = recipe.RecipeIndex;

        // Updates crafting stations
        if (recipe.requiredTile.Count == 0) displayedRecipeTiles.Add(ByHandPlaceholder);
        else {
            for (int i = 0; i < recipe.requiredTile.Count && recipe.requiredTile[i] != -1; i++) {
                if (CraftingStationsItems.TryGetValue(recipe.requiredTile[i], out int type) && type != ItemID.None) displayedRecipeTiles.Add(new(type));
                else displayedRecipeTiles.Add(TilePlaceholder(recipe.requiredTile[i]));
            }
        }

        // Updates conditions
        foreach (Condition condition in recipe.Conditions) {
            Item item;
            if (ConditionItems.TryGetValue(condition.Description.Key, out int type)) item = new(type);
            else item = ConditionPlaceholder(condition);
            displayedRecipeConditions.Add((item, condition));
        }
    }
    
    private static int s_displayedRecipe = -1;
    internal static readonly List<Item> displayedRecipeTiles = [];
    internal static readonly List<(Item item, Condition condition)> displayedRecipeConditions = [];

    private static TextureHighlight s_tileTextures = null!;
    private static TextureHighlight s_conditionTextures = null!;

    public const int TilesPerLine = 7;
    public const float TileScale = 0.46f;
    public const float TileSpacingRatio = 0.08f;
}
