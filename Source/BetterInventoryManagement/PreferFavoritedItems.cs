using SpikysLib;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace BetterInventory.BetterInventoryManagement;

public sealed class PreferFavoritedItems : ModPlayer {
    public override bool IsLoadingEnabled(Mod mod) => BetterInventoryConfig.BetterInventoryManagement;
    public override void Load() {
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(Mod, "FavoritedQuickBuff", Microsoft.Xna.Framework.Input.Keys.B);
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!BetterInventoryManagementConfig.PreferFavoritedItems) return;
        if (PreferFavoritedItemsConfig.Instance.quickBuff && FavoritedBuffKb.JustPressed) FavoritedQuickBuff();
    }

    // TODO mods adding a quickbuff from safes
    private void FavoritedQuickBuff() => ItemHelper.RunWithHiddenItems(Player.inventory, Player.QuickBuff, i => !i.favorited);

    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;
}