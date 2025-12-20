using BetterInventory.Configs;
using System.ComponentModel;

namespace BetterInventory.Improvements.BetterTrash;

using BTUnloadableAttribute = UnloadableAttribute<UnloadedBetterTrashConfig>;

public sealed class BetterTrashConfig {
    [BTUnloadableAttribute(nameof(stackTrash)), DefaultValue(true)] public bool stackTrash = true;
    [DefaultValue(true)] public bool trashTrash = true;

    public static BetterTrashConfig Instance => ImprovementsConfig.Instance.betterTrash.Value;
    public static bool StackTrash => ImprovementsConfig.BetterTrash && Instance.stackTrash && !UnloadedBetterTrashConfig.Instance.stackTrash;
    public static bool TrashTrash => ImprovementsConfig.BetterTrash && Instance.trashTrash;
}

public sealed class UnloadedBetterTrashConfig {
    public bool stackTrash;

    public static UnloadedBetterTrashConfig Instance => UnloadedImprovementsConfig.Instance.betterTrash;
}
