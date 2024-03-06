using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class SmartConsumptionItem : GlobalItem {

    public override void OnConsumeItem(Item item, Player player) {
        if (item.PaintOrCoating) {
            if(Configs.SmartConsumption.Paints) SmartConsume(item, () => player.LastStack(item, true));
        }
        else if (Configs.SmartConsumption.Consumables) SmartConsume(item, () => player.SmallestStack(item, true));
    }

    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Configs.SmartConsumption.Ammo) SmartConsume(ammo, () => player.LastStack(ammo, true));
    }

    internal static void ILOnConsumedMaterial(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(Reflection.RecipeLoader.ConsumedItems));
        cursor.EmitLdarg1();
        cursor.EmitLdloc0();
        cursor.EmitDelegate((Item item, Item consumed) => {
            if (Configs.SmartConsumption.Materials) SmartConsume(item, () => Main.LocalPlayer.SmallestStack(item, false), consumed.stack);
        });
    }

    internal static void ILOnConsumeBait(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.MatchCall(Reflection.NPC.LadyBugKilled));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(Reflection.Item.stack));
        cursor.EmitLdarg0();
        cursor.EmitLdloc1();
        cursor.EmitDelegate((Player self, Item item) => {
            if (Configs.SmartConsumption.Baits) SmartConsume(item, () => self.LastStack(item, true));
        });
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        QuickMove.AddMoveChainLine(item, tooltips);
    }

    public static void SmartConsume(Item item, Func<Item?> stackPicker, int consumed = 1) {
        while (consumed > 0) {
            Item? i = stackPicker();
            if (i == null) return;
            int amount = Math.Min(consumed, i.stack);
            item.stack += amount;
            i.stack -= amount;
            consumed -= amount;
            if (i.stack == 0) i.TurnToAir();
        }
    }
}
