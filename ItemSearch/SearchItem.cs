using BetterInventory.Items;
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

    public void Unload() {}

    private void HookRedirectCursor(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart) {
        if(Enabled && s_redir) Reflection.Main.DrawInterface_36_Cursor.Invoke();
        else orig(bonus, smart);
    }
    private Vector2 HookRedirectThickCursor(On_Main.orig_DrawThickCursor orig, bool smart) => Enabled && s_redir ? Vector2.Zero : orig(smart);
    private static void HookDrawInterfaceCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        s_redir = false;
        if (Enabled && Keybind.Current && !Main.HoverItem.IsAir && Guide.ForcedToolip is null) {
            s_allowClick = true;
            Main.cursorOverride = CursorOverrideID.Magnifiers;
        }
        orig();
        s_redir = true;
    }
    private void HookClickOverrideInterface(On_Main.orig_DrawInterface orig, Main self, GameTime time) {
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
                    SetGuideItem(forcedLeft ? new() : Main.HoverItem);
                    s_searchItemTimer = 15;
                } else if (Config.searchDrops && (forcedRight || right && Main.mouseRightRelease)) {
                    Bestiary.ToggleBestiary(true);
                    SetBestiaryItem(forcedRight ? new() : Main.HoverItem, Main.InGameUI.CurrentState != Main.BestiaryUI);
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
        if (s_bestiaryDelayedItem is null) return;
        SetBestiaryItem(s_bestiaryDelayedItem);
        s_bestiaryDelayedItem = null;
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

    public static void SetBestiaryItem(Item item, bool delayed = false) {
        if (delayed) {
            s_bestiaryDelayedItem = item;
            return;
        }
        static void PlayNoise(string content) => SoundEngine.PlaySound(SoundID.Grab);
        UISearchBar searchBar = Reflection.UIBestiaryTest._searchBar.GetValue(Main.BestiaryUI);
        BestiaryEntry? oldEntry = Reflection.UIBestiaryTest._selectedEntryButton.GetValue(Main.BestiaryUI)?.Entry;
        searchBar.OnContentsChanged += PlayNoise;
        searchBar.SetContents(item.Name, true);
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

    public static void SetGuideItem(Item item) {
        (Item mouse, Main.mouseItem) = (Main.mouseItem, item);
        (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);
        (bool left, Main.mouseLeft, bool rel, Main.mouseLeftRelease) = (Main.mouseLeft, true, Main.mouseLeftRelease, true);

        Item[] items = new Item[] { Main.guideItem, Guide.guideTile };
        int slot = Guide.Config.guideTile && Guide.IsCraftingTileItem(Main.mouseItem) ? 1 : 0;
        if (slot == 1 && !items[slot].IsAir) {
            bool flag;
            if (item.type == CraftingItem.ID) {
                if (item.createTile == -1) flag = false;
                else if (item.createTile != -1) flag = item.createTile == items[slot].createTile;
                else flag = (item.ModItem as CraftingItem)!.condition?.Description.Key == (items[slot].ModItem as CraftingItem)?.condition?.Description.Key;
            } else flag = items[1].type == item.type;
            
            if (flag) {
                slot = 0;
                items[1].TurnToAir();
            } else if (items[0].type == item.type) items[0].TurnToAir();
        }
        ItemSlot.LeftClick(items, ContextID.GuideItem, slot);
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
        if(Main.mouseItem.type == CraftingItem.ID) {
            inv[slot] = CraftingItem.WithTile(Main.mouseItem.createTile, Main.mouseItem.placeStyle);
        } else inv[slot] = new(Main.mouseItem.type, 1);

        SoundEngine.PlaySound(SoundID.Grab);
        return true;
    }

    private void HookOverrideRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Config.searchRecipes || context != ContextID.GuideItem || !Main.mouseRight || !Main.mouseRightRelease){
            orig(inv, context, slot);
            return;
        }
        if (inv[slot].IsAir) return;
        inv[slot].TurnToAir();
        SoundEngine.PlaySound(SoundID.Grab);
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

    private static Item? s_bestiaryDelayedItem;

    public static ModKeybind Keybind { get; private set; } = null!;
    private static bool s_allowClick = false;
    private static int s_searchItemTimer = 0, s_searchItemTaps = 0;

    private static bool s_redir;
}