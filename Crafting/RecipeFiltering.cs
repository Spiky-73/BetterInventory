using System.Collections.Generic;
using BetterInventory.ItemSearch;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;

namespace BetterInventory.Crafting;

public sealed class RecipeFiltering : ILoadable {

    public static RecipeFilters LocalFilters => ItemActions.BetterPlayer.LocalPlayer.RecipeFilters;

    public void Load(Mod mod) {
        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILDrawFilters, Configs.RecipeFilters.Enabled)) Configs.UnloadedCrafting.Value.recipeFilters = true;
        };

        s_recipeFilterBack = mod.Assets.Request<Texture2D>($"Assets/Recipe_Filter_Back");
        s_recipeFilters = mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters");
        s_recipeFiltersGray = mod.Assets.Request<Texture2D>($"Assets/Recipe_Filters_Gray");
        s_filtersOutline = TextureAssets.InfoIcon[13];
    }

    public void Unload() {}

    private static void ILDrawFilters(ILContext il) {
        ILCursor cursor = new(il);

        // BetterGameUI Compatibility
        int screenY = 13;
        if(cursor.TryGotoNext(i => i.SaferMatchCallvirt(Reflection.AccessorySlotLoader.DrawAccSlots))) {
            cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.screenHeight));
            cursor.GotoNextLoc(out screenY, i => true, 13);
        }

        // ...
        // if(<showRecipes>){
        cursor.GotoRecipeDraw();

        //     ++<drawFilters>
        cursor.EmitLdloc(screenY); // int num54
        cursor.EmitDelegate((int y) => {
            if (Configs.RecipeFilters.Enabled && s_recipes != 0) DrawFilters(94, 450 + y);
        });

        //     ...
        //     if(Main.numAvailableRecipes == 0) ...
        //     else {
        //         int num73 = 94;
        //         int num74 = 450 + num51;
        //         if (++false && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.InGuideCraftMenu));
        cursor.EmitDelegate((bool inGuide) => !Configs.RecipeFilters.Enabled && inGuide);
        //         ...
        //     }
    }

    public static void DrawFilters(int hammerX, int hammerY){
        static void OnFilterChanges() {
            if (!Configs.BetterGuide.AvailableRecipes) Recipe.FindRecipes();
            else Guide.FindGuideRecipes();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        EntryFilterer<Item, CreativeFilterWrapper> filters = LocalFilters.Filterer;
        
        int i = 0;
        int delta = s_filtersOutline.Width() + 2;
        int y = hammerY + TextureAssets.CraftToggle[0].Height() - TextureAssets.InfoIcon[0].Width()/2;
        while (i < LocalFilters.Filterer.AvailableFilters.Count) {
            int x = hammerX - TextureAssets.InfoIcon[0].Width() - 1;
            for(int d = 0; i < filters.AvailableFilters.Count && d < Configs.RecipeFilters.Value.filtersPerLine; i++){
                bool active = filters.IsFilterActive(i);
                if (Configs.RecipeFilters.Value.hideUnavailable && s_recipesInFilter[i] == 0 && !active) continue;
                Rectangle hitbox = new(x, y, s_recipeFilterBack.Width(), s_recipeFilterBack.Height());
                if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                    Main.LocalPlayer.mouseInterface = true;
                    int rare = 0;
                    string name = Language.GetTextValue(filters.AvailableFilters[i].GetDisplayNameKey());
                    if (s_recipesInFilter[i] != 0 || active) {
                        if (Main.mouseLeft && Main.mouseLeftRelease) {
                            bool keepOn = !active || filters.ActiveFilters.Count > 1;
                            filters.ActiveFilters.Clear();
                            if (keepOn) filters.ActiveFilters.Add(filters.AvailableFilters[i]);
                            OnFilterChanges();
                        }
                        else if (Main.mouseRight && Main.mouseRightRelease) {
                            filters.ToggleFilter(i);
                            OnFilterChanges();
                        }
                        name = Language.GetTextValue($"{Localization.Keys.UI}.Filter", name, s_recipesInFilter[i]);
                        Main.spriteBatch.Draw(s_filtersOutline.Value, hitbox.Center(), null, Main.OurFavoriteColor, 0, s_filtersOutline.Size() / 2, 1, SpriteEffects.None, 0);
                    }
                    if (s_recipesInFilter[i] == 0) rare = -1;
                    Main.instance.MouseText(name, rare);
                }

                Color color = Color.White;
                Color colorBack = s_recipesInFilter[i] != 0 ? UICommon.DefaultUIBlue : new(64, 64, 64);
                if (!active) {
                    color = color.MultiplyRGBA(new(80, 80, 80, 70));
                    colorBack = colorBack.MultiplyRGBA(new(80, 80, 80, 70));
                }

                Main.spriteBatch.Draw(s_recipeFilterBack.Value, hitbox.Center(), null, colorBack, 0, s_recipeFilterBack.Size() / 2, 1, SpriteEffects.None, 0);
                Rectangle frame = filters.AvailableFilters[i].GetSourceFrame();
                Main.spriteBatch.Draw((s_recipesInFilter[i] != 0 ? s_recipeFilters : s_recipeFiltersGray).Value, hitbox.Center(), frame, color, 0, frame.Size() / 2, 1, SpriteEffects.None, 0);

                x += delta;
                d++;
            }
            y += delta;
        }
    }

    public static void ClearFilters() {
        s_recipes = 0;
        s_recipesInFilter.Clear();
        for (int i = 0; i < LocalFilters.Filterer.AvailableFilters.Count; i++) s_recipesInFilter.Add(0);
    }

    public static bool FitsFilters(int recipe){
        EntryFilterer<Item, CreativeFilterWrapper> filterer = LocalFilters.Filterer;
        bool fits = false;
        s_recipes++;
        for (int i = 0; i < filterer.AvailableFilters.Count; i++) {
            if (filterer.AvailableFilters[i].FitsFilter(Main.recipe[recipe].createItem)) {
                s_recipesInFilter[i]++;
                fits |= filterer.ActiveFilters.Count == 0 || filterer.IsFilterActive(i);
            }
        }
        return fits;
    }

    private static readonly List<int> s_recipesInFilter = [];
    private static int s_recipes;

    private static Asset<Texture2D> s_recipeFilterBack = null!;
    internal static Asset<Texture2D> s_recipeFilters = null!;
    private static Asset<Texture2D> s_recipeFiltersGray = null!;
    private static Asset<Texture2D> s_filtersOutline = null!;
}
