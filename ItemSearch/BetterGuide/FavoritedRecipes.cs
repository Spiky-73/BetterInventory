using System.Collections.Generic;
using BetterInventory.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpikysLib.Collections;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.ItemSearch.BetterGuide;

public sealed class FavoritedRecipesPlayer : ModPlayer {
    public static FavoritedRecipesPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<FavoritedRecipesPlayer>();

    public override void Load() {
        s_favoriteTextures = new(TextureAssets.InventoryBack10, TextureAssets.InventoryBack17);
        s_blacklistedTextures = new(TextureAssets.InventoryBack5, TextureAssets.InventoryBack11);
        On_Main.HoverOverCraftingItemButton += HookFavoriteRecipe;
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookFavoritedBackground;
    }

    private static void HookFavoritedBackground(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        TextureHighlight? texture = null;
        if (Configs.BetterGuide.FavoritedRecipes && context == ItemSlot.Context.CraftingMaterial) {
            var localPlayer = LocalPlayer;
            if (localPlayer.favoritedRecipes.Exist(r => Main.recipe[r].createItem == inv[slot])) texture = s_favoriteTextures;
            else if (localPlayer.blacklistedRecipes.Exist(r => Main.recipe[r].createItem == inv[slot])) texture = s_blacklistedTextures;
        }
        if (texture is null) {
            orig(spriteBatch, inv, context, slot, position, lightColor);
            return;
        }
        (Asset<Texture2D> back, TextureAssets.InventoryBack4) = (TextureAssets.InventoryBack4, ItemSlot.DrawGoldBGForCraftingMaterial ? texture.Highlight : texture.Default);
        ItemSlot.DrawGoldBGForCraftingMaterial = false;
        orig(spriteBatch, inv, context, slot, position, lightColor);
        TextureAssets.InventoryBack4 = back;
    }

    private void HookFavoriteRecipe(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {   
        if (!Configs.BetterGuide.FavoritedRecipes || UnknownDisplayPlayer.IsUnknown(Main.recipe[Main.availableRecipe[recipeIndex]].createItem)) {
            orig(recipeIndex);
            return;
        }

        bool click = Main.mouseLeft && Main.mouseLeftRelease;
        bool clicked = false;
        var localPlayer = LocalPlayer;

        if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
            Main.cursorOverride = CursorOverrideID.FavoriteStar;
            if (click) {
                clicked = true;
                localPlayer.ToggleFavorited(Main.availableRecipe[recipeIndex]);
                Main.LockCraftingForThisCraftClickDuration();
            }
        } else if (ItemSlot.ControlInUse && !localPlayer.IsFavorited(Main.availableRecipe[recipeIndex])) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            if (click) {
                clicked = true;
                localPlayer.ToggleBlacklisted(Main.availableRecipe[recipeIndex]);
                Main.LockCraftingForThisCraftClickDuration();
            }
        }
        orig(recipeIndex);
        if (clicked) {
            Recipe.FindRecipes();
            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.mouseLeftRelease = false;
        }
    }
    private static TextureHighlight s_favoriteTextures = null!;
    private static TextureHighlight s_blacklistedTextures = null!;

    public override void SaveData(TagCompound tag) {
        List<RawRecipe> rawFavorited = [];
        List<RawRecipe> rawBlacklisted = [];
        foreach (int i in favoritedRecipes) rawFavorited.Add(new(Main.recipe[i]));
        foreach (int i in blacklistedRecipes) rawBlacklisted.Add(new(Main.recipe[i]));
        foreach ((RawRecipe r, bool f) in unloadedRecipes) (f ? rawFavorited : rawBlacklisted).Add(r);
        if (rawFavorited.Count != 0) tag[FavoritedTag] = rawFavorited;
        if (rawBlacklisted.Count != 0) tag[BlacklistedTag] = rawBlacklisted;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(FavoritedTag, out IList<RawRecipe> favorited)) {
            for (int r = 0; r < favorited.Count; r++) {
                Recipe? recipe = favorited[r].GetRecipe();
                if (recipe is null) unloadedRecipes.Add((favorited[r], true));
                else favoritedRecipes.Add(recipe.RecipeIndex);
            }
        }
        if (tag.TryGet(BlacklistedTag, out IList<RawRecipe> blacklisted)) {
            for (int r = 0; r < blacklisted.Count; r++) {
                Recipe? recipe = blacklisted[r].GetRecipe();
                if (recipe is null) unloadedRecipes.Add((blacklisted[r], false));
                else blacklistedRecipes.Add(recipe.RecipeIndex);
            }
        }
    }
    public const string FavoritedTag = "favorited";
    public const string BlacklistedTag = "blacklisted";

    public bool IsFavorited(int recipe) => favoritedRecipes.Contains(recipe);
    public bool IsBlacklisted(int recipe) => blacklistedRecipes.Contains(recipe);
    public void ToggleFavorited(int recipe, bool force = false) {
        if (!ResetRecipeState(recipe) || force) favoritedRecipes.Add(recipe);
    }
    public void ToggleBlacklisted(int recipe, bool force = false) {
        if (!ResetRecipeState(recipe) || force) blacklistedRecipes.Add(recipe);
    }
    public bool ResetRecipeState(int recipe) => favoritedRecipes.Remove(recipe) | blacklistedRecipes.Remove(recipe);

    public static void SortRecipes() {
        List<int> favorited = [];
        List<int> others = [];
        List<int> blacklisted = [];
        List<int> unknown = [];

        var player = LocalPlayer;
        for (int i = 0; i < Main.numAvailableRecipes; i++) {
            int recipe = Main.availableRecipe[i];
            if (player.IsFavorited(recipe)) favorited.Add(recipe);
            else if (player.IsBlacklisted(recipe)) blacklisted.Add(recipe);
            else if (UnknownDisplayPlayer.IsUnknown(recipe)) unknown.Add(recipe);
            else others.Add(recipe);
        }

        int index = 0;
        foreach (var recipe in favorited) Main.availableRecipe[index++] = recipe;
        foreach (var recipe in others) Main.availableRecipe[index++] = recipe;
        foreach (var recipe in blacklisted) Main.availableRecipe[index++] = recipe;
        foreach (var recipe in unknown) Main.availableRecipe[index++] = recipe;
    }

    public readonly RangeSet favoritedRecipes = [];
    public readonly RangeSet blacklistedRecipes = [];

    public readonly List<(RawRecipe, bool)> unloadedRecipes = [];
}

public sealed class UnfavoriteOnCraft : GlobalItem {
    public override void OnCreated(Item item, ItemCreationContext context) {
        if(!Configs.FavoritedRecipes.UnfavoriteOnCraft || context is not RecipeItemCreationContext recipeContext) return;
        int recipe = recipeContext.Recipe.RecipeIndex;
        var localPlayer = FavoritedRecipesPlayer.LocalPlayer;
        if(Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Favorited) && localPlayer.IsFavorited(recipe)
        || Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Blacklisted) && localPlayer.IsBlacklisted(recipe))
            localPlayer.ResetRecipeState(recipe);
    }
}

public record class TextureHighlight(Asset<Texture2D> Default, Asset<Texture2D> Highlight);