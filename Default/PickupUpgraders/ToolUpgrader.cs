using Terraria;
using BetterInventory.InventoryManagement;

namespace BetterInventory.Default.PickupUpgraders;

public sealed class ToolUpgrader : ModPickupUpgrader {
    public override bool AppliesTo(Item item) => item.pick > 0 || item.axe > 0 || item.hammer > 0;
    static bool AreComparable(Item a, Item b) => a.pick * b.pick + a.axe * b.axe + a.hammer * b.hammer > 0;
    static int CompareTools(Item a, Item b) => a.pick.CompareTo(b.pick) + a.axe.CompareTo(b.axe) + a.hammer.CompareTo(b.hammer);

    public override Item AttemptUpgrade(Player player, Item item) {
        int rep = -1;
        for (int i = 0; i < player.inventory.Length; i++) {
            if (!(!Configs.UpgradeItems.Value.importantOnly || player.inventory[i].favorited) || !AppliesTo(player.inventory[i]) || !AreComparable(player.inventory[i], item)) continue;
            if ((rep == -1 || CompareTools(player.inventory[i], player.inventory[rep]) > 0) && CompareTools(item, player.inventory[i]) > 0) rep = i;
        }
        if (rep == -1) return item;
        (player.inventory[rep], item) = (item, player.inventory[rep]);
        (player.inventory[rep].favorited, item.favorited) = (item.favorited, player.inventory[rep].favorited);
        return item;
    }
}