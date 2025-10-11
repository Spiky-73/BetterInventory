using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BetterInventory.ItemSearch;
using BetterInventory.ItemSearch.BetterGuide;
using MonoMod.Cil;
using SpikysLib;
using SpikysLib.IL;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.Default.Catalogues;

public sealed class RecipeListFakeItemContext : IFakeItemContext {
    public bool IsFake(Item item) => true;

    public bool IsHovered(Item[] inv, int context, int slot) => RecipeList.Instance.Enabled && context == ContextID.GuideItem;

    public bool WouldMoveToContext(Item[] inv, int context, int slot, [MaybeNullWhen(false)] out Item destination) {
        destination = null;
        if (!RecipeList.Instance.Enabled || !Main.InGuideCraftMenu || Main.cursorOverride != CursorOverrideID.InventoryToChest) return false;
        destination = GuideTilePlayer.GetGuideContextDestination(inv[slot], out _);
        return true;
    }
}

public sealed class RecipeList : ModEntityCatalogue {

    public static RecipeList Instance = null!;

    public override bool Enabled => base.Enabled && !Configs.UnloadedItemSearch.Value.recipeList;

    public override void Load() {
        Keybind = KeybindLoader.RegisterKeybind(Mod, Name, "Mouse1");

        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClickHistory;

        IL_Main.DrawInventory += il => {
            if (!il.ApplyTo(ILForceGuideDisplay, Enabled)) Configs.UnloadedItemSearch.Value.recipeList = true;
        };

        PlaceholderItem.AddFakeItemContext(new RecipeListFakeItemContext());
    }

    public override void Unload() {
        _guideHistory[0].Clear();
        _guideHistory[1].Clear();
    }

