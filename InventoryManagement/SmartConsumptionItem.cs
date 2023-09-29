using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class SmartConsumptionItem : GlobalItem {

    public override void OnConsumeItem(Item item, Player player) {
        if (Configs.ClientConfig.Instance.smartConsumption) OnConsume(item, player.SmallestStack(item, true));
    }
    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Configs.ClientConfig.Instance.smartAmmo) OnConsume(ammo, player.LastStack(ammo, true));
    }

    public static void OnConsume(Item consumed, Item? stack) {
        if (stack == null) return;
        consumed.stack++;
        stack.stack--;
        if (stack.stack == 0) stack.TurnToAir();
    }
}
