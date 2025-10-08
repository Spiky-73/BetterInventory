using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace BetterInventory.InventoryManagement.SmartPickup;

public sealed class PreviousSlotItem : GlobalItem {
    public override void OnConsumeItem(Item item, Player player) {
        if (!Configs.PreviousSlot.Consumption) return;
        var inventorySlot = InventoryLoader.FindItem(player, i => i == item);
        if (inventorySlot.HasValue) player.GetModPlayer<PreviousSlotPlayer>().RemoveItem(inventorySlot.Value, item, true);
    }
}

public sealed partial class PreviousSlotPlayer : ModPlayer {
    private static void HookItemSlotLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) => HookItemSlotMarkOnClick((inv, context, slot) => orig(inv, context, slot), inv, context, slot, Main.mouseLeft && Main.mouseLeftRelease);
    private static void HookItemSlotRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) => HookItemSlotMarkOnClick((inv, context, slot) => orig(inv, context, slot), inv, context, slot, Main.mouseRight || (Configs.InventoryManagement.DepositClick && Main.mouseMiddle));
    private static void HookItemSlotMarkOnClick(Action<Item[], int, int> orig, Item[] inv, int context, int slot, bool click) {
        if (!click || !(Configs.PreviousSlot.Mouse || Configs.PreviousSlot.ShiftClick) || !InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out InventorySlot mark)) {
            orig(inv, context, slot);
            return;
        }
        (int oldType, int oldMouse, bool oldFav) = (inv[slot].type, Main.mouseItem.type, inv[slot].favorited);
        int oldCount = inv[slot].stack + Main.mouseItem.stack;
        orig(inv, context, slot);
        if (Main.cursorOverride <= CursorOverrideID.DefaultCursor) {
            if (!Configs.PreviousSlot.Mouse) return;
            if (oldCount != inv[slot].stack + Main.mouseItem.stack) return; // or if an item was consumed
            if (oldType != ItemID.None && oldType != inv[slot].type && oldType != Main.mouseItem.type) return; // or if an item was moved elsewhere
            if (oldMouse != ItemID.None && oldMouse != inv[slot].type && oldMouse != Main.mouseItem.type) return;
        } else if (!Configs.PreviousSlot.ShiftClick) return;

        var modPlayer = Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>();
        if (oldType != ItemID.None) modPlayer.RemoveItem(mark, oldType, oldFav);
        if (inv[slot].type != ItemID.None) modPlayer.PlaceItem(mark, inv[slot]);
    }


    public override void OnConsumeAmmo(Item weapon, Item ammo) {
        if (!Configs.PreviousSlot.Consumption) return;
        if (Main.netMode == NetmodeID.Server) return;
        var inventorySlot = InventoryLoader.FindItem(Player, i => i == ammo);
        if (inventorySlot.HasValue) RemoveItem(inventorySlot.Value, ammo, true);
    }
    public override bool? CanConsumeBait(Item bait) {
        if (!Configs.PreviousSlot.Consumption) return null;
        var inventorySlot = InventoryLoader.FindItem(Player, i => i == bait);
        if (inventorySlot.HasValue) RemoveItem(inventorySlot.Value, bait, true);
        return null;
    }

    private static bool HookMarkConsumeMaterial(On_Recipe.orig_ConsumeForCraft orig, Recipe self, Item item, Item requiredItem, ref int stackRequired) {
        if (!Configs.PreviousSlot.Consumption) return orig(self, item, requiredItem, ref stackRequired);
        var inventorySlot = InventoryLoader.FindItem(Main.LocalPlayer, i => i == item);
        if (inventorySlot.HasValue) Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>().RemoveItem(inventorySlot.Value, item);
        return orig(self, item, requiredItem, ref stackRequired);
    }

    private static void HookMarkOnDeath(On_Player.orig_DropItems orig, Player self) {
        if (!Configs.PreviousSlot.MediumCore) {
            orig(self);
            return;
        }
        var modPlayer = self.GetModPlayer<PreviousSlotPlayer>();
        foreach (ModSubInventory inventory in InventoryLoader.GetInventories(self)) {
            IList<Item> items = inventory.Items;
            for (int i = 0; i < items.Count; i++) {
                modPlayer.RemoveItem(new(inventory, i), items[i]);
            }
        }
        orig(self);
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if (!Configs.PreviousDisplay.Icon || !InventoryLoader.IsInventorySlot(Player, inventory, context, slot, out var itemSlot)) return false;
        if (!inventory[slot].IsAir || !TryGetPreviousItem(itemSlot, out _)) return false;

        if (!ItemSlot.Options.DisableQuickTrash && (ItemSlot.Options.DisableLeftShiftTrashCan ? ItemSlot.ControlInUse : ItemSlot.ShiftInUse)) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            return true;
        }
        return false;
    }

    private static bool HookClearMark(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!Configs.PreviousDisplay.Icon || !InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out var itemSlot)) return orig(inv, context, slot);
        var modPlayer = Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>();
        if (!inv[slot].IsAir || !modPlayer.TryGetPreviousItem(itemSlot, out _) || Main.cursorOverride != CursorOverrideID.TrashCan) return orig(inv, context, slot);

        modPlayer.ClearPreviousItem(itemSlot);
        SoundEngine.PlaySound(SoundID.Grab);
        return true;
    }
}

