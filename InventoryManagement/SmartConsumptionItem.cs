using System.Collections.Generic;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class SmartConsumptionItem : GlobalItem {

    public static Configs.InventoryManagement Config => Configs.InventoryManagement.Instance;

    public override void Load() {
        IL_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += ILSmartBait;
    }

    public override void OnConsumeItem(Item item, Player player) {
        if (!Config.smartConsumption) return;
        if (item.PaintOrCoating) OnConsume(item, player.LastStack(item, true));
        else OnConsume(item, player.SmallestStack(item, true));
    }

    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Config.smartAmmo) OnConsume(ammo, player.LastStack(ammo, true));
    }

    private static void ILSmartBait(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.MatchCall(Reflection.NPC.LadyBugKilled));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(Reflection.Item.stack));
        cursor.EmitLdarg0();
        cursor.EmitLdloc1();
        cursor.EmitDelegate((Player self, Item item) => {
            if(Config.smartConsumption) OnConsume(item, self.LastStack(item, true));
        });
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        QuickMove.AddMoveChainLine(item, tooltips);
    }

    public static void OnConsume(Item consumed, Item? stack) {
        if (stack == null) return;
        consumed.stack++;
        stack.stack--;
        if (stack.stack == 0) stack.TurnToAir();
    }
}
