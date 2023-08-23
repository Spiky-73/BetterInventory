using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Globals;

public class BetterInventoryItem : GlobalItem {

    public override void OnConsumeItem(Item item, Player player) {
        if (Configs.ClientConfig.Instance.smartConsumption) OnConsume(item, player);
    }
    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Configs.ClientConfig.Instance.smartAmmo) OnConsume(ammo, player, true);
    }

    public static void OnConsume(Item consumed, Player player, bool lastStack = false) {
        Item? smartStack = lastStack ? player.LastStack(consumed, true) : player.SmallestStack(consumed, true);
        if (smartStack == null) return;
        consumed.stack++;
        smartStack.stack--;
    }

}


