using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Crafting;

public sealed class MaterialsWrapping : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || Configs.VanillaFixes.MaterialsWrapping;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILMaterialWrapping, Configs.VanillaFixes.MaterialsWrapping)) Configs.UnloadedVanillaFixes.Instance.materialsWrapping = true;
        };
    }
    public void Unload() { }


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
            if (!Configs.VanillaFixes.MaterialsWrapping) return x;
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
            if (!Configs.VanillaFixes.MaterialsWrapping || !Main.recBigList) return y;
            i = i < MaterialsPerLine[0] ? 0 : ((i - MaterialsPerLine[0]) / MaterialsPerLine[1] + 1);
            return y + (VanillaMaterialSpacing + VanillaCorrection) * i;
        });

        //             ...
        //         }
        //     }
        //     ...
        // }
    }

    public static readonly int[] MaterialsPerLine = [6, 4];
    public const int VanillaMaterialSpacing = 40;
    public const int VanillaCorrection = -2;
}
