using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class ArmorSlot : ModSubLoadoutInventory {
    public abstract bool IsArmor(Item item);
    public abstract int Index { get; }
    public virtual bool Vanity => false;

    public sealed override int EquipPage => 0;
    public sealed override ListIndices<Item> Items => new(Entity.Armor(LoadoutIndex), Vanity ? (Index + ArmorSlots.Count + AccessorySlotLoader.MaxVanillaSlotCount) : Index);
    public sealed override bool Accepts(Item item) => IsArmor(item) && (Vanity || !item.vanity);
    public sealed override bool IsPreferredInventory(Item item) => !Vanity || item.vanity;
    public sealed override int Context => Vanity ? ContextID.EquipArmorVanity : ContextID.EquipArmor;
}
public abstract class VanityArmorSlot : ArmorSlot {
    public sealed override bool Vanity => true;
}

public sealed class HeadArmor : ArmorSlot {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed override int Index => ArmorSlots.Head;
}
public sealed class BodyArmor : ArmorSlot {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed override int Index => ArmorSlots.Body;
}
public sealed class LegArmor : ArmorSlot {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed override int Index => ArmorSlots.Leg;
}
public sealed class HeadVanity : VanityArmorSlot {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed override int Index => ArmorSlots.Head;
}
public sealed class BodyVanity : VanityArmorSlot {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed override int Index => ArmorSlots.Body;
}
public sealed class LegVanity : VanityArmorSlot {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed override int Index => ArmorSlots.Leg;
}
