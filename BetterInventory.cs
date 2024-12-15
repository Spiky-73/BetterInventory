using BetterInventory.InventoryManagement;
using SpikysLib;
using Terraria.ModLoader;

namespace BetterInventory;

public sealed class BetterInventory : Mod, IPreLoadMod {
    public static BetterInventory Instance { get; private set; } = null!;


    public void PreLoadMod() => Instance = this;

    public override void Unload() {
        ItemAmmoLoader.Unload();
        PickupUpgraderLoader.Unload();
        Instance = null!;
    }
}
