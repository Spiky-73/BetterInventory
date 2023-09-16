using Terraria;
using TMain = Terraria.Main;
using TPlayer = Terraria.Player;

namespace BetterInventory.Reflection;

public static class Main {

    public static readonly StaticField<bool> InGuideCraftMenu = new(typeof(TMain), nameof(TMain.InGuideCraftMenu));
    public static readonly StaticMethod<object?> DrawInterface_36_Cursor = new(typeof(TMain), nameof(DrawInterface_36_Cursor));
}

public static class Player {
    public static readonly Method<TPlayer, int, Item, GetItemSettings, Item, int> GetItem_FillEmptyInventorySlot_VoidBag = new(nameof(GetItem_FillEmptyInventorySlot_VoidBag));
    public static readonly Method<TPlayer, int, Item, GetItemSettings, Item, int> GetItem_FillIntoOccupiedSlot_VoidBag = new(nameof(GetItem_FillIntoOccupiedSlot_VoidBag));
}