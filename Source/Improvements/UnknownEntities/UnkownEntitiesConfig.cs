using System.ComponentModel;
using BetterInventory.Configs;


namespace BetterInventory.Improvements.UnknownEntities;

using UEUnloadableAttribute = UnloadableAttribute<UnloadedUnknownEntitiesConfig>;

public enum UnknownDisplay { Vanilla, Hidden, Unknown, Known }

public sealed class UnknownEntitiesConfig {
    [DefaultValue(UnknownDisplay.Unknown)] public UnknownDisplay unknownDisplay = UnknownDisplay.Hidden;
    [UEUnloadableAttribute(nameof(bestiary)), DefaultValue(true)] public bool bestiary = true;
    [UEUnloadableAttribute(nameof(recipeList)), DefaultValue(true)] public bool recipeList = true;

    public static UnknownEntitiesConfig Instance => ImprovementsConfig.Instance.unknownEntities.Value;
    public static bool Bestiary => ImprovementsConfig.UnknownEntities && Instance.bestiary && !UnloadedUnknownEntitiesConfig.Instance.bestiary;
    public static bool RecipeList => ImprovementsConfig.UnknownEntities && Instance.recipeList && !UnloadedUnknownEntitiesConfig.Instance.recipeList;
}

public sealed class UnloadedUnknownEntitiesConfig {
    public bool bestiary;
    public bool recipeList;

    public static UnloadedUnknownEntitiesConfig Instance => UnloadedImprovementsConfig.Instance.unknownEntities;
}
