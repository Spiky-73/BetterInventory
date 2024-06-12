using System.Collections.Generic;
using BetterInventory.ItemSearch;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Default.SearchProviders;

public sealed class Bestiary : SearchProvider {

    public override void Load() {
        Keybind = KeybindLoader.RegisterKeybind(Mod, Name, "Mouse2");

        On_UIBestiaryTest.Recalculate += HookDelaySearch;
        On_UIBestiaryTest.searchCancelButton_OnClick += HookCancelSearch;
    }

    public override void Unload() {
        _npcSearchBar = null!;
        _npcHistory.Clear();
    }

    public static void HooksBestiaryUI() {
        _npcSearchBar = Reflection.UIBestiaryTest._searchBar.GetValue(Main.BestiaryUI);
        _npcSearchBar.Parent.OnRightClick += (_, _) => {
            if (!Configs.QuickSearch.RightClick) return;
            if (Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && _npcHistory.Count != 0) { // BUG nevers ending cycle undo
                string text = _npcHistory[^1];
                _npcHistory.RemoveAt(_npcHistory.Count - 1);
                int count = _npcHistory.Count;
                Search(text);
                if (count != _npcHistory.Count) _npcHistory.RemoveAt(_npcHistory.Count - 1);
            } else if (_npcSearchBar.HasContents) Search(null!);
        };
        _npcSearchBar.OnStartTakingInput += () => {
            if (!Configs.QuickSearch.RightClick || Configs.QuickSearch.Value.rightClick != Configs.RightClickAction.SearchPrevious) return;
            string? text = Reflection.UISearchBar.actualContents.GetValue(_npcSearchBar);
            if (text is null || text.Length == 0 || (_npcHistory.Count > 0 && _npcHistory[^1] == text)) return;
            _npcHistory.Add(text);
        };
    }
    
    private static void HookDelaySearch(On_UIBestiaryTest.orig_Recalculate orig, UIBestiaryTest self) {
        orig(self);
        if (s_bestiaryDelayed is null) return;
        Search(s_bestiaryDelayed);
        s_bestiaryDelayed = null;
    }
    private static void HookCancelSearch(On_UIBestiaryTest.orig_searchCancelButton_OnClick orig, UIBestiaryTest self, UIMouseEvent evt, UIElement listeningElement) {
        if (Configs.QuickSearch.RightClick && Configs.QuickSearch.Value.rightClick == Configs.RightClickAction.SearchPrevious && _npcSearchBar.HasContents) _npcHistory.Add(Reflection.UISearchBar.actualContents.GetValue(_npcSearchBar));
        orig(self, evt, listeningElement);
    }

    public override bool Visible => Main.InGameUI.CurrentState == Main.BestiaryUI;
    public override void Toggle(bool? enabled) {
        if (Visible) {
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

    public override void Search(Item item) => Search(item.Name, Main.InGameUI.CurrentState != Main.BestiaryUI);
    public static void Search(string text, bool delayed = false) {
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

    public override int ComparePositionTo(SearchProvider other) => other is RecipeList ? 1 : 0;

    private static UISearchBar _npcSearchBar = null!;
    private static string? s_bestiaryDelayed;
    private static readonly List<string> _npcHistory = new();
}