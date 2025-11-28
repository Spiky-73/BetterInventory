using System;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using ReLogic.Graphics;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace BetterInventory.VisualChanges;

public sealed class RecipeCount : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || Configs.VisualChanges.RecipeCount;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILRecipeCount, Configs.VisualChanges.RecipeCount)) Configs.UnloadedVisualChanges.Instance.recipeCount = true;
        };
    }
    public void Unload() { }

    private static void ILRecipeCount(ILContext il) {
        ILCursor cursor = new(il);

        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        //     int num77 = 340; // y
        //     int num78 = 310; // x
        //     UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow = num79;
        //     UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn = num80;
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_IconsPerColumn));
        cursor.FindPrevLoc(out _, out int y, i => i.Previous.MatchLdcI4(340), 143);
        cursor.FindPrevLoc(out _, out int x, i => i.Previous.MatchLdcI4(310), 144);

        //     <up/down buttons>
        cursor.GotoNextLoc(out _, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.recStart));

        //     ++ <drawRecipeCount>
        cursor.EmitLdloc(x).EmitLdloc(y);
        cursor.EmitDelegate((int x, int y) => {
            if (Configs.VisualChanges.RecipeCount) DrawRecipeCount(x, y);
        });

        //     while (...) <recipeList>
        // }
    }
    private static void DrawRecipeCount(int x, int y) {
        int padding = 20 - TextureAssets.CraftUpButton.Width();
        x -= 20 + padding;
        y += 2 + TextureAssets.CraftUpButton.Width() + padding / 2;
        DynamicSpriteFont font = FontAssets.ItemStack.Value;
        int displayedRecipes = UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow;
        string text = $"{Main.recStart + 1}-{Math.Min(Main.numAvailableRecipes, Main.recStart + displayedRecipes)} ({Main.numAvailableRecipes})";
        Vector2 origin = font.MeasureString(text);
        origin.Y *= 0.5f;

        Main.spriteBatch.DrawStringWithShadow(font, text, new(x, y), Color.White, 0, origin, Vector2.One);
    }
}
