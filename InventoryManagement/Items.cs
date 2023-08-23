using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public static class Items {

    public static void OnConsume(Item consumed, Player player, bool lastStack = false) {
        Item? smartStack = lastStack ? player.LastStack(consumed, true) : player.SmallestStack(consumed, true);
        if (smartStack == null) return;
        consumed.stack++;
        smartStack.stack--;
    }

    public static bool SmartPickupEnabled(Item item) => Configs.ClientConfig.Instance.smartPickup switch {
        Configs.SmartPickupLevel.AllItems => true,
        Configs.SmartPickupLevel.FavoriteOnly => item.favorited,
        Configs.SmartPickupLevel.Off or _ => false
    };

    public static bool OnGetItem(int plr, Player player, ref Item newItem, GetItemSettings settings) { // BUG when picking up an item after moving a stack (when combining stacks)
        int i; // BUG when shif click an item from a chest : goest to the chest if move before
        bool gotItems = false;
        if ((i = System.Array.IndexOf(_lastTypeOnInv, newItem.type)) != -1) {
            object[] args = new object[] { plr, newItem, settings, newItem, i };
            if (player.inventory[i].type == ItemID.None) gotItems = (bool)FillEmptyMethod.Invoke(player, args)!;
            else if (player.inventory[i].type == newItem.type && newItem.maxStack > 1) gotItems = (bool)FillOccupiedMethod.Invoke(player, args)!;
            else if (newItem.favorited || !player.inventory[i].favorited) {
                (newItem, player.inventory[i]) = (player.inventory[i], newItem);
            }
        } else if (_chest != -1 && (i = System.Array.IndexOf(_lastTypeOnChest, newItem.type)) != -1) {
            Item[] currentChest = player.Chest(_chest);
            object[] args = new object[] { plr, currentChest, newItem, settings, newItem, i };
            if (currentChest[i].type == ItemID.None) gotItems = (bool)FillEmptVoidMethod.Invoke(player, args)!;
            else if (currentChest[i].type == newItem.type && newItem.maxStack > 1) gotItems = (bool)FillOccupiedVoidMethod.Invoke(player, args)!;
            else if (newItem.favorited || !currentChest[i].favorited) (currentChest[i], newItem) = (newItem, currentChest[i]); // dupplicates the item
            if (Main.netMode == NetmodeID.MultiplayerClient && player.chest > -1) NetMessage.SendData(MessageID.SyncChestItem, number: _chest, number2: i);
        }
        return gotItems;
    }

    public static void OnOpenChest(Player player) => _lastTypeOnChest = new int[player.Chest()!.Length];
    public static void PostUpdate(Player player) => _chest = player.chest;
    public static void OnSlotLeftClick(int slot) => _leftClickedSlot = slot; 

    public static void OnItemTranfer(ItemSlot.ItemTransferInfo info) {
        if (!info.FromContenxt.InRange(0, 4) || info.ToContext != 21) return;

        for (int i = 0; i < _lastTypeOnInv.Length; i++) {
            if (_lastTypeOnInv[i] == info.ItemType) _lastTypeOnInv[i] = 0;
        }
        for (int i = 0; i < _lastTypeOnChest.Length; i++) {
            if (_lastTypeOnInv[i] == info.ItemType) _lastTypeOnInv[i] = 0;
        }
        if (info.FromContenxt.InRange(0, 2)) _lastTypeOnInv[_leftClickedSlot] = info.ItemType;
        else _lastTypeOnChest[_leftClickedSlot] = info.ItemType;
    }


    private static int _leftClickedSlot;
    private static readonly int[] _lastTypeOnInv = new int[58];
    private static int _chest; // reseted after the player update, later than player.chest and after dropItemCheck is called
    private static int[] _lastTypeOnChest = new int[40];

    public static readonly MethodInfo FillEmptyMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillOccupiedMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillEmptVoidMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillOccupiedVoidMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;

}