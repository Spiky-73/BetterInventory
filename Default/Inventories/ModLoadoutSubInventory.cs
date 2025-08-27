using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace BetterInventory.Default.Inventories;


public abstract class ModSubLoadoutInventory : ModSubInventory {
    public int LoadoutIndex { get; protected set; } = -1;
    private int _previousLoadout;
    private int _previousPage;

    public override void Focus(int slot) {
        _previousLoadout = Entity.CurrentLoadoutIndex;
        if (LoadoutIndex != -1) Entity.TrySwitchingLoadout(LoadoutIndex);
        (_previousPage, Main.EquipPageSelected) = (Main.EquipPageSelected, 0);
    }
    public override void Unfocus(int slot) {
        Entity.TrySwitchingLoadout(_previousLoadout);
        Main.EquipPageSelected = _previousPage;
    }

    public override IList<ModSubInventory> GetActiveInventories(Player player) {
        var instance = (ModSubLoadoutInventory)NewInstance(player);
        instance.LoadoutIndex = player.CurrentLoadoutIndex;
        return [instance];
    }
    public override IList<ModSubInventory> GetInventories(Player player) {
        List<ModSubInventory> inventories = [];
        for (int i = 0; i < player.Loadouts.Length; i++) {
            var inventory = (ModSubLoadoutInventory)NewInstance(player);
            inventory.LoadoutIndex = (player.CurrentLoadoutIndex + i) % player.Loadouts.Length;
            inventories.Add(inventory);
        }
        return inventories;
    }
    public override void SaveData(TagCompound tag) {
        tag[LoadoutTag] = LoadoutIndex;
    }
    public override void LoadData(TagCompound tag) {
        LoadoutIndex = tag.GetInt(LoadoutTag);
    }
    public const string LoadoutTag = "loadout";


    public override LocalizedText DisplayName => LoadoutIndex != -1 ? base.DisplayName.WithFormatArgs(LoadoutIndex + 1) : base.DisplayName;
    public override bool Equals(object? obj) => base.Equals(obj) && obj is ModSubLoadoutInventory subInventory && LoadoutIndex == subInventory.LoadoutIndex;
    public override int GetHashCode() => (LoadoutIndex, base.GetHashCode()).GetHashCode();
}