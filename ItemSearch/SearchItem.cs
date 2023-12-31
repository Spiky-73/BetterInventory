using System.Collections.Generic;
using Microsoft.Xna.Framework;
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

    public static Configs.ItemSearch Config => Configs.ItemSearch.Instance;
    public static bool Enabled => Config.searchRecipes || Bestiary.Enabled;

    public void Load(Mod mod) {
        Keybind = KeybindLoader.RegisterKeybind(mod, "SearchItem", Microsoft.Xna.Framework.Input.Keys.N);

        On_Main.DrawCursor += HookRedirectCursor;
        On_Main.DrawThickCursor += HookRedirectThickCursor;
        On_Main.DrawInterface_36_Cursor += HookDrawInterfaceCursor;
        On_Main.DrawInterface += HookClickOverrideInterface;

        On_UIBestiaryTest.Recalculate += HookDelaySearch;

        On_ItemSlot.RightClick_ItemArray_int_int += HookOverrideRightClick;

        On_Player.dropItemCheck += OndropItems;
    }

    public void Unload() {
        _npcSearchBar = null!;
        _guideHistory[0].Clear();
        _guideHistory[1].Clear();
        _npcHistory.Clear();
    }

    private static void HookRedirectCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart) {
        if(Enabled && s_redir) Reflection.Main.DrawInterface_36_Cursor.Invoke();
        else orig(bonus, smart);
    }
    private static Vector2 HookRedirectThickCursor(On_Main.orig_DrawThickCursor orig, bool smart) => Enabled && s_redir ? Vector2.Zero : orig(smart);
    private static void HookDrawInterfaceCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        s_redir = false;
        if (Enabled && Keybind.Current && !Main.HoverItem.IsAir && Guide.ForcedToolip?.Key != "Mods.BetterInventory.UI.Unknown") {
            s_allowClick = true;
            Main.cursorOverride = CursorOverrideID.Magnifiers;
        }
        orig();
        s_redir = true;
    }
    private static void HookClickOverrideInterface(On_Main.orig_DrawInterface orig, Main self, GameTime time) {
        bool interceptClicks = Enabled && Keybind.Current;
        bool left, right;
        if (interceptClicks) (left, Main.mouseLeft, right, Main.mouseRight) = (Main.mouseLeft, false, Main.mouseRight, false);
        else (left, right) = (false, false);


        orig(self, time);
        if (interceptClicks) {
            (Main.mouseLeft, Main.mouseRight) = (left, right);
            bool forcedLeft = false;
            bool forcedRight = false;
            if (Main.mouseMiddle && Main.mouseMiddleRelease) {
                if (Main.InGameUI.CurrentState == Main.BestiaryUI) forcedRight = true;
                else forcedLeft = true;
                s_allowClick = true;
            }
            if (s_allowClick) {
                if (Config.searchRecipes && (forcedLeft || left && Main.mouseLeftRelease)) {
                    Guide.ToggleRecipeList(true);
                    SetGuideItem(forcedLeft ? new() : Main.HoverItem);
                    s_searchItemTimer = 15;
                } else if (Config.searchDrops && (forcedRight || right && Main.mouseRightRelease)) {
                    Bestiary.ToggleBestiary(true);
                    SetBestiaryText((forcedRight ? new() : Main.HoverItem).Name, Main.InGameUI.CurrentState != Main.BestiaryUI);
                    s_searchItemTimer = 15;
                }
            }
            s_allowClick = false;
            Main.cursorOverride = -1;
        }
        Guide.ForcedToolip = null;
    }

    public static void ProcessSearchTap() {
        s_searchItemTimer++;
        if (Keybind.JustReleased) {
            if (s_searchItemTimer <= 10) {
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

        if (Keybind.JustPressed) {
            if (s_searchItemTimer > 10) s_searchItemTaps = 0;
            s_searchItemTimer = 0;
        }
    }

    private static void HookDelaySearch(On_UIBestiaryTest.orig_Recalculate orig, UIBestiaryTest self) {
        orig(self);
        if (s_bestiaryDelayed is null) return;
        SetBestiaryText(s_bestiaryDelayed);
        s_bestiaryDelayed = null;
    }

    private static void OndropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if (!Config.searchRecipes) {
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
        Item[] items = new Item[] { Main.guideItem, Guide.guideTile };

        if (item.IsAir) {
            if (slot == -1 || slot == 0) ItemSlot.LeftClick(items, ContextID.GuideItem, 0);
            if (slot == -1 || slot == 1) ItemSlot.LeftClick(items, ContextID.GuideItem, 1);
        } else {
            if(slot == -1) slot = Guide.Config.guideTile && Guide.IsCraftingTileItem(Main.mouseItem) ? 1 : 0;
            if (Guide.GetPlaceholderType(item) == PlaceholderType.None) {
                if (items[slot].type == item.type && Guide.IsCraftingTileItem(Main.mouseItem)) {
                    SetGuideItem(new(), slot);
                    slot = 1 - slot;
                } else if (items[1 - slot].type == item.type) SetGuideItem(new(), 1 - slot);
                items = new Item[] { Main.guideItem, Guide.guideTile };
            }
            ItemSlot.LeftClick(items, ContextID.GuideItem, slot);
        }
        (Main.guideItem, Guide.guideTile) = (items[0], items[1]);

        Main.mouseItem = mouse;
        Main.cursorOverride = cursor;
        (Main.mouseLeft, Main.mouseLeftRelease) = (left, rel);
        Recipe.FindRecipes();
    }

    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if (!Config.searchRecipes || context != ContextID.GuideItem) return false;
        if (Main.mouseItem.IsAir && !inv[slot].IsAir) Main.cursorOverride = CursorOverrideID.TrashCan;
        return true;
    }
    public static bool OverrideLeftClick(Item[] inv, int context, int slot) {
        if (!Config.searchRecipes || context != ContextID.GuideItem) return false;
        if (inv[slot].IsAir && Main.mouseItem.IsAir || ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) != 0) return true;

        if (Guide.AreSame(inv[slot], Main.mouseItem)) return true;
        if (!inv[slot].IsAir && Config.searchHistory) _guideHistory[slot].Add(inv[slot].Clone());
        
        if (Guide.GetPlaceholderType(Main.mouseItem) != PlaceholderType.None) {
            inv[slot] = new(Guide.CraftingItem.type) { createTile = Main.mouseItem.createTile};
            if (Main.mouseItem.BestiaryNotes?.StartsWith(Guide.ConditionMark) == true) inv[slot].BestiaryNotes = Main.mouseItem.BestiaryNotes;
        } else inv[slot] = new(Main.mouseItem.type, 1);

        SoundEngine.PlaySound(SoundID.Grab);
        return true;
    }

    internal static void HookOverrideRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Config.searchRecipes || context != ContextID.GuideItem || !Main.mouseRight){
            orig(inv, context, slot);
            return;
        }
        if (!Main.mouseRightRelease) return;
        if (Config.searchHistory && _guideHistory[slot].Count > 0) {
            Item item = _guideHistory[slot][^1];
            _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
            int count = _guideHistory[slot].Count;
            SetGuideItem(item, slot);
            if(count != _guideHistory[slot].Count) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        }
        else if(!inv[slot].IsAir) {
            SetGuideItem(new(), slot);
            if(Config.searchHistory) _guideHistory[slot].RemoveAt(_guideHistory[slot].Count - 1);
        }
        inv[0] = Main.guideItem;
        if (inv.Length > 1) inv[1] = Guide.guideTile;
    }

    public static void PostAddRecipes() {
        _npcSearchBar = Reflection.UIBestiaryTest._searchBar.GetValue(Main.BestiaryUI);
        _npcSearchBar.Parent.OnRightClick += (_, _) => {
            if (!Config.searchDrops) return;
            if (Config.searchHistory && _npcHistory.Count != 0) {
                string text = _npcHistory[^1];
                _npcHistory.RemoveAt(_npcHistory.Count - 1);
                int count = _npcHistory.Count;
                SetBestiaryText(text);
                if (count != _npcHistory.Count) _npcHistory.RemoveAt(_npcHistory.Count - 1);
            } else if (_npcSearchBar.HasContents) {
                SetBestiaryText("");
                if (Config.searchHistory) _npcHistory.RemoveAt(_npcHistory.Count - 1);
            }
        };
        _npcSearchBar.OnStartTakingInput += () => {
            if (!Config.searchDrops || !Config.searchHistory) return;
            string? text = Reflection.UISearchBar.actualContents.GetValue(_npcSearchBar);
            if (text is null || text.Length == 0 || (_npcHistory.Count > 0 && _npcHistory[^1] == text)) return;
            _npcHistory.Add(text);
        };
    }

    public static void UpdateGuide() {
        if (Config.searchDrops && (Main.guideItem.stack > 1 || Main.guideItem.prefix != 0)) {
            (Item item, Main.guideItem) = (Main.guideItem, new(Main.guideItem.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
        if (Config.searchDrops && (Guide.guideTile.stack > 1 || Guide.guideTile.prefix != 0)) {
            (Item item, Guide.guideTile) = (Guide.guideTile, new(Guide.guideTile.type));
            Main.LocalPlayer.GetDropItem(ref item);
        }
    }


    private static string? s_bestiaryDelayed;

    public static ModKeybind Keybind { get; private set; } = null!;
    private static bool s_allowClick = false;
    private static int s_searchItemTimer = 0, s_searchItemTaps = 0;

    private static bool s_redir;

    private static UISearchBar _npcSearchBar = null!;

    private static readonly List<Item>[] _guideHistory = new List<Item>[]{new(), new()};
    private static readonly List<string> _npcHistory = new();
}