using System.Reflection;
using Terraria;
using TMain = Terraria.Main;
using TPlayer = Terraria.Player;

namespace BetterInventory.Reflection;

public static class Main {
    public static readonly StaticField<int> numAvailableRecipes = new(typeof(TMain), nameof(TMain.numAvailableRecipes));
    public static readonly StaticField<bool> InGuideCraftMenu = new(typeof(TMain), nameof(TMain.InGuideCraftMenu));
    public static readonly StaticMethod<object?> DrawInterface_36_Cursor = new(typeof(TMain), nameof(DrawInterface_36_Cursor));
    
    public static readonly FieldInfo _mouseTextCache = typeof(TMain).GetField(nameof(_mouseTextCache), BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo _mouseTextCache_isValid = typeof(TMain).GetNestedType("MouseTextCache", BindingFlags.NonPublic)!.GetField("isValid", BindingFlags.Instance | BindingFlags.Public)!;
}

public static class Player {
    public static readonly Method<TPlayer, int, Item, GetItemSettings, Item, int> GetItem_FillEmptyInventorySlot_VoidBag = new(nameof(GetItem_FillEmptyInventorySlot_VoidBag));
    public static readonly Method<TPlayer, int, Item, GetItemSettings, Item, int> GetItem_FillIntoOccupiedSlot_VoidBag = new(nameof(GetItem_FillIntoOccupiedSlot_VoidBag));
}