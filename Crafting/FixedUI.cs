using System;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib.Extensions;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Crafting;

public sealed class FixedUI : ILoadable {

    public void Load(Mod mod) {
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILFastScroll, Configs.FixedUI.FastScroll)) Configs.UnloadedCrafting.Value.fastScroll = true;
            if (!il.ApplyTo(ILMaterialWrapping, Configs.FixedUI.Wrapping)) Configs.UnloadedCrafting.Value.wrapping = true;
            if (!il.ApplyTo(ILScrollButtonsFix, Configs.FixedUI.ScrollButtons)) Configs.UnloadedCrafting.Value.scrollButtons = true;
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
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_IconsPerRow));

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

    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Configs.FixedUI.CraftWhenHolding, out movedAnItemToAllowCrafting);

    private static int _recDelay = 0;
    public static readonly int[] MaterialsPerLine = [6, 4];

    public const int VanillaMaterialSpacing = 40;
    public const int VanillaCorrection = -2;

}
