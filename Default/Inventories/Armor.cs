using System.Collections.Generic;
using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class ArmorInventory : ModLoadoutSubInventory {
    public abstract bool IsArmor(Item item);

    public sealed override void Focus(int slot) {
        Main.EquipPageSelected = 0;
        base.Focus(slot);
    }

    public IList<Item> Armor => Entity.CurrentLoadoutIndex == LoadoutIndex ?
        Entity.armor :
        Entity.Loadouts[LoadoutIndex].Armor;
}

public abstract class ArmorSlot : ArmorInventory {
    public abstract int Index { get; }
    public override int Context => ContextID.EquipArmor;
    public override bool Accepts(Item item) => IsArmor(item) && !item.vanity;
    public override bool IsPreferredInventory(Item item) => true;
    public override ListIndices<Item> Items => new(Armor, Index);
    public override bool Equals(object? obj) => base.Equals(obj) && Index == ((ArmorSlot)obj).Index;
    public override int GetHashCode() => (Index, base.GetHashCode()).GetHashCode();

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

public abstract class VanityArmorSlot : ArmorInventory {
    public abstract int Index { get; }
    public sealed override int Context => ContextID.EquipArmorVanity;
    public override bool Accepts(Item item) => IsArmor(item);
    public override bool IsPreferredInventory(Item item) => item.vanity;
    public sealed override ListIndices<Item> Items => new(Armor, Index + ArmorSlots.Count + AccessorySlotLoader.MaxVanillaSlotCount);
    public override bool Equals(object? obj) => base.Equals(obj) && Index == ((VanityArmorSlot)obj).Index;
    public override int GetHashCode() => (Index, base.GetHashCode()).GetHashCode();
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
