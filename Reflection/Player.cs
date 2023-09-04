using Terraria;
using TPlayer = Terraria.Player;

namespace BetterInventory.Reflection;

public static class Player {
    public static readonly Method<TPlayer, int, Item, GetItemSettings, Item, int> GetItem_FillEmptyInventorySlot_VoidBag = new(nameof(GetItem_FillEmptyInventorySlot_VoidBag));
    public static readonly Method<TPlayer, int, Item, GetItemSettings, Item, int> GetItem_FillIntoOccupiedSlot_VoidBag = new(nameof(GetItem_FillIntoOccupiedSlot_VoidBag));
}