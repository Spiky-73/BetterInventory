using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.ItemSearch;

public abstract class ModEntityCatalogue : ModType, ILocalizedModType {
    protected sealed override void Register() {
        ModTypeLookup<ModEntityCatalogue>.Register(this);
        QuickSearch.Register(this);
        Language.GetOrRegister(this.GetLocalizationKey("DisplayName"), PrettyPrintName);
        Language.GetOrRegister(this.GetLocalizationKey("Tooltip"), () => string.Empty);
    }

    public virtual bool Enabled => Configs.QuickSearch.Enabled && Configs.QuickSearch.Value.catalogues.GetValueOrDefault(new(this), true);

    public ModKeybind Keybind { get; protected set; } = null!;
    public abstract bool Visible { get; }
    public abstract void Toggle(bool? enabled = null);

    public abstract void Search(Item item);

    public sealed override void SetupContent() => SetStaticDefaults();

    public string LocalizationCategory => "EntityCatalogues";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName");

    public virtual int ComparePositionTo(ModEntityCatalogue other) => 0;
}