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
using Terraria.UI.Gamepad;

namespace BetterInventory.BetterRecipeList;

public sealed class RefocusButton : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterRecipeListConfig.RefocusButton;
    public void Load(Mod mod) {
        IL_Main.DrawInventory += il => il.TryEdit(ILRefocusButton, ref UnloadedBetterRecipeListConfig.Instance.refocusButton);

        CraftCenterButton = mod.Assets.Request<Texture2D>($"Assets/RecCenter");
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
            if (BetterRecipeListConfig.RefocusButton) DrawButton(x, y);
        });

        //     while (...) <recipeList>
        // }
    }

    public static void DrawButton(int x, int y) {
        if (Main.recStart <= Main.focusRecipe && Main.focusRecipe < Main.recStart + UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn * UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow) return;
        const int size = 20;
        y += 2 + 2 * size;
        x -= size;
        Rectangle hitbox = new(x, y, CraftCenterButton.Width(), CraftCenterButton.Height());
        if (hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.LocalPlayer.mouseInterface = true;
            if (Main.mouseLeftRelease && Main.mouseLeft) {
                Main.recStart = Math.Max(0, SpikysLib.MathHelper.Snap(Main.focusRecipe, UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow, SpikysLib.MathHelper.SnapMode.Floor)
                    - UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow * (UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn / 2 - 1));
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.mouseLeftRelease = false;
            }
        }
        Main.spriteBatch.Draw(CraftCenterButton.Value, new Vector2(x, y), new(200, 200, 200, 200));
    }

    public static Asset<Texture2D> CraftCenterButton = null!;
}