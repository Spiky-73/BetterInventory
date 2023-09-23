using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch;

public sealed class SearchItem : ILoadable {

    public static bool Enabled => BetterGuide.Enabled || Bestiary.Enabled;
    private static bool _lastGuideEnabled = false;


    public void Load(Mod mod) {
        Keybind = KeybindLoader.RegisterKeybind(mod, "SearchItem", Microsoft.Xna.Framework.Input.Keys.N);
        On_Main.DrawCursor += HookRedirectCursor;
        On_Main.DrawThickCursor += HookRedirectThickCursor;
        On_Main.DrawInterface_36_Cursor += HookDrawCursor;
        On_Main.DrawInterface += HookClickOverrideInterface;

        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;

        On_Player.dropItemCheck += OndropItems;
        On_Player.SaveTemporaryItemSlotContents += HookSaveTemporaryItemSlotContents;

        On_UIBestiaryTest.Recalculate += HookDelaySearch;
        _lastGuideEnabled = BetterGuide.Enabled;
    }


    public void Unload() {}

    private void HookRedirectCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart) {
        if(Enabled && _redir) Reflection.Main.DrawInterface_36_Cursor.Invoke();
        else orig(bonus, smart);
    }

    private Vector2 HookRedirectThickCursor(On_Main.orig_DrawThickCursor orig, bool smart) {
        if (Enabled && _redir) {
            Reflection.Main.DrawInterface_36_Cursor.Invoke();
            return Vector2.Zero;
        } else return orig(smart);
    }


    private static bool _redir;

    private static void HookDrawCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        _redir = false;
        if (Enabled && Keybind.Current && !Main.HoverItem.IsAir) {
            _allowClick = true;
            Main.cursorOverride = CursorOverrideID.Magnifiers;
        }
        orig();
        _redir = true;
    }
    private void HookClickOverrideInterface(On_Main.orig_DrawInterface orig, Main self, GameTime time) {
        bool interceptClicks = Enabled && Keybind.Current;
        bool left; bool right;
        if (interceptClicks) {
            (left, right) = (Main.mouseLeft, Main.mouseRight);
            (Main.mouseLeft, Main.mouseRight) = (false, false);
        } else (left, right) = (false, false);

        orig(self, time);
        if (interceptClicks) {
            bool forcedLeft = false;
            bool forcedRight = false;
            if(Main.mouseMiddle && Main.mouseMiddleRelease) {
                if (Main.InGameUI.CurrentState == Main.BestiaryUI) forcedRight = true;
                else forcedLeft = true;
                _allowClick = true;
            }
            if (_allowClick) {
                if (BetterGuide.Enabled && (forcedLeft || left && Main.mouseLeftRelease)) {
                    s_searchItemTimer = 15;
                    bool? rec = Main.HoverItem.type == Main.guideItem.type ? Main.recBigList : null;
                    SetGuideItem(Main.HoverItem.type);
                    ToggleRecipeList(true);
                    if (rec.HasValue && rec != Main.recBigList) SoundEngine.PlaySound(SoundID.Grab);
                } else if (Bestiary.Enabled && (forcedRight || right && Main.mouseRightRelease)) {
                    bool delay = Main.InGameUI.CurrentState != Main.BestiaryUI;
                    ToggleBestiary(true);
                    SetBestiaryItem(Main.HoverItem.type, delay);
                    s_searchItemTimer = 15;
                }
            }
            (Main.mouseLeft, Main.mouseRight) = (left, right);
            _allowClick = false;
            Main.cursorOverride = -1;
        }

    }
    public static void ProcessSearchTap() {
        if (!Enabled && !Bestiary.Enabled) return;
        s_searchItemTimer++;
        if (Keybind.JustReleased) {
            if (s_searchItemTimer <= 10) {
                if (Main.InGameUI.CurrentState == Main.BestiaryUI) {
                    ToggleBestiary();
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    s_searchItemTaps = -1;
                } else {
                    if (s_searchItemTaps % 2 == 0) {
                        ToggleRecipeList();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    } else {
                        ToggleRecipeList(false);
                        ToggleBestiary();
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


    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled) {
            orig(inv, context, slot);
            return;
        }
        if (Keybind.Current) return;
        if (context == ItemSlot.Context.GuideItem && ((!Main.guideItem.IsAir && Main.mouseItem.IsAir) || ItemSlot.ShiftInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        else if (Main.InGuideCraftMenu && ItemSlot.ShiftInUse && !inv[slot].IsAir) Main.cursorOverride = CursorOverrideID.Magnifiers;
        else orig(inv, context, slot);
    }
    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!BetterGuide.Enabled) return orig(inv, context, slot);
        if (Keybind.Current) return true;

        if (context == ItemSlot.Context.GuideItem) {
            if (Main.cursorOverride == CursorOverrideID.TrashCan) {
                SetGuideItem(ItemID.None);
            } else {
                SetGuideItem(Main.mouseItem.type);
                ToggleRecipeList(true);
                Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            }
            return true;
        }
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.Magnifiers) {
            SetGuideItem(inv[slot].type);
            ToggleRecipeList(true);
            return true;
        }
        return orig(inv, context, slot);
    }
    private static void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (Keybind.Current) return;
        if (BetterGuide.Enabled && context == ItemSlot.Context.GuideItem && Main.mouseRight && Main.mouseRightRelease) {
            SetGuideItem(ItemID.None);
            return;
        }
        orig(inv, context, slot);
    }

    private static void OndropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if (!BetterGuide.Enabled) {
            orig(self);
            return;
        }
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = true;
        orig(self);
        Main.InGuideCraftMenu = old;
    }
    private static void HookSaveTemporaryItemSlotContents(On_Player.orig_SaveTemporaryItemSlotContents orig, Player self, BinaryWriter writer) {
        if (!BetterGuide.Enabled || Main.guideItem.IsAir) {
            orig(self, writer);
            return;
        }
        Main.guideItem.stack = 0;
        orig(self, writer);
        Main.guideItem.stack = 1;
    }
    public static void UpdateMouseItem() {
        if(_lastGuideEnabled == BetterGuide.Enabled) return;
        if (!BetterGuide.Enabled) Main.guideItem.TurnToAir();
        else if (!Main.guideItem.IsAir) {
            Main.LocalPlayer.GetDropItem(ref Main.guideItem);
            Main.guideItem = new(Main.guideItem.type);
        }
        _lastGuideEnabled = BetterGuide.Enabled;
        Recipe.FindRecipes();
    }


    public static void ToggleRecipeList(bool? enabled = null) {
        if (Main.playerInventory && Main.recBigList) {
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
                if (!Main.InGuideCraftMenu) Main.LocalPlayer.SetTalkNPC(-1);
            }
            Main.recBigList = Main.numAvailableRecipes > 0;
        }
    }
    public static void SetGuideItem(int type) {
        if (type != Main.guideItem.type) {
            Main.guideItem.SetDefaults(type);
            Recipe.FindRecipes(false);
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }


    private static void HookDelaySearch(On_UIBestiaryTest.orig_Recalculate orig, UIBestiaryTest self) {
        orig(self);
        if (s_bestiaryDelayedType == ItemID.None) return;
        SetBestiaryItem(s_bestiaryDelayedType);
        s_bestiaryDelayedType = ItemID.None;
    }
    public static void ToggleBestiary(bool? enabled = null) {
        if (Main.InGameUI.CurrentState == Main.BestiaryUI) {
            if (enabled == true) return;
            IngameFancyUI.Close();
        } else {
            if (enabled == false) return;
            Main.LocalPlayer.SetTalkNPC(-1, false);
            Main.npcChatCornerItem = 0;
            Main.npcChatText = "";
            Main.mouseLeftRelease = false;
            IngameFancyUI.OpenUIState(Main.BestiaryUI);
            Main.BestiaryUI.OnOpenPage();
        }
    }
    public static void SetBestiaryItem(int type, bool delayed = false) {
        if (delayed) {
            s_bestiaryDelayedType = type;
            return;
        }
        static void PlayNoise(string content) => SoundEngine.PlaySound(SoundID.Grab);
        UISearchBar searchBar = Reflection.UIBestiaryTest._searchBar.GetValue(Main.BestiaryUI);
        BestiaryEntry? oldEntry = Reflection.UIBestiaryTest._selectedEntryButton.GetValue(Main.BestiaryUI)?.Entry;
        searchBar.OnContentsChanged += PlayNoise;
        searchBar.SetContents(Lang.GetItemNameValue(type), true);
        if (searchBar.IsWritingText) searchBar.ToggleTakingText();
        searchBar.OnContentsChanged -= PlayNoise;
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

    private static int s_bestiaryDelayedType;

    public static ModKeybind Keybind { get; private set; } = null!;
    private static bool _allowClick = false;
    private static int s_searchItemTimer = 0, s_searchItemTaps = 0;
} 