public sealed partial class PreviousSlotPlayer : ModPlayer {

    private static void ILDrawFakeItem(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int scale, i => i.Previous.MatchLdsfld(Reflection.Main.inventoryScale), 2);
        cursor.GotoNextLoc(out int color, i => i.Previous.MatchCall(Reflection.Color.White.GetMethod!), 3);
        cursor.GotoNextLoc(out int texture, i => i.Previous.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!), 7);

        cursor.GotoNext(i => i.SaferMatchCallvirt(Reflection.AccessorySlotLoader.DrawSlotTexture));
        cursor.GotoPrevLoc(out int icon, i => i.Previous.MatchLdcI4(0) && i.Next.MatchBr(out _), 11);

        // ...
        // int num9 = context switch { ... };
        // if ((item.type <= 0 || item.stack <= 0) && ++[!<drawMark>] && num9 != -1) <drawSlotTexture>
        cursor.GotoNext(MoveType.After, i => i.MatchLdloc(icon));
        cursor.EmitLdarg0().EmitLdarg1().EmitLdarg2().EmitLdarg3().EmitLdarg(4).EmitLdloc(scale).EmitLdloc(texture).EmitLdloc(color);
        cursor.EmitDelegate((int num9, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color) => {
            if (!Configs.PreviousDisplay.FakeItem || !TryDrawMark(spriteBatch, inv, context, slot, position, scale, texture, color, Configs.PreviousDisplay.Value.fakeItem.Value)) return num9;
            s_ilBackgroundMark = true;
            return -1;
        });
    }
    private static void ILDrawIcon(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNextLoc(out int scale, i => i.Previous.MatchLdsfld(Reflection.Main.inventoryScale), 2);
        cursor.GotoNextLoc(out int color, i => i.Previous.MatchCall(Reflection.Color.White.GetMethod!), 3);
        cursor.GotoNextLoc(out int texture, i => i.Previous.MatchCallvirt(Reflection.Asset<Texture2D>.Value.GetMethod!), 7);

        // ...
        // if(...) {
        // } else if (context == 6) {
        //     ...
        //     spriteBatch.Draw(value10, position4, null, new Color(100, 100, 100, 100), 0f, default(Vector2), inventoryScale, 0, 0f);
        // }
        // if (context == 0 && ++[!<hideKeys> && slot < 10]) {
        //     ...
        // }
        cursor.GotoNext(i => i.SaferMatchCall(typeof(UILinkPointNavigator), nameof(UILinkPointNavigator.SetPosition)));
        cursor.GotoPrev(i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdarg2());

        // ++ <drawMark>
        cursor.EmitLdarg0().EmitLdarg1().EmitLdarg2().EmitLdarg3().EmitLdarg(4).EmitLdloc(scale).EmitLdloc(texture).EmitLdloc(color);
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color) => {
            if (!Main.gameMenu && !s_ilBackgroundMark && Configs.PreviousDisplay.Icon) TryDrawMark(spriteBatch, inv, context, slot, position, scale, texture, color, Configs.PreviousDisplay.Value.icon.Value);
            s_ilBackgroundMark = false;
        });
    }

    private static bool TryDrawMark(SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color, Configs.IPreviousDisplay ui) {
        var modPlayer = Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>();
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out InventorySlot itemSlot) || !modPlayer.TryGetPreviousItem(itemSlot, out Item? mark)) return false;
        ItemSlot.DrawItemIcon(mark, ItemSlot.Context.InWorld, spriteBatch, position + texture.Size() * ui.position * scale, scale * ui.scale, 32f, color * Main.cursorAlpha * ui.intensity);
        return true;
    }

    private static bool s_ilBackgroundMark;
}

