using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib;
using SpikysLib.Collections;
using SpikysLib.DataStructures;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch;

public sealed partial class Guide : ModSystem {

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

    private static void HookUpdatedOwnedItems(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player) {
        orig(player);
        if(player.whoAmI != Main.myPlayer) return;
        foreach(var key in PlayerHelper.OwnedItems.Keys) {
            if (key < 1000000 && !GuideCraftInMenu.LocalFilters.HasOwnedItem(key)) GuideCraftInMenu.LocalFilters.AddOwnedItem(new(key));
        }
    }

    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        if (Configs.BetterGuide.RecipeOrdering) s_ilOrderedRecipes = GetDisplayedRecipes().GetEnumerator();
        orig();
        s_ilOrderedRecipes = null;
    }

    private static void ILGuideRecipeOrder(ILContext il) {
        ILCursor cursor = new(il);

        Utility.GotoRecipeDisabled(cursor, out ILLabel endLoop, out int index, out _);

        cursor.GotoLabel(endLoop!);
        cursor.GotoNext(i => i.MatchStloc(index));
        cursor.GotoNext(MoveType.After, i => i.MatchLdloc(index));
        cursor.EmitDelegate((int index) => {
            if (!Configs.BetterGuide.RecipeOrdering) return index;
            return s_ilOrderedRecipes!.MoveNext() ? s_ilOrderedRecipes.Current : Recipe.numRecipes;
        });
        cursor.EmitDup();
        cursor.EmitStloc(index);

        //     }
        // }
    }
    
    private static IEnumerable<int> GetDisplayedRecipes() {
        static bool Skip(int r) {
            // Skip unknown recipes
            if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay != Configs.UnknownDisplay.Known && !GuideCraftInMenu.LocalFilters.IsKnownRecipe(Main.recipe[r])) {
                s_unknownRecipes.Add(r);
                return true;
            }
            // Skip Favorited recipes
            if (Configs.BetterGuide.FavoritedRecipes) {
                if (GuideCraftInMenu.LocalFilters.FavoritedRecipes.Contains(r)) return true;
                if (GuideCraftInMenu.LocalFilters.BlacklistedRecipes.Contains(r)) return true;
            }
            return false;
        }

        // Add favorited recipes
        if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in GuideCraftInMenu.LocalFilters.FavoritedRecipes) yield return r;
        
        // Add "normal" recipes
        for (int r = 0; r < Recipe.numRecipes; r++) if (!Skip(r)) yield return r;

        // Add blacklisted recipes
        if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in GuideCraftInMenu.LocalFilters.BlacklistedRecipes) yield return r;
        
        // Add "???" recipes
        if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay == Configs.UnknownDisplay.Unknown) foreach (int r in s_unknownRecipes) yield return r;
    }

    private static void ILFavoriteRecipe(ILContext il) {
        ILCursor cursor = new(il);

        // <flags>
        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(Reflection.Main.focusRecipe));

        // ++ if(<favorite>) goto skip;
        cursor.EmitLdarg0();
        cursor.EmitDelegate((int recipeIndex) => {
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[recipeIndex])) {
                PlaceholderItem.hideTooltip = true;
                return false;
            }
            if (Configs.BetterGuide.FavoritedRecipes) {
                bool click = Main.mouseLeft && Main.mouseLeftRelease;
                if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
                    Main.cursorOverride = CursorOverrideID.FavoriteStar;
                    if (click) {
                        GuideCraftInMenu.LocalFilters.ToggleFavorited(Main.availableRecipe[recipeIndex]);
                        FindGuideRecipes();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        return true;
                    }
                } else if (ItemSlot.ControlInUse && !GuideCraftInMenu.LocalFilters.IsFavorited(Main.availableRecipe[recipeIndex])) {
                    Main.cursorOverride = CursorOverrideID.TrashCan;
                    if (click) {
                        GuideCraftInMenu.LocalFilters.ToggleBlacklisted(Main.availableRecipe[recipeIndex]);
                        FindGuideRecipes();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        });
        ILLabel skip = cursor.DefineLabel();
        cursor.EmitBrtrue(skip);
        cursor.MarkLabel(skip); // Here in case of exception

        // if (Main.focusRecipe == recipeIndex && Main.guideItem.IsAir) ...
        // else ...
        // ++ skip:
        // throw new Exception();
        cursor.GotoNext(i => i.MatchStsfld(Reflection.Main.craftingHide));
        cursor.GotoPrev(MoveType.AfterLabel, i => i.MatchLdcI4(1));
        cursor.MarkLabel(skip);
        // Main.craftingHide = true;
    }

    private static void IlUnfavoriteOnCraft(ILContext il) {
        ILCursor cursor = new(il);

        // Item crafted = r.createItem.Clone();
        // r.Create();
        // RecipeLoader.OnCraft(crafted, r, Main.mouseItem);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(RecipeLoader), nameof(RecipeLoader.OnCraft)));

        // ++ <unFavorite>
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => {
            if (!Configs.FavoritedRecipes.UnfavoriteOnCraft) return;
            if (!(GetFavoriteState(r.RecipeIndex) switch {
                FavoriteState.Favorited => Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Favorited),
                FavoriteState.Blacklisted => Configs.FavoritedRecipes.Value.unfavoriteOnCraft.HasFlag(Configs.UnfavoriteOnCraft.Blacklisted),
                FavoriteState.Default or _ => false,
            })) return;
            GuideCraftInMenu.LocalFilters.ResetRecipeState(r.RecipeIndex);
            FindGuideRecipes();
        });
    }

    public static FavoriteState GetFavoriteState(int recipe) {
        if (!Configs.BetterGuide.FavoritedRecipes) return FavoriteState.Default;
        if (GuideCraftInMenu.LocalFilters.IsFavorited(recipe)) return FavoriteState.Favorited;
        if (GuideCraftInMenu.LocalFilters.IsBlacklisted(recipe)) return FavoriteState.Blacklisted;
        return FavoriteState.Default;
    }

    public static bool IsUnknown(int recipe) => s_unknownRecipes.Contains(recipe);
    public static void ClearUnknownRecipes() => s_unknownRecipes.Clear();

    private static IEnumerator<int>? s_ilOrderedRecipes;
    private static readonly RangeSet s_unknownRecipes = [];

    public static LocalizedText? forcedTooltip;
}

public enum FavoriteState : byte { Default, Blacklisted, Favorited }
