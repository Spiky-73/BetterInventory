using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;

namespace BetterInventory.Improvements.UnlockFilter;

public sealed class UnlockFilter : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || ImprovementsConfig.UnlockFilter;
    public void Load(Mod mod) {
        On_Filters.ByUnlockState.GetDisplayNameKey += HookCustomUnlockFilterName;
        On_Filters.ByUnlockState.FitsFilter += HookCustomUnlockFilter;
    }
    public void Unload() {}

    private static string HookCustomUnlockFilterName(On_Filters.ByUnlockState.orig_GetDisplayNameKey orig, Filters.ByUnlockState self) => ImprovementsConfig.UnlockFilter ? $"{Localization.Keys.UI}.FullUnlock" : orig(self);
    private static bool HookCustomUnlockFilter(On_Filters.ByUnlockState.orig_FitsFilter orig, Filters.ByUnlockState self, BestiaryEntry entry) => ImprovementsConfig.UnlockFilter ? entry.UIInfoProvider.GetEntryUICollectionInfo().UnlockState != BestiaryEntryUnlockState.CanShowDropsWithDropRates_4 : orig(self, entry);
}