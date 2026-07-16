using System;
using MonoMod.Cil;
using SpikysLib.IL;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace BetterInventory.BetterRecipeList;

public sealed class NoRecGridOffset : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.NoRecGridOffset;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += il => il.TryEdit(ILNoRecStartOffset, ref UnloadedBetterRecipeListConfig.Instance.noRecGridOffset);
    }
    public void Unload() { }

    private static void ILNoRecStartOffset(ILContext il) {
        ILCursor cursor = new(il);

        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        cursor.GotoNext(i => i.MatchStsfld(() => UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchStsfld(() => Main.recStart));
        //     ++<no max bound>
        cursor.EmitDelegate((int rs) => {
            return BetterRecipeListConfig.NoRecGridOffset ? Main.recStart : rs;
        });

        //     <handle scroll>
        //     ++<set max bound and snap>
        cursor.GotoNext(i => i.MatchLdsfld(() => TextureAssets.CraftDownButton));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(() => Main.recStart));
        cursor.EmitDelegate(() => {
            if (!BetterRecipeListConfig.NoRecGridOffset) return;
            Main.recStart -= Main.recStart % UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow;
            Main.recStart = Math.Min(Main.recStart, Math.Max(0, SpikysLib.MathHelper.Snap(Main.numAvailableRecipes, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Ceiling) - UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn));
        });
    }
}