    private static void ILForceGuideDisplay(ILContext il) {
        ILCursor cursor = new(il);

        // if(Main.InReforgeMenu){
        //     ...
        // } else if(Main.InGuideCraftMenu) {
        //     if(<closeGuideUI>) ...
        //     else {
        ILLabel endGuide = null!;
        cursor.GotoNext(i => i.SaferMatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(MoveType.After, i => i.MatchBr(out endGuide!));

        //         ++ guide:
        ILLabel guide = cursor.DefineLabel();
        cursor.MarkLabel(guide);

        cursor.GotoLabel(endGuide, MoveType.Before);

        //         ++ if(<alternateGuideDraw>) goto recipe;
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitDelegate(() => Instance.Enabled && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(recipe);
        cursor.MarkLabel(recipe); // Here in case of exception
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoRecipeDraw();
        
        //     ++ if(<alternateGuideDraw>) goto guide;
        cursor.EmitDelegate(() => Instance.Enabled && !Main.InGuideCraftMenu);
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

    public override void Search(Item item) {
        Guid guid = item.UniqueId();
        searchedFromGuide = guid == Main.guideItem.UniqueId() || guid == GuideTile.guideTile.UniqueId();
        (bool inGuide, Main.InGuideCraftMenu) = (Main.InGuideCraftMenu, true);
        (int cursor, Main.cursorOverride) = (Main.cursorOverride, CursorOverrideID.InventoryToChest);
        (bool left, Main.mouseLeft, bool rel, Main.mouseLeftRelease) = (Main.mouseLeft, true, Main.mouseLeftRelease, true);
        ItemSlot.LeftClick([item], 0);
        Main.InGuideCraftMenu = inGuide;
        Main.cursorOverride = cursor;
        (Main.mouseLeft, Main.mouseLeftRelease) = (left, rel);
    }
    internal static bool searchedFromGuide;

    public static void OnGuideSlotChange(Item item, int slot) {
        if (Configs.QuickSearch.Value.rightClick != Configs.RightClickAction.SearchPrevious) return;
        if(_guideHistory[slot].Count == 0 || !PlaceholderHelper.AreSame(item, _guideHistory[slot][^1])) _guideHistory[slot].Add(item.Clone());
    }
    public static void SearchPrevious(Item[] inv, int context, int slot, bool allowAir = false) {
        Item? item = null;
        if(Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && _guideHistory[slot].Count > 0) {
            do {
                if(_guideHistory[slot].Count == 0) {
                    item = null;
                    break;
                }
                item = _guideHistory[slot][^1];
                _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
            } while (!allowAir && item.IsAir || PlaceholderHelper.AreSame(item, inv[slot]));
        }

        if (item is null || item.IsAir) {
            inv[slot].TurnToAir();
            SoundEngine.PlaySound(SoundID.Grab);
            OnGuideSlotChange(item ?? new(), slot);
        } else {
            (int cursor, Main.cursorOverride) = (Main.cursorOverride, CursorOverrideID.DefaultCursor);
            (bool left, Main.mouseLeft, bool rel, Main.mouseLeftRelease) = (Main.mouseLeft, true, Main.mouseLeftRelease, true);
            (Item mouse, Main.mouseItem) = (Main.mouseItem, item);
            ItemSlot.LeftClick(inv, context, slot);
            Main.cursorOverride = cursor;
            (Main.mouseLeft, Main.mouseLeftRelease) = (left, rel);
            Main.mouseItem = mouse;
        }

        // Updated guideItem/Tile before Finding the recipes as ItemSlot.LeftClick is called with ref Main.guideItem and has only been updated in inv, no really.
        if (slot == 0) Main.guideItem = inv[slot];
        else GuideTile.guideTile = inv[slot];

        Recipe.FindRecipes();
    }

    public static void UpdateGuide() {
        if (Bestiary.Instance.Enabled && (Main.guideItem.stack > 1 || Main.guideItem.prefix != 0)) {
            (Item item, Main.guideItem) = (Main.guideItem, new(Main.guideItem.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
        if (Bestiary.Instance.Enabled && (GuideTile.guideTile.stack > 1 || GuideTile.guideTile.prefix != 0)) {
            (Item item, GuideTile.guideTile) = (GuideTile.guideTile, new(GuideTile.guideTile.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
    }

    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Instance.Enabled || !(Main.mouseLeft && Main.mouseLeftRelease)) {
            orig(inv, context, slot);
            return;
        }

        if(Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) {
            Item item = inv[slot].Clone();
            item.GetGlobalItem<ItemGuid>().UniqueId = Guid.NewGuid();
            item.stack = 1;
            GuideTilePlayer.GetGuideContextDestination(item, out var guideSlot);
            orig([item], context, 0);
            OnGuideSlotChange(item, guideSlot);
            return;
        }

        if(context == ContextID.GuideItem && Main.cursorOverride <= CursorOverrideID.DefaultCursor && !Main.mouseItem.IsAir && ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == 0) {
            (Item mouse, Main.mouseItem) = (Main.mouseItem, Main.mouseItem.Clone());
            Main.mouseItem.GetGlobalItem<ItemGuid>().UniqueId = Guid.NewGuid();
            Main.mouseItem.stack = 1;
            orig(inv, context, slot);
            OnGuideSlotChange(inv[slot], slot);
            Main.mouseItem = mouse;
            return;
        }
        
        orig(inv, context, slot);
    }

    private static void HookRightClickHistory(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Instance.Enabled || !Configs.QuickSearch.RightClick || context != ContextID.GuideItem || !Main.mouseRight) {
            orig(inv, context, slot);
            return;
        }
        if (!Main.mouseRightRelease) return;
        SearchPrevious(inv, context, slot);
    }

    internal static void OnSearchBarInit(UISearchBar searchBar) {
        searchBar.Parent.OnRightClick += (_, _) => {
            if (!Instance.Enabled || !Configs.QuickSearch.RightClick) return;
            int count = 0;
            if (Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && _searchHistory.Count != 0) {
                string text = _searchHistory[^1];
                _searchHistory.RemoveAt(_searchHistory.Count - 1);
                count = _searchHistory.Count;
                searchBar.SetContents(text);
                SoundEngine.PlaySound(SoundID.Grab);
            } else if (searchBar.HasContents) {
                searchBar.SetContents(null!);
                SoundEngine.PlaySound(SoundID.Grab);
            }
            if (_searchHistory.Count > count) _searchHistory.RemoveAt(_searchHistory.Count - 1);
        };
        searchBar.OnStartTakingInput += () => {
            if (!Instance.Enabled || Configs.QuickSearch.Value.rightClick != Configs.RightClickAction.SearchPrevious) return;
            string? text = Reflection.UISearchBar.actualContents.GetValue(searchBar);
            if (text is null || text.Length == 0 || (_searchHistory.Count > 0 && _searchHistory[^1] == text)) return;
            _searchHistory.Add(text);
        };
    }

    public static void HookSearchRecipe_Cancel(UISearchBar searchBar) {
        if (Instance.Enabled && Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && searchBar.HasContents) _searchHistory.Add(Reflection.UISearchBar.actualContents.GetValue(searchBar));
    }

    private static readonly List<Item>[] _guideHistory = [[], []];
    private static readonly List<string> _searchHistory = [];
}