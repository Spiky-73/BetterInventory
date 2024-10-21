using System.Linq;
using BetterInventory.InventoryManagement;
using SpikysLib;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Default.ItemAmmos;

public sealed class WeaponAmmo : ModItemAmmo {
    public sealed override bool UsesAmmo(Item item) => item.useAmmo > AmmoID.None;
    public sealed override Item? GetAmmo(Player player, Item item) => player.ChooseAmmo(item);

    public sealed override TooltipLine GetTooltip(Item ammo) => new(Mod, "WeaponConsumes", Lang.tip[52].Value + ammo.Name);
}

public sealed class FishingPoleAmmo : ModItemAmmo {
    public sealed override bool UsesAmmo(Item item) => item.fishingPole > 0;
    public sealed override Item? GetAmmo(Player player, Item item) => player.PickBait();

    public sealed override TooltipLine GetTooltip(Item ammo) => new(Mod, "PoleConsumes", Lang.tip[52].Value +ammo.Name);
}

public sealed class WandAmmo : ModItemAmmo {
    public sealed override bool UsesAmmo(Item item) => item.tileWand != -1;
    public sealed override Item? GetAmmo(Player player, Item item) => player.FindItemRaw(item.tileWand);

    public sealed override TooltipLine GetTooltip(Item ammo) => new(Mod, "WandConsumes", Lang.tip[52].Value +ammo.Name);
}

public sealed class FlexibleWandAmmo : ModItemAmmo {
    public sealed override bool UsesAmmo(Item item) => item.GetFlexibleTileWand() is not null;
    public sealed override Item? GetAmmo(Player player, Item item) => item.GetFlexibleTileWand().TryGetPlacementOption(player, Player.FlexibleWandRandomSeed, Player.FlexibleWandCycleOffset, out _, out Item i) ? i : null;

    public sealed override TooltipLine GetTooltip(Item ammo) => new(Mod, "WandConsumes", Lang.tip[52].Value +ammo.Name);
}

public sealed class WrenchAmmo : ModItemAmmo {
    public static readonly int[] Wrenches = [ItemID.Wrench, ItemID.BlueWrench, ItemID.GreenWrench, ItemID.YellowWrench, ItemID.MulticolorWrench, ItemID.WireKite];
    public sealed override bool UsesAmmo(Item item) => Wrenches.Contains(item.type);
    public sealed override Item? GetAmmo(Player player, Item item) => player.FindItemRaw(ItemID.Wire);

    public sealed override TooltipLineID TooltipPosition => TooltipLineID.Tooltip;
    public sealed override TooltipLine GetTooltip(Item ammo) => new(Mod, "Tooltip0", Lang.tip[52].Value +ammo.Name);
}

public sealed class PaintAmmo : ModItemAmmo {
    public static readonly int[] PaintingItems = [ItemID.Paintbrush, ItemID.SpectrePaintbrush, ItemID.PaintRoller, ItemID.SpectrePaintRoller];
    public sealed override bool UsesAmmo(Item item) => PaintingItems.Contains(item.type);
    public sealed override Item? GetAmmo(Player player, Item item) => player.PickPaint();

    public sealed override TooltipLine GetTooltip(Item ammo) => new(Mod, "PaintConsumes", Lang.tip[52].Value +ammo.Name);
}