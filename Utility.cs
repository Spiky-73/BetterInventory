using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory;

public static class Utility {

    public static Item? LastStack(this Player player, Item item, bool notArg = false) {
        for (int i = player.inventory.Length - 1 - 8; i >= 0; i--) {
            if (item.type == player.inventory[i].type && (!notArg || player.inventory[i] != item))
                return player.inventory[i];
        }
        for (int i = player.inventory.Length - 1; i >= 8; i--) {
            if (item.type == player.inventory[i].type && (!notArg || player.inventory[i] != item))
                return player.inventory[i];
        }
        return null;
    }

   public static Item? SmallestStack(this Player player, Item item, bool notArg = false) {
        Item? currentMin = null;
        for (int i = player.inventory.Length - 1; i >= 0; i--) {
            if (item.type == player.inventory[i].type
                    && (currentMin is null || player.inventory[i].stack < currentMin.stack)
                    && (!notArg || player.inventory[i] != item))
                currentMin = player.inventory[i];
        }
        return currentMin;
    }

    public static void GetDropItem(this Player player, ref Item item) {
        if (item.IsAir) return;
        Main.mouseItem.position = player.Center;
        Item rem = player.GetItem(player.whoAmI, item, GetItemSettings.GetItemInDropItemCheck);
        if (rem.stack > 0) {
            int i = Item.NewItem(new EntitySource_OverfullInventory(player, null), (int)player.position.X, (int)player.position.Y, player.width, player.height, rem.type, rem.stack, false, rem.prefix, true, false);
            Main.item[i] = rem.Clone();
            Main.item[i].newAndShiny = false;
            if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i, 1f, 0f, 0f, 0, 0, 0);
        }
        item = new();
        Recipe.FindRecipes(false);
    }

    public enum InclusionFlag {
        Min = 0x01,
        Max = 0x10,
        Both = Min | Max
    }

    public static bool InRange<T>(this T self, T min, T max, InclusionFlag flags = InclusionFlag.Both) where T : System.IComparable<T> => InRange(Comparer<T>.Default, self, min, max, flags);
    public static bool InRange<T>(this IComparer<T> comparer, T value, T min, T max, InclusionFlag flags = InclusionFlag.Both) {
        int l = comparer.Compare(value, min);
        int r = comparer.Compare(value, max);
        return (l > 0 || (flags.HasFlag(InclusionFlag.Min) && l == 0)) && (r < 0 || (flags.HasFlag(InclusionFlag.Max) && r == 0));
    }

    public enum SnapMode {
        Round,
        Ceiling,
        Floor
    }

    public static bool InChest(this Player player, [MaybeNullWhen(false)] out Item[] chest) => (chest = player.Chest()) is not null;
    [return: NotNullIfNotNull("chest")]
    public static Item[]? Chest(this Player player, int? chest = null) {
        int c = chest ?? player.chest;
        return c switch {
            > -1 => Main.chest[c].item,
            -2 => player.bank.item,
            -3 => player.bank2.item,
            -4 => player.bank3.item,
            -5 => player.bank4.item,
            _ => null
        };
    }

    public static void RunWithHiddenItems(Item[] chest, System.Predicate<Item> hidden, System.Action action) {
        Dictionary<int, Item> hiddenItems = new();
        for (int i = 0; i < chest.Length; i++) {
            if (!hidden(chest[i])) continue;
            hiddenItems[i] = chest[i];
            chest[i] = new();
        }
        action();
        foreach ((int slot, Item item) in hiddenItems) {
            chest[slot] = item;
        }
    }

    public static void ApplyRGB(ref this Color color, float mult) {
        color.R = (byte)(color.R * mult);
        color.G = (byte)(color.G * mult);
        color.B = (byte)(color.B * mult);
    }

    public static ReadOnlyDictionary<int, int> OwnedItems => Data.ownedItems;
    
    private class Data : ILoadable {
        public void Load(Mod mod) => ownedItems = new(Reflection.Recipe._ownedItems.GetValue());
        public void Unload() => ownedItems = null!;
        public static ReadOnlyDictionary<int, int> ownedItems = null!;
    }
}
