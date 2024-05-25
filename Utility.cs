using System;
using Terraria;

namespace BetterInventory;

[Flags] public enum AllowedItems : byte { None = 0b00, Self = 0b01, Mouse = 0b10}

public static class Utility {
    public static Item? LastStack(this Player player, Item item, AllowedItems allowedItems = AllowedItems.None) {
        bool Check(Item i) => item.type == i.type && (allowedItems.HasFlag(AllowedItems.Self) || i != item);

        for (int i = 49; i >= 0; i--) if (Check(player.inventory[i])) return player.inventory[i];
        for (int i = 57; i >= 50; i--) if (Check(player.inventory[i])) return player.inventory[i];
        if (allowedItems.HasFlag(AllowedItems.Mouse) && Check(player.inventory[58])) return player.inventory[58];
        return null;
    }

   public static Item? SmallestStack(this Player player, Item item, AllowedItems allowedItems = AllowedItems.None) {
        Item? min = null;
        void Check(Item i) {
            if (item.type == i.type && (min is null || i.stack < min.stack) && (allowedItems.HasFlag(AllowedItems.Self) || i != item)) min = i;
        }

        for (int i = 49; i >= 0; i--) Check(player.inventory[i]);
        for (int i = 57; i >= 50; i--) Check(player.inventory[i]);
        if (allowedItems.HasFlag(AllowedItems.Mouse)) Check(player.inventory[58]);
        return min;
    }
}
