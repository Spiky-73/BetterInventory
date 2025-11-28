using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed class InventorySlotsTexture : ILoadable {
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILHideInterfaceText, Configs.InventoryManagement.InventorySlotsTexture)) Configs.UnloadedInventoryManagement.Value.inventorySlotsTexture = true;
        };
        IL_Main.GUIHotbarDrawInner += static il => {
            if (!il.ApplyTo(ILHideInterfaceText, Configs.InventoryManagement.InventorySlotsTexture)) Configs.UnloadedInventoryManagement.Value.inventorySlotsTexture = true;
        };
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILDrawSlotTexture, Configs.InventoryManagement.InventorySlotsTexture)) Configs.UnloadedInventoryManagement.Value.inventorySlotsTexture = true;
        };

        s_inventorySlotsTextures = mod.Assets.Request<Texture2D>($"Assets/Inventory_Slots_Textures");
    }

    public void Unload() { }

    private static void ILHideInterfaceText(ILContext context) {
        ILCursor cursor = new(context);

        // <ld args>
        // if (!hidden) DynamicSpriteFontExtensionMethods.DrawString(...)
        // else <pop args>
        while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.SaferMatchCall(Reflection.DynamicSpriteFontExtensionMethods.DrawString_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_float_SpriteEffects_float))) {
            ILLabel skipLabel = cursor.DefineLabel();
            ILLabel postLabel = cursor.DefineLabel();
            cursor.EmitDelegate(() => Configs.InventoryManagement.InventorySlotsTexture);
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

        cursor.GotoNextLoc(out int inventoryScale, i => i.Previous.MatchLdsfld(Reflection.Main.inventoryScale), 2);
        // cursor.GotoNextLoc(out int color, i => i.Previous.MatchCall(Reflection.Color.White.GetMethod!), 3);
        // cursor.GotoNextLoc(out int texture, i => i.Previous.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!), 7);

        // ...
        // int num9 = context switch { ... };
        // ++ if (context == [...]) num9 = 1;
        cursor.GotoNext(i => i.SaferMatchCallvirt(Reflection.AccessorySlotLoader.DrawSlotTexture));
        cursor.GotoPrevLoc(out int icon, i => i.Previous.MatchLdcI4(0) && i.Next.MatchBr(out _), 11);
        cursor.GotoPrev(MoveType.After, i => i.MatchStloc(icon) && i.Previous.MatchLdcI4(-1));
        cursor.EmitLdarg2().EmitLdloc(icon);
        cursor.EmitDelegate((int context, int icon) => Configs.InventoryManagement.InventorySlotsTexture && HasCustomTexture(context) ? 1 : icon);
        cursor.EmitStloc(icon);
        
        // if ((item.type <= 0 || item.stack <= 0) && num9 != -1) {
        //     if (<modded slot>) <draw modded textures>
        //     ++ else if (context == [...]) <draw inventory textures>
        //     else <draw vanilla textures>
        // }
        cursor.GotoNext(i => i.SaferMatchCallvirt(Reflection.AccessorySlotLoader.DrawSlotTexture));
        ILLabel postTextureIf = null!;
        cursor.GotoNext(i => i.MatchBr(out postTextureIf!));
        cursor.GotoNext(MoveType.AfterLabel, i => true);

        cursor.EmitLdarg0().EmitLdarg2().EmitLdarg3().EmitLdarg(4).EmitLdloc(inventoryScale);
        cursor.EmitDelegate((SpriteBatch spriteBatch, int context, int slot, Vector2 position, float inventoryScale) => {
            if (!Configs.InventoryManagement.InventorySlotsTexture || !HasCustomTexture(context)) return false;
            Asset<Texture2D> texture = s_inventorySlotsTextures;
            int frame = context switch {
                ItemSlot.Context.InventoryAmmo => 0,
                ItemSlot.Context.InventoryCoin => 1,
                ItemSlot.Context.GuideItem => slot == 0 ? 2 : 3,
                ItemSlot.Context.PrefixItem => 4,
                _ => -1
            };
            Rectangle rectangle = texture.Frame(5, 1, frame);
            spriteBatch.Draw(texture.Value, position + TextureAssets.InventoryBack.Size() / 2f * inventoryScale, rectangle, Color.White * 0.35f, 0f, rectangle.Size() / 2f, inventoryScale, 0, 0f);
            return true;
        });
        cursor.EmitBrtrue(postTextureIf);
    }

    public static bool HasCustomTexture(int context) => context is ItemSlot.Context.InventoryAmmo or ItemSlot.Context.InventoryCoin or ItemSlot.Context.GuideItem or ItemSlot.Context.PrefixItem;

    private static Asset<Texture2D> s_inventorySlotsTextures = null!;
}