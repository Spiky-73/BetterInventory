using BetterInventory.Crafting;
using SpikysLib.DataStructures;
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
using SpikysLib;
using BetterInventory.Default.Catalogues;
using System.Collections.Generic;
using BetterInventory.Crafting.UI;

namespace BetterInventory.ItemSearch;

public sealed partial class Guide : ModSystem {

    internal static GameTime _lastUpdateUiGameTime = null!;
    internal static UserInterface recipeInterface = null!;
    internal static RecipeUI recipeUI = null!;

    public static VisibilityFilters LocalFilters => BetterPlayer.LocalPlayer.VisibilityFilters;

    public override void UpdateUI(GameTime gameTime) {
        _lastUpdateUiGameTime = gameTime;
        recipeInterface.Update(gameTime);
    }

    private static void ILCustomDrawCreateItem(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoRecipeDraw();

        cursor.GotoNextLoc(out int recipeIndex, i => i.Next.MatchBr(out _), 124);

        //     for (<recipeIndex>) {
        //         ...
        //         if(<visible>) {
        //             ...
        //             if (Main.numAvailableRecipes > 0) {
        //                 ...
        //                 Main.inventoryBack = ...;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.HoverOverCraftingItemButton));
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //                 ++ <overrideBackground>
        cursor.EmitLdloc(recipeIndex); // int num63
        cursor.EmitDelegate((int i) => {
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[i])) PlaceholderItem.hideNextItem = true;
            if (Configs.BetterGuide.AvailableRecipes) {
                OverrideRecipeTexture(GetFavoriteState(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, IsAvailable(Main.availableRecipe[i]));
                ItemSlot.DrawGoldBGForCraftingMaterial = false;
            }
        });

