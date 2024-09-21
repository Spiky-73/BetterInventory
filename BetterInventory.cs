using Terraria.ModLoader;

namespace BetterInventory;

// TODO assets loading
public sealed class BetterInventory : Mod {
    public static BetterInventory Instance { get; private set; } = null!;

    public override void Load() {
        Instance = this;
    }

    public override void Unload() {
        Instance = null!;
    }
}
