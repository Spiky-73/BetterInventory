using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public sealed class CraftOnRecipeGrid : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeGridConfig.CraftOnRecGrid;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += il => il.TryEdit(ILCraftOnList, ref UnloadedBetterRecipeGridConfig.Instance.craftOnRecipeGrid);
    }
    public void Unload() { }

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
            if (!BetterRecipeGridConfig.CraftOnRecGrid) return false;
            int f = Main.focusRecipe;
            if (CraftOnRecipeGridConfig.Instance.focusHovered) Main.focusRecipe = i;
            Main.HoverOverCraftingItemButton(i);
            if (f != Main.focusRecipe) Main.recFastScroll = true;
            Main.craftingHide = false;
            return true;
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
            if (!BetterRecipeGridConfig.CraftOnRecGrid) return;
            if (Main.numAvailableRecipes > 0 && Main.focusRecipe == i && !CraftOnRecipeGridConfig.Instance.focusHovered) ItemSlot.DrawGoldBGForCraftingMaterial = true;
        });
        //     }
        // }
    }
}