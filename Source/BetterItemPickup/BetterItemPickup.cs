using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;

namespace BetterInventory.BetterItemPickup;

public sealed class BetterItemPickup : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || BetterInventoryConfig.BetterItemPickup;
    public void Load(Mod mod) {
        On_Player.GetItem += HookGetItem;
    }
    public void Unload() { }

    /*
    * get_previousSlot
    > get_smartPickup
    > get_banks
    > auto_quickStack
    * auto_equip
    * auto_upgrade
    > get_earlyVoidBag

    get_ammoSlots (vanilla)
    get_inventory (vanilla)
    get_voidBag (vanilla)
    */
    private static Item HookGetItem(On_Player.orig_GetItem orig, Player self, int plr, Item newItem, GetItemSettings settings) {
        if (_inner || !BetterInventoryConfig.BetterItemPickup || newItem.noGrabDelay > 0) return orig(self, plr, newItem, settings);

        if (BetterItemPickupConfig.SmartPickup && SmartPickup.GetItem_SmartPickup(self, plr, newItem, settings)) return new();
        if (BetterItemPickupConfig.PickupToBanks && PickupToBanks.GetItem_Banks(self, plr, newItem, settings)) return new();
        if (BetterItemPickupConfig.AutoQuickStack && AutoQuickStack.GetItem_QuickStack(self, plr, newItem, settings)) return new();

        if (BetterItemPickupConfig.PrioritizeVoidBag && PrioritizeVoidBag.GetItem_EarlyVoidVault(self, plr, newItem, settings)) return new();

        return orig(self, plr, newItem, settings);
    }

    private static bool _inner;
    public static Item GetItem_Inner(Player player, int plr, Item item, GetItemSettings settings) {
        _inner = true;
        var res = player.GetItem(plr, item, settings);
        _inner = false;
        return res;
    }

    public static void HandlePickup(Player player, int plr, Item item, int numTransferred, GetItemSettings settings, PopupTextContext context = PopupTextContext.RegularItemPickup) {
        if (!settings.NoText) PopupText.NewText(context, item, numTransferred, false, settings.LongText);
        if (plr == Main.myPlayer) Recipe.FindRecipes();
        AchievementsHelper.NotifyItemPickup(player, item);
    }
}