using System.Collections.Generic;
using BetterInventory.ItemSearch;
using MonoMod.Cil;
using SpikysLib.Extensions;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.SearchProviders;

public sealed class RecipeList : SearchProvider {

    public override void Load() {
        Keybind = KeybindLoader.RegisterKeybind(Mod, Name, "Mouse1");

        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClickHistory;
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;

        On_Player.dropItemCheck += HookDropItems;
    }

    public override void Unload() {
        _guideHistory[0].Clear();
        _guideHistory[1].Clear();
    }

    internal static void ILForceGuideDisplay(ILContext il) {
        ILCursor cursor = new(il);

        // if(Main.InReforgeMenu){
        //     ...
        // } else if(Main.InGuideCraftMenu) {
        //     if(<closeGuideUI>) ...
        //     else {
        ILLabel? endGuide = null;
        cursor.GotoNext(i => i.SaferMatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(MoveType.After, i => i.MatchBr(out endGuide));

        //         ++ guide:
        ILLabel guide = cursor.DefineLabel();
        cursor.MarkLabel(guide);

        cursor.GotoLabel(endGuide!, MoveType.Before);

        //         ++ if(<alternateGuideDraw>) goto recipe;
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitDelegate(() => Configs.QuickSearch.Recipes && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(recipe);
        cursor.MarkLabel(recipe); // Here in case of exception
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStloc(124)); // int num63
        cursor.GotoPrev(MoveType.After, i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentRecipeSmall));

        //     ++ if(<alternateGuideDraw>) goto guide;
        cursor.EmitDelegate(() => Configs.QuickSearch.Recipes && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(guide);

        //     ++ recipe:
        cursor.MarkLabel(recipe);
    }


    public override bool Visible => Main.playerInventory && Main.recBigList && !Main.CreativeMenu.Enabled;

    public override void Toggle(bool? enabled = null) {
        if (Visible) {
            if (enabled == true) return;
            Main.recBigList = false;
        } else {
            if (enabled == false) return;
            if (!Main.playerInventory) {
                Main.LocalPlayer.ToggleInv();
                if (!Main.playerInventory) Main.LocalPlayer.ToggleInv();
            } else {
                Main.CreativeMenu.CloseMenu();
                Main.LocalPlayer.tileEntityAnchor.Clear();
            }
            Main.recBigList = Main.numAvailableRecipes > 0;
        }
    }

    public override void Search(Item item) => Search(item, -1);
    public static void Search(Item item, int slot = -1) {
        (Item mouse, Main.mouseItem) = (Main.mouseItem, item);
        (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);
        (bool left, Main.mouseLeft, bool rel, Main.mouseLeftRelease) = (Main.mouseLeft, true, Main.mouseLeftRelease, true);
        Item[] items = Guide.GuideItems;

        if (item.IsAir) {
            if (slot == -1 || slot == 0) ItemSlot.LeftClick(items, ContextID.GuideItem, 0);
            if (slot == -1 || slot == 1) ItemSlot.LeftClick(items, ContextID.GuideItem, 1);
        } else {
            if (slot == -1) slot = Configs.BetterGuide.Tile && Guide.IsCraftingStation(item) ? 1 : 0;
            if (Configs.BetterGuide.Tile && Guide.FitsCraftingTile(item) && Guide.GetPlaceholderType(item) == PlaceholderType.None) {
                bool clearOtherSlot = item.tooltipContext != ContextID.GuideItem;
                if (Guide.AreSame(items[slot], item)) {
                    if (clearOtherSlot) SearchPrevious(items, slot, true);
                    slot = 1 - slot;
                } else if (Guide.AreSame(items[1 - slot], item) && clearOtherSlot) SearchPrevious(items, 1 - slot, true);
                items = Guide.GuideItems;
            }
            ItemSlot.LeftClick(items, ContextID.GuideItem, slot);
        }
        Guide.GuideItems = items;

        Main.mouseItem = mouse;
        Main.cursorOverride = cursor;
        (Main.mouseLeft, Main.mouseLeftRelease) = (left, rel);
        Recipe.FindRecipes();
    }
    private static void SearchPrevious(Item[] inv, int slot, bool allowAir = false) {
        void MoveItem(Item item) {
            if (!Configs.QuickSearch.Recipes) {
                int i = Main.LocalPlayer.FindItem(item.type);
                (Item mouse, Main.mouseItem) = (Main.mouseItem, new());
                (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);
                (bool left, Main.mouseLeft, bool rel, Main.mouseLeftRelease) = (Main.mouseLeft, true, Main.mouseLeftRelease, true);
                Item[] items = Guide.GuideItems;
                if (i != -1) ItemSlot.LeftClick(Main.LocalPlayer.inventory, ContextID.InventoryItem, i);
                ItemSlot.LeftClick(items, ContextID.GuideItem, slot);
                if (i != -1) ItemSlot.LeftClick(Main.LocalPlayer.inventory, ContextID.InventoryItem, i);
                Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
                Guide.GuideItems = items;
                Main.mouseItem = mouse;
                Main.cursorOverride = cursor;
                (Main.mouseLeft, Main.mouseLeftRelease) = (left, rel);
            } else {
                Search(item, slot);
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        if (Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && _guideHistory[slot].Count > 0) {
            Item item;
            do {
                item = _guideHistory[slot][^1];
                _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
            } while (_guideHistory[slot].Count > 0 && (!allowAir && item.IsAir || Guide.AreSame(item, inv[slot])));
            int count = _guideHistory[slot].Count;
            MoveItem(item);
            if (_guideHistory[slot].Count > count) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        } else if (!inv[slot].IsAir) {
            MoveItem(new());
            if (_guideHistory[slot].Count > 0) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        }
    }

    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Configs.QuickSearch.Recipes || context != ContextID.GuideItem) return false;
        if (!inv[slot].IsAir && (Main.mouseItem.IsAir || ItemSlot.ShiftInUse || ItemSlot.ControlInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        return true;
    }

    public static void UpdateGuide() {
        if (Configs.QuickSearch.Drops && (Main.guideItem.stack > 1 || Main.guideItem.prefix != 0)) {
            (Item item, Main.guideItem) = (Main.guideItem, new(Main.guideItem.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
        if (Configs.QuickSearch.Drops && (Guide.guideTile.stack > 1 || Guide.guideTile.prefix != 0)) {
            (Item item, Guide.guideTile) = (Guide.guideTile, new(Guide.guideTile.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
    }

    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (context != ContextID.GuideItem || !(Main.mouseLeft && Main.mouseLeftRelease)) {
            orig(inv, context, slot);
            return;
        }

        if (Configs.QuickSearch.RightClick && Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && !Guide.AreSame(Main.mouseItem, inv[slot])) _guideHistory[slot].Add(inv[slot].Clone());

        if (!Configs.QuickSearch.Recipes) {
            orig(inv, context, slot);
            return;
        }

        (Item mouse, int cursor) = (Main.mouseItem, Main.cursorOverride);
        inv[slot].TurnToAir();
        if (Main.cursorOverride > CursorOverrideID.DefaultCursor) {
            SoundEngine.PlaySound(SoundID.Grab);
            return;
        } else {
            Main.mouseItem = Main.mouseItem.Clone();
            if (!Main.mouseItem.IsAir) {
                Main.mouseItem.stack = 1;
                inv[slot].TurnToAir();
            }
        }

        orig(inv, context, slot);

        (Main.mouseItem, Main.cursorOverride) = (mouse, cursor);
    }

    private static void HookRightClickHistory(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.QuickSearch.RightClick || context != ContextID.GuideItem || !Main.mouseRight) {
            orig(inv, context, slot);
            return;
        }
        if (!Main.mouseRightRelease) return;
        SearchPrevious(inv, slot);
        inv[0] = Main.guideItem;
        if (inv.Length > 1) inv[1] = Guide.guideTile;
    }

    private static void HookDropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if (!Configs.QuickSearch.Recipes) {
            orig(self);
            Guide.dropItemCheck(self);
            return;
        }
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = true;
        orig(self);
        Guide.dropItemCheck(self);
        Main.InGuideCraftMenu = old;
    }

    private static readonly List<Item>[] _guideHistory = [[], []];
}