using System.ComponentModel;

namespace BetterInventory.VisualChanges.RecipeTooltip;

public sealed class RecipeTooltipConfig {
    [DefaultValue(false)] public bool objectsLine = false;

    public static RecipeTooltipConfig Instance => VisualChangesConfig.Instance.recipeTooltip.Value;
}
