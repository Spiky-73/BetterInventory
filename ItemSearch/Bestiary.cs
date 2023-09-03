
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.UI;

namespace BetterInventory.ItemSearch;


public static class Bestiary {

    public static bool Enabled => Configs.ClientConfig.Instance.searchDrops;

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
            _delay = 2;
            _bestiaryDelayedType = type;
            return;
        }
        static void PlayNoise(string content) => SoundEngine.PlaySound(SoundID.Grab);
        UISearchBar searchBar = (UISearchBar)BestiarySearchBarField.GetValue(Main.BestiaryUI)!;
        BestiaryEntry? oldEntry = ((UIBestiaryEntryButton)BestiarySelectedEntryField.GetValue(Main.BestiaryUI)!)?.Entry;
        searchBar.OnContentsChanged += PlayNoise;
        searchBar.SetContents(Lang.GetItemNameValue(type), true);
        if (searchBar.IsWritingText) searchBar.ToggleTakingText();
        searchBar.OnContentsChanged -= PlayNoise;
        UIBestiaryEntryGrid grid = (UIBestiaryEntryGrid)BestiaryGridField.GetValue(Main.BestiaryUI)!;
        if (oldEntry is not null) {
            foreach (UIElement element in grid.Children) {
                if (element is not UIBestiaryEntryButton button || button.Entry != oldEntry) continue;
                SelectEntryButtonMethod.Invoke(Main.BestiaryUI, new object[] { button });
                return;
            }
        }
        foreach (UIElement element in grid.Children) {
            if (element is not UIBestiaryEntryButton button) continue;
            SelectEntryButtonMethod.Invoke(Main.BestiaryUI, new object[] { button });
            break;
        }
    }
    public static void TryDelaySetBestiaryItem() {
        _delay--;
        if (!_bestiaryDelayedType.HasValue || _delay > 0) return;
        SetBestiaryItem(_bestiaryDelayedType.Value);
        _bestiaryDelayedType = null;
    }

    private static int _delay;
    private static int? _bestiaryDelayedType;

    public static readonly FieldInfo BestiarySearchBarField = typeof(UIBestiaryTest).GetField("_searchBar", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiarySelectedEntryField = typeof(UIBestiaryTest).GetField("_selectedEntryButton", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiaryGridField = typeof(UIBestiaryTest).GetField("_entryGrid", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo SelectEntryButtonMethod = typeof(UIBestiaryTest).GetMethod("SelectEntryButton", BindingFlags.Instance | BindingFlags.NonPublic)!;
}