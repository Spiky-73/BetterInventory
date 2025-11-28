using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemActions;

public class FixedTooltipPosition : ILoadable {
    
    public void Load(Mod mod) {
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookFindSlotPosition;
        IL_Main.MouseTextInner += static il => {
            if (!il.ApplyTo(ILFixTooltip, Configs.ItemActions.FixedTooltipPosition)) Configs.UnloadedItemActions.Value.fixedTooltipPosition = true;
        };
    }
    public void Unload() {}
    
    private static void HookFindSlotPosition(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        if (Configs.ItemActions.FixedTooltipPosition) {
            Rectangle rect = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
            if (rect.Contains(Main.mouseX, Main.mouseY)) {
                _slotPosition = position;
                _scale = Main.inventoryScale;
            }
        }
        orig(spriteBatch, inv, context, slot, position, lightColor);
    }

    private static void ILFixTooltip(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(Reflection.Main.MouseText_DrawItemTooltip));
        cursor.EmitDelegate((int x, int y) => {
            if (Configs.ItemActions.FixedTooltipPosition && _slotPosition.HasValue) {
                x = (int)(_slotPosition.Value.X + TextureAssets.InventoryBack.Width() * _scale * 1.1f);
                y = (int)_slotPosition.Value.Y;
                _slotPosition = null;
            }
            _ilY = y;
            return x;
        });
        cursor.EmitDelegate(() => _ilY);
    }
    private static int _ilY;

    private static Vector2? _slotPosition = null;
    private static float _scale;
}
