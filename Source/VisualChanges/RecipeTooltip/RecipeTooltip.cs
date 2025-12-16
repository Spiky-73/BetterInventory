using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BetterInventory.ItemSearch;
using SpikysLib;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BetterInventory.VisualChanges.RecipeTooltip;

public class RecipeTooltipItem : GlobalItem {

    public override void Load() {
        On_ItemTagHandler.ItemSnippet.ctor += HookItemGroupName;
        On_Recipe.ClearAvailableRecipes += HookClearAvailableRecipes;
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;
    }

    private static void HookClearAvailableRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        if (VisualChangesConfig.RecipeTooltip) RecipeTooltip.PreClearAvailableRecipes();
        orig();
    }

    private static void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        if (VisualChangesConfig.RecipeTooltip) RecipeTooltip.PreCollectGuideRecipes();
        orig();
    }

    private static void HookItemGroupName(On_ItemTagHandler.ItemSnippet.orig_ctor orig, TextSnippet self, Item item) {
        if (VisualChangesConfig.RecipeTooltip) RecipeTooltip.ModifyRecipeGroupName(item);
        orig(self, item);
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (VisualChangesConfig.RecipeTooltip) RecipeTooltip.ModifyTooltips(item, tooltips);
    }
}

public static class RecipeTooltip {
    public static void PreClearAvailableRecipes() => _guideRecipes = false;
    public static void PreCollectGuideRecipes() => _guideRecipes = true;

    public static void ModifyRecipeGroupName(Item item) {
        if (_hoveredRecipe is not null) {
            item.tooltipContext = ItemSlot.Context.CraftingMaterial;
            Guid guid = item.UniqueId();
            if (_hoveredRecipe.requiredItem.Exists(i => i.UniqueId() == guid) && _hoveredRecipe.ProcessGroupsForText(item.type, out var text)) {
                item.SetNameOverride(text);
            }
        }
    }

    public static void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        _hoveredRecipe = null;
        if (!ShouldDisplayRequiredItems(item, out _hoveredRecipe)) return;
        int index = tooltips.FindIndex(l => l.Name == nameof(TooltipLineID.ItemName)) + 1;
        tooltips.InsertRange(index, GetRecipeLines(_hoveredRecipe));
    }


    public static bool ShouldDisplayRequiredItems(Item item, [MaybeNullWhen(false)] out Recipe recipe) {
        recipe = null;
        if (item.tooltipContext != ItemSlot.Context.CraftingMaterial || Main.numAvailableRecipes == 0) return false;
        Guid guid = item.UniqueId();
        for (int i = 0; i < Main.numAvailableRecipes; i++) {
            recipe = Main.recipe[Main.availableRecipe[i]];
            if (recipe.createItem.UniqueId() == guid) return true;
        }
        return false;
    }

    public static List<TooltipLine> GetRecipeLines(Recipe recipe) {
        if (_lineRecipeIndex == recipe.RecipeIndex) return _requiredItemsTooltips;
        _lineRecipeIndex = recipe.RecipeIndex;
        string materials = string.Join(string.Empty, recipe.requiredItem.Select(ItemTagHandler.GenerateTag));

        _requiredItemsTooltips = [new(BetterInventory.Instance, "RequiredItems", materials)];
        if (_guideRecipes) {
            string objectsText;
            if (Configs.BetterGuide.RequiredObjectsDisplay) {
                if (recipe.requiredTile.Count == 0) _displayedTiles = [PlaceholderItem.FromTile(PlaceholderItem.ByHandTile)];
                else _displayedTiles = [.. recipe.requiredTile.TakeWhile(t => t != -1).Select(PlaceholderItem.FromTile)];
                _displayedConditions = [.. recipe.Conditions.Select(PlaceholderItem.FromCondition)];
                objectsText = string.Join(string.Empty, _displayedTiles.Select(ItemTagHandler.GenerateTag)) + string.Join(string.Empty, _displayedConditions.Select(ItemTagHandler.GenerateTag));
            } else {
                List<string> objects = [];
                objects.AddRange(recipe.requiredTile.TakeWhile(t => t != -1).Select(t => Lang.GetMapObjectName(MapHelper.TileToLookup(t, Recipe.GetRequiredTileStyle(t)))));
                objects.AddRange(recipe.Conditions.Select(c => c.Description.Value));
                objectsText = objects.Count == 0 ? Lang.inter[23].Value : string.Join(", ", objects);
            }

            if (!RecipeTooltipConfig.Instance.objectsLine) _requiredItemsTooltips[0].Text += $" @ {objectsText}";
            else _requiredItemsTooltips.Add(new(BetterInventory.Instance, "RequiredObjects", $"@ {objectsText}"));
        }
        return _requiredItemsTooltips;
    }

    private static Recipe? _hoveredRecipe;

    public static (Recipe?, Item[], Item[]) GetHoveredRecipeData() => (_hoveredRecipe, _displayedTiles, _displayedConditions);

    private static List<TooltipLine> _requiredItemsTooltips = [];
    private static Item[] _displayedTiles = [];
    private static Item[] _displayedConditions = [];

    private static int _lineRecipeIndex;

    private static bool _guideRecipes;
}