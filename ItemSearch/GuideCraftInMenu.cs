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
using BetterInventory.ItemActions;
using SpikysLib.IL;
using BetterInventory.Default.Catalogues;

namespace BetterInventory.ItemSearch;

public sealed class GuideCraftInMenu : ILoadable {

    // TODO split
    public static VisibilityFilters LocalFilters => BetterPlayer.LocalPlayer.VisibilityFilters;

    public void Load(Mod mod) {
        On_Player.AdjTiles += HookGuideTileAdj;
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILVisibility, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        };
        IL_Main.HoverOverCraftingItemButton += static il => {
            if (!il.ApplyTo(ILCraftInGuideMenu, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        };

        s_inventoryTickBorder = mod.Assets.Request<Texture2D>($"Assets/Inventory_Tick_Border");
    }

    public void Unload() { }


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
            if (Configs.BetterGuide.GuideTile) Guide.DrawGuideTile(x, y);
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
            LocalFilters.ShowAllRecipes = !LocalFilters.ShowAllRecipes;
            SoundEngine.PlaySound(SoundID.MenuTick);
            Recipe.FindRecipes();
        }
        return Main.player[Main.myPlayer].mouseInterface = s_visibilityHover = true;
    }
    public static void DrawVisibility() {
        VisibilityFilters filters = LocalFilters;
        Asset<Texture2D> tick = filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Color color = Color.White * 0.7f;
        Main.spriteBatch.Draw(tick.Value, s_hitBox.Center(), null, color, 0f, tick.Value.Size() / 2, 1, 0, 0f);
        if (s_visibilityHover) {
            string key = filters.ShowAllRecipes ? $"{Localization.Keys.UI}.ShowAll" : $"{Localization.Keys.UI}.ShowAvailable";
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
        cursor.EmitDelegate((bool isAir) => (isAir && (!Configs.BetterGuide.GuideTile || Guide.guideTile.IsAir)) || Configs.BetterGuide.CraftInMenu);

        //     <craft>
        // }
    }

    public static Item? GetGuideMaterials() => Configs.BetterGuide.CraftInMenu && !RecipeList.Instance.Enabled ? Main.guideItem : null;
    private static void HookGuideTileAdj(On_Player.orig_AdjTiles orig, Player self) {
        orig(self);
        if (!Configs.BetterGuide.CraftInMenu || !Configs.BetterGuide.GuideTile || RecipeList.Instance.Enabled || Guide.guideTile.createTile < TileID.Dirt) return;
        self.adjTile[Guide.guideTile.createTile] = true;
        Recipe.FindRecipes();
    }

    // TODO make independent
    public static bool ShowAllRecipes() => Configs.BetterGuide.CraftInMenu ? LocalFilters.ShowAllRecipes : GuideAvailableRecipes.s_guideRecipes;

    private static Asset<Texture2D> s_inventoryTickBorder = null!;

    private static Item s_dispGuide = new(), s_dispTile = new();


    private static bool s_visibilityHover;
    private static Rectangle s_hitBox;
}
