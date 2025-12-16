using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public sealed class BetterRecipeGridHooks : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || ImprovementsConfig.BetterRecipeGrid;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            il.TryEdit(ILRefocusButton, ref UnloadedBetterRecipeGridConfig.Instance.refocusButton);
            il.TryEdit(ILNoRecStartOffset, ref UnloadedBetterRecipeGridConfig.Instance.noRecGridOffset);
            il.TryEdit(ILNoRecListClose, ref UnloadedBetterRecipeGridConfig.Instance.noRecGridClose);
            il.TryEdit(ILCraftOnList, ref UnloadedBetterRecipeGridConfig.Instance.craftOnRecipeGrid);
            il.TryEdit(ILScrollButtonsFix, ref UnloadedBetterRecipeGridConfig.Instance.pageScroll);
        };

        On_Main.DrawInterface_Resources_ClearBuffs += HookRememberListPosition;
        On_Recipe.ClearAvailableRecipes += HookClearAvailableRecipes;

        RefocusButton.Load(mod);
    }
    public void Unload() { }

    private static void ILRefocusButton(ILContext il) {
        ILCursor cursor = new(il);

        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        //     int num77 = 340; // y
        //     int num78 = 310; // x
        //     UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow = num79;
        //     UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn = num80;
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(() => UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn));
        cursor.FindPrevLoc(out _, out int y, i => i.Previous.MatchLdcI4(340), 143);
        cursor.FindPrevLoc(out _, out int x, i => i.Previous.MatchLdcI4(310), 144);

        //     <up/down buttons>
        cursor.GotoNextLoc(out _, i => i.Previous.MatchLdsfld(() => Main.recStart), 153);
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdsfld(() => Main.recStart));

        //     ++ <drawRecipeCount>
        cursor.EmitLdloc(x).EmitLdloc(y);
        cursor.EmitDelegate((int x, int y) => {
            if (!ImprovementsConfig.BetterRecipeGrid) return;
            if (BetterRecipeGridConfig.RefocusButton) RefocusButton.DrawButton(x, y);
        });

        //     while (...) <recipeList>
        // }
    }

    private static void ILNoRecStartOffset(ILContext il) {
        ILCursor cursor = new(il);

        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        cursor.GotoNext(i => i.MatchStsfld(() => UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchStsfld(() => Main.recStart));
        //     ++<no max bound>
        cursor.EmitDelegate((int rs) => {
            if (!ImprovementsConfig.BetterRecipeGrid) return rs;
            return BetterRecipeGridConfig.NoRecGridOffset ? Main.recStart : rs;
        });

        //     <handle scroll>
        //     ++<set max bound and snap>
        cursor.GotoNext(i => i.MatchLdsfld(() => TextureAssets.CraftDownButton));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(() => Main.recStart));
        cursor.EmitDelegate(() => {
            if (!ImprovementsConfig.BetterRecipeGrid) return;
            if (BetterRecipeGridConfig.NoRecGridOffset) NoRecGridOffset.PostScroll();
        });
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
        //         if (++[false] && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(() => TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(() => Main.numAvailableRecipes));
        cursor.EmitDelegate((int numAvailableRecipes) => {
            if (!ImprovementsConfig.BetterRecipeGrid) return numAvailableRecipes;
            return BetterRecipeGridConfig.NoRecGridClose && numAvailableRecipes == 0 ? 1 : numAvailableRecipes;
        });
        //         ...
        //     }
    }

    private static void HookRememberListPosition(On_Main.orig_DrawInterface_Resources_ClearBuffs orig) {
        if (!ImprovementsConfig.BetterRecipeGrid || !BetterRecipeGridConfig.RememberGridPosition) {
            orig();
            return;
        }
        RememberGridPosition.PreClearBuffs();
        orig();
        RememberGridPosition.PostClearBuffs();
    }


    private static void HookClearAvailableRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        if (ImprovementsConfig.BetterRecipeGrid && BetterRecipeGridConfig.RememberGridPosition) {
            RememberGridPosition.PreClearAvailableRecipes();
        }
        orig();
    }

    // TODO Called in DisplayedRecipes
    internal static void HookTryRefocusingList(On_Recipe.orig_TryRefocusingRecipe orig, int oldRecipe) {
        orig(oldRecipe);
        if (!ImprovementsConfig.BetterRecipeGrid) return;
        if (BetterRecipeGridConfig.RememberGridPosition) RememberGridPosition.TryRefocusingRecipe();
    }

    private static void ILCraftOnList(ILContext il) {

        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int recipeListIndex, i => i.Previous.MatchLdsfld(() => Main.recStart), 153);

        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (<mouseHover>) {
        //             Main.player[Main.myPlayer].mouseInterface = true;
        cursor.GotoNext(i => i.SaferMatchCall(() => Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld((Player p) => p.mouseInterface));

        ILLabel skipVanillaHover = null!;
        cursor.FindPrev(out _, i => i.MatchBrtrue(out skipVanillaHover!));

        //             if(++[!craftInList] &&<click>) {
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!ImprovementsConfig.BetterRecipeGrid) return false;
            if (BetterRecipeGridConfig.CraftOnRecGrid) {
                CraftOnRecipeGrid.PreCraftItem(i);
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(skipVanillaHover);
        //                 <scrollList>
        //                 ...
        //             }
        //             ...
        //         }

        cursor.GotoLabel(skipVanillaHover, MoveType.AfterLabel);
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!ImprovementsConfig.BetterRecipeGrid) return;
            if (BetterRecipeGridConfig.CraftOnRecGrid) CraftOnRecipeGrid.PostHoverRecipe(i);
        });
        //     }
        // }
    }

    private static void ILScrollButtonsFix(ILContext il) {
        ILCursor cursor = new(il);
        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        cursor.GotoNext(i => i.MatchStsfld(() => UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn));

        cursor.FindNext(out ILCursor[] cursors,
            i => i.MatchLdsfld(() => TextureAssets.CraftUpButton) && i.Next.MatchGetppt((Asset<Texture2D> t) => t.Value),
            i => i.MatchLdsfld(() => TextureAssets.CraftDownButton) && i.Next.MatchGetppt((Asset<Texture2D> t) => t.Value)
        );
        for (int j = 0; j < cursors.Length; j++) {
            ILCursor c = cursors[j];
            // if (<upVisible> / <downVisible>) {
            //     if(<hover>) {
            //         Main.player[Main.myPlayer].mouseInterface = true;
            c.GotoPrev(i => i.MatchStfld((Player p) => p.mouseInterface));
            c.GotoNext(i => i.MatchStsfld(() => Main.recStart));
            c.GotoPrev(MoveType.AfterLabel, i => j == 0 ? i.MatchSub() : i.MatchAdd());

            //         ++ <listScroll>
            c.EmitDelegate((int delta) => {
                if (!ImprovementsConfig.BetterRecipeGrid) return delta;
                if (BetterRecipeGridConfig.PageScroll) return PageScroll.ModifyRecipeScroll(delta);
                return delta;
            });
            //     }
            // }
        }
        // }

    }
}
