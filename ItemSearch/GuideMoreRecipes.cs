using System;
using SpikysLib;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;


public sealed class GuideMoreRecipes : ModPlayer {

    public override void Load() {
        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
        On_ItemSlot.OverrideLeftClick += HookValidateShiftClick;
        GuideRecipeFiltering.AddGuideItemFilter(r => Configs.BetterGuide.MoreRecipes && r.HasResult(Main.guideItem.type));
    }

    private static int HookAllowGuideItem(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if (Configs.BetterGuide.MoreRecipes && context == ContextID.GuideItem && slot == 0) return 0;
        return orig(inv, context, slot, checkItem);
    }
    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if(!Configs.BetterGuide.MoreRecipes || !Main.InGuideCraftMenu || !ItemSlot.ShiftInUse) return false;
        if(inventory[slot].IsAir || inventory[slot].favorited) return false;
        if(Array.IndexOf(PlayerHelper.InventoryContexts, context) == -1) return false;
        Main.cursorOverride = CursorOverrideID.InventoryToChest;
        return true;
    }
    private bool HookValidateShiftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if(!Configs.BetterGuide.MoreRecipes || context == 0 || Main.cursorOverride != CursorOverrideID.InventoryToChest) return orig(inv, context, slot);
        bool res = orig(inv, context, slot);
        if (ItemSlot.PickItemMovementAction(inv, context, slot, inv[slot]) == -1) Main.LocalPlayer.GetDropItem(ref inv[slot]);
        return res;
    }
}
