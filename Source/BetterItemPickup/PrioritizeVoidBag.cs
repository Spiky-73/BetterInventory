using Terraria;

namespace BetterInventory.BetterItemPickup;

public sealed class PrioritizeVoidBag {
    public static bool GetItem_EarlyVoidVault(Player player, int plr, Item item, GetItemSettings settings) {
        if (settings.NoText) return false;
        if (!settings.CanGoIntoVoidVault || !player.IsVoidVaultEnabled || !player.CanVoidVaultAccept(item)) return false;
        return player.GetItem_VoidVault(plr, player.bank4.item, item, settings, item);
    }
}
