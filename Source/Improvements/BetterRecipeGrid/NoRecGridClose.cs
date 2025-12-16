using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public sealed class NoRecGridClose : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeGridConfig.NoRecGridClose;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            il.TryEdit(ILNoRecListClose, ref UnloadedBetterRecipeGridConfig.Instance.noRecGridClose);
        };
    }
    public void Unload() { }

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
            return BetterRecipeGridConfig.NoRecGridClose && numAvailableRecipes == 0 ? 1 : numAvailableRecipes;
        });
        //         ...
        //     }
    }
}
