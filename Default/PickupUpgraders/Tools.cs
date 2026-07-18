using Terraria;
using BetterInventory.InventoryManagement;
using Terraria.ID;

namespace BetterInventory.Default.PickupUpgraders;

public sealed class Tools : ModPickupUpgrader {
    public override bool AppliesTo(Item item) => item.pick > 0 || item.axe > 0 || item.hammer > 0;

    public override void CheckLockedItems(Player player) {
        int item = GetSmartSelectItem(player, ToolStrategy.Pickaxe);
        if (_upgradedPickaxe != ItemID.None && item >= 0 && _upgradedPickaxe == player.inventory[item].type) {
                Configs.UpgradeItems.Value.Lock(new(_upgradedPickaxe));
                _upgradedPickaxe = ItemID.None;
        }
        item = GetSmartSelectItem(player, ToolStrategy.Axe);
        if (_upgradedAxe != ItemID.None && item >= 0 && _upgradedAxe == player.inventory[item].type) {
                Configs.UpgradeItems.Value.Lock(new(_upgradedAxe));
                _upgradedAxe = ItemID.None;
        }
        item = GetSmartSelectItem(player, ToolStrategy.Hammer);
        if (_upgradedHammer != ItemID.None && item >= 0 && _upgradedHammer == player.inventory[item].type) {
                Configs.UpgradeItems.Value.Lock(new(_upgradedHammer));
                _upgradedHammer = ItemID.None;
        }
    }

    public static bool IsAnUpgrade(Item a, Item b) {
        bool greater = false;
        if (b.pick > 0) {
            if (a.pick <= 0) return false;
            int delta = a.pick.CompareTo(b.pick);
            if (delta < 0) return false;
            greater |= delta > 0;
        }
        if (b.axe > 0) {
            if (a.axe <= 0) return false;
            int delta = a.axe.CompareTo(b.axe);
            if (delta < 0) return false;
            greater |= delta > 0;
        }
        if (b.hammer > 0) {
            if (a.hammer <= 0) return false;
            int delta = a.hammer.CompareTo(b.hammer);
            if (delta < 0) return false;
            greater |= delta > 0;
        }
        return greater;
    }

    public override Item AttemptUpgrade(Player player, Item item) {
        if (item.pick > 0) item = CheckItem(player, item, GetSmartSelectItem(player, ToolStrategy.Pickaxe), ref _upgradedPickaxe);
        if (item.axe > 0) item = CheckItem(player, item, GetSmartSelectItem(player, ToolStrategy.Axe), ref _upgradedAxe);
        if (item.hammer > 0) item = CheckItem(player, item, GetSmartSelectItem(player, ToolStrategy.Hammer), ref _upgradedHammer);
        return item;
    }

    public static Item CheckPickaxe(Player player, Item item) {
        if (item.pick <= 0) return item;
        int equipped = GetSmartSelectItem(player, ToolStrategy.Pickaxe);
        if (equipped < 0 || Configs.UpgradeItems.Value.importantOnly && !player.inventory[equipped].favorited) return item;
        if (Configs.UpgradeItems.Value.IsLocked(new(player.inventory[equipped].type))) return item;

        return item;
    }

    public static Item CheckItem(Player player, Item item, int slot, ref int upgraded) {
        if (slot < 0) return item;
        Item equipped = player.inventory[slot];
        if (equipped.IsAir || Configs.UpgradeItems.Value.importantOnly && !equipped.favorited) return item;
        if (Configs.UpgradeItems.Value.IsLocked(new(equipped.type))) return item;
        if (!IsAnUpgrade(item, equipped)) return item;

        (player.inventory[slot], item) = (item, player.inventory[slot]);
        (player.inventory[slot].favorited, item.favorited) = (item.favorited, player.inventory[slot].favorited);
        upgraded = item.type;
        return item;
    }

    public static int GetSmartSelectItem(Player player, ToolStrategy strategy) {
        (var nonTorch, var selectedItem) = (player.nonTorch, player.selectedItem);
        player.selectedItem = -1;
        Reflection.Player.SmartSelect_PickToolForStrategy.Invoke(player, Player.tileTargetX, Player.tileTargetY, (int)strategy, false);
        int res = player.selectedItem;
        (player.nonTorch, player.selectedItem) = (nonTorch, selectedItem);
        return res;
    }

    private static int _upgradedHammer = ItemID.None;
    private static int _upgradedAxe = ItemID.None;
    private static int _upgradedPickaxe = ItemID.None;
}

public enum ToolStrategy {
    Light = 0,
    Hammer = 1,
    Axe = 2,
    Pickaxe = 3,
    WetLight = 4,
    FlareGun = 4,
    Cannon = 6,
    Extractinator = 7,
    PaintScrapper = 8,
}