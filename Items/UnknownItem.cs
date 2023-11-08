using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Items;

public sealed class UnknownItem : ModItem {
    public static int ID => ModContent.ItemType<UnknownItem>();
    public static Item Instance { get; private set; } = null!;

    public override void SetStaticDefaults() => Instance = new(ID);
    public override void Unload() => Instance = null!;
    
    public override void SetDefaults() => Item.maxStack = 1;

    public override void ModifyTooltips(List<TooltipLine> tooltips) {
        for (int i = 1; i < tooltips.Count; i++) tooltips[i].Hide();
    }
}