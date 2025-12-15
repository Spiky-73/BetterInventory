using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;

namespace BetterInventory.Features.QuickMove;

public static class MoveChainInputs {

    public static void ProcessTriggers(TriggersSet triggersSet) {
        // Break the chain if we close the inventory
        if (!Main.playerInventory) {
            QuickMove.BreakChain();
            return;
        }

        // Prevents Terraria from changing the selected item during the chain because of hotbar keys
        if (QuickMove.InChain()) triggersSet.KeyStatus[MoveKeyNames[_chainKey]] = false;
        // Save the selected item before the chain to restore it when it starts, this happens after the vanilla Hotbar behavior 
        else _preChainSelectedItem = Main.LocalPlayer.selectedItem;
    }

    public static void HoverSlot(Item[] inventory, int context, int slot) {
        HandleInput(() => InventoryLoader.GetInventorySlot(Main.LocalPlayer, inventory, context, slot));
    }

    public static void PostDrawInventory() {
        // Check for inputs if we did not hover any ItemSlot
        if (QuickMove.InChain() && !Main.LocalPlayer.mouseInterface) HandleInput(() => null);
    }

    private static void HandleInput(Func<InventorySlot?> getSourceSlot) {
        // Breaks the chain if the cursors goes outside the original slot or if we waited too long
        if (!_slotPosition.Contains(Main.mouseX, Main.mouseY) || _graceTime == 0) QuickMove.BreakChain();
        if (_graceTime > 0) _graceTime--;

        // Check if we pressed a MoveKey
        int moveKey = Array.FindIndex(MoveKeyNames, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (moveKey == -1) return;

        if (!QuickMove.InChain() || _chainKey != moveKey) {
            // Break the chain if we cannot get the InventorySlot
            var itemSlot = getSourceSlot();
            if (!itemSlot.HasValue) {
                QuickMove.BreakChain();
                return;
            }
            // Otherwise create a new one
            _chainKey = moveKey;
            _saveSlotPosition = true;
            Main.LocalPlayer.selectedItem = _preChainSelectedItem;
            QuickMove.SetupChain(itemSlot.Value, c => HotkeyToSlot(moveKey, c));
        }
        QuickMove.ContinueChain();
        SoundEngine.PlaySound(SoundID.Grab);
        _graceTime = QuickMoveConfig.Instance.graceTime;
    }

    public static void PostDrawItemSlot(Vector2 position) {
        if (!_saveSlotPosition) return;
        _saveSlotPosition = false;
        _slotPosition = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
    }


    public static readonly string[] MoveKeyNames = [.. new SpikysLib.DataStructures.Range(0, 10).Select(i => $"Hotbar{i + 1}")];

    public static int HotkeyToSlotRaw(int hotkey, int slotCount) => QuickMoveConfig.Instance.hotkeyMode switch {
        HotkeyMode.FromEnd => slotCount - MoveKeyNames.Length + hotkey,
        HotkeyMode.Reversed => MoveKeyNames.Length - hotkey - 1,
        HotkeyMode.Hotbar or _ => hotkey
    };
    public static int HotkeyToSlot(int hotkey, int slotCount) => Math.Clamp(HotkeyToSlotRaw(hotkey, slotCount), 0, slotCount - 1);

    public static int ChainKey() => _chainKey;

    private static int _preChainSelectedItem = -1;

    private static int _graceTime;
    private static int _chainKey = -1;

    private static bool _saveSlotPosition;
    private static Rectangle _slotPosition;
}
