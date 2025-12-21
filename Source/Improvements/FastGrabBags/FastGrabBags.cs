using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Improvements.FastGrabBags;

public sealed class FastGrabBags : ModPlayer {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || ImprovementsConfig.FastGrabBags;
    public override void Load() {
        On_ItemSlot.TryOpenContainer += HookTryOpenContainer;
        On_Player.DropItemFromExtractinator += HookFastExtractinator;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!FastGrabBagsConfig.FastContainerOpening) return;
        if (Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    private static void HookTryOpenContainer(On_ItemSlot.orig_TryOpenContainer orig, Item item, Player player) {
        if (!FastGrabBagsConfig.FastContainerOpening) {
            orig(item, player);
            return;
        }
        int split = Main.stackSplit;
        for (int i = 0; i < Main.superFastStack + 1; i++) orig(item, player);
        Main.stackSplit = split;
        ItemSlot.RefreshStackSplitCooldown();
    }
    private static void HookFastExtractinator(On_Player.orig_DropItemFromExtractinator orig, Player self, int itemType, int stack) {
        orig(self, itemType, stack);
        if (!FastGrabBagsConfig.FastExtractinator || self.ItemTimeIsZero) return;
        ItemSlot.RefreshStackSplitCooldown();
        self.itemTime = self.itemTimeMax = Main.stackSplit - 1;
    }
}