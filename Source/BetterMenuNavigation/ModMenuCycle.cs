using Microsoft.Xna.Framework.Input;
using Terraria.ModLoader;

namespace BetterInventory.BetterMenuNavigation;

public abstract class ModMenuCycle : ModType {
    
    public int Type { get; internal set; }

    protected sealed override void Register() {
        MenuCycleLoader.Register(this);
        ModTypeLookup<ModMenuCycle>.Register(this);
    }
    public sealed override void SetupContent() {
        SetStaticDefaults();
    }

    public abstract int MenusCount { get; }
    public abstract int CurrentMenu();
    public abstract void ShowMenu(int index);
    public virtual string DefaultKeybind => Keys.None.ToString();
}