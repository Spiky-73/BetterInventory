using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;


public sealed class GuideMoreRecipes : ModPlayer {

    public override void Load() {
        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
        GuideRecipeFiltering.AddGuideItemFilter(r => Configs.BetterGuide.MoreRecipes && r.HasResult(Main.guideItem.type));
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if(!Configs.BetterGuide.MoreRecipes || !Main.InGuideCraftMenu || !ItemSlot.ShiftInUse) return false;
        if(inventory[slot].IsAir || inventory[slot].favorited) return false;
        // TODO re-add contexts 
        // if(Array.IndexOf(PlayerHelper.InventoryContexts, context) == -1) return false;
        if(context != 0) return false;
        Main.cursorOverride = CursorOverrideID.InventoryToChest;
        return true;
    }

    private static int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (Configs.BetterGuide.MoreRecipes && context == ContextID.GuideItem && slot == 0) return 0;
        return orig(inv, context, slot, checkItem);
    }
}
