using System.Reflection;
using TMain = Terraria.Main;
using TPlayer = Terraria.Player;
using TItem = Terraria.Item;
using TNPC = Terraria.NPC;
using Microsoft.Xna.Framework;

namespace BetterInventory.Reflection;

public static class Main {
    public static readonly StaticField<int> numAvailableRecipes = new(typeof(TMain), nameof(TMain.numAvailableRecipes));
    public static readonly StaticField<bool> recBigList = new(typeof(TMain), nameof(TMain.recBigList));
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
    public static readonly Field<TPlayer, TItem> trashItem = new(nameof(TPlayer.trashItem));
}

public static class Item {
    public static readonly Field<TItem, int> stack = new(nameof(TItem.stack));
    public static readonly Property<TItem, bool> IsAir = new(nameof(TItem.IsAir));
    public static readonly Field<TItem, bool> favorited = new(nameof(TItem.favorited));
    public static readonly Method<TItem, bool> FitsAmmoSlot = new(nameof(TItem.FitsAmmoSlot));
}

public static class NPC {
    public static readonly StaticMethod<Vector2, bool, object?> LadyBugKilled = new(typeof(TNPC), nameof(TNPC.LadyBugKilled));
}