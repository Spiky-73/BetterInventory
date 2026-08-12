using System;
using MonoMod.Cil;
using SpikysLib.Constants;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.BetterItemPickup;

public sealed class AutoQuickStack : ILoadable {
    public bool IsLoadingEnabled(Mod mod) => BetterInventoryConfig.BetterItemPickup;
    public void Load(Mod mod) {
        On_ChestUI.LootAll += HookQuickStackLootAll;
        On_ChestUI.QuickStack += HookNoQuickStackToSameChest;

        IL_Player.QuickStackAllChests += il => il.TryEdit(IlQuickStackChestsMultiplayer, ref FailedBetterItemPickupConfig.Instance.autoQuickStack_Multiplayer);
        On_Player.GetItem_FillEmptyInventorySlot += HookQuickStackMultiplayerFix;
        On_Player.GetItem_FillEmptyInventorySlot_VoidBag += HookQuickStackMultiplayerFixVoidSlot;

        _fakeInventory = new Item[InventorySlots.Count];
        Item air = new();
        Array.Fill(_fakeInventory, air);
    }
    public void Unload() { }

    private static Item[] _fakeInventory = null!;
    public static bool GetItem_QuickStack(Player player, int plr, Item item, GetItemSettings settings) {
        if (settings.NoText) return false;

        var newItem = item.Clone();
        _fakeInventory[0] = item;
        if (Main.netMode == NetmodeID.MultiplayerClient) _quickStackNoChests = true;
        (var inventory, player.inventory) = (player.inventory, _fakeInventory);
        player.QuickStackAllChests();
        player.inventory = inventory;
        _quickStackNoChests = false;
        var numTransferred = newItem.stack - _fakeInventory[0].stack;
        if (numTransferred > 0) {
            SoundEngine.PlaySound(SoundID.MenuTick);
            BetterItemPickup.HandlePickup(player, plr, newItem, numTransferred, settings, PopupTextContext.ItemPickupToVoidContainer);
        }
        return _fakeInventory[0].IsAir;
    }


    private static void HookQuickStackLootAll(On_ChestUI.orig_LootAll orig) {
        if (BetterItemPickupConfig.AutoQuickStack) _chestSkipQuickStack = Main.LocalPlayer.chest;
        orig();
        _chestSkipQuickStack = -1;
    }

    private static void HookNoQuickStackToSameChest(On_ChestUI.orig_QuickStack orig, ContainerTransferContext context, bool voidStack) {
        if (BetterItemPickupConfig.AutoQuickStack && Main.LocalPlayer.chest == _chestSkipQuickStack) return;
        orig(context, voidStack);
    }
    private static int _chestSkipQuickStack = -1;

    private static void IlQuickStackChestsMultiplayer(ILContext context) {
        ILCursor cursor = new(context);

        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(() => Main.netMode));

        cursor.EmitDelegate(() => BetterItemPickupConfig.AutoQuickStack && _quickStackNoChests);
        ILLabel label = cursor.DefineLabel();
        cursor.EmitBrfalse(label);
        cursor.EmitRet();
        cursor.MarkLabel(label);
    }
    private static bool _quickStackNoChests;

    private static bool HookQuickStackMultiplayerFix(On_Player.orig_GetItem_FillEmptyInventorySlot orig, Player self, int plr, Item newItem, GetItemSettings settings, Item returnItem, int i) {
        if (!BetterItemPickupConfig.AutoQuickStack || Main.netMode != NetmodeID.MultiplayerClient) return orig(self, plr, newItem, settings, returnItem, i);
        if (!orig(self, plr, newItem, settings, returnItem, i)) return false;
        NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, plr, PlayerItemSlotID.Inventory0 + i, self.inventory[i].prefix);
        NetMessage.SendData(MessageID.QuickStackChests, -1, -1, null, PlayerItemSlotID.Inventory0 + i);
        self.inventoryChestStack[i] = true;
        return true;
    }

    private static bool HookQuickStackMultiplayerFixVoidSlot(On_Player.orig_GetItem_FillEmptyInventorySlot_VoidBag orig, Player self, int plr, Item[] inv, Item newItem, GetItemSettings settings, Item returnItem, int i) {
        if (!BetterItemPickupConfig.AutoQuickStack || Main.netMode != NetmodeID.MultiplayerClient) return orig(self, plr, inv, newItem, settings, returnItem, i);
        if (!orig(self, plr, inv, newItem, settings, returnItem, i)) return false;
        NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, plr, PlayerItemSlotID.Bank4_0 + i, self.bank4.item[i].prefix);
        NetMessage.SendData(MessageID.QuickStackChests, -1, -1, null, PlayerItemSlotID.Bank4_0 + i);
        self.disableVoidBag = i;
        return true;
    }
}
