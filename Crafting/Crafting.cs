using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class Crafting : ILoadable {
    
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
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(Reflection.Player.mouseInterface));

        ILLabel? skipVanillaHover = null;
        cursor.FindPrev(out _, i => i.MatchBrtrue(out skipVanillaHover));

        //             if(++[!craftInList] &&<click>) {
        cursor.EmitLdloc(153); // int num87
        cursor.EmitDelegate((int i) => {
            if (!Configs.CraftOnList.Enabled) return false;
            int f = Main.focusRecipe;
            if (Configs.CraftOnList.Value.focusRecipe) Main.focusRecipe = i;
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
            if (!Configs.CraftOnList.Enabled) return;
            if (Main.numAvailableRecipes > 0 && Main.focusRecipe == i && !Configs.CraftOnList.Value.focusRecipe) ItemSlot.DrawGoldBGForCraftingMaterial = true;
        });
        //     }
        // }
    }

    public static Item? GetMouseMaterial() => Configs.Crafting.MouseMaterial ? Main.mouseItem : null;
}
