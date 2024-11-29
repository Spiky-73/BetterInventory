using System.Collections.Generic;
using BetterInventory.Crafting.UI;
using BetterInventory.ItemSearch;
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

public sealed class RecipeList : ModEntityCatalogue {

    public static RecipeList Instance = null!;

    public override bool Enabled => base.Enabled && !Configs.UnloadedItemSearch.Value.recipeList;

    public override void Load() {
        Keybind = KeybindLoader.RegisterKeybind(Mod, Name, "Mouse1");

        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClickHistory;
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;

        On_Player.dropItemCheck += HookDropItems;
        IL_Main.DrawInventory += il => {
            if (!il.ApplyTo(ILForceGuideDisplay, Enabled)) Configs.UnloadedItemSearch.Value.recipeList = true;
        };
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
            if (slot == -1) slot = Configs.BetterGuide.GuideTile && GuideGuideTile.IsCraftingStation(item) ? 1 : 0;
            if (ItemSlot.PickItemMovementAction(items, ContextID.GuideItem, 1 - slot, item) != -1) {
                bool clearOtherSlot = item.tooltipContext != ContextID.GuideItem;
                if (PlaceholderHelper.AreSame(items[slot], item)) {
                    if (clearOtherSlot) SearchPrevious(items, slot, true);
                    slot = 1 - slot;
                } else if (PlaceholderHelper.AreSame(items[1 - slot], item) && clearOtherSlot) SearchPrevious(items, 1 - slot, true);
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
            Search(item, slot);
            SoundEngine.PlaySound(SoundID.Grab);
        }

        if (Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && _guideHistory[slot].Count > 0) {
            Item item;
            do {
                item = _guideHistory[slot][^1];
                _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
            } while (_guideHistory[slot].Count > 0 && (!allowAir && item.IsAir || PlaceholderHelper.AreSame(item, inv[slot])));
            int count = _guideHistory[slot].Count;
            MoveItem(item);
            if (_guideHistory[slot].Count > count) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        } else if (!inv[slot].IsAir) {
            MoveItem(new());
            if (_guideHistory[slot].Count > 0) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        }
    }

    public static void UpdateGuide() {
        if (Bestiary.Instance.Enabled && (Main.guideItem.stack > 1 || Main.guideItem.prefix != 0)) {
            (Item item, Main.guideItem) = (Main.guideItem, new(Main.guideItem.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
        if (Bestiary.Instance.Enabled && (GuideGuideTile.guideTile.stack > 1 || GuideGuideTile.guideTile.prefix != 0)) {
            (Item item, GuideGuideTile.guideTile) = (GuideGuideTile.guideTile, new(GuideGuideTile.guideTile.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
    }

    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Instance.Enabled || context != ContextID.GuideItem || !(Main.mouseLeft && Main.mouseLeftRelease)) {
            orig(inv, context, slot);
            return;
        }

        if (Configs.QuickSearch.RightClick && Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && !PlaceholderHelper.AreSame(Main.mouseItem, inv[slot])) _guideHistory[slot].Add(inv[slot].Clone());

        // Moves a fake item instead of the real one
        (Item mouse, Main.mouseItem) = (Main.mouseItem, Main.mouseItem.Clone());
        Main.mouseItem.stack = 1;
        orig(inv, context, slot);
        Main.mouseItem = mouse;
    }

    private static void HookRightClickHistory(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Instance.Enabled || !Configs.QuickSearch.RightClick || context != ContextID.GuideItem || !Main.mouseRight) {
            orig(inv, context, slot);
            return;
        }
        if (!Main.mouseRightRelease) return;
        SearchPrevious(inv, slot);
        inv[0] = Main.guideItem;
        if (inv.Length > 1) inv[1] = GuideGuideTile.guideTile;
    }

    private static void HookDropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if (!Instance.Enabled) {
            orig(self);
            GuideGuideTile.dropGuideTileCheck(self);
            return;
        }
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = true;
        orig(self);
        GuideGuideTile.dropGuideTileCheck(self);
        Main.InGuideCraftMenu = old;
    }

    internal static void OnRecipeUIInit(RecipeUI element) {
        var searchBar = element.searchBar;
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