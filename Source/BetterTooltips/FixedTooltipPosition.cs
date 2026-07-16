using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterTooltips;

public class FixedTooltipPositionHooks : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterTooltipsConfig.FixedTooltipPosition;
    public void Load(Mod mod) {
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookFindSlotPosition;
        On_Main.DrawInterface_41_InterfaceLogic4 += HookFixTooltipPosition;
    }
    public void Unload() { }

    private static void HookFindSlotPosition(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        if (BetterTooltipsConfig.FixedTooltipPosition && !inv[slot].IsAir) {
            Rectangle rect = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (rect.Contains(Main.mouseX, Main.mouseY)) {
                FixedTooltipPosition.FixNextTooltipPosition((int)(position.X + TextureAssets.InventoryBack.Width() * Main.inventoryScale * 1.1f), (int)position.Y);
            }
        }
        orig(spriteBatch, inv, context, slot, position, lightColor);
    }

    private static void HookFixTooltipPosition(On_Main.orig_DrawInterface_41_InterfaceLogic4 orig) {
        if (BetterTooltipsConfig.FixedTooltipPosition) FixedTooltipPosition.ModifyTooltipPosition(ref Main.instance._mouseTextCache);
        orig();
    }
}

public static class FixedTooltipPosition {
    public static void FixNextTooltipPosition(Vector2 position) => FixNextTooltipPosition((int)position.X, (int)position.Y);
    public static void FixNextTooltipPosition(int x, int y) {
        if (!Main.ThickMouse) {
            x += 6;
            y += 6;
        }
        int mouseOffset = 10 + Main.toolTipDistance;
        _position = new(x - mouseOffset, y - mouseOffset + 1);
    }

    public static void ModifyTooltipPosition(ref Main.MouseTextCache info) {
        if (!_position.HasValue) return;
        info.X = _position.Value.X;
        info.Y = _position.Value.Y;
        _position = null;
    }
    private static Point? _position = null;
}