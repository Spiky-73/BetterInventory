using System.Collections.Generic;
using Terraria.ModLoader;
using TAccPlayer = Terraria.ModLoader.Default.ModAccessorySlotPlayer;
using TBuilderLoader = Terraria.ModLoader.BuilderToggleLoader;

namespace BetterInventory.Reflection;

public static class ModAccessorySlotPlayer {
    public static readonly Field<TAccPlayer, Terraria.Item[]> exAccessorySlot = new(nameof(exAccessorySlot));
    public static readonly Field<TAccPlayer, Terraria.Item[]> exDyesAccessory = new(nameof(exDyesAccessory));
}

public static class BuilderToggleLoader {
    public static readonly StaticField<List<BuilderToggle>> BuilderToggles = new(typeof(TBuilderLoader), nameof(BuilderToggles));
}