using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader.Default;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class Dyes : ModSubInventory {
    public override int Context => ContextID.EquipDye;
    public override void Focus(Player player, int slot) => Main.EquipPageSelected = 0;

    public sealed override int? MaxStack => 1;
    public sealed override bool Accepts(Item item) => item.dye != 0;
    public sealed override bool IsPrimaryFor(Item item) => false;

    public static Item[] ModdedDyes(Player player) => Reflection.ModAccessorySlotPlayer.exDyesAccessory.GetValue(player.GetModPlayer<ModAccessorySlotPlayer>());
}
public sealed class ArmorDyes : Dyes {
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.dye, Range.FromCount(0, AArmor.Count));
}
public sealed class AccessoryDyes : Dyes {
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new(
        new ListIndices<Item>(player.dye, Accessories.UnlockedVanillaSlots(player)),
        new ListIndices<Item>(ModdedDyes(player), Accessories.UnlockedModdedSlots(player))
    );
    public override int ComparePositionTo(ModSubInventory other) => other is ArmorDyes ? 1 : 0;
}
public sealed class EquipmentDyes : Dyes {
    public sealed override int Context => ContextID.EquipMiscDye;
    public sealed override void Focus(Player player, int slot) => Main.EquipPageSelected = 2;
    public sealed override Joined<ListIndices<Item>, Item> Items(Player player) => new ListIndices<Item>(player.miscDyes);
    public override int ComparePositionTo(ModSubInventory other) => other is AccessoryDyes ? 1 : 0;
}