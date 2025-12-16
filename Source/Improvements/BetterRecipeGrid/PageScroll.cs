using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public sealed class PageScroll : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeGridConfig.PageScroll;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += il => il.TryEdit(ILScrollButtonsFix, ref UnloadedBetterRecipeGridConfig.Instance.pageScroll);
    }
    public void Unload() { }

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
                if (BetterRecipeGridConfig.PageScroll) return UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn;
                return delta;
            });
            //     }
            // }
        }
        // }

    }
}