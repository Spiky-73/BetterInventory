using System.Collections.Generic;
using BetterInventory.ItemSearch;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.Crafting;

public sealed class RecipeFiltering {

    public static RecipeFilters LocalFilters => ItemActions.BetterPlayer.LocalPlayer.RecipeFilters;

    public void Load(Mod mod) {}
    public void Unload() { }

    internal static void ILDrawFilters(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStloc(124)); // int num63
        cursor.GotoPrev(MoveType.After, i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentRecipeSmall));

        //     ++<drawFilters>
        cursor.EmitLdloc(13); // int num54
        cursor.EmitDelegate((int screenY) => {
            if (Configs.RecipeFiltering.Enabled && s_recipes != 0) DrawFilters(94, 450 + screenY);
        });

        //     ...
        //     if(Main.numAvailableRecipes == 0) ...
        //     else {
        //         int num73 = 94;
        //         int num74 = 450 + num51;
        //         if (++false && Main.InGuideCraftMenu) num74 -= 150;
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.TextureAssets.CraftToggle));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.InGuideCraftMenu));
        cursor.EmitDelegate((bool inGuide) => !Configs.RecipeFiltering.Enabled && inGuide);
        //         ...
        //     }
    }

    public static void DrawFilters(int hammerX, int hammerY){
        static void OnFilterChanges() {
            if (!Configs.BetterGuide.AvailablesRecipes) Recipe.FindRecipes();
            else Guide.FindGuideRecipes();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        EntryFilterer<Item, CreativeFilterWrapper> filters = LocalFilters.Filterer;
        
        int i = 0;
        int delta = TextureAssets.InfoIcon[13].Width() + 2;
        int y = hammerY + TextureAssets.CraftToggle[0].Height() - TextureAssets.InfoIcon[0].Width()/2;
        while (i < LocalFilters.Filterer.AvailableFilters.Count) {
            int x = hammerX - TextureAssets.InfoIcon[0].Width() - 1;
            for(int d = 0; i < filters.AvailableFilters.Count && d < Configs.RecipeFiltering.Value.width; i++){
                bool active = filters.IsFilterActive(i);
                if (Configs.RecipeFiltering.Value.hideUnavailable && s_recipesInFilter[i] == 0 && !active) continue;
                Rectangle hitbox = new(x, y, RecipeFilterBack.Width(), RecipeFilterBack.Height());
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
                        Main.spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, hitbox.Center(), null, Main.OurFavoriteColor, 0, TextureAssets.InfoIcon[13].Size() / 2, 1, SpriteEffects.None, 0);
                    }
                    if (s_recipesInFilter[i] == 0) rare = -1;
                    Main.instance.MouseText(name, rare);
                }

                Color color;
                if (s_recipesInFilter[i] != 0) {
                    color = Color.White;
                    if (!active) color *= 0.2f;
                }
                else {
                    color = new Color(96, 96, 96);
                    if (!active) color *= 0.05f;
                }

                Main.spriteBatch.Draw(RecipeFilterBack.Value, hitbox.Center(), null, color, 0, RecipeFilterBack.Size() / 2, 1, SpriteEffects.None, 0);
                Rectangle frame = filters.AvailableFilters[i].GetSourceFrame();
                Main.spriteBatch.Draw(RecipeFilters.Value, hitbox.Center(), frame, color, 0, frame.Size() / 2, 1, SpriteEffects.None, 0);

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

    private static readonly List<int> s_recipesInFilter = new();
    private static int s_recipes;

    public static Asset<Texture2D> RecipeFilterBack => ModContent.Request<Texture2D>($"BetterInventory/Assets/Recipe_Filter_Back");
    public static Asset<Texture2D> RecipeFilters => ModContent.Request<Texture2D>($"BetterInventory/Assets/Recipe_Filters");
}
