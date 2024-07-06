using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ID;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class Equipment : ModSubInventory {
    public abstract int Index { get; }
    public sealed override ListIndices<Item> Items(Player player) => new(player.miscEquips, Index);
    public sealed override bool IsPrimaryFor(Item item) => true;
    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 2;
}
public sealed class Pet : Equipment {
    public sealed override int Index => EquipmentSlots.Pet;
    public sealed override int Context => ContextID.EquipPet;
    public sealed override bool Accepts(Item item) => item.buffType > 0 && Main.vanityPet[item.buffType];
}
public sealed class LightPet : Equipment {
    public sealed override int Index => EquipmentSlots.LightPet;
    public sealed override int Context => ContextID.EquipLight;
    public sealed override bool Accepts(Item item) => item.buffType > 0 && Main.lightPet[item.buffType];
}
public sealed class Minecart : Equipment {
    public sealed override int Index => EquipmentSlots.Minecart;
    public sealed override int Context => ContextID.EquipMinecart;
    public sealed override bool Accepts(Item item) => item.mountType != -1 && MountID.Sets.Cart[item.mountType];
}
public sealed class Mount : Equipment {
    public sealed override int Index => EquipmentSlots.Mount;
    public sealed override int Context => ContextID.EquipMount;
    public sealed override bool Accepts(Item item) => item.mountType != -1 && !MountID.Sets.Cart[item.mountType];
}
public sealed class Grapple : Equipment {
    public sealed override int Index => EquipmentSlots.Grapple;
    public sealed override int Context => ContextID.EquipGrapple;
    public sealed override bool Accepts(Item item) => Main.projHook[item.shoot];
}
