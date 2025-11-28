using System.Runtime.CompilerServices;
using SPIC;
using SPIC.Default.Infinities;
using Terraria;
using Terraria.ModLoader;
namespace BetterInventory.CrossMod;

[JITWhenModsEnabled(ModName)]
public static class SpysInfiniteConsumablesIntegration {
    public const string ModName = "SPIC";

    public static bool Enabled => ModLoader.HasMod(ModName);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long GetItemInfinity(Player player, Item item) => InfinityManager.GetInfinity(player, item, ConsumableItem.Instance);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long GetItemRequirement(Item item) => InfinityManager.GetRequirement(item, ConsumableItem.Instance);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long GetCountToInfinity(Player player, Item item) => GetItemRequirement(item) - InfinityManager.CountConsumables(player, item, ConsumableItem.Instance);
}