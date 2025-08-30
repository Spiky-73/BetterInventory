using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace BetterInventory.InventoryManagement.SmartPickup;

public sealed class PreviousSlotItem : GlobalItem {
    public override void OnConsumeItem(Item item, Player player) {
        if (!Configs.PreviousSlot.Consumption) return;
        var inventorySlot = InventoryLoader.FindItem(player, i => i == item);
        if (inventorySlot.HasValue) player.GetModPlayer<PreviousSlotPlayer>().MarkWithConfig(item.type, inventorySlot.Value, item.favorited);
    }
}
public sealed class PreviousSlotPlayer : ModPlayer {

    public override void Load() {
        On_ItemSlot.LeftClick_ItemArray_int_int += HookMarkOnLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookMarkOnRightClick;
        On_Player.DropItems += HookMarkOnDeath;

        On_Recipe.ConsumeForCraft += HookMarkConsumeMaterial;

        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static il => {
            if (!il.ApplyTo(ILDrawFakeItem, Configs.PreviousDisplay.FakeItem)) Configs.UnloadedInventoryManagement.Value.displayFakeItem = true;
            if (!il.ApplyTo(ILDrawIcon, Configs.PreviousDisplay.Icon)) Configs.UnloadedInventoryManagement.Value.displayIcon = true;
        };
    }

