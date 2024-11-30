using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class SmartConsumptionItem : GlobalItem {

    public override void Load() {
        On_ItemSlot.DrawItemIcon += HookDrawItemContext;
        IL_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += static il => {
            if(!il.ApplyTo(ILOnConsumeBait, Configs.SmartConsumption.Baits)) Configs.UnloadedInventoryManagement.Value.baits = true;

        };
        IL_Recipe.ConsumeForCraft += static il => {
            if (!il.ApplyTo(ILOnConsumedMaterial, Configs.SmartConsumption.Materials)) Configs.UnloadedInventoryManagement.Value.materials = true;
        };

    }

    public override void OnConsumeItem(Item item, Player player) {
        if (item.PaintOrCoating) {
            if (Configs.SmartConsumption.Paints) SmartConsume(player, item, () => player.LastStack(item, Configs.SmartConsumption.Mouse));
        } else {
            if (Configs.SmartConsumption.Consumables) SmartConsume(player, item, () => player.SmallestStack(item, Configs.SmartConsumption.Mouse));
        }
    }

    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Configs.SmartConsumption.Ammo) SmartConsume(player, ammo, () => player.LastStack(ammo, Configs.SmartConsumption.Mouse));
    }

    private static void ILOnConsumedMaterial(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int consumed, i => i.Previous.SaferMatchCallvirt(Reflection.Item.Clone), 0);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(Reflection.RecipeLoader.ConsumedItems));
        cursor.EmitLdarg1();
        cursor.EmitLdloc(consumed);
        cursor.EmitDelegate((Item item, Item consumed) => {
            if (Configs.SmartConsumption.Materials) SmartConsume(Main.LocalPlayer, item, () => Main.LocalPlayer.SmallestStack(item, AllowedItems.Self | Configs.SmartConsumption.Mouse), consumed.stack);
        });
    }

    private static void ILOnConsumeBait(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int i, i => i.Previous.MatchLdcI4(-1), 0);

        cursor.GotoNext(i => i.SaferMatchCall(Reflection.NPC.LadyBugKilled));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(Reflection.Item.stack));
        cursor.EmitLdarg0();
        cursor.EmitLdloc(i);
        cursor.EmitDelegate((Player self, int i) => {
            if (Configs.SmartConsumption.Baits) SmartConsume(self, self.inventory[i], () => self.LastStack(self.inventory[i], Configs.SmartConsumption.Mouse));
        });
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        AddWeaponConsumeLine(item, tooltips);
        Crafting.Crafting.AddAvailableMaterials(item, tooltips);
        QuickMove.AddMoveChainLine(item, tooltips);
        ClickOverrides.AddCraftStackLine(item, tooltips);
    }

    private static void AddWeaponConsumeLine(Item item, List<TooltipLine> tooltips) {
        if (!Configs.ItemAmmo.Tooltip) return;
        if (!ItemHelper.IsInventoryContext(item.tooltipContext)) return;
        foreach(var itemAmmo in ItemAmmoLoader.ItemAmmos) {
            if (itemAmmo.TryGetAmmo(Main.LocalPlayer, item, out var ammo)) tooltips.FindOrAddLine(itemAmmo.GetTooltip(ammo), itemAmmo.TooltipPosition);
        }
    }

    public static void SmartConsume(Player player, Item item, Func<Item?> stackPicker, int consumed = 1) {
        while (consumed > 0) {
            Item? i = stackPicker();
            if (i == null) return;
            int amount = Math.Min(consumed, i.stack);
            item.stack += amount;
            i.stack -= amount;
            if(player.whoAmI == Main.myPlayer) {
                if (item == player.inventory[InventorySlots.Mouse]) Main.mouseItem.stack += amount;
                if (i == player.inventory[InventorySlots.Mouse]) Main.mouseItem.stack -= amount;
            }
            consumed -= amount;
            if (i.stack == 0) i.TurnToAir();
        }
    }

    public static DrawItemIconParams drawItemIconParams = new(-1, 1);
    private static float HookDrawItemContext(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        (DrawItemIconParams prevParams, drawItemIconParams) = (drawItemIconParams, new(context, scale));
        var finalScale = orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
        drawItemIconParams = prevParams;
        return finalScale;
    }


    public sealed override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
        if (!Configs.ItemAmmo.ItemSlot) return;
        if (!ItemHelper.IsInventoryContext(drawItemIconParams.Context)) return;
        foreach (var itemAmmo in ItemAmmoLoader.ItemAmmos) {
            if (!itemAmmo.TryGetAmmo(Main.LocalPlayer, item, out var ammo)) continue;
            float size = Configs.ItemAmmo.Value.itemSlot.Value.size;
            int width = TextureAssets.InventoryBack.Width();
            Vector2 direction = Configs.ItemAmmo.Value.itemSlot.Value.position switch {
                Configs.Corner.TopLeft => new Vector2(-1, -1),
                Configs.Corner.TopRight => new Vector2(1, -1),
                Configs.Corner.BottomRight => new Vector2(1, 1),
                Configs.Corner.BottomLeft or _ => new Vector2(-1, 1),
            };
            Vector2 delta = direction * width * (0.5f - size/2 - 0.1f*(1-size));

            if (Configs.ItemAmmo.Value.itemSlot.Value.hover){
                float sizeHitbox = Configs.ItemAmmo.Value.itemSlot.Value.size * 0.75f;
                Vector2 deltaHitbox = direction * width * (0.5f - sizeHitbox/2);
                if (new Rectangle((int)(position.X + deltaHitbox.X - width * sizeHitbox / 2), (int)(position.Y + deltaHitbox.Y - width * sizeHitbox / 2), (int)(width * sizeHitbox), (int)(width * sizeHitbox)).Contains(Main.mouseX, Main.mouseY)) {
                    Item displayed = ammo.Clone();
                    displayed.stack = 1;
                    ItemSlot.MouseHover([displayed], ItemSlot.Context.InventoryAmmo, 0);
                }
            }
            ItemSlot.DrawItemIcon(ammo, ItemSlot.Context.InventoryAmmo, spriteBatch, position + delta, drawItemIconParams.Scale * size, width * size, Color.White);
            break;
        }
    }
}

public readonly record struct DrawItemIconParams(int Context, float Scale);