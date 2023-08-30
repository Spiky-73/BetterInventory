using Terraria.ModLoader;

namespace BetterInventory;
public sealed class BetterInventory : Mod {
    public static BetterInventory Instance { get; private set; } = null!;

    public override void Load() {
        Instance = this;
        BetterCrafting.Load();
    }

    public override void Unload() {
        Instance = null!;
    }
}
