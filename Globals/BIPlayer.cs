using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Globals;

public class BetterInventoryPlayer : ModPlayer {
    public override void Load() {
        On_ItemSlot.TryOpenContainer += HookTryOpenContainer;
        
        On_Player.OpenChest += HookOpenChest;
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        ItemSlot.OnItemTransferred += InventoryManagement.Items.OnItemTranfer;
        On_Player.GetItem += HookGetItem;

    }

    public override void Initialize() => ResetEffects();


    public override void SetControls() {
        if (Configs.ClientConfig.Instance.fastRightClick) InventoryManagement.Actions.AttemptFastRightClick();
    }
    public override void ProcessTriggers(TriggersSet triggersSet) {
        InventoryManagement.Actions.ProcessShortcuts(Player);
        if (Configs.ClientConfig.Instance.itemSwap) InventoryManagement.Actions.AttemptItemSwap(Player, triggersSet);
    }

    public override bool PreItemCheck() {
        if (Configs.ClientConfig.Instance.itemRightClick && InventoryManagement.Actions.AttemptItemRightClick(Player)) return false;
        return true;
    }

    public override void PostUpdate() {
        InventoryManagement.Items.PostUpdate(Player);
    }

    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        InventoryManagement.Items.OnSlotLeftClick(slot);
        orig(inv, context, slot);
    }

    private static void HookTryOpenContainer(On_ItemSlot.orig_TryOpenContainer orig, Item item, Player player) {
        int stackSplit = Main.stackSplit;
        orig(item, player);
        if (Configs.ClientConfig.Instance.fastRightClick) InventoryManagement.Actions.FastRightClick(stackSplit);
    }
    private static Item HookGetItem(On_Player.orig_GetItem orig, Player self, int plr, Item newItem, GetItemSettings settings) {
        if (InventoryManagement.Items.SmartPickupEnabled(newItem) && InventoryManagement.Items.OnGetItem(plr, self, ref newItem, settings)) return new();
        return orig(self, plr, newItem, settings);
    }

    private static void HookOpenChest(On_Player.orig_OpenChest orig, Player self, int x, int y, int newChest) {
        orig(self, x, y, newChest);
        InventoryManagement.Items.OnOpenChest(self);
    }
}
