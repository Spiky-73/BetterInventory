using Microsoft.Xna.Framework;
using SpikysLib;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

// BUG items not counted when manually placing items into guide slots
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

    private static bool CheckGuideTileFilter(Recipe recipe) {
        if (guideTile.IsAir) return true;
        if (guideTile.TryGetGlobalItem(out PlaceholderItem placeholder)) {
            if (placeholder.tile == PlaceholderItem.ByHandTile) return recipe.requiredTile.Count == 0;
            if (placeholder.tile >= 0) return recipe.requiredTile.Contains(placeholder.tile);
            if (placeholder.condition is not null) return recipe.Conditions.Exists(c => c.Description.Key == placeholder.condition.Key);
        }
        
        return CraftingStationsItems.ContainsKey(guideTile.createTile) ?
            recipe.requiredTile.Contains(guideTile.createTile) :
            recipe.Conditions.Exists(c => PlaceholderItem.ConditionItems.TryGetValue(c.Description.Key, out int type) && type == guideTile.type);
    }


    private static int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (!Configs.BetterGuide.Enabled || context != ContextID.GuideItem) return orig(inv, context, slot, checkItem);
        return slot switch {
            0 when (!Main.mouseItem.IsAPlaceholder()) && (Configs.BetterGuide.MoreRecipes || orig(inv, context, slot, checkItem) != -1) => 0,
            1 when Configs.BetterGuide.GuideTile && (checkItem.IsAir || FitsCraftingTile(Main.mouseItem)) => 0,
            _ => -1,
        };
    }

    public static void dropGuideTileCheck(Player self) {
        if (Main.InGuideCraftMenu || guideTile.IsAir) return;
        if (guideTile.IsAPlaceholder()) guideTile.TurnToAir();
        else self.GetDropItem(ref guideTile);
    }


    public static Item guideTile = new();
    private static GuideRecipeFilterGroup _guideTileFilters = null!;
}
