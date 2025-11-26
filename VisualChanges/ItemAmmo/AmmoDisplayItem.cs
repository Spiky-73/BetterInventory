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

    public override void Load() {
        On_ItemSlot.DrawItemIcon += HookDrawItemContext;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        // TODO change
        ClickOverrides.AddCraftStackLine(item, tooltips);

        if (!Configs.ItemAmmo.Tooltip) return;
        if (!ItemHelper.IsInventoryContext(item.tooltipContext)) return;
        foreach (var (itemAmmo, ammo) in ItemAmmoLoader.GetAmmos(Main.LocalPlayer, item)) {
            tooltips.FindOrAddLine(itemAmmo.GetTooltip(ammo), itemAmmo.TooltipPosition);
        }
    }

    public sealed override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
        if (!Configs.ItemAmmo.ItemSlot) return;
        if (!ItemHelper.IsInventoryContext(drawItemIconParams.Context)) return;
        foreach (var (itemAmmo, ammo) in ItemAmmoLoader.GetAmmos(Main.LocalPlayer, item)) {
            float size = Configs.ItemAmmo.Instance.itemSlot.Value.size;
            int width = TextureAssets.InventoryBack.Width();
            Vector2 direction = Configs.ItemAmmo.Instance.itemSlot.Value.position switch {
                Configs.Corner.TopLeft => new Vector2(-1, -1),
                Configs.Corner.TopRight => new Vector2(1, -1),
                Configs.Corner.BottomRight => new Vector2(1, 1),
                Configs.Corner.BottomLeft or _ => new Vector2(-1, 1),
            };
            Vector2 delta = direction * width * (0.5f - size / 2 - 0.1f * (1 - size));

            if (Configs.ItemAmmo.Instance.itemSlot.Value.hover) {
                float sizeHitbox = Configs.ItemAmmo.Instance.itemSlot.Value.size * 0.75f;
                Vector2 deltaHitbox = direction * width * (0.5f - sizeHitbox / 2);
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

    public static DrawItemIconParams drawItemIconParams = new(-1, 1);
    private static float HookDrawItemContext(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        (DrawItemIconParams prevParams, drawItemIconParams) = (drawItemIconParams, new(context, scale));
        var finalScale = orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
        drawItemIconParams = prevParams;
        return finalScale;
    }
}

public readonly record struct DrawItemIconParams(int Context, float Scale);
