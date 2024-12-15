using System.Linq;
using System.ComponentModel;
using BetterInventory.ItemSearch;
using BetterInventory.InventoryManagement;
using SpikysLib;

namespace BetterInventory;

[TypeConverter("BetterInventory.IO.ToFromStringConverterFix")]
public sealed class EntityCatalogueDefinition : EntityDefinition<EntityCatalogueDefinition, ModEntityCatalogue> {
    public EntityCatalogueDefinition() : base() { }
    public EntityCatalogueDefinition(string key) : base(key) { }
    public EntityCatalogueDefinition(string mod, string name) : base(mod, name) { }
    public EntityCatalogueDefinition(ModEntityCatalogue catalogue) : base(catalogue) { }

    public override ModEntityCatalogue? Entity => QuickSearch.GetEntityCatalogue(Mod, Name);

    public override EntityCatalogueDefinition[] GetValues() => QuickSearch.EntityCatalogues.Select(prov => new EntityCatalogueDefinition(prov)).ToArray();
}

[TypeConverter("BetterInventory.IO.ToFromStringConverterFix")]
public sealed class PickupUpgraderDefinition : EntityDefinition<PickupUpgraderDefinition, ModPickupUpgrader> {
    public PickupUpgraderDefinition() : base() { }
    public PickupUpgraderDefinition(string key) : base(key) { }
    public PickupUpgraderDefinition(string mod, string name) : base(mod, name) { }
    public PickupUpgraderDefinition(ModPickupUpgrader catalogue) : base(catalogue) { }

    public override ModPickupUpgrader? Entity => PickupUpgraderLoader.GetUpgrader(Mod, Name);
    
    public override PickupUpgraderDefinition[] GetValues() => PickupUpgraderLoader.Upgraders.Select(up => new PickupUpgraderDefinition(up)).ToArray();
}