        //                 ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //                 ++ <restoreBackground>
        cursor.EmitDelegate(RestoreBack4);
        //             }
        //         }
        //     }

    }
    private static void ILCustomDrawMaterials(ILContext il) {
        ILCursor cursor = new(il);

        //     if (++<known> && Main.numAvailableRecipes > 0) {
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentIngredientsCount));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.numAvailableRecipes));
        cursor.EmitDelegate((int num) => num == 0 || Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[Main.focusRecipe]) ? 0 : num);

        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentRecipeSmall));
        cursor.GotoNextLoc(out int materialIndex, i => i.Previous.MatchLdcI4(0), 130);

        //         for (<focusRecipeMaterialIndex>) {
        //             ...
        //             Item tempItem = ...;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.SetRecipeMaterialDisplayName));
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(materialIndex);
        cursor.EmitDelegate((int matI) => {
            if (!Configs.BetterGuide.AvailableRecipes) return;
            Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];
            bool canCraft = IsAvailable(recipe.RecipeIndex);
            if (!canCraft) {
                Item material = recipe.requiredItem[matI];
                canCraft = PlayerHelper.OwnedItems.GetValueOrDefault(material.type, 0) >= material.stack;
                if (!canCraft) {
                    int g = recipe.acceptedGroups.FindIndex(g => RecipeGroup.recipeGroups[g].IconicItemId == material.type);
                    if (g != -1) {
                        RecipeGroup group = RecipeGroup.recipeGroups[recipe.acceptedGroups[g]];
                        canCraft = PlayerHelper.OwnedItems.GetValueOrDefault(group.GetGroupFakeItemId(), 0) >= material.stack;
                    }
                }
            }
            OverrideRecipeTexture(FavoriteState.Default, false, canCraft);
        });

        //             ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //             ++ <restoreBackground>
        cursor.EmitDelegate(RestoreBack4);
        //         }
        //     }
        //     ...
        // }

    }
    private static void ILCustomDrawRecipeList(ILContext il) {
        ILCursor cursor = new(il);

        // ----- Recipe big list background and ??? -----
        // Main.hidePlayerCraftingMenu = false;
        // if(<recBigListVisible>) {
        //     ...
        //     while (<showingRecipes>) {
        cursor.GotoNextLoc(out int recipeListIndex, i => i.Previous.MatchLdsfld(Reflection.Main.recStart), 153);

        //         ...
        //         if (<mouseHover>) {
        //             if (<click>) ...
        //             Main.craftingHide = true;
        cursor.GotoNext(i => i.SaferMatchCall(Reflection.Main.LockCraftingForThisCraftClickDuration));
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(Reflection.Main.craftingHide));

        //             ++ <GuideHover>
        cursor.EmitLdloc(recipeListIndex); // int num87
        cursor.EmitDelegate((int r) => {
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[r])) PlaceholderItem.hideTooltip = true;
        });
        //             ...
        //         }

        //         if (Main.numAvailableRecipes > 0) {
        //             ...
        //             Main.inventoryBack = ...;
        cursor.GotoNext(MoveType.Before, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //             ++ <overrideBackground>
        cursor.EmitLdloc(recipeListIndex);
        cursor.EmitDelegate((int i) => {
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[i])) PlaceholderItem.hideNextItem = true;
            if (Configs.BetterGuide.AvailableRecipes) {
                OverrideRecipeTexture(GetFavoriteState(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, s_availableRecipes.Contains(Main.availableRecipe[i]));
                ItemSlot.DrawGoldBGForCraftingMaterial = false;
            }
        });

        //             ItemSlot.Draw(...);
        cursor.GotoNext(MoveType.After, i => true);

        //             ++ <restoreBackground>
        cursor.EmitDelegate(RestoreBack4);
        //         }
        //         ...
        //     }
        // }
    }
    private static void RestoreBack4() => TextureAssets.InventoryBack4 = s_inventoryBack4;


    private static void ILDrawVisibility(ILContext il) {
        ILCursor cursor = new(il);

        //         Main.DrawGuideCraftText(...);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(Main), "DrawGuideCraftText"));
        ILLabel? noHover = null;
        cursor.FindNext(out _, i => i.MatchBlt(out noHover));
        //         ++ if(!<visibilityHover>) {
        cursor.EmitDelegate(() => s_visibilityHover);
        cursor.EmitBrtrue(noHover!);

        //             <handle guide item slot>
        //         ++ }
        //         ItemSlot.Draw(Main.spriteBatch, ref Main.guideItem, ...);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));

        //         ++ <drawVisibility>
        cursor.EmitDelegate(() => {
            if (Configs.BetterGuide.CraftInMenu) DrawVisibility();
        });
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
    public static void HandleVisibility(int x, int y) {
        s_visibilityHover = false;
        x += (int)((TextureAssets.InventoryBack.Width() - 2) * Main.inventoryScale);
        y += (int)(4 * Main.inventoryScale);
        Vector2 size = s_inventoryTickBorder.Size() * Main.inventoryScale;
        s_hitBox = new(x - (int)(size.X / 2f), y - (int)(size.Y / 2f), (int)size.X, (int)size.Y);

        if (PlayerInput.IgnoreMouseInterface || !s_hitBox.Contains(Main.mouseX, Main.mouseY)) return;

        Main.player[Main.myPlayer].mouseInterface = true;
        if (Main.mouseLeft && Main.mouseLeftRelease) {
            LocalFilters.ShowAllRecipes = !LocalFilters.ShowAllRecipes;
            SoundEngine.PlaySound(SoundID.MenuTick);
            FindGuideRecipes();
        }
        s_visibilityHover = true;
    }

    public static void FindGuideRecipes() {
        s_collectingGuide = true;
        int oldRecipe = Main.availableRecipe[Main.focusRecipe];
        float focusY = Main.availableRecipeY[Main.focusRecipe];
        Recipe.ClearAvailableRecipes();
        Reflection.Recipe.CollectGuideRecipes.Invoke();
        Reflection.Recipe.TryRefocusingRecipe.Invoke(oldRecipe);
        Reflection.Recipe.VisuallyRepositionRecipes.Invoke(focusY);
    }

    private static void HookClearRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        if (Configs.BetterGuide.AvailableRecipes && !s_collectingGuide) {
            ClearAvailableRecipes();
            return;
        }
        InventoryLoader.ClearCache();
        RecipeFiltering.ClearFilters();
        ClearUnknownRecipes();
        orig();
    }
    private void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if (canDelayCheck) {
            orig(canDelayCheck);
            return;
        }

        // Add owned items before updating recipes
        bool forced = Configs.BetterGuide.UnknownDisplay && UpdateOwnedItems();

        if (Configs.BetterGuide.AvailableRecipes) {
            // Update available if no guide item changed
            bool guideChange = !PlaceholderHelper.AreSame(Main.guideItem, s_dispGuide) || !PlaceholderHelper.AreSame(guideTile, s_dispTile);
            if (!guideChange) orig(canDelayCheck);
            // Update guide recipes if we don't show everything
            if (forced || guideChange || !ShowAllRecipes()) FindGuideRecipes();

        } else {
            // Display guide recipes if there's a guide tile
            if (Configs.BetterGuide.GuideTile && !guideTile.IsAir) FindGuideRecipes();
            else orig(canDelayCheck);
        }
    }

    private static void ILSkipGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);

        // Recipe.ClearAvailableRecipes()
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(Reflection.Recipe.ClearAvailableRecipes));

        // ++ if (Enabled) goto skipGuide
        ILLabel skipGuide = cursor.DefineLabel();
        cursor.EmitDelegate(() => Configs.BetterGuide.AvailableRecipes);
        cursor.EmitBrtrue(skipGuide);
        cursor.MarkLabel(skipGuide); // Here in case of exception


        // if(<guideItem>) {
        //     <guideRecipes>
        // }
        // ++ skipGuide:
        // Player localPlayer = Main.LocalPlayer;
        // Recipe.CollectItemsToCraftWithFrom(localPlayer);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(Reflection.Recipe.CollectItemsToCraftWithFrom));
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);
    }

    internal static bool HighjackAddRecipe(int recipeIndex) {
        if (!Configs.BetterGuide.AvailableRecipes || s_collectingGuide) return false;
        s_availableRecipes.Add(recipeIndex);
        return true;
    }

    private static void ILCraftInGuideMenu(ILContext il) {
        ILCursor cursor = new(il);

        // if (Main.focusRecipe == recipeIndex && ++[Main.guideItem.IsAir || <craftInMenu>]) {
        cursor.GotoNext(i => i.MatchLdsfld(Reflection.Main.guideItem));
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.Item.IsAir.GetMethod!));
        cursor.EmitDelegate((bool isAir) => (isAir && (!Configs.BetterGuide.GuideTile || guideTile.IsAir)) || Configs.BetterGuide.CraftInMenu);

        //     <craft>
        // }
    }

    private static void HookDisableWhenNonAvailable(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (Configs.BetterGuide.AvailableRecipes && !IsAvailable(Main.availableRecipe[recipeIndex])) Main.LockCraftingForThisCraftClickDuration();
        orig(recipeIndex);
    }

    public static Item? GetGuideMaterials() => Configs.BetterGuide.AvailableRecipes && !RecipeList.Instance.Enabled ? Main.guideItem : null;
    private static void HookGuideTileAdj(On_Player.orig_AdjTiles orig, Player self) {
        orig(self);
        if (!Configs.BetterGuide.AvailableRecipes || !Configs.BetterGuide.GuideTile || RecipeList.Instance.Enabled || guideTile.createTile < TileID.Dirt) return;
        self.adjTile[guideTile.createTile] = true;
        Recipe.FindRecipes();
    }

    public static bool IsAvailable(int recipe) => s_availableRecipes.Contains(recipe);
    public static void ClearAvailableRecipes() => s_availableRecipes.Clear();

    public static bool ShowAllRecipes() => Configs.BetterGuide.CraftInMenu ? LocalFilters.ShowAllRecipes : !Main.guideItem.IsAir || (Configs.BetterGuide.GuideTile && !guideTile.IsAir);

    private static Asset<Texture2D> s_inventoryTickBorder = null!;

    private static bool s_collectingGuide;
    private static readonly RangeSet s_availableRecipes = [];

    private static Item s_dispGuide = new(), s_dispTile = new();


    private static bool s_visibilityHover;
    private static Rectangle s_hitBox;

    public static void OverrideRecipeTexture(FavoriteState state, bool highlight, bool available) => OverrideRecipeTexture(state switch {
        FavoriteState.Default => s_defaultTextures,
        FavoriteState.Favorited => s_favoriteTextures,
        FavoriteState.Blacklisted or _ => s_blacklistedTextures,
    }, highlight, available);
    public static void OverrideRecipeTexture(TextureHighlight textures, bool highlight, bool available) {
        TextureAssets.InventoryBack4 = highlight ? textures.Highlight : textures.Default;
        if (!available) Main.inventoryBack.ApplyRGB(0.5f);
    }
    private static TextureHighlight s_defaultTextures = null!;
    private static TextureHighlight s_favoriteTextures = null!;
    private static TextureHighlight s_blacklistedTextures = null!;
    private static Asset<Texture2D> s_inventoryBack4 = null!;
}
