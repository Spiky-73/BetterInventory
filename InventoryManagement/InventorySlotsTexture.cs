using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.InventoryManagement;

public sealed class InventorySlotsTexture : ILoadable {
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILHideInterfaceText, Configs.InventoryManagement.InventorySlotsBackground)) Configs.UnloadedInventoryManagement.Value.inventorySlotsBackground = true;
        };
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
            cursor.EmitDelegate(() => Configs.InventoryManagement.InventorySlotsBackground);
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
}