using System.Collections.Generic;
using SpikysLib.Reflection;
using Terraria.ModLoader;
using TItem = Terraria.Item;
using TRecipe = Terraria.Recipe;
using TAccPlayer = Terraria.ModLoader.Default.ModAccessorySlotPlayer;
using TBuilderLoader = Terraria.ModLoader.BuilderToggleLoader;
using TLoader = Terraria.ModLoader.RecipeLoader;

namespace BetterInventory.Reflection;

public static class ModAccessorySlotPlayer {
    public static readonly Field<TAccPlayer, TItem[]> exAccessorySlot = new(nameof(exAccessorySlot));
    public static readonly Field<TAccPlayer, TItem[]> exDyesAccessory = new(nameof(exDyesAccessory));
}

public static class BuilderToggleLoader {
    public static readonly StaticField<List<BuilderToggle>> BuilderToggles = new(typeof(TBuilderLoader), nameof(BuilderToggles));
}

public static class RecipeLoader {
    public static readonly StaticMethod<object?> OnCraft = new(typeof(TLoader), nameof(TLoader.OnCraft), typeof(TItem), typeof(TRecipe), typeof(List<TItem>), typeof(TItem));
    public static readonly StaticField<List<TItem>> ConsumedItems = new(typeof(TLoader), nameof(ConsumedItems));
}