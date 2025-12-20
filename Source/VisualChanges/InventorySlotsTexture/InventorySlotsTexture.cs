using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using ReLogic.Graphics;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.VisualChanges.InventorySlotsTexture;

public sealed class InventorySlotsTexture : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || VisualChangesConfig.InventorySlotsTexture;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += il => il.TryEdit(ILHideInterfaceText, ref UnloadedVisualChangesConfig.Instance.inventorySlotsTexture);
        IL_Main.GUIHotbarDrawInner += il => il.TryEdit(ILHideInterfaceText, ref UnloadedVisualChangesConfig.Instance.inventorySlotsTexture);
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += il => il.TryEdit(ILDrawSlotTexture, ref UnloadedVisualChangesConfig.Instance.inventorySlotsTexture);

        InventorySlotsTextures = mod.Assets.Request<Texture2D>($"Assets/Inventory_Slots_Textures");
    }
    public void Unload() { }

    private static void ILHideInterfaceText(ILContext context) {
        ILCursor cursor = new(context);

        // <ld args>
        // if (!hidden) DynamicSpriteFontExtensionMethods.DrawString(...)
        // else <pop args>
        while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.SaferMatchCall((SpriteBatch s, DynamicSpriteFont f, Vector2 v, Color c, SpriteEffects e) => s.DrawString(f, "", v, c, 0, v, 0, e, 0)))) {
            ILLabel skipLabel = cursor.DefineLabel();
            ILLabel postLabel = cursor.DefineLabel();
            cursor.EmitDelegate(() => VisualChangesConfig.InventorySlotsTexture);
            cursor.EmitBrtrue(skipLabel);
            cursor.MarkLabel(skipLabel); // Here in case of exception
            cursor.GotoNext(MoveType.After, i => true);
            cursor.EmitBr(postLabel);
            cursor.MarkLabel(postLabel); // Here in case of exception
            cursor.MarkLabel(skipLabel);
            for (int i = 0; i < 10; i++) cursor.EmitPop();
            cursor.MarkLabel(postLabel);
        }
    }

    private static void ILDrawSlotTexture(ILContext context) {
        ILCursor cursor = new(context);

        cursor.GotoNextLoc(out int inventoryScale, i => i.Previous.MatchLdsfld(() => Main.inventoryScale), 2);

        // ...
        // int num9 = context switch { ... };
        // ++ if (context == [...]) num9 = 1;
        cursor.GotoNext(i => i.SaferMatchCallvirt((AccessorySlotLoader l) => l.DrawSlotTexture));
        cursor.GotoPrevLoc(out int icon, i => i.Previous.MatchLdcI4(0) && i.Next.MatchBr(out _), 11);
        cursor.GotoPrev(MoveType.After, i => i.MatchStloc(icon) && i.Previous.MatchLdcI4(-1));
        cursor.EmitLdarg2().EmitLdarg3().EmitLdloc(icon);
        cursor.EmitDelegate((int context, int slot, int icon) => VisualChangesConfig.InventorySlotsTexture && TryGetCustomTexture(context, slot, out _) ? 1 : icon);
        cursor.EmitStloc(icon);

        // if ((item.type <= 0 || item.stack <= 0) && num9 != -1) {
        //     if (<modded slot>) <draw modded textures>
        //     ++ else if (context == [...]) <draw inventory textures>
        //     else <draw vanilla textures>
        // }
        cursor.GotoNext(i => i.SaferMatchCallvirt((AccessorySlotLoader l) => l.DrawSlotTexture));
        ILLabel postTextureIf = null!;
        cursor.GotoNext(i => i.MatchBr(out postTextureIf!));
        cursor.GotoNext(MoveType.AfterLabel, i => true);

        cursor.EmitLdarg0().EmitLdarg2().EmitLdarg3().EmitLdarg(4).EmitLdloc(inventoryScale);
        cursor.EmitDelegate((SpriteBatch spriteBatch, int context, int slot, Vector2 position, float inventoryScale) => {
            if (!VisualChangesConfig.InventorySlotsTexture || !TryGetCustomTexture(context, slot, out int frame)) return false;
            Rectangle rectangle = InventorySlotsTextures.Frame(5, 1, frame);
            spriteBatch.Draw(InventorySlotsTextures.Value, position + TextureAssets.InventoryBack.Size() / 2f * inventoryScale, rectangle, Color.White * 0.35f, 0f, rectangle.Size() / 2f, inventoryScale, 0, 0f);
            return true;
        });
        cursor.EmitBrtrue(postTextureIf);
    }

    public static bool TryGetCustomTexture(int context, int slot, out int frame) => ContextCustomTextureFrame.TryGetValue((context, slot), out frame) || ContextCustomTextureFrame.TryGetValue((context, -1), out frame);
    public static readonly Dictionary<(int context, int slot), int> ContextCustomTextureFrame = new(){
        {(ItemSlot.Context.InventoryAmmo, -1), 0},
        {(ItemSlot.Context.InventoryCoin, -1), 1},
        {(ItemSlot.Context.GuideItem, -1), 2},
        {(ItemSlot.Context.GuideItem, 1), 3},
        {(ItemSlot.Context.PrefixItem, -1), 4},
    };

    public static Asset<Texture2D> InventorySlotsTextures = null!;
}