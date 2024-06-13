using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

public abstract class SearchProvider : ModType, ILocalizedModType {
    protected sealed override void Register() {
        ModTypeLookup<SearchProvider>.Register(this);
        QuickSearch.Register(this);
    }

    public virtual bool Enabled => Configs.QuickSearch.Enabled && Configs.QuickSearch.Value.providers[new(this)];

    public ModKeybind Keybind { get; protected set; } = null!;
    public abstract bool Visible { get; }
    public abstract void Toggle(bool? enabled = null);

    public abstract void Search(Item item);

    public sealed override void SetupContent() => SetStaticDefaults();

    public string LocalizationCategory => "SearchProviders";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName", PrettyPrintName);

    public virtual int ComparePositionTo(SearchProvider other) => 0;
}