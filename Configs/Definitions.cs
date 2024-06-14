using System.Linq;
using Terraria.ModLoader;
using SpikysLib.Configs.UI;
using System.ComponentModel;
using BetterInventory.ItemSearch;
using BetterInventory.InventoryManagement;

namespace BetterInventory.Configs;

[TypeConverter("BetterInventory.IO.ToFromStringConverterFix")]
public sealed class SearchProviderDefinition : EntityDefinition<SearchProviderDefinition> {
    public SearchProviderDefinition() : base() { }
    public SearchProviderDefinition(string key) : base(key) { }
    public SearchProviderDefinition(string mod, string name) : base(mod, name) { }
    public SearchProviderDefinition(SearchProvider provider) : this(provider.Mod.Name, provider.Name) { }

    public override int Type => global::BetterInventory.ItemSearch.QuickSearch.GetProvider(Mod, Name) is null ? -1 : 1;

    public override string DisplayName => global::BetterInventory.ItemSearch.QuickSearch.GetProvider(Mod, Name)?.DisplayName.Value ?? base.DisplayName;
    public override string? Tooltip => global::BetterInventory.ItemSearch.QuickSearch.GetProvider(Mod, Name)?.GetLocalization("Tooltip").Value;

    public override SearchProviderDefinition[] GetValues() => global::BetterInventory.ItemSearch.QuickSearch.Providers.Select(prov => new SearchProviderDefinition(prov)).ToArray();
}

[TypeConverter("BetterInventory.IO.ToFromStringConverterFix")]
public sealed class ModPickupUpgraderDefinition : EntityDefinition<ModPickupUpgraderDefinition> {
    public ModPickupUpgraderDefinition() : base() { }
    public ModPickupUpgraderDefinition(string key) : base(key) { }
    public ModPickupUpgraderDefinition(string mod, string name) : base(mod, name) { }
    public ModPickupUpgraderDefinition(ModPickupUpgrader provider) : this(provider.Mod.Name, provider.Name) { }

    public override int Type => global::BetterInventory.InventoryManagement.SmartPickup.GetPickupUpgrader(Mod, Name) is null ? -1 : 1;

    public override string DisplayName => global::BetterInventory.InventoryManagement.SmartPickup.GetPickupUpgrader(Mod, Name)?.DisplayName.Value ?? base.DisplayName;
    public override string? Tooltip => global::BetterInventory.InventoryManagement.SmartPickup.GetPickupUpgrader(Mod, Name)?.GetLocalization("Tooltip").Value;

    public override ModPickupUpgraderDefinition[] GetValues() => global::BetterInventory.InventoryManagement.SmartPickup.Upgraders.Select(up => new ModPickupUpgraderDefinition(up)).ToArray();
}
