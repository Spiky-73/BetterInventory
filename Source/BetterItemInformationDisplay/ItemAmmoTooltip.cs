using System.Collections.Generic;
using SpikysLib;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.BetterItemInformationDisplay;

public sealed class ItemAmmoTooltipItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterItemInformationDisplayConfig.ItemAmmo;

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!ItemAmmoConfig.Tooltip) return;
        if (!ItemHelper.IsInventoryContext(item.tooltipContext)) return;
        foreach (var (itemAmmo, ammo) in ItemAmmoLoader.GetAmmos(Main.LocalPlayer, item)) {
            tooltips.FindOrAddLine(itemAmmo.GetTooltip(ammo), itemAmmo.TooltipPosition);
        }
    }
}