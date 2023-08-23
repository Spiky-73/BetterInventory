using BetterInventory.InventoryManagement;
using Terraria.ModLoader;

namespace BetterInventory;
public class BetterInventory : Mod {
    public static BetterInventory Instance { get; private set; } = null!;

    public override void Load() {
        Instance = this;

        Actions.Load();
    }

    public override void Unload() {
        Instance = null!;
    }
}
