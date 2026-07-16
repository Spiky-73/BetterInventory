using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterInventoryManagement;

public sealed class FastGrabBags : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryManagementConfig.FasterExtractinator;
    public void Load(Mod mod) {
        On_Player.DropItemFromExtractinator += HookFastExtractinator;
    }
    public void Unload() { }

    private static void HookFastExtractinator(On_Player.orig_DropItemFromExtractinator orig, Player self, int itemType, int stack) {
        orig(self, itemType, stack);
        if (!BetterInventoryManagementConfig.FasterExtractinator || self.ItemTimeIsZero) return;
        ItemSlot.RefreshStackSplitCooldown();
        self.itemTime = self.itemTimeMax = Main.stackSplit - 1;
    }
}