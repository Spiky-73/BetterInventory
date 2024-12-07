using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SpikysLib;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace BetterInventory.ItemActions;

public class RequiredTooltipItem : GlobalItem {

    public override void Load() {
        On_ItemTagHandler.ItemSnippet.ctor += HookItemGroupName;

        On_Recipe.ClearAvailableRecipes += HookClearAvailableRecipes;
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;
    }

    private void HookClearAvailableRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        _guideRecipes = false;
        orig();
    }

    private void HookCollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        _guideRecipes = true;
        orig();
    }

    private void HookItemGroupName(On_ItemTagHandler.ItemSnippet.orig_ctor orig, TextSnippet self, Item item) {
        if(Configs.RecipeTooltip.Enabled && HoveredRecipe is not null) {
            item.tooltipContext = ItemSlot.Context.ChatItem;
            Guid guid = item.UniqueId();
            if (HoveredRecipe.requiredItem.Exists(i => i.UniqueId() == guid) && HoveredRecipe.ProcessGroupsForText(item.type, out var text)) {
                item.SetNameOverride(text);
            }
        }
        orig(self, item);
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!Configs.RecipeTooltip.Enabled || !ShouldDisplayRequiredItems(item, out Recipe? recipe)) return;
        HoveredRecipe = recipe;
        int index = tooltips.FindIndex(l => l.Name == nameof(TooltipLineID.ItemName)) + 1;
        tooltips.InsertRange(index, GetRecipeLines(recipe));
    }

    private static bool ShouldDisplayRequiredItems(Item item, [MaybeNullWhen(false)] out Recipe recipe) {
        recipe = null;
        if (item.tooltipContext != ItemSlot.Context.CraftingMaterial || Main.numAvailableRecipes == 0) return false;
        Guid guid = item.UniqueId();
        for (int i = 0; i < Main.numAvailableRecipes; i++) {
            recipe = Main.recipe[Main.availableRecipe[i]];
            if (recipe.createItem.UniqueId() == guid) return true;
        }
        return false;
    }

    // TODO guide Condition display
    // TODO guide availableRecipes (enough or not)
    private static List<TooltipLine> GetRecipeLines(Recipe recipe) {
        Guid guid = recipe.createItem.UniqueId();
        if (_lineItemGuid == guid) return _requiredItemsTooltips;
        _lineItemGuid = guid;
        string materials = string.Join(string.Empty, recipe.requiredItem.Select(ItemTagHandler.GenerateTag));

        _requiredItemsTooltips = [new(BetterInventory.Instance, "RequiredItems", materials)];
        if(_guideRecipes) {
            List<string> objects = [];
            objects.AddRange(recipe.requiredTile.TakeWhile(t => t != -1).Select(t => {
                int requiredTileStyle = Recipe.GetRequiredTileStyle(t);
                return Lang.GetMapObjectName(MapHelper.TileToLookup(t, requiredTileStyle));
            }));
            objects.AddRange(recipe.Conditions.Select(c => c.Description.Value));
            string str = objects.Count == 0 ? Lang.inter[23].Value : string.Join(", ", objects);
            if (!Configs.RecipeTooltip.Value.objectsLine) _requiredItemsTooltips[0].Text += $" @ {str}";
            else _requiredItemsTooltips.Add(new(BetterInventory.Instance, "RequiredObjects", $"@ {str}"));
        }
        return _requiredItemsTooltips;
    }

    public static Recipe? HoveredRecipe;

    private static List<TooltipLine> _requiredItemsTooltips = [];
    private static Guid _lineItemGuid;

    private static bool _guideRecipes;
}
