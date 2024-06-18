using BetterInventory.InventoryManagement;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Default.PickupUpgraders;

public sealed class EquipementUpgrader : ModPickupUpgrader {
    public override bool AppliesTo(Item item) => Main.projHook[item.shoot] || item.wingSlot != -1;

    public override Item AttemptUpgrade(Player player, Item item) {
        if (Main.projHook[item.shoot] && !player.miscEquips[4].IsAir) {
            if (item.shootSpeed + GrappleRange(item.shoot) / 16 >= player.miscEquips[4].shootSpeed + GrappleRange(player.miscEquips[4].shoot) / 16) {
                (player.miscEquips[4], item) = (item, player.miscEquips[4]);
                (player.miscEquips[4].favorited, item.favorited) = (false, player.miscEquips[4].favorited);
            }
        } else if (item.wingSlot != -1 && player.equippedWings != null) {
            if (player.GetWingStats(item.wingSlot).FlyTime > player.GetWingStats(player.equippedWings.wingSlot).FlyTime){
                object?[] args = [player, item, null];
                Reflection.ItemSlot.AccessorySwap.Invoke(args);
                item = (Item)args[2]!;
            }
        }


        return item;
    }
    public static float GrappleRange(int grappleProj) => grappleProj switch {
        >= ProjectileID.GemHookAmethyst and <= ProjectileID.GemHookDiamond => 300 + (grappleProj - ProjectileID.GemHookAmethyst) * 30,
        ProjectileID.Hook => 300f,
        ProjectileID.SlimeHook => 300f,
        ProjectileID.SquirrelHook => 300f,
        ProjectileID.SkeletronHand => 350f,
        ProjectileID.Web => 375f,
        ProjectileID.CandyCaneHook => 400f,
        ProjectileID.FishHook => 400f,
        ProjectileID.IvyWhip => 400f,
        ProjectileID.AmberHook => 420f,
        ProjectileID.DualHookBlue or ProjectileID.DualHookRed => 440f,
        ProjectileID.TendonHook or ProjectileID.ThornHook or ProjectileID.IlluminantHook or ProjectileID.WormHook => 480f,
        ProjectileID.BatHook => 500f,
        ProjectileID.AntiGravityHook => 500f,
        ProjectileID.QueenSlimeHook => 500f,
        ProjectileID.WoodHook or ProjectileID.ChristmasHook => 550f,
        >= ProjectileID.LunarHookSolar and <= ProjectileID.LunarHookStardust => 550f,
        ProjectileID.StaticHook => 600f,
        _ => ProjectileLoader.GetProjectile(grappleProj)?.GrappleRange() ?? 0
    };
}