using System.Linq;
using Terraria.ModLoader;
using SpikysLib.Configs.UI;
using System.ComponentModel;
using BetterInventory.ItemSearch;
using BetterInventory.InventoryManagement;

namespace BetterInventory.Configs;

[TypeConverter("BetterInventory.IO.ToFromStringConverterFix")]
public sealed class EntityCatalogueDefinition : EntityDefinition<EntityCatalogueDefinition> {
    public EntityCatalogueDefinition() : base() { }
    public EntityCatalogueDefinition(string key) : base(key) { }
    public EntityCatalogueDefinition(string mod, string name) : base(mod, name) { }
    public EntityCatalogueDefinition(ModEntityCatalogue catalogue) : this(catalogue.Mod.Name, catalogue.Name) { }

    public override int Type => global::BetterInventory.ItemSearch.QuickSearch.GetEntityCatalogue(Mod, Name) is null ? -1 : 1;

    public override string DisplayName => global::BetterInventory.ItemSearch.QuickSearch.GetEntityCatalogue(Mod, Name)?.DisplayName.Value ?? base.DisplayName;
    public override string? Tooltip => global::BetterInventory.ItemSearch.QuickSearch.GetEntityCatalogue(Mod, Name)?.GetLocalization("Tooltip").Value;

    public override EntityCatalogueDefinition[] GetValues() => global::BetterInventory.ItemSearch.QuickSearch.EntityCatalogue.Select(prov => new EntityCatalogueDefinition(prov)).ToArray();
}

[TypeConverter("BetterInventory.IO.ToFromStringConverterFix")]
public sealed class PickupUpgraderDefinition : EntityDefinition<PickupUpgraderDefinition> {
    public PickupUpgraderDefinition() : base() { }
    public PickupUpgraderDefinition(string key) : base(key) { }
    public PickupUpgraderDefinition(string mod, string name) : base(mod, name) { }
    public PickupUpgraderDefinition(ModPickupUpgrader catalogue) : this(catalogue.Mod.Name, catalogue.Name) { }

    public override int Type => global::BetterInventory.InventoryManagement.SmartPickup.GetPickupUpgrader(Mod, Name) is null ? -1 : 1;

    public override string DisplayName => global::BetterInventory.InventoryManagement.SmartPickup.GetPickupUpgrader(Mod, Name)?.DisplayName.Value ?? base.DisplayName;
    public override string? Tooltip => global::BetterInventory.InventoryManagement.SmartPickup.GetPickupUpgrader(Mod, Name)?.GetLocalization("Tooltip").Value;

    public override PickupUpgraderDefinition[] GetValues() => global::BetterInventory.InventoryManagement.SmartPickup.Upgraders.Select(up => new PickupUpgraderDefinition(up)).ToArray();
}
