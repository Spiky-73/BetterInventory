using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpikysLib.Collections;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch;

public sealed class GuideFavoritedRecipes : ILoadable {

    public void Load(Mod mod) {
        s_favoriteTextures = new(TextureAssets.InventoryBack10, TextureAssets.InventoryBack17);
        s_blacklistedTextures = new(TextureAssets.InventoryBack5, TextureAssets.InventoryBack11);
        On_Main.HoverOverCraftingItemButton += HookFavoriteRecipe;
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookFavoritedBackground;
    }

    public void Unload() { }

    private void HookFavoritedBackground(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        TextureHighlight? texture = null;
        if (Configs.BetterGuide.FavoritedRecipes && context == ItemSlot.Context.CraftingMaterial) {
            if (GuideCraftInMenu.LocalFilters.FavoritedRecipes.Exist(r => Main.recipe[r].createItem == inv[slot])) texture = s_favoriteTextures;
            else if (GuideCraftInMenu.LocalFilters.BlacklistedRecipes.Exist(r => Main.recipe[r].createItem == inv[slot])) texture = s_blacklistedTextures;
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
        if (!Configs.BetterGuide.FavoritedRecipes || GuideUnknownDisplay.IsUnknown(Main.recipe[Main.availableRecipe[recipeIndex]].createItem)) {
            orig(recipeIndex);
            return;
        }

        bool click = Main.mouseLeft && Main.mouseLeftRelease;
        bool clicked = false;
        if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
            Main.cursorOverride = CursorOverrideID.FavoriteStar;
            if (click) {
                clicked = true;
                GuideCraftInMenu.LocalFilters.ToggleFavorited(Main.availableRecipe[recipeIndex]);
                Main.LockCraftingForThisCraftClickDuration();
            }
        } else if (ItemSlot.ControlInUse && !GuideCraftInMenu.LocalFilters.IsFavorited(Main.availableRecipe[recipeIndex])) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            if (click) {
                clicked = true;
                GuideCraftInMenu.LocalFilters.ToggleBlacklisted(Main.availableRecipe[recipeIndex]);
                Main.LockCraftingForThisCraftClickDuration();
            }
        }
        orig(recipeIndex);
        if (clicked) {
            Guide.FindGuideRecipes();
            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.mouseLeftRelease = false;
        }
    }
    private static TextureHighlight s_favoriteTextures = null!;
    private static TextureHighlight s_blacklistedTextures = null!;
}

public sealed class UnfavoriteOnCraft : GlobalItem {
    public override void OnCreated(Item item, ItemCreationContext context) {
        if(!Configs.FavoritedRecipes.UnfavoriteOnCraft || context is not RecipeItemCreationContext recipeContext) return;
        int recipe = recipeContext.Recipe.RecipeIndex;
        if(Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Favorited) && GuideCraftInMenu.LocalFilters.IsFavorited(recipe)
        || Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Blacklisted) && GuideCraftInMenu.LocalFilters.IsBlacklisted(recipe))
            GuideCraftInMenu.LocalFilters.ResetRecipeState(recipe);
    }
}
