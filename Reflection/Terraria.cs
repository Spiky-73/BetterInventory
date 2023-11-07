using System.Reflection;
using Terraria;
using TMain = Terraria.Main;
using TPlayer = Terraria.Player;
using TItem = Terraria.Item;

namespace BetterInventory.Reflection;

public static class Main {
    public static readonly StaticField<int> numAvailableRecipes = new(typeof(TMain), nameof(TMain.numAvailableRecipes));
    public static readonly StaticField<int> focusRecipe = new(typeof(TMain), nameof(TMain.focusRecipe));
    public static readonly StaticField<bool> craftingHide = new(typeof(TMain), nameof(TMain.craftingHide));
    public static readonly StaticField<bool> InGuideCraftMenu = new(typeof(TMain), nameof(TMain.InGuideCraftMenu));
    public static readonly StaticField<TItem> guideItem = new(typeof(TMain), nameof(TMain.guideItem));
    public static readonly StaticField<bool> _preventCraftingBecauseClickWasUsedToChangeFocusedRecipe = new(typeof(TMain), nameof(_preventCraftingBecauseClickWasUsedToChangeFocusedRecipe));
    public static readonly StaticMethod<object?> DrawInterface_36_Cursor = new(typeof(TMain), nameof(DrawInterface_36_Cursor));
    public static readonly StaticMethod<int, object?> HoverOverCraftingItemButton = new(typeof(TMain), nameof(HoverOverCraftingItemButton));
    
    public static readonly FieldInfo _mouseTextCache = typeof(TMain).GetField(nameof(_mouseTextCache), BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo _mouseTextCache_isValid = typeof(TMain).GetNestedType("MouseTextCache", BindingFlags.NonPublic)!.GetField("isValid", BindingFlags.Instance | BindingFlags.Public)!;

    public static readonly Assembly tModLoader = Assembly.Load("tModLoader");
}

public static class Player {
    public static readonly Method<TPlayer, int, TItem, GetItemSettings, TItem, int> GetItem_FillEmptyInventorySlot_VoidBag = new(nameof(GetItem_FillEmptyInventorySlot_VoidBag));
    public static readonly Method<TPlayer, int, TItem, GetItemSettings, TItem, int> GetItem_FillIntoOccupiedSlot_VoidBag = new(nameof(GetItem_FillIntoOccupiedSlot_VoidBag));
}

public static class Item {
    public static readonly Property<TItem, bool> IsAir = new(nameof(TItem.IsAir));
    public static readonly Field<TItem, bool> favorited = new(nameof(TItem.favorited));
}