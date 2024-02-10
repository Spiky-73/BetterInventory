using BetterInventory.Configs.UI;
using Terraria.ModLoader;

namespace BetterInventory;
public sealed class BetterInventory : Mod {
    public static BetterInventory Instance { get; private set; } = null!;

    public override void Load() {
        Instance = this;
        MonoModHooks.Modify(Reflection.ConfigElement.DrawSelf, Text.ILColors);
    }

    public override void Unload() {
        Instance = null!;
    }
}
