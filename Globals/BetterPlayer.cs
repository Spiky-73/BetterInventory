using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Globals;

public sealed record class BuilderAccToggle {
    public BuilderAccToggle(string name, System.Predicate<Player> available, int number) : this(name, available, player => BetterPlayer.CycleAccState(player, number)) {}
    public BuilderAccToggle(string name, System.Predicate<Player> available, System.Action<Player> toggle) {
        Name = name;
        IsAvailable = available;
        Toggle = toggle;
        Keybind = null!;
    }

    public void AddKeybind(Mod mod) => Keybind = KeybindLoader.RegisterKeybind(mod, Name, Microsoft.Xna.Framework.Input.Keys.None);
    public void UnloadKeybind() => Keybind = null!;

    public void Process(Player player){
        if (Keybind.JustPressed && IsAvailable(player)) Toggle(player);
    }

    public string Name { get; }
    public ModKeybind Keybind { get; private set; }
    public System.Predicate<Player> IsAvailable { get; }
    public System.Action<Player> Toggle { get; }
}

public sealed class BetterPlayer : ModPlayer {

    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;
    public static readonly BuilderAccToggle[] BuilderAccToggles = new BuilderAccToggle[] {
        new("RulerLine", player => player.rulerLine, 0),
        new("RulerGrid", player => player.rulerGrid, 1),
        new("AutoActuator", player => player.autoActuator, 2),
        new("AutoPaint", player => player.autoPaint, 3),
        new("WireDisplay", player => player.InfoAccMechShowWires, player => {
            CycleAccState(player, 4, 3);
            for (int i = 5; i < 8; i++) player.builderAccStatus[i] = player.builderAccStatus[4];
            player.builderAccStatus[9] = player.builderAccStatus[4];
        }),
        new("ForcedWires", player => player.InfoAccMechShowWires, 8),
        new("BlockSwap", player => true, 10),
        new("BiomeTorches", player => player.unlockedBiomeTorches, 11)
    };
    public static readonly string[] SwapSlots = new[] {
        "Hotbar1",
        "Hotbar2",
        "Hotbar3",
        "Hotbar4",
        "Hotbar5",
        "Hotbar6",
        "Hotbar7",
        "Hotbar8",
        "Hotbar9",
        "Hotbar10"
    };

    public override void Load() {
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(Mod, "FavoritedQuickBuff", Microsoft.Xna.Framework.Input.Keys.N);
        foreach(BuilderAccToggle bat in BuilderAccToggles) bat.AddKeybind(Mod);
        
        On_ItemSlot.TryOpenContainer += HookTryOpenContainer;
        On_Player.OpenChest += HookOpenChest;
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        ItemSlot.OnItemTransferred += OnItemTranfer;
        On_Player.GetItem += HookGetItem;

    }
    public override void Unload() {
        FavoritedBuffKb = null!;
        foreach (BuilderAccToggle bat in BuilderAccToggles) bat.UnloadKeybind();
    }

    public override void Initialize() {
        _lastTypeInv = new int[Player.inventory.Length];
    }

    public override void SetControls() {
        if (Configs.ClientConfig.Instance.fastRightClick && Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (FavoritedBuffKb.JustPressed) FavoritedBuff(Player);

        foreach (BuilderAccToggle bat in BuilderAccToggles) bat.Process(Player);

        if (Configs.ClientConfig.Instance.itemSwap && Main.playerInventory && !Main.mouseItem.IsAir) { // TODO swap from chest and mouse item
            for (int destSlot = 0; destSlot < SwapSlots.Length; destSlot++) {
                if (!triggersSet.KeyStatus[SwapSlots[destSlot]]) continue;
                if (_swapping){
                    _swapping = true;

                    int sourceSlot = System.Array.FindIndex(Player.inventory, i => !i.IsNotSameTypePrefixAndStack(Main.HoverItem));
                    if(!sourceSlot.InRange(0, 50, Utility.InclusionFlag.Min)) goto swapped;

                    (Player.inventory[destSlot], Player.inventory[sourceSlot]) = (Player.inventory[sourceSlot], Player.inventory[destSlot]);
                    ItemSlot.AnnounceTransfer(new(Player.inventory[destSlot], ItemSlot.Context.InventoryItem, ItemSlot.Context.InventoryItem));
                    ItemSlot.AnnounceTransfer(new(Player.inventory[sourceSlot], ItemSlot.Context.InventoryItem, ItemSlot.Context.InventoryItem));
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                goto swapped;
            }
            _swapping = false;
        swapped:;
        }
    }

    public override bool PreItemCheck() {
        if (Configs.ClientConfig.Instance.itemRightClick && Player.controlUseTile && Player.releaseUseItem && !Player.controlUseItem && !Player.tileInteractionHappened
                && !Player.mouseInterface && !Terraria.Graphics.Capture.CaptureManager.Instance.Active && !Main.HoveringOverAnNPC && !Main.SmartInteractShowingGenuine
                && Main.HoverItem.IsAir && Player.altFunctionUse == 0 && Player.selectedItem < 10) {
            int split = Main.stackSplit;
            Main.stackSplit = 2;
            ItemSlot.RightClick(Player.inventory, 0, Player.selectedItem); // TODO disable if would pickup item in mouse
            if (Main.stackSplit == 2) Main.stackSplit = split;
            return false;
        }
        return true;
    }

    private static void HookTryOpenContainer(On_ItemSlot.orig_TryOpenContainer orig, Item item, Player player) {
        int stackSplit = Main.stackSplit;
        orig(item, player);
        if (Configs.ClientConfig.Instance.fastRightClick && Main.stackSplit == stackSplit) {
            Main.stackSplit = stackSplit;
            ItemSlot.RefreshStackSplitCooldown();
        }
    }

    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        _leftClickedSlot = slot;
        orig(inv, context, slot);
    }
    private static void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) { // TODO add
        _leftClickedSlot = slot;
        orig(inv, context, slot);
    }

