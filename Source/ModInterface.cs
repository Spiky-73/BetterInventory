using Terraria.ModLoader;

namespace BetterInventory;

public abstract class ModInterface : ModType {
    public abstract bool Active { get; }
    public abstract void Activate();

    public int Type { get; internal set; }
    protected sealed override void Register() {
        ModTypeLookup<ModInterface>.Register(this);
        InterfaceLoader.Add(this);
    }
}
