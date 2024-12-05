using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using SpikysLib.IL;
using BetterInventory.Default.Catalogues;
using System.Collections.Generic;
using Terraria.ModLoader.IO;

namespace BetterInventory.ItemSearch.BetterGuide;

public sealed class CraftInMenuPlayer : ModPlayer {
    public static CraftInMenuPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<CraftInMenuPlayer>();

    public override void Load() {

        IL_Player.AdjTiles += static il => {
            if (!il.ApplyTo(ILGuideTileAdj, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        };

        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILVisibility, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        };
        IL_Main.HoverOverCraftingItemButton += static il => {
            if (!il.ApplyTo(ILCraftInGuideMenu, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        };

        s_inventoryTickBorder = Mod.Assets.Request<Texture2D>($"Assets/Inventory_Tick_Border");
    }

    private static void ILVisibility(ILContext il) {
        ILCursor cursor = new(il);

        //         Main.DrawGuideCraftText(num51, color3, out inventoryX, out inventoryY);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(Reflection.Main.DrawGuideCraftText));
        int inventoryY = 123;
        cursor.FindPrev(out var invLocsCursor, i => i.MatchLdloca(out inventoryY));
        int inventoryX = 122;
        invLocsCursor[0].GotoPrev(i => i.MatchLdloca(out inventoryX));
        
        //         ++ if(!<visibilityHover>) {
        //             <handle guide item slot>
        //         ++ }
        //         ItemSlot.Draw(Main.spriteBatch, ref Main.guideItem, ...);
        ILLabel? noHover = null;
        cursor.FindNext(out _, i => i.MatchBlt(out noHover));

        cursor.EmitLdloc(inventoryX).EmitLdloc(inventoryY);
        cursor.EmitDelegate((int x, int y) => Configs.BetterGuide.CraftInMenu && HandleVisibility(x, y));
        cursor.EmitBrtrue(noHover!);

        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //         ++ <drawVisibility>
        cursor.EmitLdloc(inventoryX).EmitLdloc(inventoryY);
        cursor.EmitDelegate((int x, int y) => {
            // TODO GuideTile dependency
            if (Configs.BetterGuide.GuideTile) GuideTilePlayer.DrawGuideTile(x, y);
            if (Configs.BetterGuide.CraftInMenu) DrawVisibility();
        });
    }
    public static bool HandleVisibility(int x, int y) {
        s_visibilityHover = false;
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = s_inventoryTickBorder.Size() * Main.inventoryScale;
        s_hitBox = new(x - (int)(size.X / 2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (PlayerInput.IgnoreMouseInterface || !s_hitBox.Contains(Main.mouseX, Main.mouseY)) return false;

        if (Main.mouseLeft && Main.mouseLeftRelease) {
            var localPlayer = LocalPlayer;
            localPlayer.ToggleFlag(CurrentVisibilityFlag);
            SoundEngine.PlaySound(SoundID.MenuTick);
            Recipe.FindRecipes();
        }
        return Main.player[Main.myPlayer].mouseInterface = s_visibilityHover = true;
    }
    public static void DrawVisibility() {
        bool showAll = LocalPlayer.visibility.HasFlag(CurrentVisibilityFlag);
        Asset<Texture2D> tick = showAll ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Color color = Color.White * 0.7f;
        Main.spriteBatch.Draw(tick.Value, s_hitBox.Center(), null, color, 0f, tick.Value.Size() / 2, 1, 0, 0f);
        if (s_visibilityHover) {
            string key = showAll ? $"{Localization.Keys.UI}.ShowAll" : $"{Localization.Keys.UI}.ShowAvailable";
            Main.instance.MouseText(Language.GetTextValue($"{Localization.Keys.UI}.Filter", Language.GetTextValue(key), Main.numAvailableRecipes));
            Main.spriteBatch.Draw(s_inventoryTickBorder.Value, s_hitBox.Center(), null, color, 0f, s_inventoryTickBorder.Value.Size() / 2, 1, 0, 0f);
        }
    }

    // TODO GuideTile dependency
    private static void ILCraftInGuideMenu(ILContext il) {
        ILCursor cursor = new(il);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <craftInMenu>]) {
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.guideItem));
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.Item.IsAir.GetMethod!));
        cursor.EmitDelegate((bool isAir) => (isAir && (!Configs.BetterGuide.GuideTile || GuideTile.guideTile.IsAir)) || Configs.BetterGuide.CraftInMenu);

        //     <craft>
        // }
    }

    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback itemConsumedCallback) {
        itemConsumedCallback = null!;
        return Configs.BetterGuide.CraftInMenu && !RecipeList.Instance.Enabled ? [Main.guideItem] : [];
    }
    private static void ILGuideTileAdj(ILContext il) {
        ILCursor cursor = new(il);

        // <resetAdjTile>
        cursor.GotoNext(MoveType.Before, i => i.MatchLdfld(Reflection.Player.adjWater));

        // ++ <guideTileAdj>
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Player self) => {
            if (!Configs.BetterGuide.CraftInMenu || !Configs.BetterGuide.GuideTile || RecipeList.Instance.Enabled || GuideTile.guideTile.createTile < TileID.Dirt) return;
            self.adjTile[GuideTile.guideTile.createTile] = true;
        });
    }

    public static bool ShowAllRecipes() => ((Main.InGuideCraftMenu || RecipeList.Instance.Enabled) && Configs.BetterGuide.CraftInMenu ? LocalPlayer.visibility : RecipeVisibility.Default).HasFlag(CurrentVisibilityFlag);

    private static Asset<Texture2D> s_inventoryTickBorder = null!;

    private static Item s_dispGuide = new(), s_dispTile = new();

    private static bool s_visibilityHover;
    private static Rectangle s_hitBox;

    public override void SaveData(TagCompound tag) {
        if (visibility != RecipeVisibility.Default) tag[VisibilityTag] = (byte)visibility;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(VisibilityTag, out byte raw)) visibility = (RecipeVisibility)raw;
    }
    public const string VisibilityTag = "visibility";

    public static RecipeVisibility CurrentVisibilityFlag => AvailableRecipes.s_guideRecipes ? RecipeVisibility.ShowAllGuide : RecipeVisibility.ShowAllAir;

    private void ToggleFlag(RecipeVisibility flag) => SetFlag(flag, !visibility.HasFlag(flag));
    private void SetFlag(RecipeVisibility flag, bool set) {
        if (set) visibility |= flag;
        else visibility &= ~flag;
    }
    public RecipeVisibility visibility = RecipeVisibility.Default;

}

[System.Flags]
public enum RecipeVisibility {
    Default = ShowAllGuide,
    ShowAllAir = 1 << 0,
    ShowAllGuide = 1 << 1,
}