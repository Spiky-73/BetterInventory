using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace BetterInventory.Default.Inventories;


public abstract class ModSubEquipmentInventory : ModSubInventory {
    public sealed override int? MaxStack => 1;

    public abstract int EquipPage { get; }
    private int _previousPage;

    public override void Focus(int slot) => (_previousPage, Main.EquipPageSelected) = (Main.EquipPageSelected, EquipPage);
    public override void Unfocus(int slot) => Main.EquipPageSelected = _previousPage;
}

public abstract class ModSubLoadoutInventory : ModSubEquipmentInventory {
    public int LoadoutIndex { get; protected set; } = -1;
    private int _previousLoadout;

    public override void Focus(int slot) {
        base.Focus(slot);
        _previousLoadout = Entity.CurrentLoadoutIndex;
        if (LoadoutIndex != -1) Entity.TrySwitchingLoadout(LoadoutIndex);
    }
    public override void Unfocus(int slot) {
        base.Unfocus(slot);
        Entity.TrySwitchingLoadout(_previousLoadout);
    }

    public override IEnumerable<ModSubInventory> GetInventories(Player player) {
        for (int i = 0; i < player.Loadouts.Length; i++) {
            var inventory = (ModSubLoadoutInventory)NewInstance(player);
            inventory.LoadoutIndex = (player.CurrentLoadoutIndex + i) % player.Loadouts.Length;
            yield return inventory;
        }
    }
    public override void SaveData(TagCompound tag) {
        if (LoadoutIndex != -1) tag[LoadoutTag] = LoadoutIndex;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(LoadoutTag, out int loadout)) LoadoutIndex = loadout;
    }
    public override bool Equals(object? obj) => base.Equals(obj) && LoadoutIndex == ((ModSubLoadoutInventory)obj).LoadoutIndex;
    public override int GetHashCode() => (base.GetHashCode(), LoadoutIndex).GetHashCode();
    
    public override IEnumerable<ModSubInventory> GetActiveInventories(Player player) {
        var instance = (ModSubLoadoutInventory)NewInstance(player);
        instance.LoadoutIndex = player.CurrentLoadoutIndex;
        return [instance];
    }
    public override bool IsActive() => Entity.CurrentLoadoutIndex == LoadoutIndex;

    public override LocalizedText DisplayName => LoadoutIndex != -1 ? base.DisplayName.WithFormatArgs(LoadoutIndex + 1) : base.DisplayName;

    public const string LoadoutTag = "loadout";
}