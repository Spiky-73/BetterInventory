using System;
using SpikysLib;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

// BUG items not counted when manually placing items into guide slots
namespace BetterInventory.ItemSearch;


public sealed class GuideMoreRecipes : ModPlayer {

    public override void Load() {
        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
        GuideRecipeFiltering.AddGuideItemFilter(r => Configs.BetterGuide.MoreRecipes && r.HasResult(Main.guideItem.type));
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        // Allow any item into guideItem -including from ammo / coin- slots if it could be placed in it
        if (Configs.BetterGuide.MoreRecipes && ItemSlot.ShiftInUse && !inventory[slot].favorited
        && Main.InGuideCraftMenu && Array.IndexOf(PlayerHelper.InventoryContexts, context) != -1 && !inventory[slot].IsAir
        && ItemSlot.PickItemMovementAction(inventory, ContextID.GuideItem, 0, inventory[slot]) == 0) {
            Main.cursorOverride = CursorOverrideID.InventoryToChest;
            return true;
        }
        return false;
    }

    private static int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (!Configs.BetterGuide.Enabled || context != ContextID.GuideItem) return orig(inv, context, slot, checkItem);
        return slot switch {
            0 when (!Main.mouseItem.IsAPlaceholder()) && (Configs.BetterGuide.MoreRecipes || orig(inv, context, slot, checkItem) != -1) => 0,
            1 when Configs.BetterGuide.GuideTile && (checkItem.IsAir || GuideGuideTile.FitsCraftingTile(Main.mouseItem)) => 0,
            _ => -1,
        };
    }
}
