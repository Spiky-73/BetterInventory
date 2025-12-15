using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace BetterInventory.Improvements.BetterRecipeGrid;

public static class RefocusButton {

    public static void Load(Mod mod) {
        CraftCenterButton = mod.Assets.Request<Texture2D>($"Assets/RecCenter");
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