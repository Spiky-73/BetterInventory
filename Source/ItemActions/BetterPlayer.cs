using System.Collections.Generic;
using BetterInventory.ItemSearch;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.ID;
using SpikysLib.UI;
using Terraria.Localization;
using BetterInventory.CrossMod;
using SpikysLib.Configs;
using MonoMod.Utils;
using BetterInventory.ItemSearch.BetterGuide;
using SpikysLib.CrossMod;
using BetterInventory.BetterRecipeList;
using BetterInventory.BetterInventoryManagement;

namespace BetterInventory.ItemActions;

public sealed class BetterPlayer : ModPlayer {

    public override void OnEnterWorld() {
        DisplayUpdate();
        DisplayCompatibility();
        DisplaySpicWarning();
        DisplayMagicStorageStackWarning();
    }

    public void DisplayUpdate() {
        LocalizedLine line;
        if (Configs.Version.Instance.lastPlayedVersion.Length == 0) line = new(Language.GetText($"{Localization.Keys.Chat}.Download"));
        else if (Mod.Version > new System.Version(Configs.Version.Instance.lastPlayedVersion)) line = new(Language.GetText($"{Localization.Keys.Chat}.Update"));
        else return;
        Configs.Version.Instance.lastPlayedVersion = Mod.Version.ToString();
        Configs.Version.Instance.Save();

        if (Language.GetText($"{Localization.Keys.Chat}.Summary").Value.Length != 0) {
            InGameNotificationsTracker.AddNotification(new InGameNotification(Mod, line, new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.Bug"), Colors.RarityAmber)) { timeLeft = 15 * 60 });
        }
    }

    public void DisplayCompatibility() {
        LocalizedLine line;
        if (Utility.FailedILs > Configs.Compatibility.Instance.failedILs) line = new(Language.GetText($"{Localization.Keys.Chat}.UnloadedMore"), Colors.RarityAmber);
        else if (Utility.FailedILs < Configs.Compatibility.Instance.failedILs) line = new(Language.GetText(Utility.FailedILs == 0 ? $"{Localization.Keys.Chat}.UnloadedNone" : $"{Localization.Keys.Chat}.UnloadedLess"), Colors.RarityGreen);
        else return;
        Configs.Compatibility.Instance.failedILs = Utility.FailedILs;
        Configs.Compatibility.Instance.Save();

        InGameNotificationsTracker.AddNotification(new InGameNotification(Mod, line));
    }

    public void DisplaySpicWarning() {
        if (!BetterInventoryManagementConfig.CraftStack || CraftStackConfig.Instance.maxItems.Key.Choice != nameof(MaxCraftAmountConfig.spicRequirement) || SpysInfiniteConsumablesIntegration.Enabled) return;
        InGameNotificationsTracker.AddNotification(new InGameNotification(Mod, new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.SPICWarning"), Colors.RarityAmber)));
    }

    public void DisplayMagicStorageStackWarning() {
        if (!BetterRecipeListConfig.RecipeTooltip || !BetterRecipeListConfig.AvailableMaterialsCount || !MagicStorageIntegration.StackingFix) return;
        InGameNotificationsTracker.AddNotification(new InGameNotification(Mod, new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.MagicStorageStackWarning"), Colors.RarityAmber)));
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        QuickSearch.ProcessTriggers();
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if (PlaceholderItem.OverrideHover(inventory, context, slot)) return true;
        return false;
    }
    public override void SaveData(TagCompound tag) { }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(CraftInMenuPlayer.VisibilityTag, out VisibilityFilters visibility)) { // Compatibility version < v0.8
            Player.GetModPlayer<CraftInMenuPlayer>().visibility = (RecipeVisibility)visibility.Visibility;
            Player.GetModPlayer<FavoritedRecipesPlayer>().favoritedRecipes.AddRange(visibility.FavoritedRecipes);
            Player.GetModPlayer<FavoritedRecipesPlayer>().blacklistedRecipes.AddRange(visibility.BlacklistedRecipes);
            Player.GetModPlayer<FavoritedRecipesPlayer>().unloadedRecipes.AddRange(visibility.UnloadedRecipes);
            Player.GetModPlayer<UnknownRecipesPlayer>().ownedItems.AddRange(visibility.OwnedItems);
            Player.GetModPlayer<UnknownRecipesPlayer>().unloadedItems.AddRange(visibility.UnloadedItems);
        }
        if (tag.TryGet(GuideTilePlayer.GuideTileTag, out Item tile)) { // Compatibility version < v0.8
            Player.GetModPlayer<GuideTilePlayer>()._tempGuideTile = tile;
        }
    }
}
