using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class Crafting : ILoadable {

    public static Configs.Crafting Config => Configs.Crafting.Instance;
    
    public void Load(Mod mod) {}
    public void Unload(){}

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

        ILLabel? skipVanillaHover = null;
        cursor.FindPrev(out _, i => i.MatchBrtrue(out skipVanillaHover));

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
        cursor.EmitBrtrue(skipVanillaHover!);
        //                 <scrollList>
        //                 ...
        //             }
        //             ...
        //         }

        cursor.GotoLabel(skipVanillaHover!, MoveType.AfterLabel);
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (Main.numAvailableRecipes > 0 && Main.focusRecipe == i && Config.craftOnList.Parent && !Config.craftOnList.Value.focusRecipe) ItemSlot.DrawGoldBGForCraftingMaterial = true;
        });
        //     }
        // }
    }

    public static Item? GetMouseMaterial() => Config.mouseMaterial ? Main.mouseItem : null;
}
