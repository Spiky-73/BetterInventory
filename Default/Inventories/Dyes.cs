using System.Collections;
using System.Collections.Generic;
using SpikysLib.Constants;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Inventories;

public abstract class DyesInventory : ModSubLoadoutInventory {
    public override int Context => ContextID.EquipDye;

    public sealed override int? MaxStack => 1;
    public sealed override bool Accepts(Item item) => item.dye != 0;
    public sealed override bool IsPreferredInventory(Item item) => false;

    public Item[] VanillaDyes => Entity.CurrentLoadoutIndex == LoadoutIndex ?
        Entity.dye :
        Entity.Loadouts[LoadoutIndex].Dye;
    public Item[] ModdedDyes => Entity.CurrentLoadoutIndex == LoadoutIndex || LoadoutIndex == -1 ?
        Reflection.ModAccessorySlotPlayer.exDyesAccessory.GetValue(Entity.GetModPlayer<ModAccessorySlotPlayer>()) :
        Reflection.ModAccessorySlotPlayer.ExEquipmentLoadout.ExDyesAccessory.GetValue(((IList)Reflection.ModAccessorySlotPlayer.exLoadouts.GetValue(Entity.GetModPlayer<ModAccessorySlotPlayer>())!)[LoadoutIndex]!)!;

}
public sealed class ArmorDyes : DyesInventory {
    public sealed override ListIndices<Item> Items => new(VanillaDyes, Range.FromCount(0, ArmorSlots.Count));
}
public sealed class AccessoryDyes : DyesInventory {
    public sealed override JoinedLists<Item> Items => new(
        new ListIndices<Item>(VanillaDyes, AccessoryInventory.UnlockedVanillaSlots(Entity)),
        new ListIndices<Item>(ModdedDyes, AccessoryInventory.UnlockedModdedSlots(Entity, false))
    );
    public sealed override int ComparePositionTo(ModSubInventory other) => other is ArmorDyes ? 1 : 0;
}
public sealed class SharedAccessoryDyes : DyesInventory {
    public sealed override ListIndices<Item> Items => new(ModdedDyes, AccessoryInventory.UnlockedModdedSlots(Entity, true));
    public sealed override int ComparePositionTo(ModSubInventory other) => other is AccessoryDyes ? 1 : 0;
    public sealed override IList<ModSubInventory> GetInventories(Player player) => [NewInstance(player)];
    public override void SaveData(TagCompound tag) { }
    public override void LoadData(TagCompound tag) { }
}
public sealed class EquipmentDyes : DyesInventory {
    private int _previousPage;
    public sealed override int Context => ContextID.EquipMiscDye;
    public sealed override void Focus(int slot) => (_previousPage, Main.EquipPageSelected) = (Main.EquipPageSelected, 2);
    public override void Unfocus(int slot) => Main.EquipPageSelected = _previousPage;
    public sealed override Item[] Items => Entity.miscDyes;
    public sealed override int ComparePositionTo(ModSubInventory other) => other is SharedAccessoryDyes ? 1 : 0;
    public sealed override IList<ModSubInventory> GetInventories(Player player) => [NewInstance(player)];
    public override void SaveData(TagCompound tag) { }
    public override void LoadData(TagCompound tag) { }
}