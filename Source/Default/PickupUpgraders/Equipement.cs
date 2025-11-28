using BetterInventory.InventoryManagement;
using SpikysLib.Constants;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Default.PickupUpgraders;

public sealed class Equipement : ModPickupUpgrader {
    public override bool AppliesTo(Item item) => Main.projHook[item.shoot] || item.wingSlot != -1;

    public override Item AttemptUpgrade(Player player, Item item) {
        if (Main.projHook[item.shoot] && !player.miscEquips[EquipmentSlots.Grapple].IsAir) {
            if (item.shootSpeed + GrappleRange(item.shoot) / 16 >= player.miscEquips[EquipmentSlots.Grapple].shootSpeed + GrappleRange(player.miscEquips[EquipmentSlots.Grapple].shoot) / 16) {
                (player.miscEquips[EquipmentSlots.Grapple], item) = (item, player.miscEquips[EquipmentSlots.Grapple]);
                (player.miscEquips[EquipmentSlots.Grapple].favorited, item.favorited) = (false, player.miscEquips[EquipmentSlots.Grapple].favorited);
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
        ProjectileID.Hook or ProjectileID.SlimeHook or ProjectileID.SquirrelHook => 300f,
        >= ProjectileID.GemHookAmethyst and <= ProjectileID.GemHookDiamond => 300 + (grappleProj - ProjectileID.GemHookAmethyst) * 30,
        ProjectileID.SkeletronHand => 350f,
        ProjectileID.Web => 375f,
        ProjectileID.CandyCaneHook or ProjectileID.FishHook or ProjectileID.IvyWhip => 400f,
        ProjectileID.AmberHook => 420f,
        ProjectileID.DualHookBlue or ProjectileID.DualHookRed => 440f,
        ProjectileID.TendonHook or ProjectileID.ThornHook or ProjectileID.IlluminantHook or ProjectileID.WormHook => 480f,
        ProjectileID.BatHook or ProjectileID.AntiGravityHook or ProjectileID.QueenSlimeHook => 500f,
        ProjectileID.WoodHook or ProjectileID.ChristmasHook or (>= ProjectileID.LunarHookSolar and <= ProjectileID.LunarHookStardust) => 550f,
        ProjectileID.StaticHook => 600f,
        _ => ProjectileLoader.GetProjectile(grappleProj)?.GrappleRange() ?? 0
    };
}