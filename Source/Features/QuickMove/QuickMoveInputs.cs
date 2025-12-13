using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Features.QuickMove;

public sealed class QuickMovePlayer : ModPlayer {

    public static QuickMovePlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<QuickMovePlayer>();

    public override void Load() {
        On_Main.DrawInventory += HookDrawInventory;
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += HookSaveSlotPosition;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (!Configs.Features.QuickMove) return;

        // Break the chain if we close the inventory
        if (!Main.playerInventory) {
            QuickMoveChain.BreakChain();
            return;
        }

        // Prevents Terraria from changing the selected item during the chain because of hotbar keys
        if (QuickMoveChain.InChain()) PlayerInput.Triggers.Current.KeyStatus[QuickMoveUtils.MoveKeyNames[_chainKey]] = false;
        // Save the selected item before the chain to restore it when it starts, this happens after the vanilla Hotbar behavior 
        else _preChainSelectedItem = Player.selectedItem;
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        QuickMoveItem.HoverSlot(inventory, context, slot);
        if (Configs.Features.QuickMove) HandleInput(() => InventoryLoader.GetInventorySlot(Player, inventory, context, slot));
        return false;
    }

    private static void HookDrawInventory(On_Main.orig_DrawInventory orig, Main self) {
        orig(self);
        // Check for inputs if we did not hover any ItemSlot
        if (Configs.Features.QuickMove && QuickMoveChain.InChain() && !Main.LocalPlayer.mouseInterface) LocalPlayer.HandleInput(() => null);
    }

    private void HandleInput(Func<InventorySlot?> getSourceSlot) {
        // Breaks the chain if the cursors goes outside the original slot or if we waited too long
        if (!_slotPosition.Contains(Main.mouseX, Main.mouseY) || _graceTime == 0) QuickMoveChain.BreakChain();
        if (_graceTime > 0) _graceTime--;

        // Check if we pressed a MoveKey
        int moveKey = Array.FindIndex(QuickMoveUtils.MoveKeyNames, key => PlayerInput.Triggers.JustPressed.KeyStatus[key]);
        if (moveKey == -1) return;

        if (!QuickMoveChain.InChain() || _chainKey != moveKey) {
            // Break the chain if we cannot get the InventorySlot
            var itemSlot = getSourceSlot();
            if (!itemSlot.HasValue) {
                QuickMoveChain.BreakChain();
                return;
            }
            // Otherwise create a new one
            _chainKey = moveKey;
            _saveSlotPosition = true;
            Player.selectedItem = _preChainSelectedItem;
            QuickMoveChain.SetupChain(itemSlot.Value, c => QuickMoveUtils.HotkeyToSlot(moveKey, c));
        }
        QuickMoveChain.ContinueChain();
        SoundEngine.PlaySound(SoundID.Grab);
        _graceTime = Configs.QuickMove.Instance.graceTime;
    }

    private static void HookSaveSlotPosition(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
        orig(spriteBatch, inv, context, slot, position, lightColor);
        var player = LocalPlayer;
        if (!player._saveSlotPosition) return;
        player._saveSlotPosition = false;
        player._slotPosition = new((int)position.X, (int)position.Y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
    }

    public int ChainKey() => _chainKey;

    private int _preChainSelectedItem = -1;

    private int _graceTime;
    private int _chainKey = -1;

    private bool _saveSlotPosition;
    private Rectangle _slotPosition;
}
