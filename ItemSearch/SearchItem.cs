using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SpikysLib.Extensions;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;

public sealed class SearchItem : ILoadable {

    public void Load(Mod mod) {
        QuickSearchKb = KeybindLoader.RegisterKeybind(mod, "QuickSearch", Microsoft.Xna.Framework.Input.Keys.N);

        On_Main.DrawCursor += HookRedirectCursor;
        On_Main.DrawThickCursor += HookRedirectThickCursor;
        On_Main.DrawInterface_36_Cursor += HookDrawInterfaceCursor;
        On_Main.DrawInterface += HookClickOverrideInterface;

        On_UIBestiaryTest.Recalculate += HookDelaySearch;
        On_UIBestiaryTest.searchCancelButton_OnClick += HookCancelSearch;

        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClickHistory;
        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;

        On_Player.dropItemCheck += HookDropItems;
    }

    public void Unload() {
        _npcSearchBar = null!;
        _guideHistory[0].Clear();
        _guideHistory[1].Clear();
        _npcHistory.Clear();
    }

    private static void HookRedirectCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart) {
        if(Configs.SearchItems.Enabled && s_redirect) Reflection.Main.DrawInterface_36_Cursor.Invoke();
        else orig(bonus, smart);
    }
    private static Vector2 HookRedirectThickCursor(On_Main.orig_DrawThickCursor orig, bool smart) => Configs.SearchItems.Enabled && s_redirect ? Vector2.Zero : orig(smart);
    private static void HookDrawInterfaceCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        s_redirect = false;
        if (Configs.SearchItems.Enabled && QuickSearchKb.Current && !Main.HoverItem.IsAir && Guide.forcedTooltip?.Key != $"{Localization.Keys.UI}.Unknown") {
            s_allowClick = true;
            Main.cursorOverride = CursorOverrideID.Magnifiers;
        }
        orig();
        s_redirect = true;
    }
    private static void HookClickOverrideInterface(On_Main.orig_DrawInterface orig, Main self, GameTime time) {
        bool interceptClicks = Configs.SearchItems.Enabled && QuickSearchKb.Current;
        bool left, right;
        if (interceptClicks) (left, Main.mouseLeft, right, Main.mouseRight) = (Main.mouseLeft, false, Main.mouseRight, false);
        else (left, right) = (false, false);


        orig(self, time);
        if (interceptClicks) {
            (Main.mouseLeft, Main.mouseRight) = (left, right);
            bool forcedLeft = false;
            bool forcedRight = false;
            if (s_allowClick) {
                if (Configs.SearchItems.Recipes && (forcedLeft || left && Main.mouseLeftRelease)) {
                    Guide.ToggleRecipeList(true);
                    SetGuideItem(forcedLeft ? new() : Main.HoverItem);
                    s_searchItemTimer = 15;
                } else if (Configs.SearchItems.Drops && (forcedRight || right && Main.mouseRightRelease)) {
                    Bestiary.ToggleBestiary(true);
                    SetBestiaryText((forcedRight ? new() : Main.HoverItem).Name, Main.InGameUI.CurrentState != Main.BestiaryUI);
                    s_searchItemTimer = 15;
                }
            }
            s_allowClick = false;
            Main.cursorOverride = -1;
        }
        Guide.forcedTooltip = null;
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
        cursor.EmitDelegate(() => Configs.SearchItems.Recipes && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(recipe);
        cursor.MarkLabel(recipe); // Here in case of exception
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStloc(124)); // int num63
        cursor.GotoPrev(MoveType.After, i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentRecipeSmall));

        //     ++ if(<alternateGuideDraw>) goto guide;
        cursor.EmitDelegate(() => Configs.SearchItems.Recipes && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(guide);

        //     ++ recipe:
        cursor.MarkLabel(recipe);
    }

    public static void ProcessSearchTap() {
        s_searchItemTimer++;
        if (QuickSearchKb.JustReleased) {
            if (s_searchItemTimer <= Configs.QuickList.Value.tap) {
                if (Main.InGameUI.CurrentState == Main.BestiaryUI) {
                    Bestiary.ToggleBestiary();
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    s_searchItemTaps = -1;
                } else {
                    if (s_searchItemTaps % 2 == 0) {
                        Guide.ToggleRecipeList();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    } else {
                        Guide.ToggleRecipeList(false);
                        Bestiary.ToggleBestiary();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                }
                s_searchItemTaps++;
            } else s_searchItemTaps = 0;
            s_searchItemTimer = 0;
        }

        if (QuickSearchKb.JustPressed) {
            if (s_searchItemTimer > Configs.QuickList.Value.delay) s_searchItemTaps = 0;
            s_searchItemTimer = 0;
        }
    }

    private static void HookDelaySearch(On_UIBestiaryTest.orig_Recalculate orig, UIBestiaryTest self) {
        orig(self);
        if (s_bestiaryDelayed is null) return;
        SetBestiaryText(s_bestiaryDelayed);
        s_bestiaryDelayed = null;
    }
    private static void HookCancelSearch(On_UIBestiaryTest.orig_searchCancelButton_OnClick orig, UIBestiaryTest self, UIMouseEvent evt, UIElement listeningElement) {
        if (Configs.SearchItems.RightClick && Configs.SearchItems.Value.rightClick == Configs.RightClickAction.SearchPrevious && _npcSearchBar.HasContents) _npcHistory.Add(Reflection.UISearchBar.actualContents.GetValue(_npcSearchBar));
        orig(self, evt, listeningElement);
    }

    private static void HookDropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if (!Configs.SearchItems.Recipes) {
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

    public static void SetBestiaryText(string text, bool delayed = false) {
        if (delayed) {
            s_bestiaryDelayed = text;
            return;
        }
        if (text == Reflection.UISearchBar.actualContents.GetValue(_npcSearchBar)) return;
        BestiaryEntry? oldEntry = Reflection.UIBestiaryTest._selectedEntryButton.GetValue(Main.BestiaryUI)?.Entry;
        if (!_npcSearchBar.IsWritingText) _npcSearchBar.ToggleTakingText();
        _npcSearchBar.SetContents(text, true);
        _npcSearchBar.ToggleTakingText();
        SoundEngine.PlaySound(SoundID.Grab);
        UIBestiaryEntryGrid grid = Reflection.UIBestiaryTest._entryGrid.GetValue(Main.BestiaryUI);
        if (oldEntry is not null) {
            foreach (UIElement element in grid.Children) {
                if (element is not UIBestiaryEntryButton button || button.Entry != oldEntry) continue;
                Reflection.UIBestiaryTest.SelectEntryButton.Invoke(Main.BestiaryUI, button);
                return;
            }
        }
        foreach (UIElement element in grid.Children) {
            if (element is not UIBestiaryEntryButton button) continue;
            Reflection.UIBestiaryTest.SelectEntryButton.Invoke(Main.BestiaryUI, button);
            break;
        }
    }

    public static void SetGuideItem(Item item, int slot = -1) {
        (Item mouse, Main.mouseItem) = (Main.mouseItem, item);
        (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);
        (bool left, Main.mouseLeft, bool rel, Main.mouseLeftRelease) = (Main.mouseLeft, true, Main.mouseLeftRelease, true);
        Item[] items = Guide.GuideItems;

        if (item.IsAir) {
            if (slot == -1 || slot == 0) ItemSlot.LeftClick(items, ContextID.GuideItem, 0);
            if (slot == -1 || slot == 1) ItemSlot.LeftClick(items, ContextID.GuideItem, 1);
        } else {
            if(slot == -1) slot = Configs.BetterGuide.Tile && Guide.IsCraftingStation(item) ? 1 : 0;
            if (Configs.BetterGuide.Tile && Guide.FitsCraftingTile(item) && Guide.GetPlaceholderType(item) == PlaceholderType.None) {
                bool clearOtherSlot = item.tooltipContext != ContextID.GuideItem;
                if (Guide.AreSame(items[slot], item)) {
                    if(clearOtherSlot) SetPreviousGuideItem(items, slot, true);
                    slot = 1 - slot;
                } else if (Guide.AreSame(items[1-slot], item) && clearOtherSlot) SetPreviousGuideItem(items, 1-slot, true);
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

    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Configs.SearchItems.Recipes || context != ContextID.GuideItem) return false;
        if (!inv[slot].IsAir && (Main.mouseItem.IsAir || ItemSlot.ShiftInUse || ItemSlot.ControlInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        return true;
    }
    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (context != ContextID.GuideItem || !(Main.mouseLeft && Main.mouseLeftRelease)) {
            orig(inv, context, slot);
            return;
        }
        
        if (Configs.SearchItems.RightClick && Configs.SearchItems.Value.rightClick == Configs.RightClickAction.SearchPrevious && !Guide.AreSame(Main.mouseItem, inv[slot])) _guideHistory[slot].Add(inv[slot].Clone());
        
        if (!Configs.SearchItems.Recipes) {
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
            if(!Main.mouseItem.IsAir) {
                Main.mouseItem.stack = 1;
                inv[slot].TurnToAir();
            }
        }

        orig(inv, context, slot);

        (Main.mouseItem, Main.cursorOverride) = (mouse, cursor);
    }

    private static void HookRightClickHistory(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Configs.SearchItems.RightClick || context != ContextID.GuideItem || !Main.mouseRight) {
            orig(inv, context, slot);
            return;
        }   
        if (!Main.mouseRightRelease) return; 
        SetPreviousGuideItem(inv, slot);
        inv[0] = Main.guideItem;
        if (inv.Length > 1) inv[1] = Guide.guideTile;
    }

    private static void SetPreviousGuideItem(Item[] inv, int slot, bool allowAir = false) {
        void MoveItem(Item item) {
            if (!Configs.SearchItems.Recipes) {
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
                SetGuideItem(item, slot);
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }
        
        if (Configs.SearchItems.Value.rightClick == Configs.RightClickAction.SearchPrevious && _guideHistory[slot].Count > 0) {
            Item item;
            do {
                item = _guideHistory[slot][^1];
                _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
            } while (_guideHistory[slot].Count > 0 && (!allowAir && item.IsAir || Guide.AreSame(item, inv[slot])));
            int count = _guideHistory[slot].Count;
            MoveItem(item);
            if (_guideHistory[slot].Count > count) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        }
        else if (!inv[slot].IsAir) {
            MoveItem(new());
            if (_guideHistory[slot].Count > 0) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        }
    }


    public static void HooksBestiaryUI() {
        _npcSearchBar = Reflection.UIBestiaryTest._searchBar.GetValue(Main.BestiaryUI);
        _npcSearchBar.Parent.OnRightClick += (_, _) => {
            if (!Configs.SearchItems.RightClick) return;
            if (Configs.SearchItems.Value.rightClick == Configs.RightClickAction.SearchPrevious && _npcHistory.Count != 0) {
                string text = _npcHistory[^1];
                _npcHistory.RemoveAt(_npcHistory.Count - 1);
                int count = _npcHistory.Count;
                SetBestiaryText(text);
                if (count != _npcHistory.Count) _npcHistory.RemoveAt(_npcHistory.Count - 1);
            } else if (_npcSearchBar.HasContents) SetBestiaryText(null!);
        };
        _npcSearchBar.OnStartTakingInput += () => {
            if (!Configs.SearchItems.RightClick || Configs.SearchItems.Value.rightClick != Configs.RightClickAction.SearchPrevious) return;
            string? text = Reflection.UISearchBar.actualContents.GetValue(_npcSearchBar);
            if (text is null || text.Length == 0 || (_npcHistory.Count > 0 && _npcHistory[^1] == text)) return;
            _npcHistory.Add(text);
        };
    }

    public static void UpdateGuide() {
        if (Configs.SearchItems.Drops && (Main.guideItem.stack > 1 || Main.guideItem.prefix != 0)) {
            (Item item, Main.guideItem) = (Main.guideItem, new(Main.guideItem.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
        if (Configs.SearchItems.Drops && (Guide.guideTile.stack > 1 || Guide.guideTile.prefix != 0)) {
            (Item item, Guide.guideTile) = (Guide.guideTile, new(Guide.guideTile.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
    }


    private static string? s_bestiaryDelayed;

    public static ModKeybind QuickSearchKb { get; private set; } = null!;
    private static bool s_allowClick = false;
    private static int s_searchItemTimer = 0, s_searchItemTaps = 0;

    private static bool s_redirect;

    private static UISearchBar _npcSearchBar = null!;

    private static readonly List<Item>[] _guideHistory = new List<Item>[]{new(), new()};
    private static readonly List<string> _npcHistory = new();
}