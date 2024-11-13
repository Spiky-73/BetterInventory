using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace BetterInventory.ItemActions;

public static class FixedTooltip {
    public static void Load() {
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookFindSlotPosition;
        IL_Main.MouseText_DrawItemTooltip += static il => {
            if (!il.ApplyTo(ILFixTooltip, Configs.ItemActions.FixedTooltip)) Configs.UnloadedItemActions.Value.fixedTooltip = true;
        };
    }

    private static void HookFindSlotPosition(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        if (Configs.ItemActions.FixedTooltip) {
            Rectangle rect = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (rect.Contains(Main.mouseX, Main.mouseY)) {
                _slotPosition = position;
                _scale = Main.inventoryScale;
            }
        }
        orig(spriteBatch, inv, context, slot, position, lightColor);
    }


    // Does not work because Main.MouseTextCache is private
    // private static void HookFixTooltip(On_Main.orig_MouseText_DrawItemTooltip orig, Main self, ValueType info, int rare, byte diff, int X, int Y) {
    //     if (Configs.ItemActions.FixedTooltip && _slotPosition.HasValue) {
    //         (X, Y) = ((int)_slotPosition.Value.X, (int)_slotPosition.Value.Y);
    //         _slotPosition = null;
    //     }
    //     orig(self, info, rare, diff, X, Y);
    // }
    private static void ILFixTooltip(ILContext il) {
        ILCursor cursor = new(il);

        cursor.EmitDelegate(() => Configs.ItemActions.FixedTooltip && _slotPosition.HasValue);
        ILLabel notFixed = cursor.DefineLabel();
        cursor.EmitBrfalse(notFixed);
        cursor.EmitDelegate(() => (int)(_slotPosition!.Value.X + TextureAssets.InventoryBack.Width()*_scale*1.1f));
        cursor.EmitStarg(4);
        cursor.EmitDelegate(() => {
            int y = (int)_slotPosition!.Value.Y;
            _slotPosition = null;
            return y;
        });
    cursor.EmitStarg(5);
        cursor.MarkLabel(notFixed);
    }

    private static Vector2? _slotPosition = null;
    private static float _scale;
}