    private static void HookMarkOnLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) => UpdateMark((inv, context, slot) => orig(inv, context, slot), inv, context, slot, Main.mouseLeft && Main.mouseLeftRelease);
    private static void HookMarkOnRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) => UpdateMark((inv, context, slot) => orig(inv, context, slot), inv, context, slot, Main.mouseRight);
    private static void UpdateMark(Action<Item[], int, int> orig, Item[] inv, int context, int slot, bool update) {
        if (!update || !Configs.PreviousSlot.Mouse || !InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out InventorySlot mark)) {
            orig(inv, context, slot);
            return;
        }
        (int oldType, int oldMouse, bool oldFav) = (inv[slot].type, Main.mouseItem.type, inv[slot].favorited);
        orig(inv, context, slot);
        if (Main.mouseItem.type == oldMouse) return;

        bool removed = oldType != ItemID.None && (Configs.SmartPickup.Value.previousSlot == Configs.ItemPickupLevel.AllItems || mark.Inventory.CanBePreferredInventory || oldFav);
        bool placed = inv[slot].type != ItemID.None && (Configs.SmartPickup.Value.previousSlot == Configs.ItemPickupLevel.AllItems || mark.Inventory.CanBePreferredInventory || inv[slot].favorited);

        var modPlayer = Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>();

        if (placed && removed) modPlayer.Remark(inv[slot].type, oldType, oldFav);
        else if (removed) modPlayer.Mark(oldType, mark, oldFav);
        else if (placed) modPlayer.Unmark(inv[slot].type);
        if (placed) modPlayer.Unmark(mark);
    }


    public override void OnConsumeAmmo(Item weapon, Item ammo) {
        if (!Configs.PreviousSlot.Consumption) return;
        if (Main.netMode == NetmodeID.Server) return;
        var inventorySlot = InventoryLoader.FindItem(Player, i => i == ammo);
        if (inventorySlot.HasValue) MarkWithConfig(ammo.type, inventorySlot.Value, ammo.favorited);
    }
    public override bool? CanConsumeBait(Item bait) {
        if (!Configs.PreviousSlot.Consumption) return null;
        var inventorySlot = InventoryLoader.FindItem(Player, i => i == bait);
        if (inventorySlot.HasValue) MarkWithConfig(bait.type, inventorySlot.Value, bait.favorited);
        return null;
    }

    private static bool HookMarkConsumeMaterial(On_Recipe.orig_ConsumeForCraft orig, Recipe self, Item item, Item requiredItem, ref int stackRequired) {
        if (!Configs.PreviousSlot.Consumption) return orig(self, item, requiredItem, ref stackRequired);
        var inventorySlot = InventoryLoader.FindItem(Main.LocalPlayer, i => i == item);
        if (inventorySlot.HasValue) Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>().MarkWithConfig(item.type, inventorySlot.Value, item.favorited);
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
                modPlayer.MarkWithConfig(items[i].type, new(inventory, i), items[i].favorited);
            }
        }
        orig(self);
    }

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
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc(scale);
        cursor.EmitLdloc(texture);
        cursor.EmitLdloc(color);
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
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitLdarg3();
        cursor.EmitLdarg(4);
        cursor.EmitLdloc(scale);
        cursor.EmitLdloc(texture);
        cursor.EmitLdloc(color);
        cursor.EmitDelegate((SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color) => {
            if (!Main.gameMenu && !s_ilBackgroundMark && Configs.PreviousDisplay.Icon) TryDrawMark(spriteBatch, inv, context, slot, position, scale, texture, color, Configs.PreviousDisplay.Value.icon.Value);
            s_ilBackgroundMark = false;
        });
    }
    private static bool s_ilBackgroundMark;

    private static bool TryDrawMark(SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, float scale, Texture2D texture, Color color, Configs.IPreviousDisplay ui) {
        var modPlayer = Main.LocalPlayer.GetModPlayer<PreviousSlotPlayer>();
        if (!InventoryLoader.IsInventorySlot(Main.LocalPlayer, inv, context, slot, out InventorySlot itemSlot) || !modPlayer.TryGetMark(itemSlot, out Item? mark)) return false;
        if (inv[slot].type == mark.type) return false;
        ItemSlot.DrawItemIcon(mark, ItemSlot.Context.InWorld, spriteBatch, position + texture.Size() * ui.position * scale, scale * ui.scale, 32f, color * Main.cursorAlpha * ui.intensity);
        return true;
    }

    public Item PickupItemToPreviousSlot(Player player, Item item, GetItemSettings settings) {
        if (player.whoAmI != Main.myPlayer) return item;

        List<InventorySlot> slots = new();
        while (ConsumeMark(item.type, out (InventorySlot slot, bool favorited) mark)) {
            IList<Item> items = mark.slot.Inventory.Items;
            if (mark.slot.Index >= items.Count) continue;
            if (!(item.favorited || mark.favorited) && items[mark.slot.Index].favorited) continue;

            item.favorited |= mark.favorited;
            Item? moved = null;
            if (Configs.PreviousSlot.Value.moveItems) (moved, items[mark.slot.Index]) = (items[mark.slot.Index], new());
            Item toMove = item.Clone();
            toMove.stack = 1;
            if (mark.slot.GetItem(toMove, settings).IsAir) item.stack--;
            if (moved is not null && !moved.IsAir) {
                moved = mark.slot.GetItem(moved, settings);
                player.GetDropItem(ref moved);
            }
            if (item.IsAir) return item;
            slots.Add(mark.slot);
        }
        foreach (InventorySlot slot in slots) {
            item = slot.GetItem(item, settings);
            if (item.IsAir) return item;
        }
        return item;
    }

    public bool ConsumeMark(int type, [MaybeNullWhen(false)] out (InventorySlot slot, bool favorited) mark) {
        Queue<(int type, int depth)> items = [];
        int checksLeft = Configs.PreviousSlot.Value.materials ? Configs.PreviousSlot.Value.materials.Value.maxChecks : 1;
        items.Enqueue((type, Configs.PreviousSlot.Value.materials.Value.maxDepth));
        HashSet<int> added = [type];
        while (items.TryDequeue(out var item)) {
            if (TryGetLastMarkedSlot(item.type, out var slot) && TryGetMark(slot, out var markedItem)) {
                mark = (slot, markedItem.favorited);
                Unmark(slot);
                return true;
            }
            checksLeft--;
            if (checksLeft == 0) break;
            item.depth--;
            if (item.depth == 0) continue;
            foreach (int recipeIndex in ItemID.Sets.CraftingRecipeIndices[item.type]) {
                foreach (Item material in Main.recipe[recipeIndex].requiredItem) {
                    if (added.Add(material.type)) items.Enqueue((material.type, item.depth));
                }
            }
        }
        mark = default;
        return false;
    }

    public bool IsMarked(int type) => _marksPerType.TryGetValue(type, out var marks) && marks.Count > 0;
    public bool IsMarked(InventorySlot slot) => _marksPerSlot.ContainsKey(slot);
    public bool TryGetMark(InventorySlot slot, [MaybeNullWhen(false)] out Item mark) => _marksPerSlot.TryGetValue(slot, out mark);
    public bool TryGetLastMarkedSlot(int type, [MaybeNullWhen(false)] out InventorySlot slot) {
        if (!_marksPerType.TryGetValue(type, out var marks) || marks.Count == 0) {
            slot = default;
            return false;
        }
        slot = marks[^1];
        return true;
    }

    public void MarkWithConfig(int type, InventorySlot slot, bool favorited) {
        if (Configs.SmartPickup.Value.previousSlot == Configs.ItemPickupLevel.AllItems || slot.Inventory.CanBePreferredInventory || favorited) Mark(type, slot, favorited);
    }

    public void Mark(int type, InventorySlot slot, bool favorited) {
        if (IsMarked(slot)) {
            if (!Configs.PreviousSlot.Value.overridePrevious) return;
            Unmark(slot);
        }
        _marksPerType.TryAdd(type, new());
        _marksPerType[type].Add(slot);
        _marksPerSlot[slot] = new(type) { favorited = favorited };
    }
    public void Unmark(int type) {
        if (!IsMarked(type)) return;
        foreach (InventorySlot mark in _marksPerType[type]) _marksPerSlot.Remove(mark);
        _marksPerType.Remove(type);
    }
    public void Unmark(InventorySlot slot) {
        if (!IsMarked(slot)) return;
        _marksPerType[_marksPerSlot[slot].type].Remove(slot);
        _marksPerSlot.Remove(slot);
    }
    public void Remark(int oldType, int newType, bool? favorited = null) {
        Unmark(newType);
        if (!IsMarked(oldType)) return;
        foreach (InventorySlot slot in _marksPerType[oldType]) _marksPerSlot[slot] = new(newType) { favorited = favorited ?? _marksPerSlot[slot].favorited };
        _marksPerType[newType] = _marksPerType[oldType];
        _marksPerType.Remove(oldType);
    }

    private readonly Dictionary<InventorySlot, Item> _marksPerSlot = [];
    private readonly Dictionary<int, List<InventorySlot>> _marksPerType = [];

    public const string MarksTag = "marks";
    public const string ItemTag = "item";
    public const string SlotsTag = "slots";
    public const string ModTag = "mod";
    public const string NameTag = "name";
    public const string DataTag = "data";
    public const string IndexTag = "index";
    public const string FavoritedTag = "favorited";
}