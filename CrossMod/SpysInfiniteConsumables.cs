using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
namespace SpikysLib.CrossMod;

[JITWhenModsEnabled(ModName)]
public static class SpysInfiniteConsumables {
    public const string ModName = "SPIC";

    public static bool Enabled => ModLoader.HasMod(ModName);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long GetMixedRequirement(Player player, Item item) => SPIC.Default.Infinities.Items.Instance.GetGroupInfinity(player, item).Mixed.Requirement.Count;
}