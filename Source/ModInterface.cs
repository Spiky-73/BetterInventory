using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory;

public abstract class ModInterface : ModType, ILocalizedModType {
    public virtual bool Available => true;
    public abstract bool Active { get; }
    public abstract void Activate();

    public int Type { get; internal set; }

    protected sealed override void Register() {
        ModTypeLookup<ModInterface>.Register(this);
        InterfaceLoader.Add(this);
    }

    public sealed override void SetupContent() {
        _ = DisplayName;
        SetStaticDefaults();
    }

    public string LocalizationCategory => "Interfaces";
    public virtual LocalizedText DisplayName => this.GetLocalization(nameof(DisplayName), PrettyPrintName);
}
