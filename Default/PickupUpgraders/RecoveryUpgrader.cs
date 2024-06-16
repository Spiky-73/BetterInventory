using Terraria;
using BetterInventory.InventoryManagement;

namespace BetterInventory.Default.PickupUpgraders;

public sealed class RecoveryUpgrader : ModPickupUpgrader {
    public override bool AppliesTo(Item item) => item.healLife > 0 || item.healMana > 0;
    static bool AreComparable(Item a, Item b) => a.healLife * b.healLife + a.healMana * b.healMana > 0;
    static int CompareRecovery(Item a, Item b) => a.healLife.CompareTo(b.healLife) + a.healLife.CompareTo(b.healLife);

    public override Item AttemptUpgrade(Player player, Item item) {
        int rep = -1;
        for (int i = 0; i < player.inventory.Length; i++) {
            if (!(!Configs.UpgradeItems.Value.importantOnly || player.inventory[i].favorited) || !AppliesTo(player.inventory[i]) || !AreComparable(player.inventory[i], item)) continue;
            if ((rep == -1 || CompareRecovery(player.inventory[i], player.inventory[rep]) > 0) && CompareRecovery(item, player.inventory[i]) > 0) rep = i;
        }
        if (rep == -1) return item;
        (player.inventory[rep], item) = (item, player.inventory[rep]);
        (player.inventory[rep].favorited, item.favorited) = (item.favorited, player.inventory[rep].favorited);
        return item;
    }
}