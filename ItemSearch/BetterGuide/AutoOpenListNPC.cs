using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch.BetterGuide;

public sealed class AutoOpenListNPC : GlobalNPC {
    public override void Load() {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookOpenOnClick;
    }

    private void HookOpenOnClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.BetterGuide.AutoOpenList || !(Main.mouseLeftRelease && Main.mouseLeft)) {
            orig(inv, context, slot);
            return;
        }
        if(ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == 0) Main.recBigList = true;
        if(Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) Main.recBigList = true;
        orig(inv, context, slot);
    }

    public override void OnChatButtonClicked(NPC npc, bool firstButton) {
        if (!Configs.BetterGuide.AutoOpenList || npc.type != NPCID.Guide) return;
        Main.InGuideCraftMenu = true;
        Main.recBigList = true;
        Utility.FindDisplayedRecipes();
    }
}