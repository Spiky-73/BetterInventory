using Terraria;
using TMain = Terraria.Main;

namespace BetterInventory.Reflection;

public static class Player {
    public static readonly Method<TPlayer, int, TItem, GetItemSettings, TItem, int> GetItem_FillEmptyInventorySlot_VoidBag = new(nameof(GetItem_FillEmptyInventorySlot_VoidBag));
    public static readonly Method<TPlayer, int, TItem, GetItemSettings, TItem, int> GetItem_FillIntoOccupiedSlot_VoidBag = new(nameof(GetItem_FillIntoOccupiedSlot_VoidBag));
}
