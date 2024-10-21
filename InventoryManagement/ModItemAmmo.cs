using System.Diagnostics.CodeAnalysis;
using SpikysLib;
using SpikysLib.Configs;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement; 


public abstract class ModItemAmmo : ModType {
    protected override void InitTemplateInstance() => ConfigHelper.SetInstance(this);
    protected sealed override void Register() {
        ItemAmmoLoader.Add(this);
        ModTypeLookup<ModItemAmmo>.Register(this);
    }
    public sealed override void SetupContent() => SetStaticDefaults();
    public override void Unload() => ConfigHelper.SetInstance(this, true);


    public abstract bool UsesAmmo(Item item);
    public abstract Item? GetAmmo(Player player, Item item);
    public bool TryGetAmmo(Player player, Item item, [MaybeNullWhen(false)] out Item ammo) {
        ammo = null;
        if (!UsesAmmo(item)) return false;
        ammo = GetAmmo(player, item);
        return ammo is not null;
    }

    public virtual TooltipLineID TooltipPosition => TooltipLineID.WandConsumes;
    public abstract TooltipLine GetTooltip(Item ammo);

}