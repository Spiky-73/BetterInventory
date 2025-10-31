using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace BetterInventory.Improvements;

public sealed class BetterRecipeGrid : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => !Configs.Compatibility.CompatibilityMode || Configs.Improvements.BetterRecipeGrid;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILRefocusButton, Configs.BetterRecipeGrid.RefocusButton)) Configs.UnloadedImprovements.Instance.betterRecipeGrid_refocusButton = true;
            if (!il.ApplyTo(ILNoRecStartOffset, Configs.BetterRecipeGrid.NoRecStartOffset)) Configs.UnloadedImprovements.Instance.betterRecipeGrid_noRecStartOffset = true;
            if (!il.ApplyTo(ILNoRecListClose, Configs.BetterRecipeGrid.NoRecListClose)) Configs.UnloadedImprovements.Instance.betterRecipeGrid_noRecListClose = true;
            if (!il.ApplyTo(ILCraftOnList, Configs.BetterRecipeGrid.CraftOnRecList)) Configs.UnloadedImprovements.Instance.betterRecipeGrid_craftOnRecipeGrid = true;
            if (!il.ApplyTo(ILScrollButtonsFix, Configs.BetterRecipeGrid.PageScroll)) Configs.UnloadedImprovements.Instance.betterRecipeGrid_pageScroll = true;
        };

        On_Main.DrawInterface_Resources_ClearBuffs += HookRememberListPosition;
        On_Recipe.ClearAvailableRecipes += HookClearAvailableRecipes;

        _craftCenterButton = mod.Assets.Request<Texture2D>($"Assets/RecCenter");
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
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_IconsPerColumn));
        cursor.FindPrevLoc(out _, out int y, i => i.Previous.MatchLdcI4(340), 143);
        cursor.FindPrevLoc(out _, out int x, i => i.Previous.MatchLdcI4(310), 144);

        //     <up/down buttons>
        cursor.GotoNextLoc(out _, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.recStart));

        //     ++ <drawRecipeCount>
        cursor.EmitLdloc(x).EmitLdloc(y);
        cursor.EmitDelegate((int x, int y) => {
            if (Configs.BetterRecipeGrid.RefocusButton) DrawFocusButton(x, y);
        });

        //     while (...) <recipeList>
        // }
    }
    private static void DrawFocusButton(int x, int y) {
        int line = GetRecipeLine(Main.focusRecipe);
        if (0 <= line && line < UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn) return;
        const int size = 20;
        y += 2 + 2 * size;
        x -= size;
        Rectangle hitbox = new(x, y, _craftCenterButton.Width(), _craftCenterButton.Height());
        if (hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.LocalPlayer.mouseInterface = true;
            if (Main.mouseLeftRelease && Main.mouseLeft) {
                Main.recStart = Math.Max(0, SpikysLib.MathHelper.Snap(Main.focusRecipe, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Floor)
                    - UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * (UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn / 2 - 1));
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.mouseLeftRelease = false;
            }
        }
        Main.spriteBatch.Draw(_craftCenterButton.Value, new Vector2(x, y), new(200, 200, 200, 200));
    }

    private static void ILNoRecStartOffset(ILContext il) {
        ILCursor cursor = new(il);

        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_IconsPerColumn));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchStsfld(Reflection.Main.recStart));
        //     ++<no max bound>
        cursor.EmitDelegate((int rs) => !Configs.BetterRecipeGrid.NoRecStartOffset ? rs : Main.recStart);

        //     <handle scroll>
        //     ++<set max bound and snap>
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.TextureAssets.CraftDownButton));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(Reflection.Main.recStart));
        cursor.EmitDelegate(() => {
            if (!Configs.BetterRecipeGrid.NoRecStartOffset) return;
            Main.recStart -= Main.recStart % UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow;
            Main.recStart = Math.Min(Main.recStart, SpikysLib.MathHelper.Snap(Main.numAvailableRecipes, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Ceiling) - UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn);
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
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.numAvailableRecipes));
        cursor.EmitDelegate((int numAvailableRecipes) => Configs.BetterRecipeGrid.NoRecListClose && numAvailableRecipes == 0 ? 1 : numAvailableRecipes);
        //         ...
        //     }
    }

    private static void HookRememberListPosition(On_Main.orig_DrawInterface_Resources_ClearBuffs orig) {
        var start = Main.recStart;
        orig();
        if (Configs.BetterRecipeGrid.RememberListPosition) Main.recStart = start;
    }


    private void HookClearAvailableRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        _focusedRecipeLine = GetRecipeLine(Main.focusRecipe);
        _focusedVisible = !_skipFollow && 0 <= _focusedRecipeLine && _focusedRecipeLine < UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn;
        orig();
    }

    public static void DontFollowOnNextRefocus() {
        _skipFollow = true;
    }

    // TODO Called in DisplayedRecipes
    internal static void HookTryRefocusingList(On_Recipe.orig_TryRefocusingRecipe orig, int oldRecipe) {
        orig(oldRecipe);
        _skipFollow = false;
        if (!Configs.BetterRecipeGrid.RememberListPosition || !_focusedVisible) return;
        Main.recStart = Math.Max(0, SpikysLib.MathHelper.Snap(Main.focusRecipe, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Floor)
            - _focusedRecipeLine * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow);
    }

    private static void ILCraftOnList(ILContext il) {

        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int recipeListIndex, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);

        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        //         ...
        //         if (<mouseHover>) {
        //             Main.player[Main.myPlayer].mouseInterface = true;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(Reflection.Player.mouseInterface));

        ILLabel skipVanillaHover = null!;
        cursor.FindPrev(out _, i => i.MatchBrtrue(out skipVanillaHover!));

        //             if(++[!craftInList] &&<click>) {
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (!Configs.BetterRecipeGrid.CraftOnRecList) return false;
            int f = Main.focusRecipe;
            if (Configs.CraftOnRecipeGrid.Instance.focusHovered) Main.focusRecipe = i;
            Reflection.Main.HoverOverCraftingItemButton.Invoke(i);
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
            if (!Configs.BetterRecipeGrid.CraftOnRecList) return;
            if (Main.numAvailableRecipes > 0 && Main.focusRecipe == i && !Configs.CraftOnRecipeGrid.Instance.focusHovered) ItemSlot.DrawGoldBGForCraftingMaterial = true;
        });
        //     }
        // }
    }

    private static void ILScrollButtonsFix(ILContext il) {
        ILCursor cursor = new(il);
        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_IconsPerColumn));

        cursor.FindNext(out ILCursor[] cursors,
            i => i.MatchLdsfld(Reflection.TextureAssets.CraftUpButton) && i.Next.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!),
            i => i.MatchLdsfld(Reflection.TextureAssets.CraftDownButton) && i.Next.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!)
        );
        for (int j = 0; j < cursors.Length; j++) {
            ILCursor c = cursors[j];
            // if (<upVisible> / <downVisible>) {
            //     if(<hover>) {
            //         Main.player[Main.myPlayer].mouseInterface = true;
            c.GotoPrev(i => i.MatchStfld(Reflection.Player.mouseInterface));
            c.GotoNext(i => i.MatchStsfld(Reflection.Main.recStart));
            c.GotoPrev(MoveType.AfterLabel, i => j == 0 ? i.MatchSub() : i.MatchAdd());

            //         ++ <listScroll>
            c.EmitDelegate((int delta) => {
                if (!Configs.BetterRecipeGrid.PageScroll) return delta;
                return UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn;
            });
            //     }
            // }
        }
        // foreach (ILCursor c in cursors) {
        //     // if (<upVisible> / <downVisible>) {
        //     //     if(<hover>) {
        //     //         Main.player[Main.myPlayer].mouseInterface = true;
        //     c.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));

        //     //         ++ <listScroll>
        //     c.EmitDelegate(() => {
        //         if (!Configs.FixedUI.ScrollButtons || !Main.mouseLeft) return;
        //         if (Main.mouseLeftRelease || _recDelay == 0) {
        //             Main.mouseLeftRelease = true;
        //             _recDelay = 1;
        //         } else _recDelay--;
        //     });
        //     //     }
        //     // }
        // }
        // }

    }

    public static int GetRecipeLine(int availableRecipeIndex) {
        int delta = availableRecipeIndex - Main.recStart;
        int line = delta / UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow;
        if (delta < 0) line--;
        return line;
    }

    private static bool _skipFollow;
    private static bool _focusedVisible;
    private static int _focusedRecipeLine;

    private static Asset<Texture2D> _craftCenterButton = null!;
}
