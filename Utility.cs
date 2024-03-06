using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using log4net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ObjectData;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory;

public static class Utility {

    public static string FormatTagKeys(this string format, List<(string, string)> Tags) {
        if (Tags.Count == 0) return format;
        foreach ((string key, string tag) in Tags) {
            string word = Language.GetTextValue(key);
            format = format.Replace(word, $"[{tag}:{word}]");
        }
        return format;
    }

    public static void DrawTileFrame(SpriteBatch spriteBatch, int tile, Vector2 position, Vector2 origin, float scale) {
        Main.instance.LoadTiles(tile);

        TileObjectData tileObjectData = TileObjectData.GetTileData(tile, 0);
        (int width, int height, int padding) = tileObjectData is null ? (1, 1, 0) : (tileObjectData.Width, tileObjectData.Height, tileObjectData.CoordinatePadding);

        Vector2 topLeft = position - new Vector2(width, height) * 16 * origin * scale;
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                spriteBatch.Draw(TextureAssets.Tile[tile].Value, topLeft + new Vector2(i * 16, j * 16) * scale, new Rectangle(i * 16 + i * padding, j * 16 + j * padding, 16, 16), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
    }

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
        Recipe.FindRecipes();
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

    public static Item MoveInto(Item item, Item toMove, out int transferred, int? maxStack = null, bool canFavorite = true) {
        transferred = 0;
        if (toMove.IsAir) return item;
        if (item.IsAir) {
            transferred = maxStack.HasValue ? Math.Min(maxStack.Value, toMove.stack) : toMove.stack;
            item = toMove.Clone();
            item.stack = 0;
            ItemLoader.SplitStack(item, toMove, transferred);
        } else if (item.type == toMove.type && item.stack < (maxStack ?? item.maxStack)) {
            int oldStack = item.maxStack;
            if (maxStack.HasValue) item.maxStack = maxStack.Value;
            ItemLoader.TryStackItems(item, toMove, out transferred);
            item.maxStack = oldStack;
        }
        if (transferred != 0) {
            item.favorited = item.favorited || canFavorite && toMove.favorited;
            if (toMove.IsAir) toMove.TurnToAir(true);
        }
        return item;
    }

    public static int FindIndex<T>(this IList<T> list, Predicate<T> predicate) {
        for (int i = 0; i < list.Count; i++) if (predicate(list[i])) return i;
        return -1;
    }

    public static void SaveConfig(this ModConfig config) => Reflection.ConfigManager.Save.Invoke(config);

    public static void SetInstance<T>(T instance, bool unload = false) where T: notnull => instance.GetType().GetField("Instance", BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public)?.SetValue(null, unload ? null : instance);

    public static long CountCurrency(Player player, int currencyIndex = -1) {
        if (currencyIndex != -1) {
            CustomCurrencyManager.TryGetCurrencySystem(currencyIndex, out CustomCurrencySystem customCurrencySystem);
            return customCurrencySystem.CombineStacks(out _, new long[] {
                customCurrencySystem.CountCurrency(out _, player.inventory, new int[] { 58, 57, 56, 55, 54 }),
                customCurrencySystem.CountCurrency(out _, player.bank.item, Array.Empty<int>()),
                customCurrencySystem.CountCurrency(out _, player.bank2.item, Array.Empty<int>()),
                customCurrencySystem.CountCurrency(out _, player.bank3.item, Array.Empty<int>()),
                customCurrencySystem.CountCurrency(out _, player.bank4.item, Array.Empty<int>())
            });
        }
        return Utils.CoinsCombineStacks(out _, new long[] {
            Utils.CoinsCount(out _, player.inventory, new int[] { 58, 57, 56, 55, 54 }),
            Utils.CoinsCount(out _, player.bank.item, Array.Empty<int>()),
            Utils.CoinsCount(out _, player.bank2.item, Array.Empty<int>()),
            Utils.CoinsCount(out _, player.bank3.item, Array.Empty<int>()),
            Utils.CoinsCount(out _, player.bank4.item, Array.Empty<int>())
        });
    }

    public static ReadOnlyDictionary<int, int> OwnedItems => Data.ownedItems;

    public static ILog Logger => BetterInventory.Instance.Logger;


    public static readonly int[] InventoryContexts = new int[] { ContextID.InventoryItem, ContextID.InventoryAmmo, ContextID.InventoryCoin };

    private class Data : ILoadable {
        public void Load(Mod mod) => ownedItems = new(Reflection.Recipe._ownedItems.GetValue());
        public void Unload() => ownedItems = null!;
        public static ReadOnlyDictionary<int, int> ownedItems = null!;
    }
}
