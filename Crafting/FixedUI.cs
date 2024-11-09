using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

namespace BetterInventory.Crafting;

public sealed class FixedUI : ILoadable {

    public void Load(Mod mod) {
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILFastScroll, Configs.FixedUI.FastScroll)) Configs.UnloadedCrafting.Value.fastScroll = true;
            if (!il.ApplyTo(ILMaterialWrapping, Configs.FixedUI.Wrapping)) Configs.UnloadedCrafting.Value.wrapping = true;
            if (!il.ApplyTo(ILScrollButtonsFix, Configs.FixedUI.ScrollButtons)) Configs.UnloadedCrafting.Value.scrollButtons = true;
            if (!il.ApplyTo(ILRecipeCount, Configs.FixedUI.RecipeCount)) Configs.UnloadedCrafting.Value.recipeCount = true;
            if (!il.ApplyTo(ILNoRecStartOffset, Configs.FixedUI.NoRecStartOffset)) Configs.UnloadedCrafting.Value.noRecStartOffset = true;
            if (!il.ApplyTo(ILNoRecListClose, Configs.FixedUI.NoRecListClose)) Configs.UnloadedCrafting.Value.noRecListClose = true;
        };

    }
    public void Unload(){}

    private static void ILFastScroll(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoRecipeDraw();

        // ...
        // if(<showRecipes>){
        //     for (<recipeIndex>) { 
        cursor.GotoNextLoc(out int recipeIndex, i => i.Next.MatchBr(out _), 124);

        for (int j = 0; j < 2; j++) { // Up and Down

            //     if(<scrool>) {
            //         if(...) SoundEngine.PlaySound(...);
            //         Main.availableRecipeY[num63] += 6.5f;
            cursor.GotoNext(i => i.SaferMatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)));
            cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.recFastScroll));

            // ++ <fastScroll>
            cursor.EmitLdloc(recipeIndex); // int num63
            int s = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int r) => {
                if (!Configs.FixedUI.FastScroll) return;
                Main.availableRecipeY[r] += s * 6.5f;
                float d = Main.availableRecipeY[r] - (r - Main.focusRecipe) * 65;
                bool recFast = Main.recFastScroll && Configs.FastScroll.Value.listScroll;
                if (recFast) d *= 3;
                float old = Main.availableRecipeY[r];
                Main.availableRecipeY[r] -= s == 1 ? MathF.Max(s * 6.5f, d / 10) : MathF.Min(s * 6.5f, d / 10);
                if (old * Main.availableRecipeY[r] < 0) SoundEngine.PlaySound(SoundID.MenuTick);
                if (recFast) Main.availableRecipeY[r] += 130000f * s;
            });
            //         ...
            //     }
        }
        //         ...
        //     }
        //     ...
        // }
    }
    private static void ILMaterialWrapping(ILContext il) {
        ILCursor cursor = new(il);

        // if(<showRecipes>){
        //     ...
        //     if (Main.numAvailableRecipes > 0) {
        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentIngredientsCount));

        cursor.FindPrevLoc(out _, out int materialIndex, i => true, 130); // int num68

        //             int num69 = 80 + num68 * 40;
        cursor.GotoNext(i => i.MatchLdcI4(40));
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(out _));

        //             ++ <wrappingX>
        cursor.EmitLdloc(materialIndex);
        cursor.EmitDelegate((int x, int i) => {
            if (!Configs.FixedUI.Wrapping) return x;
            if (!Main.recBigList) return x + VanillaCorrection * i;
            x -= i * VanillaMaterialSpacing;
            if (i >= MaterialsPerLine[0]) i = MaterialsPerLine[0] - MaterialsPerLine[1] + (i - MaterialsPerLine[0]) % MaterialsPerLine[1];
            return x + (VanillaMaterialSpacing + VanillaCorrection) * i;
        });

        //             int num70 = 380 + num51;
        cursor.GotoNext(i => i.MatchLdcI4(380));
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(out _)); // int num70

        //             ++ <wrappingY>
        cursor.EmitLdloc(materialIndex);
        cursor.EmitDelegate((int y, int i) => {
            if (!Configs.FixedUI.Wrapping || !Main.recBigList) return y;
            i = i < MaterialsPerLine[0] ? 0 : ((i - MaterialsPerLine[0]) / MaterialsPerLine[1] + 1);
            return y + (VanillaMaterialSpacing + VanillaCorrection) * i;
        });

        //             ...
        //         }
        //     }
        //     ...
        // }
    }
    private static void ILScrollButtonsFix(ILContext il) {
        ILCursor cursor = new(il);
        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_IconsPerColumn));

        cursor.FindNext(out ILCursor[] cursors,
            i => i.MatchLdsfld(Reflection.TextureAssets.CraftUpButton) && i.Next.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!),
            i => i.MatchLdsfld(Reflection.TextureAssets.CraftDownButton) && i.Next.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!)
        );
        foreach(ILCursor c in cursors) {
            // if (<upVisible> / <downVisible>) {
            //     if(<hover>) {
            //         Main.player[Main.myPlayer].mouseInterface = true;
            c.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

            //         ++ <listScroll>
            c.EmitDelegate(() => {
                if (!Configs.FixedUI.ScrollButtons || !Main.mouseLeft) return;
                if (Main.mouseLeftRelease || _recDelay == 0) {
                    Main.mouseLeftRelease = true;
                    _recDelay = 1;
                } else _recDelay--;
            });
            //     }
            // }
        }
        // }

    }

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
        cursor.GotoNextLoc(MoveType.After, out int recipeListIndex, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);

        //     ++ <drawRecipeCount>
        cursor.EmitLdloc(x).EmitLdloc(y);
        cursor.EmitDelegate(DraxRecipeCount);

        //     while (...) <recipeList>
        // }
    }
    private static void DraxRecipeCount(int x, int y) {
        if (!Configs.FixedUI.RecipeCount) return;
        int padding = 20 - TextureAssets.CraftUpButton.Width();
        x -= 20 + padding;
        y += 2 + TextureAssets.CraftUpButton.Width() + padding / 2;
        DynamicSpriteFont font = FontAssets.ItemStack.Value;
        int displayedRecipes = UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow;
        string text = $"{Main.recStart+1}-{Math.Min(Main.numAvailableRecipes, Main.recStart + displayedRecipes)} ({Main.numAvailableRecipes})";
        Vector2 origin = font.MeasureString(text);
        origin.Y *= 0.5f;

        Main.spriteBatch.DrawStringWithShadow(font, text, new(x, y), Color.White, 0, origin, Vector2.One);
    }

    private static void ILNoRecStartOffset(ILContext il) {
        ILCursor cursor = new(il);

        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_IconsPerColumn));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchStsfld(Reflection.Main.recStart));
        cursor.EmitDelegate((int rs) => {
            if (!Configs.FixedUI.NoRecStartOffset) return rs;
            int emptySlots = Main.recStart + UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn - Main.numAvailableRecipes;
            if (emptySlots > UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow) Main.recStart -= SpikysLib.MathHelper.Snap(emptySlots,UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Floor);
            return Main.recStart;
        } );

    }

    private static void ILNoRecListClose(ILContext il) {
        ILCursor cursor = new(il);
        // ...
        // if(<showRecipes>){
        cursor.GotoRecipeDraw();

        //     ...
        //     if(Main.numAvailableRecipes == 0) Main.recBigList = false;
        //     else {
        //         int num73 = 94;
        //         int num74 = 450 + num51;
        //         if (++false && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.numAvailableRecipes));
        cursor.EmitDelegate((int numAvailableRecipes) => Configs.FixedUI.NoRecListClose && numAvailableRecipes == 0 ? 1 : numAvailableRecipes);
        //         ...
        //     }
    }

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Configs.FixedUI.CraftWhenHolding, out movedAnItemToAllowCrafting);

    private static int _recDelay = 0;
    public static readonly int[] MaterialsPerLine = [6, 4];

    public const int VanillaMaterialSpacing = 40;
    public const int VanillaCorrection = -2;

}