public sealed partial class PreviousSlotPlayer : ModPlayer {

    public override void Load() {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookItemSlotLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookItemSlotRightClick;
        On_Player.DropItems += HookMarkOnDeath;

        On_Recipe.ConsumeForCraft += HookMarkConsumeMaterial;

        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILDrawFakeItem, Configs.PreviousDisplay.FakeItem)) Configs.UnloadedInventoryManagement.Value.displayFakeItem = true;
            if (!il.ApplyTo(ILDrawIcon, Configs.PreviousDisplay.Icon)) Configs.UnloadedInventoryManagement.Value.displayIcon = true;
        };

        On_ItemSlot.OverrideLeftClick += HookClearMark;
    }

    public Item PickupItemToAnyPreviousSlot(Item item, GetItemSettings settings) => PickupItemToPreviousSlot(item, settings, [.. _inventoryPreviousSlots.Keys]);
    public Item PickupItemToPreviousSlot(Item item, GetItemSettings settings, params ModSubInventory[] inventories) {
        foreach (var inventory in inventories) {
            item = PickupItemToPreviousSlot(item, settings, inventory);
            if (item.IsAir) return item;
        }
        return item;
    }
    public Item PickupItemToPreviousSlot(Item item, GetItemSettings settings, ModSubInventory inventory) {
        if (Player.whoAmI != Main.myPlayer) return item;
        if (!_inventoryPreviousSlots.TryGetValue(inventory, out var previousItemSlots)) return item;

        var slots = previousItemSlots.GetSlots(item);
        IList<Item> items = inventory.Items;

        foreach (int slot in slots) {
            Item mark = previousItemSlots.Get(slot);
            if (slot >= items.Count) continue;

            Item oldItem = items[slot];

            if (!oldItem.IsAir && Configs.PreviousSlot.Value.movePolicy switch {
                Configs.MovePolicy.Always => oldItem.favorited && !(mark.favorited || item.favorited),
                Configs.MovePolicy.NotFavorited => oldItem.favorited,
                Configs.MovePolicy.Never or _ => true,
            }) continue;

            items[slot] = new(); // stored in `oldItem`
            item = inventory.GetItem(item, slot, settings);
            if (items[slot].type == mark.type) {
                items[slot].favorited |= mark.favorited;
                previousItemSlots.Clear(slot);
            }

            if (!oldItem.IsAir) oldItem = inventory.GetItem(oldItem, slot, settings);
            if (!oldItem.IsAir) oldItem = inventory.GetItem(oldItem, settings);
            if (!oldItem.IsAir) Player.GetDropItem(ref oldItem);

            if (item.IsAir) return item;
        }
        return item;
    }

    public void PlaceItem(InventorySlot slot, Item item) {
        if (_inventoryPreviousSlots.TryGetValue(slot.Inventory, out var previousItems)) {
            if (previousItems.TryGet(slot.Index, out Item? oldItem)) previousItems.Replace(item, oldItem);
            previousItems.Clear(slot.Index);
        }
        foreach ((var _, var value) in _inventoryPreviousSlots) value.ClearSlots(item);
    }
    public void RemoveItem(InventorySlot slot, int type, bool favorited, bool delayed = false) => RemoveItem(slot, new(type, 1) { favorited = favorited }, delayed);
    public void RemoveItem(InventorySlot slot, Item item, bool delayed = false) {
        if (delayed) {
            _delayedRemovals.Add((slot, item.type, item.favorited));
            return;
        }
        if (Configs.SmartPickup.Value.previousSlot != Configs.ItemPickupLevel.AllItems && !slot.Inventory.CanBePreferredInventory && !item.favorited) return;
        if (!_inventoryPreviousSlots.TryGetValue(slot.Inventory, out var previousItems)) _inventoryPreviousSlots[slot.Inventory] = previousItems = new();
        if (previousItems.TryGet(slot.Index, out _) && !Configs.PreviousSlot.Value.overridePrevious) return;
        previousItems.Set(slot.Index, item);
    }

    public bool TryGetPreviousItem(InventorySlot slot, [MaybeNullWhen(false)] out Item item) {
        item = default;
        return _inventoryPreviousSlots.TryGetValue(slot.Inventory, out var previousSlots) && previousSlots.TryGet(slot.Index, out item);
    }
    public void ClearPreviousItem(InventorySlot slot) {
        _inventoryPreviousSlots[slot.Inventory].Clear(slot.Index);
    }

    public override void PostUpdate() {
        foreach ((var slot, var type, var favorited) in _delayedRemovals) {
            if (slot.Item.IsAir || slot.Item.type != type) RemoveItem(slot, type, favorited, false);
        }
        _delayedRemovals.Clear();
    }

    public override void SaveData(TagCompound tag) {
        foreach ((ModSubInventory inventory, InventoryPreviousItemSlot previousItemSlots) in _inventoryPreviousSlots) {
            TagCompound slotsTag = [new(SlotsTag, previousItemSlots)];
            TagCompound dataTag = [];
            inventory.SaveData(dataTag);
            if (dataTag.Count > 0) slotsTag[DataTag] = dataTag;
            tag[inventory.FullName] = slotsTag;
        }
        foreach ((var key, var value) in _unloadedInventories) tag[key] = value;
    }

    public override void LoadData(TagCompound tag) => _loadedData = tag;
    public override void OnEnterWorld() {
        _inventoryPreviousSlots.Clear();
        _unloadedInventories.Clear();
        foreach ((var key, var value) in _loadedData) {
            var definitionParts = key.Split('/', 2);
            TagCompound slotsTag = (TagCompound)value;
            if (!ModContent.TryFind<ModSubInventory>(definitionParts[0], definitionParts[1], out var inventoryTemplate)) {
                _unloadedInventories[key] = value;
                continue;
            }
            var inventory = inventoryTemplate.NewInstance(Player);
            if (slotsTag.TryGet(DataTag, out TagCompound data)) inventory.LoadData(data);
            if (inventory.ForceUnloaded) {
                _unloadedInventories[key] = value;
                continue;
            }
            _inventoryPreviousSlots[inventory] = slotsTag.Get<InventoryPreviousItemSlot>(SlotsTag);
        }
    }

    private readonly Dictionary<ModSubInventory, InventoryPreviousItemSlot> _inventoryPreviousSlots = [];
    private readonly List<(InventorySlot slot, int type, bool favorited)> _delayedRemovals = [];

    private readonly Dictionary<string, object> _unloadedInventories = [];
    private TagCompound _loadedData = [];

    public const string DataTag = "data";
    public const string SlotsTag = "slots";
}