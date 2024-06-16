using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public abstract class ModPickupUpgrader : ModType, ILocalizedModType {
    protected sealed override void Register() {
        ModTypeLookup<ModPickupUpgrader>.Register(this);
        SmartPickup.Register(this);
    }

    public virtual bool Enabled => Configs.QuickSearch.Enabled && Configs.UpgradeItems.Value.upgraders.GetValueOrDefault(new(this), true);

    public abstract bool AppliesTo(Item item);
    public abstract Item AttemptUpgrade(Player player, Item item);

    public sealed override void SetupContent() => SetStaticDefaults();

    public string LocalizationCategory => "PickupUpgraders";
    public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName", PrettyPrintName);
}