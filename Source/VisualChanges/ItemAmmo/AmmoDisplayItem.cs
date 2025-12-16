using System.Collections.Generic;
using BetterInventory.InventoryManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.VisualChanges.ItemAmmo;

public sealed class AmmoDisplayItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || VisualChangesConfig.ItemAmmo;
    public override void Load() {
        On_ItemSlot.DrawItemIcon += HookDrawItemContext;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        // TODO change
        ClickOverrides.AddCraftStackLine(item, tooltips);

        if (!VisualChangesConfig.ItemAmmo) return;
        if (ItemAmmoConfig.Tooltip) ItemAmmo.Tooltip_ModifyTooltips(item, tooltips);
    }

    public sealed override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
        if (!VisualChangesConfig.ItemAmmo) return;
        if (ItemAmmoConfig.ItemSlot) ItemAmmo.ItemSlot_PostDrawInInventory(item, spriteBatch, position);
    }

    private static float HookDrawItemContext(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        if (!VisualChangesConfig.ItemAmmo || !ItemAmmoConfig.ItemSlot) return orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
        ItemAmmo.ItemSlot_PreDrawItemIcon(context, scale);
        var finalScale = orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
        ItemAmmo.ItemSlot_PostDrawItemIcon();
        return finalScale;
    }
}

public static class ItemAmmo {
    public static void Tooltip_ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!ItemHelper.IsInventoryContext(item.tooltipContext)) return;
        foreach (var (itemAmmo, ammo) in ItemAmmoLoader.GetAmmos(Main.LocalPlayer, item)) {
            tooltips.FindOrAddLine(itemAmmo.GetTooltip(ammo), itemAmmo.TooltipPosition);
        }
    }

    public static void ItemSlot_PreDrawItemIcon(int context, float scale) => _drawItemIconParams = new(context, scale);
    public static void ItemSlot_PostDrawItemIcon() => _drawItemIconParams = new(-1, 1);


    public static void ItemSlot_PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position) {
        if (!ItemHelper.IsInventoryContext(_drawItemIconParams.Context)) return;
        foreach (var (itemAmmo, ammo) in ItemAmmoLoader.GetAmmos(Main.LocalPlayer, item)) {
            float size = ItemSlotAmmoConfig.Instance.size;
            int width = TextureAssets.InventoryBack.Width();
            Vector2 direction = ItemSlotAmmoConfig.Instance.position switch {
                Corner.TopLeft => new Vector2(-1, -1),
                Corner.TopRight => new Vector2(1, -1),
                Corner.BottomRight => new Vector2(1, 1),
                Corner.BottomLeft or _ => new Vector2(-1, 1),
            };
            Vector2 delta = direction * width * (0.5f - size / 2 - 0.1f * (1 - size));

            if (ItemSlotAmmoConfig.Instance.hover) {
                float sizeHitbox = ItemSlotAmmoConfig.Instance.size * 0.75f;
                Vector2 deltaHitbox = direction * width * (0.5f - sizeHitbox / 2);
                if (new Rectangle((int)(position.X + deltaHitbox.X - width * sizeHitbox / 2), (int)(position.Y + deltaHitbox.Y - width * sizeHitbox / 2), (int)(width * sizeHitbox), (int)(width * sizeHitbox)).Contains(Main.mouseX, Main.mouseY)) {
                    Item displayed = ammo.Clone();
                    displayed.stack = 1;
                    ItemSlot.MouseHover([displayed], ItemSlot.Context.InventoryAmmo, 0);
                }
            }
            ItemSlot.DrawItemIcon(ammo, ItemSlot.Context.InventoryAmmo, spriteBatch, position + delta, _drawItemIconParams.Scale * size, width * size, Color.White);
            break;
        }
    }
    private static DrawItemIconParams _drawItemIconParams = new(-1, 1);
}

public readonly record struct DrawItemIconParams(int Context, float Scale);
