using System.Collections.Generic;
using Terraria;

namespace BetterInventory.InventoryManagement;


public static class Chests {

    public static void AddCratingMaterials(Dictionary<int, int> materials) {
        if (!Main.mouseItem.IsAir) materials[Main.mouseItem.netID] = materials.GetValueOrDefault(Main.mouseItem.netID) + Main.mouseItem.stack;
    }

    public static bool HideRecipe(Recipe recipe) {
        if (Main.mouseItem.IsAir) return false;
        int filterType = Main.mouseItem.type;
        if (recipe.createItem.type == filterType || recipe.requiredItem.Exists(i => i.type == filterType) || recipe.acceptedGroups.Exists(g => RecipeGroup.recipeGroups[g].ContainsItem(filterType))) return false;
        return true;
    }
}