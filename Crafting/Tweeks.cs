using System;
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
        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
    }

    public void Unload(){}

    internal static void ILFastScroll(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if(<showRecipes>){
        //     for (<recipeIndex>) { 
        cursor.GotoNext(i => i.MatchStloc(124)); // int num63

        for (int j = 0; j < 2; j++) { // Up and Down

            //     if(<scrool>) {
            //         if(...) SoundEngine.PlaySound(...);
            //         Main.availableRecipeY[num63] += 6.5f;
            cursor.GotoNext(i => i.MatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)));
            cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.recFastScroll));

            // ++ <custom scroll>
            cursor.EmitLdloc(124); // int num63
            int s = j == 0 ? -1 : 1;
            cursor.EmitDelegate((int r) => {
                if (!Configs.Crafting.Instance.recipeScroll.Parent) return;
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
        //     ...
        // }
    }
    internal static void ILMaterialWrapping(ILContext il) {
        ILCursor cursor = new(il);

        // if(<showRecipes>){
        //     ...
        //     if (Main.numAvailableRecipes > 0) {
        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentIngredientsCount));
        
        //             int num69 = 80 + num68 * 40;
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(131)); // int num69

        //             ++ <wrappingX>
        cursor.EmitLdloc(130); // int num68
        cursor.EmitDelegate((int x, int i) => {
            if (!Config.tweeks) return x;
            if (!Main.recBigList) return x + VanillaCurrection * i;
            x -= i * VanillaMaterialSpcacing;
            if (i >= MaterialsPerLine[0]) i = MaterialsPerLine[0] - MaterialsPerLine[1] + (i - MaterialsPerLine[0]) % MaterialsPerLine[1];
            return x + (VanillaMaterialSpcacing + VanillaCurrection) * i;
        });

        //             int num70 = 380 + num51;
        cursor.GotoNext(MoveType.Before, i => i.MatchStloc(132)); // int num70

        //             ++ <wrappingY>
        cursor.EmitLdloc(130); // int num68
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


    }
    internal static void ILListScrollFix(ILContext il) {
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
            cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

            //         ++ <autoScroll>
            cursor.EmitDelegate(() => {
                if (!Config.tweeks || !Main.mouseLeft) return;
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
    internal static void ILCraftOnList(ILContext il) {
        ILCursor cursor = new(il);

        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (<mouseHover>) {
        //             Main.player[Main.myPlayer].mouseInterface = true;
        cursor.GotoNext(i => i.MatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(Reflection.Player.mouseInterface));

        ILLabel? noClick = null;
        cursor.FindPrev(out _, i => i.MatchBrtrue(out noClick));

        //             if(++[!craftInList] &&<click>) {
        cursor.EmitLdloc(153); // int num87
        cursor.EmitDelegate((int i) => {
            if (!Config.craftOnList.Parent) return false;
            int f = Main.focusRecipe;
            if (Config.craftOnList.Value.focusRecipe) Main.focusRecipe = i;
            Reflection.Main.HoverOverCraftingItemButton.Invoke(i);
            if (f != Main.focusRecipe) Main.recFastScroll = true;
            Main.craftingHide = false;
            return true;
        });
        cursor.EmitBrtrue(noClick!);
        //                 <scrollList>
        //                 ...
        //             }
        //             Main.craftingHide = true;

        cursor.GotoNext(i => i.MatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoNext(MoveType.After, i => i.MatchStfld(Reflection.Main.craftingHide));

        //             ...
        //         }
        //     }
        // }
    }
    
    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Config.tweeks, out movedAnItemToAllowCrafting);

    public static Item? GetMouseMaterial() => Config.tweeks ? Main.mouseItem : null;

    private static int _recDelay = 0;
    public static readonly int[] MaterialsPerLine = new int[] { 6, 4 };

    public const int VanillaMaterialSpcacing = 40;
    public const int VanillaCurrection = -2;

}