    private static void HookOpenChest(On_Player.orig_OpenChest orig, Player self, int x, int y, int newChest) {
        _lastTypeChest = new int[self.Chest(newChest)!.Length];
        orig(self, x, y, newChest);
    }

    public static void OnItemTranfer(ItemSlot.ItemTransferInfo info) {
        int[] inv;
        if (info.FromContenxt.InRange(ItemSlot.Context.InventoryItem, ItemSlot.Context.InventoryAmmo)) inv = _lastTypeInv;
        else if (info.FromContenxt.InRange(ItemSlot.Context.ChestItem, ItemSlot.Context.BankItem)) inv = _lastTypeChest;
        else if (info.ToContext.InRange(ItemSlot.Context.InventoryItem, ItemSlot.Context.InventoryAmmo)) inv = _lastTypeInv;
        else if (info.ToContext.InRange(ItemSlot.Context.ChestItem, ItemSlot.Context.BankItem)) inv = _lastTypeChest;
        else return;
        for (int i = 0; i < inv.Length; i++) if (inv[i] == info.ItemType) inv[i] = 0;
        inv[_leftClickedSlot] = info.ItemType;
    }

    private static Item HookGetItem(On_Player.orig_GetItem orig, Player self, int plr, Item newItem, GetItemSettings settings) {
        // if (Configs.ClientConfig.SmartPickupEnabled(newItem)){
        //     bool gotItems = false;
        //     int slot;
        //     if (self.InChest(out Item[]? chest) && (slot = System.Array.IndexOf(_lastTypeChest, newItem.type)) != -1 && chest[slot] != newItem) {
        //         object[] args = new object[] { plr, chest, newItem, settings, newItem, slot };
        //         if (chest[slot].type == ItemID.None) gotItems = (bool)FillEmptVoidMethod.Invoke(self, args)!;
        //         else if (chest[slot].type == newItem.type && newItem.maxStack > 1) gotItems = (bool)FillOccupiedVoidMethod.Invoke(self, args)!;
        //         else if (newItem.favorited || !chest[slot].favorited) (chest[slot], newItem) = (newItem, chest[slot]);
        //         if (Main.netMode == NetmodeID.MultiplayerClient && self.chest > -1) NetMessage.SendData(MessageID.SyncChestItem, number: self.chest, number2: slot);
        //     } else if ((slot = System.Array.IndexOf(_lastTypeInv, newItem.type)) != -1 && self.inventory[slot] != newItem) {
        //         object[] args = new object[] { plr, newItem, settings, newItem, slot };
        //         if (self.inventory[slot].type == ItemID.None) gotItems = (bool)FillEmptyMethod.Invoke(self, args)!;
        //         else if (self.inventory[slot].type == newItem.type && newItem.maxStack > 1) gotItems = (bool)FillOccupiedMethod.Invoke(self, args)!;
        //         else if (newItem.favorited || !self.inventory[slot].favorited) {
        //             (newItem, self.inventory[slot]) = (self.inventory[slot], newItem);
        //         }
        //     }
        //     if(gotItems) return new();
        // }
        return orig(self, plr, newItem, settings);
    }


    public static void CycleAccState(Player player, int index, int cycle = 2) => player.builderAccStatus[index] = (player.builderAccStatus[index] + 1) % cycle;
    public static void FavoritedBuff(Player player) => Utility.RunWithHiddenItems(player.inventory, i => !i.favorited, player.QuickBuff);

    private static bool _swapping;

    private static int _leftClickedSlot;
    private static int[] _lastTypeInv = System.Array.Empty<int>();
    private static int[] _lastTypeChest = System.Array.Empty<int>();

    public static readonly MethodInfo FillEmptyMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillOccupiedMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillEmptVoidMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillOccupiedVoidMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
}
