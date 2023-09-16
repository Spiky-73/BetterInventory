using System;
using System.Collections.Generic;
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

public sealed class RecipeFiltering : ILoadable {

    public static bool Enabled => Configs.ClientConfig.Instance.recipeFiltering;
    public static RecipeFilters LocalFilters => Globals.BetterPlayer.LocalPlayer.RecipeFilters;

    public void Load(Mod mod) {
        IL_Main.DrawInventory += IlDrawFilters;

        On_Recipe.FindRecipes += HookFilterRecipes;
    }

    public void Unload() {}

    private static void IlDrawFilters(ILContext il) {
        ILCursor cursor = new(il);

        // Mark + Apply noHammer
        ILLabel? postHammer = null;
        cursor.GotoNext(i => i.MatchStloc(138));
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.InGuideCraftMenu) && i.Next.MatchBrfalse(out postHammer));
        cursor.EmitBr(postHammer!);
        cursor.GotoLabel(postHammer!, MoveType.After);
        cursor.EmitLdloc(140);
        cursor.EmitLdloc(141);
        cursor.EmitDelegate(DrawFilters);
    }

    public static void DrawFilters(int hammerX, int hammerY){
        static void OnFilterChanges() {
            FilterRecipes();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        
        if(!Enabled) return;
        int i = 0;
        int x = hammerX - TextureAssets.InfoIcon[0].Width() - 1;

        int delta = TextureAssets.InfoIcon[13].Width() + 2;

        EntryFilterer<Item, CreativeFilterWrapper> filters = LocalFilters.Filterer;
        while (i < LocalFilters.Filterer.AvailableFilters.Count) {
            int y = hammerY + TextureAssets.CraftToggle[0].Height() - TextureAssets.InfoIcon[0].Width()/2;
            int j = 0;
            do {
                bool active = filters.IsFilterActive(i);
                Rectangle hitbox = new(x, y, RecipeFilterBack.Width(), RecipeFilterBack.Height());
                if (hitbox.Contains(Main.mouseX, Main.mouseY)) {
                    Main.LocalPlayer.mouseInterface = true;
                    int rare = 0;
                    string name = Language.GetTextValue(filters.AvailableFilters[i].GetDisplayNameKey());
                    if(RecipesInFilter[i] != 0) {
                        if (Main.mouseLeft && Main.mouseLeftRelease) {
                            bool keepOn = !active || filters.ActiveFilters.Count > 1;
                            filters.ActiveFilters.Clear();
                            if (keepOn) filters.ActiveFilters.Add(filters.AvailableFilters[i]);
                            OnFilterChanges();
                        } else if (Main.mouseRight && Main.mouseRightRelease) {
                            filters.ToggleFilter(i);
                            OnFilterChanges();
                        }
                        name = Language.GetTextValue("Mods.BetterInventory.UI.Filter", name, RecipesInFilter[i]);
                        Main.spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, hitbox.Center(), null, Main.OurFavoriteColor, 0, TextureAssets.InfoIcon[13].Size() / 2, 1, SpriteEffects.None, 0);
                    } else rare = -1;
                    Main.instance.MouseText(name, rare);

                }

                Color color = active ? Color.White : Color.Gray;
                if(RecipesInFilter[i] == 0) color *= 0.33f;
                Main.spriteBatch.Draw(RecipeFilterBack.Value, hitbox.Center(), null, color, 0, RecipeFilterBack.Size() / 2, 1, SpriteEffects.None, 0);
                Rectangle frame = filters.AvailableFilters[i].GetSourceFrame();
                Main.spriteBatch.Draw(RecipeFilters.Value, hitbox.Center(), frame, color, 0, frame.Size() / 2, 1, SpriteEffects.None, 0);
                y += delta;
                j++;
                i++;
            } while (y < Main.ScreenSize.Y && i < filters.AvailableFilters.Count && j < 6);
            x += delta;
        }
    }

    
    private void HookFilterRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        orig(canDelayCheck);
        if(!canDelayCheck) FilterRecipes(true);
    }

    public static void FilterRecipes(bool saveFilters = false){
        int oldRecipe = Main.focusRecipe;
        float focusY = Main.availableRecipeY[Main.focusRecipe];

        EntryFilterer<Item, CreativeFilterWrapper> filterer = LocalFilters.Filterer;
        Recipe.ClearAvailableRecipes();
        if(saveFilters) RecipesInFilter.Clear();
        for (int i = 0; i < filterer.AvailableFilters.Count; i++) RecipesInFilter.Add(0);
        
        if(saveFilters) {
            foreach(int index in ItemSearch.BetterGuide.GetRecipesInOrder()) {
                for (int i = 0; i < filterer.AvailableFilters.Count; i++) if(filterer.AvailableFilters[i].FitsFilter(Main.recipe[index].createItem)) RecipesInFilter[i]++;
            }
            for (int i = 0; i < filterer.AvailableFilters.Count; i++) {
                if(RecipesInFilter[i] == 0 && filterer.IsFilterActive(i)) filterer.ToggleFilter(i);
            }
        }
        foreach(int index in ItemSearch.BetterGuide.GetRecipesInOrder()) {
            if (Enabled && !filterer.FitsFilter(Main.recipe[index].createItem)) continue;
            Reflection.Recipe.AddToAvailableRecipes.Invoke(index);
        }

        Reflection.Recipe.TryRefocusingRecipe.Invoke(oldRecipe);
        Reflection.Recipe.VisuallyRepositionRecipes.Invoke(focusY);
    }

    public static readonly List<int> RecipesInFilter = new();

    public static Asset<Texture2D> RecipeFilterBack => ModContent.Request<Texture2D>($"BetterInventory/Assets/Recipe_Filter_Back");
    public static Asset<Texture2D> RecipeFilters => ModContent.Request<Texture2D>($"BetterInventory/Assets/Recipe_Filters");
}
