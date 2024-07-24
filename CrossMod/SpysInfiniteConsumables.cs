using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
namespace SpikysLib.CrossMod;

[JITWhenModsEnabled(ModName)]
public static class SpysInfiniteConsumables {
    public const string ModName = "SPIC";

    public static bool Enabled => ModLoader.HasMod(ModName);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long GetMixedInfinity(Player player, Item item) => SPIC.Default.Infinities.Items.Instance.GetGroupInfinity(player, item).Mixed.Infinity;
    public static long GetMixedRequirement(Player player, Item item) => SPIC.Default.Infinities.Items.Instance.GetGroupInfinity(player, item).Mixed.Requirement.Count;
    public static long GetMixedCountToInfinity(Player player, Item item) {
        SPIC.FullInfinity inf = SPIC.Default.Infinities.Items.Instance.GetGroupInfinity(player, item).Mixed;
        return inf.Requirement.Count - inf.Count;
    }
}