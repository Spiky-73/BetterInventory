using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib;
using SpikysLib.DataStructures;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch;

public sealed partial class Guide : ModSystem {

    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        if (Configs.BetterGuide.AvailableRecipes) {
            s_collectingGuide = true;
            s_dispGuide = Main.guideItem.Clone();
            s_dispTile = guideTile.Clone();
        }
        if (Configs.BetterGuide.RecipeOrdering) s_ilOrderedRecipes = GetDisplayedRecipes().GetEnumerator();
        orig();
        s_ilOrderedRecipes = null;
        s_collectingGuide = false;
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
            if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay != Configs.UnknownDisplay.Known && !LocalFilters.IsKnownRecipe(Main.recipe[r])) {
                s_unknownRecipes.Add(r);
                return true;
            }
            // Skip Favorited recipes
            if (Configs.BetterGuide.FavoritedRecipes) {
                if (LocalFilters.FavoritedRecipes.Contains(r)) return true;
                if (LocalFilters.BlacklistedRecipes.Contains(r)) return true;
            }
            return false;
        }

        // Add favorited recipes
        if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in LocalFilters.FavoritedRecipes) yield return r;
        
        // Add "normal" recipes
        for (int r = 0; r < Recipe.numRecipes; r++) if (!Skip(r)) yield return r;

        // Add blacklisted recipes
        if (Configs.BetterGuide.FavoritedRecipes) foreach (int r in LocalFilters.BlacklistedRecipes) yield return r;
        
        // Add "???" recipes
        if (Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay == Configs.UnknownDisplay.Unknown) foreach (int r in s_unknownRecipes) yield return r;
    }

    public delegate List<TooltipLine> ModifyTooltipsFn(Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex);
    private static List<TooltipLine> HookHideTooltip(ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        string? name = GetPlaceholderType(item) switch {
            PlaceholderType.ByHand => Language.GetTextValue($"{Localization.Keys.UI}.ByHand"),
            PlaceholderType.Tile => Lang.GetMapObjectName(MapHelper.TileToLookup(item.createTile, item.placeStyle)),
            PlaceholderType.Condition => Language.GetTextValue(item.BestiaryNotes[ConditionMark.Length..]),
            _ => forcedTooltip?.Value,
        };
        if (name is null) return orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);
        List<TooltipLine> tooltips = [new(BetterInventory.Instance, names[0], name)];
        numTooltips = 1;
        text = [tooltips[0].Text];
        modifier = [tooltips[0].IsModifier];
        badModifier = [tooltips[0].IsModifierBad];
        oneDropLogo = -1;
        overrideColor = [null];
        return tooltips;
    }
    
    private static void HookHideItem(On_ItemSlot.orig_Draw_SpriteBatch_refItem_int_Vector2_Color orig, SpriteBatch spriteBatch, ref Item inv, int context, Vector2 position, Color lightColor) {
        if (s_hideNextItem) {
            Item item = Placeholder;
            orig(spriteBatch, ref item, context, position, lightColor);
            s_hideNextItem = false;
        } else {
            orig(spriteBatch, ref inv, context, position, lightColor);
        }
    }

    private static void ILFavoriteRecipe(ILContext il) {
        ILCursor cursor = new(il);

        // <flags>
        cursor.GotoNext(MoveType.Before, i => i.MatchLdsfld(Reflection.Main.focusRecipe));

        // ++ if(<favorite>) goto skip;
        cursor.EmitLdarg0();
        cursor.EmitDelegate((int recipeIndex) => {
            if (Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.availableRecipe[recipeIndex])) {
                forcedTooltip = Language.GetText($"{Localization.Keys.UI}.Unknown");
                return false;
            }
            if (Configs.BetterGuide.FavoritedRecipes) {
                bool click = Main.mouseLeft && Main.mouseLeftRelease;
                if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
                    Main.cursorOverride = CursorOverrideID.FavoriteStar;
                    if (click) {
                        LocalFilters.ToggleFavorited(Main.availableRecipe[recipeIndex]);
                        FindGuideRecipes();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        return true;
                    }
                } else if (ItemSlot.ControlInUse && !LocalFilters.IsFavorited(Main.availableRecipe[recipeIndex])) {
                    Main.cursorOverride = CursorOverrideID.TrashCan;
                    if (click) {
                        LocalFilters.ToggleBlacklisted(Main.availableRecipe[recipeIndex]);
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
            LocalFilters.ResetRecipeState(r.RecipeIndex);
            FindGuideRecipes();
        });
    }

    public static FavoriteState GetFavoriteState(int recipe) {
        if (!Configs.BetterGuide.FavoritedRecipes) return FavoriteState.Default;
        if (LocalFilters.IsFavorited(recipe)) return FavoriteState.Favorited;
        if (LocalFilters.IsBlacklisted(recipe)) return FavoriteState.Blacklisted;
        return FavoriteState.Default;
    }

    public static bool UpdateOwnedItems() {
        bool added = false;
        if (!Main.mouseItem.IsAir) added |= LocalFilters.AddOwnedItem(Main.mouseItem);
        foreach (Item item in Main.LocalPlayer.inventory) if (!item.IsAir) added |= LocalFilters.AddOwnedItem(item);
        if (Main.LocalPlayer.InChest(out Item[]? chest)) {
            foreach (Item item in chest) if (!item.IsAir) added |= LocalFilters.AddOwnedItem(item);
        }
        return added;
    }

    public static bool IsUnknown(int recipe) => s_unknownRecipes.Contains(recipe);
    public static void ClearUnknownRecipes() => s_unknownRecipes.Clear();

    private static IEnumerator<int>? s_ilOrderedRecipes;
    private static readonly RangeSet s_unknownRecipes = [];

    private static bool s_hideNextItem;
    public static LocalizedText? forcedTooltip;

    private static Asset<Texture2D> s_unknownTexture = null!;
}

public enum FavoriteState : byte { Default, Blacklisted, Favorited }
