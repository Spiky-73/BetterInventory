using BetterInventory.InventoryManagement;
using SpikysLib.Constants;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Default.PickupUpgraders;

public sealed class Equipement : ModPickupUpgrader {
    public override bool AppliesTo(Item item) => Main.projHook[item.shoot] || item.wingSlot != -1;

    public override void CheckLockedItems(Player player) {
        if (_upgradedGrapple != ItemID.None && !player.miscEquips[EquipmentSlots.Grapple].IsAir && _upgradedGrapple == player.miscEquips[EquipmentSlots.Grapple].type) {
            Configs.UpgradeItems.Value.Lock(new(_upgradedGrapple));
            _upgradedGrapple = ItemID.None;
        }
        if (_upgradedWings != ItemID.None && !player.equippedWings.IsAir && _upgradedWings == player.equippedWings.type) {
            Configs.UpgradeItems.Value.Lock(new(_upgradedWings));
            _upgradedWings = ItemID.None;
        }
    }

    public override Item AttemptUpgrade(Player player, Item item) {
        if (Main.projHook[item.shoot] && !player.miscEquips[EquipmentSlots.Grapple].IsAir) {
            if (Configs.UpgradeItems.Value.IsLocked(new(player.miscEquips[EquipmentSlots.Grapple].type))) return item;
            if (item.shootSpeed + GrappleRange(item.shoot) / 16 >= player.miscEquips[EquipmentSlots.Grapple].shootSpeed + GrappleRange(player.miscEquips[EquipmentSlots.Grapple].shoot) / 16) {
                (player.miscEquips[EquipmentSlots.Grapple], item) = (item, player.miscEquips[EquipmentSlots.Grapple]);
                (player.miscEquips[EquipmentSlots.Grapple].favorited, item.favorited) = (item.favorited && Reflection.ItemSlot.canFavoriteAt.GetValue()[ItemSlot.Context.EquipGrapple], player.miscEquips[EquipmentSlots.Grapple].favorited);
                _upgradedGrapple = item.type;
            }
        } else if (item.wingSlot != -1 && player.equippedWings != null) {
            if (Configs.UpgradeItems.Value.IsLocked(new(player.equippedWings.type))) return item;
            if (player.GetWingStats(item.wingSlot).FlyTime > player.GetWingStats(player.equippedWings.wingSlot).FlyTime) {
                object?[] args = [player, item, null];
                Reflection.ItemSlot.AccessorySwap.Invoke(args);
                item = (Item)args[2]!;
                _upgradedWings = item.type;
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

    private static int _upgradedWings;
    private static int _upgradedGrapple;
}