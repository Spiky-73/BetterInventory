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
    public override bool IsDefault(Item item) => true;
    public override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.armor, Index);

    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 0;
    public const int Count = 3;
}
public abstract class AVanityArmor : AArmor {
    public sealed override int Context => ContextID.EquipArmorVanity;
    public override bool Accepts(Item item) => IsArmor(item);
    public override bool IsDefault(Item item) => item.vanity;
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.armor, Index + Count + AccessorySlotLoader.MaxVanillaSlotCount);
}
public sealed class HeadArmor : AArmor {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed override int Index => 0;
}
public sealed class BodyArmor : AArmor {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed override int Index => 1;
}
public sealed class LegArmor : AArmor {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed override int Index => 2;
}
public sealed class HeadVanity : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.headSlot != -1;
    public sealed override int Index => 0;
}
public sealed class BodyVanity : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.bodySlot != -1;
    public sealed override int Index => 1;
}
public sealed class LegVanity : AVanityArmor {
    public sealed override bool IsArmor(Item item) => item.legSlot != -1;
    public sealed override int Index => 2;
}
