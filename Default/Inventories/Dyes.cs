using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class DyesInventory : ModSubEquipmentInventory {
    public sealed override bool Accepts(Item item) => item.dye != 0;
    public sealed override bool IsPreferredInventory(Item item) => false;
}
public abstract class LoadoutDyesInventory : ModSubLoadoutInventory {
    public sealed override int Context => ContextID.EquipDye;

    public sealed override bool Accepts(Item item) => item.dye != 0;
    public sealed override bool IsPreferredInventory(Item item) => false;
}
public sealed class ArmorDyes : LoadoutDyesInventory {
    public sealed override int EquipPage => 0;
    public sealed override ListIndices<Item> Items => new(Entity.Dyes(LoadoutIndex), Range.FromCount(0, ArmorSlots.Count));
}
public sealed class AccessoryDyes : LoadoutDyesInventory {
    public sealed override int EquipPage => 0;
    public sealed override JoinedLists<Item> Items => new(
        new ListIndices<Item>(Entity.Dyes(LoadoutIndex), Entity.UnlockedVanillaSlots()),
        new ListIndices<Item>(Entity.ExDyes(LoadoutIndex), Entity.UnlockedModdedSlots(false))
    );
    public sealed override int ComparePositionTo(ModSubInventory other) => other is ArmorDyes ? 1 : 0;
}
public sealed class SharedAccessoryDyes : DyesInventory {
    public sealed override int EquipPage => 0;
    public sealed override int Context => ContextID.EquipDye;
    public sealed override ListIndices<Item> Items => new(Entity.ExDyes(), Entity.UnlockedModdedSlots(true));
    public sealed override int ComparePositionTo(ModSubInventory other) => other is AccessoryDyes ? 1 : 0;
}
public sealed class EquipmentDyes : DyesInventory {
    public sealed override int EquipPage => 2;
    public sealed override int Context => ContextID.EquipMiscDye;
    public sealed override Item[] Items => Entity.miscDyes;
    public sealed override int ComparePositionTo(ModSubInventory other) => other is SharedAccessoryDyes ? 1 : 0;
}