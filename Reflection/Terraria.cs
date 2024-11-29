using System.Reflection;
using TMain = Terraria.Main;
using TPlayer = Terraria.Player;
using TItem = Terraria.Item;
using TNPC = Terraria.NPC;
using TRecipe = Terraria.Recipe;
using SpikysLib.Reflection;
using Terraria;
using TColor = Microsoft.Xna.Framework.Color;
using TVector2 = Microsoft.Xna.Framework.Vector2;
using System;

namespace BetterInventory.Reflection;

public static class Main {
    public static readonly StaticField<int[]> availableRecipe = new(typeof(TMain), nameof(TMain.availableRecipe));
    public static readonly StaticField<int> numAvailableRecipes = new(typeof(TMain), nameof(TMain.numAvailableRecipes));
    public static readonly StaticField<int> recStart = new(typeof(TMain), nameof(TMain.recStart));
    public static readonly StaticField<float> inventoryScale = new(typeof(TMain), nameof(TMain.inventoryScale));
    public static readonly StaticField<bool> recBigList = new(typeof(TMain), nameof(TMain.recBigList));
    public static readonly StaticField<bool> recFastScroll = new(typeof(TMain), nameof(TMain.recFastScroll));
    public static readonly StaticField<int> focusRecipe = new(typeof(TMain), nameof(TMain.focusRecipe));
    public static readonly StaticField<bool> craftingHide = new(typeof(TMain), nameof(TMain.craftingHide));
    public static readonly StaticField<bool> hidePlayerCraftingMenu = new(typeof(TMain), nameof(TMain.hidePlayerCraftingMenu));
    public static readonly StaticField<int> screenHeight = new(typeof(TMain), nameof(TMain.screenHeight));
    public static readonly StaticField<bool> InGuideCraftMenu = new(typeof(TMain), nameof(TMain.InGuideCraftMenu));
    public static readonly StaticField<TItem> guideItem = new(typeof(TMain), nameof(TMain.guideItem));
    public static readonly StaticField<bool> _preventCraftingBecauseClickWasUsedToChangeFocusedRecipe = new(typeof(TMain), nameof(_preventCraftingBecauseClickWasUsedToChangeFocusedRecipe));
    public static readonly StaticField<int> toolTipDistance = new(typeof(TMain), nameof(toolTipDistance));
    public static readonly StaticField<bool> SettingsEnabled_OpaqueBoxBehindTooltips = new(typeof(TMain), nameof(TMain.SettingsEnabled_OpaqueBoxBehindTooltips));
    public static readonly StaticMethod<object?> DrawInterface_36_Cursor = new(typeof(TMain), nameof(DrawInterface_36_Cursor));
    public static readonly StaticMethod<object?> HoverOverCraftingItemButton = new(typeof(TMain), nameof(HoverOverCraftingItemButton), typeof(int));
    public static readonly StaticMethod<object?> LockCraftingForThisCraftClickDuration = new(typeof(TMain), nameof(TMain.LockCraftingForThisCraftClickDuration));
    public static readonly StaticMethod<object?> SetRecipeMaterialDisplayName = new(typeof(TMain), nameof(SetRecipeMaterialDisplayName), typeof(int));
    public static readonly StaticMethod<object?> DrawGuideCraftText = new(typeof(TMain), nameof(DrawGuideCraftText), typeof(int), typeof(TColor), typeof(int).MakeByRefType(), typeof(int).MakeByRefType());
    
    public static readonly Type MouseTextCache = typeof(TMain).GetNestedType(nameof(MouseTextCache), BindingFlags.NonPublic)!;
    public static readonly Method<TMain, object?> MouseText_DrawItemTooltip = new(nameof(MouseText_DrawItemTooltip), MouseTextCache, typeof(int), typeof(byte), typeof(int), typeof(int));
    public static readonly FieldInfo _mouseTextCache = typeof(TMain).GetField(nameof(_mouseTextCache), BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo _mouseTextCache_isValid = MouseTextCache.GetField("isValid", BindingFlags.Instance | BindingFlags.Public)!;
}

public static class Player {
    public static readonly Field<TPlayer, TItem> trashItem = new(nameof(TPlayer.trashItem));
    public static readonly Field<TPlayer, bool> mouseInterface = new(nameof(TPlayer.mouseInterface));
    public static readonly Method<TPlayer, bool> GetItem_FillEmptyInventorySlot = new(nameof(GetItem_FillEmptyInventorySlot), typeof(int), typeof(TItem), typeof(GetItemSettings), typeof(TItem), typeof(int));
    public static readonly Method<TPlayer, bool> HasItem = new(nameof(TPlayer.HasItem), typeof(int));
}

public static class Item {
    public static readonly Field<TItem, int> stack = new(nameof(TItem.stack));
    public static readonly Field<TItem, int> useStyle = new(nameof(TItem.useStyle));
    public static readonly Field<TItem, bool> favorited = new(nameof(TItem.favorited));
    public static readonly Property<TItem, string> Name = new(nameof(TItem.Name));
    public static readonly Property<TItem, bool> IsAir = new(nameof(TItem.IsAir));
    public static readonly Property<TItem, bool> IsACoin = new(nameof(TItem.IsACoin));
    public static readonly Method<TItem, bool> FitsAmmoSlot = new(nameof(TItem.FitsAmmoSlot));
    public static readonly Method<TItem, TItem> Clone = new(nameof(TItem.Clone));
    public static readonly Field<TItem, bool> DD2Summon = new(nameof(TItem.DD2Summon));
}

public static class NPC {
    public static readonly StaticMethod<object?> LadyBugKilled = new(typeof(TNPC), nameof(TNPC.LadyBugKilled), typeof(TVector2), typeof(bool));
}

public static class Recipe {
    public static readonly Property<TRecipe, bool> Disabled = new(nameof(TRecipe.Disabled));
    public static readonly Field<TRecipe, TItem> createItem = new(nameof(TRecipe.createItem));
    public static readonly Method<TRecipe, object?> Create = new(nameof(TRecipe.Create));
    public static readonly Field<TRecipe, bool> needWater = new(nameof(needWater));
    public static readonly Field<TRecipe, bool> needHoney = new(nameof(needHoney));
    public static readonly Field<TRecipe, bool> needLava = new(nameof(needLava));
    public static readonly Field<TRecipe, bool> needSnowBiome = new(nameof(needSnowBiome));
    public static readonly Field<TRecipe, bool> needGraveyardBiome = new(nameof(needGraveyardBiome));

    public static readonly StaticMethod<object?> CollectGuideRecipes = new(typeof(TRecipe), nameof(CollectGuideRecipes));
    public static readonly StaticMethod<object?> TryRefocusingRecipe = new(typeof(TRecipe), nameof(TryRefocusingRecipe), typeof(int));
    public static readonly StaticMethod<object?> VisuallyRepositionRecipes = new(typeof(TRecipe), nameof(VisuallyRepositionRecipes), typeof(float));
    public static readonly StaticMethod<object?> AddToAvailableRecipes = new(typeof(TRecipe), nameof(AddToAvailableRecipes), typeof(int));
    public static readonly StaticMethod<object?> ClearAvailableRecipes = new(typeof(TRecipe), nameof(TRecipe.ClearAvailableRecipes));
    public static readonly StaticMethod<object?> CollectItemsToCraftWithFrom = new(typeof(TRecipe), nameof(CollectItemsToCraftWithFrom), typeof(TPlayer));
}
