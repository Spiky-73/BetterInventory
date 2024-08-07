using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class AArmor : ModSubInventory {
    public abstract int Index { get; }
    public abstract bool IsArmor(Item item);

    public override int Context => ContextID.EquipArmor;
    public override bool Accepts(Item item) => IsArmor(item) && !item.vanity;
    public override bool IsPrimaryFor(Item item) => true;
    public override ListIndices<Item> Items(Player player) => new(player.armor, Index);

    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 0;
}
public abstract class AVanityArmor : AArmor {
    public sealed override int Context => ContextID.EquipArmorVanity;
    public override bool Accepts(Item item) => IsArmor(item);
    public override bool IsPrimaryFor(Item item) => item.vanity;
    public sealed override ListIndices<Item> Items(Player player) => new(player.armor, Index + ArmorSlots.Count + AccessorySlotLoader.MaxVanillaSlotCount);
}
public sealed class HeadArmor : AArmor {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed override int Index => ArmorSlots.Head;
}
public sealed class BodyArmor : AArmor {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed override int Index => ArmorSlots.Body;
}
public sealed class LegArmor : AArmor {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed override int Index => ArmorSlots.Leg;
}
public sealed class HeadVanity : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed override int Index => ArmorSlots.Head;
}
public sealed class BodyVanity : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed override int Index => ArmorSlots.Body;
}
public sealed class LegVanity : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed override int Index => ArmorSlots.Leg;
}
