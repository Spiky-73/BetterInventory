using System;
using BetterInventory.ItemSearch;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BetterInventory.Crafting;

public sealed class Tweeks : ILoadable {

    public static Configs.Crafting Config => Configs.Crafting.Instance;
    
    public void Load(Mod mod) {
        IL_Main.DrawInventory += ILScrolls;
    }

    public void Unload(){}

    private static void ILScrolls(ILContext il) {
        ILCursor cursor = new(il);

        // ----- Recipe fast scroll -----
        // ...
        // if(<showRecipes>){
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));

        //     for (<recipeIndex>) {
        //         ...
        for (int j = 0; j < 2; j++) { // Up and Down

            //     if(<scrool>) {
            //         if(...) SoundEngine.PlaySound(...);
            //         Main.availableRecipeY[num63] += 6.5f;
            cursor.GotoNext(i => i.MatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)));
            cursor.GotoNext(i => i.MatchLdsfld(typeof(Main), nameof(Main.recFastScroll)));

            // ++ <custom scroll>
            cursor.EmitLdloc(124);
            int s = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int r) => {
                if (!Configs.Crafting.Instance.recipeScroll) return;
                Main.availableRecipeY[r] += s * 6.5f;
                float d = Main.availableRecipeY[r] - (r - Main.focusRecipe) * 65;
                bool recFast = Main.recFastScroll && Config.recipeScroll.Value.listScroll;
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

        // ----- Material wrapping -----
        //     if (Main.numAvailableRecipes > 0) {
        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        //             int num69 = 80 + num68 * 40;
        //             int num70 = 380 + num51;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(131));

        //             ++ <wrappingX>
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int x, int i) => {
            if (!Config.tweeks) return x;
            if (!Main.recBigList) return x + VanillaCurrection * i;
            x -= i * VanillaMaterialSpcacing;
            if (i >= MaterialsPerLine[0]) i = MaterialsPerLine[0] - MaterialsPerLine[1] + (i - MaterialsPerLine[0]) % MaterialsPerLine[1];
            return x + (VanillaMaterialSpcacing + VanillaCurrection) * i;
        });

        //             ++ <wrappingY>
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(132));
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int y, int i) => {
            if (!Config.tweeks || !Main.recBigList) return y;
            i = i < MaterialsPerLine[0] ? 0 : ((i - MaterialsPerLine[0]) / MaterialsPerLine[1] + 1);
            return y + (VanillaMaterialSpcacing + VanillaCurrection) * i;
        });

        //             ...
        //         }
        //     }
        //     ...
        // }

        // ----- recBigList Scroll Fix ----- 
        // Main.hidePlayerCraftingMenu = false;
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));
        // if(<recBigListVisible>) {
        //     ...
        for (int i = 0; i < 2; i++) {

            // if (<upVisible> / <downVisible>) {
            //     if(<hover>) {
            //         Main.player[Main.myPlayer].mouseInterface = true;
            cursor.GotoNext(i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
            cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

            //         ++ <autoScroll>
            cursor.EmitDelegate(() => { // TODO add audio
                if (!Config.tweeks || !Main.mouseLeft) return;
                if (Main.mouseLeftRelease || _recDelay == 0) {
                    Main.mouseLeftRelease = true;
                    _recDelay = 1;
                } else _recDelay--;
            });

            //         ...
            //     }
            //     Main.spriteBatch.Draw(...);
            cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
            // }
        }

        // ----- Cursor override for recBigList -----
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (<mouseHover>) {
        //             Main.player[Main.myPlayer].mouseInterface = true;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), nameof(Main.LockCraftingForThisCraftClickDuration)));
        cursor.GotoPrev(i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));
        ILLabel? noClick = null;
        cursor.GotoPrev(i => i.MatchBrtrue(out noClick));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

        //             ++ if(<enabled>) goto noClick;
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (Config.focusRecipe) {
                Main.focusRecipe = i;
                Main.recFastScroll = true;
            }
            if (Config.craftingOnRecList) {
                int f = Main.focusRecipe;
                Reflection.Main.HoverOverCraftingItemButton.Invoke(i);
                if (f != Main.focusRecipe) Main.recFastScroll = true;
                Main.craftingHide = false;
                return true;
            }
            Guide.RecipeListHover(i);
            return false;
        });
        cursor.EmitBrtrue(noClick!);
        //             if(<click>) <scrollList>
        //             ...
        //         }
        //         ++ noClick:
        //         ...
        //     }
        // }
        // ...
    }

    private static bool TryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Config.tweeks, out movedAnItemToAllowCrafting);

    private static int _recDelay = 0;
    public static readonly int[] MaterialsPerLine = new int[] { 6, 4 };

    public const int VanillaMaterialSpcacing = 40;
    public const int VanillaCurrection = -2;

}
