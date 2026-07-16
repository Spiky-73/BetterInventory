using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class FasterGrabBagsPlayer : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.FasterGrabBags;
    public override void Load() {
        On_ItemSlot.TryOpenContainer += HookTryOpenContainer;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!BetterInventoryManagementConfig.FasterGrabBags) return;
        if (Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    private static void HookTryOpenContainer(On_ItemSlot.orig_TryOpenContainer orig, Item item, Player player) {
        if (!BetterInventoryManagementConfig.FasterGrabBags) {
            orig(item, player);
            return;
        }
        int split = Main.stackSplit;
        for (int i = 0; i < Main.superFastStack + 1; i++) orig(item, player);
        Main.stackSplit = split;
        ItemSlot.RefreshStackSplitCooldown();
    }
}