using System.Collections.Generic;
using Terraria;
using Terraria.Localization;

namespace BetterInventory.Default.Inventories;

public abstract class ModLoadoutSubInventory : ModSubInventory {
    public int LoadoutIndex { get; protected set; } = -1;

    public override void Focus(int slot) {
        if (LoadoutIndex != -1) Entity.TrySwitchingLoadout(LoadoutIndex);
    }


    public ModSubInventory GetCurrentInventory(Player player) {
        var instance = (ModLoadoutSubInventory)NewInstance(player);
        instance.LoadoutIndex = player.CurrentLoadoutIndex;
        return instance;
    }
    public override IList<ModSubInventory> GetInventories(Player player) {
        List<ModSubInventory> inventories = [];
        for (int i = 0; i < player.Loadouts.Length; i++) {
            var inventory = (ModLoadoutSubInventory)NewInstance(player);
            inventory.LoadoutIndex = (player.CurrentLoadoutIndex + i) % player.Loadouts.Length;
            inventories.Add(inventory);
        }
        return inventories;
    }

    public override LocalizedText DisplayName => LoadoutIndex != -1 ? base.DisplayName.WithFormatArgs(LoadoutIndex + 1) : base.DisplayName;
    public override bool Equals(object? obj) => base.Equals(obj) && obj is ModLoadoutSubInventory subInventory && LoadoutIndex == subInventory.LoadoutIndex;
    public override int GetHashCode() => (LoadoutIndex, base.GetHashCode()).GetHashCode();
}