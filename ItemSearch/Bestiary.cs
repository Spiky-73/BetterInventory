using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch;


public sealed class Bestiary : ILoadable {

    public static bool Enabled => Configs.ClientConfig.Instance.searchDrops;

    public void Load(Mod mod) {
        On_UIBestiaryTest.Recalculate += HookDelaySearch;
        On_UIBestiaryEntryButton.ctor += HookFakeUnlockEntry;
        IL_UIBestiaryEntryIcon.Update += ILUIBestiaryIconFakeUnlock;
        IL_UIBestiaryEntryIcon.DrawSelf += ILDrawNPCIcon;

        IL_UIBestiaryEntryInfoPage.AddInfoToList += IlInfoPageFakeUnlock;
        On_UIBestiaryEntryInfoPage.AddInfoToList += HookDarkenPage;

        IL_Filters.BySearch.FitsFilter += ILSearchFilter;

        IL_UIBestiaryFilteringOptionsGrid.UpdateAvailability += ILFakeFilters;
        On_UIBestiaryFilteringOptionsGrid.UpdateButtonSelections += HookDarkenFilters;
        // On_UIBestiaryFilteringOptionsGrid.GetIsFilterAvailableForEntries += HookFakeFilters;
    }

    public void Unload() {}


    private void HookFakeUnlockEntry(On_UIBestiaryEntryButton.orig_ctor orig, UIBestiaryEntryButton self, BestiaryEntry entry, bool isAPrettyPortrait) {
        orig(self, entry, isAPrettyPortrait);
        if(!Enabled || self.Entry.Icon.GetUnlockState(self.Entry.UIInfoProvider.GetEntryUICollectionInfo())) return;
        ((UIImage)self.Children.First().Children.First()).Color.ApplyRGB(IconDark);
        ((UIImage)BestiaryButtonBorder.GetValue(self)!).Color.ApplyRGB(IconDark);
        ((UIImage)BestiaryButtonGlow.GetValue(self)!).Color.ApplyRGB(IconDark);
    }

    private static void ILUIBestiaryIconFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // this._collectionInfo = this._entry.UIInfoProvider.GetEntryUICollectionInfo();
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(IBestiaryUICollectionInfoProvider), nameof(IBestiaryUICollectionInfoProvider.GetEntryUICollectionInfo)));

        // ++ <fakeUnlock> 
        cursor.EmitDelegate(ForceUnlock);
        // ...
    }

    private static void ILDrawNPCIcon(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // bool unlockState = this._entry.Icon.GetUnlockState(this._collectionInfo);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(IEntryIcon), nameof(IEntryIcon.GetUnlockState)));

        // ++ <changeVisibleState>
        cursor.EmitDelegate((bool unlocked) => unlocked || Enabled);
    }
    
    private static void IlInfoPageFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // BestiaryUICollectionInfo uICollectionInfo = this.GetUICollectionInfo(entry, extraInfo);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(UIBestiaryEntryInfoPage), "GetUICollectionInfo"));

        // ++ <fakeUnlock> 
        cursor.EmitDelegate(ForceUnlock);
        // ...
    }

    public static BestiaryUICollectionInfo ForceUnlock(BestiaryUICollectionInfo info) {
        // if (Enabled && info.UnlockState < BestiaryEntryUnlockState.CanShowDropsWithoutDropRates_3) info.UnlockState = BestiaryEntryUnlockState.CanShowDropsWithoutDropRates_3;
        if (Enabled) info.UnlockState = BestiaryEntryUnlockState.CanShowDropsWithDropRates_4;
        return info;
    } 

    private static void HookDarkenPage(On_UIBestiaryEntryInfoPage.orig_AddInfoToList orig, UIBestiaryEntryInfoPage self, BestiaryEntry entry, ExtraBestiaryInfoPageInformation extraInfo) {
        orig(self, entry, extraInfo);
        if(!Enabled) return;
        bool swap = s_darkPage != (entry.UIInfoProvider.GetEntryUICollectionInfo().UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0);
        if(swap) {
            DarkenElement(self, s_darkPage ? (1 / PageDark) : PageDark, 1);
            s_darkPage = !s_darkPage;
        }
        if (s_darkPage) DarkenElement((UIList)PageListField.GetValue(self)!, PageDark);
    }

    private static void ILSearchFilter(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // BestiaryUICollectionInfo info = entry.UIInfoProvider.GetEntryUICollectionInfo();
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(IBestiaryUICollectionInfoProvider), nameof(IBestiaryUICollectionInfoProvider.GetEntryUICollectionInfo)));

        // ++ <fakeUnlock> 
        cursor.EmitDelegate(ForceUnlock);
        // ...
    }

    private void ILFakeFilters(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // for (<filter>) {
        //     ...
        //     bool b = this.GetIsFilterAvailableForEntries(bestiaryEntryFilter, entries);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(UIBestiaryFilteringOptionsGrid), "GetIsFilterAvailableForEntries"));

        //     ++ <fakeUnlock> 
        cursor.EmitLdloc(13);
        cursor.EmitLdloc(14);
        cursor.EmitDelegate((bool on, IBestiaryEntryFilter filter, List<BestiaryEntry> entries) => {
            if(filter.ForcedDisplay.HasValue) return on;
            if (Enabled) {
                for (int i = 0; i < entries.Count; i++) if (filter.FitsFilter(entries[i])) return true;
            }
            return on;
        });

    }

    private void HookDarkenFilters(On_UIBestiaryFilteringOptionsGrid.orig_UpdateButtonSelections orig, UIBestiaryFilteringOptionsGrid self) {
        orig(self);
        EntryFilterer<BestiaryEntry, IBestiaryEntryFilter> filterer = (EntryFilterer<BestiaryEntry, IBestiaryEntryFilter>)FiltererField.GetValue(self)!;
        List<GroupOptionButton<int>> filters = (List<GroupOptionButton<int>>)FiltersField.GetValue(self)!;
        for (int i = 0; i < filterer.AvailableFilters.Count; i++){
            if ((bool)FilterAvailableMethod.Invoke(self, new object[] { filterer.AvailableFilters[i], ((List<List<BestiaryEntry>>)FiltersTestsField.GetValue(self)!)[i] })!) continue;
            Color color = (Color)GroupOptionColorField.GetValue(filters[i])!;
            color.ApplyRGB(IconDark);
            GroupOptionColorField.SetValue(filters[i], color);
        }
    }

    public static void DarkenElement(UIElement element, float dark, int depth = -1){
        if (element is UIHorizontalSeparator sep) sep.Color.ApplyRGB(dark);
        else if (element is UIBestiaryNPCEntryPortrait portrait) ((UIImage)portrait.Children.Last()).Color.ApplyRGB(dark);
        else if (element is UIPanel panel) {
            panel.BorderColor.ApplyRGB(dark);
            panel.BackgroundColor.ApplyRGB(dark);
        }
        if (element is UIBestiaryInfoItemLine item) {
            item.OnMouseOver += (_, _) => item.BorderColor.ApplyRGB(dark);
            item.OnMouseOut += (_, _) => item.BorderColor.ApplyRGB(dark);
        }

        if (depth != 0) {
            depth--;
            if (element is UIList list) foreach (UIElement e in list) DarkenElement(e, dark, depth);
            else foreach(UIElement e in element.Children) DarkenElement(e, dark, depth);
        }
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
    private static void HookDelaySearch(On_UIBestiaryTest.orig_Recalculate orig, UIBestiaryTest self) {
        orig(self);
        if (s_bestiaryDelayedType == ItemID.None) return;
        SetBestiaryItem(s_bestiaryDelayedType);
        s_bestiaryDelayedType = ItemID.None;
    }

    public const float PageDark = 0.7f;
    public const float IconDark = 0.5f;

    private static int s_bestiaryDelayedType;

    private static bool s_darkPage = false;

    public static readonly FieldInfo BestiarySearchBarField = typeof(UIBestiaryTest).GetField("_searchBar", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiarySelectedEntryField = typeof(UIBestiaryTest).GetField("_selectedEntryButton", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiaryGridField = typeof(UIBestiaryTest).GetField("_entryGrid", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo SelectEntryButtonMethod = typeof(UIBestiaryTest).GetMethod("SelectEntryButton", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static readonly FieldInfo BestiaryIconEntryField = typeof(UIBestiaryEntryIcon).GetField("_entry", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiaryIconInfoField = typeof(UIBestiaryEntryIcon).GetField("_collectionInfo", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiaryButtonBorder = typeof(UIBestiaryEntryButton).GetField("_borders", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiaryButtonGlow = typeof(UIBestiaryEntryButton).GetField("_bordersGlow", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static readonly FieldInfo PageListField = typeof(UIBestiaryEntryInfoPage).GetField("_list", BindingFlags.Instance | BindingFlags.NonPublic)!;
    
    public static readonly FieldInfo FiltererField = typeof(UIBestiaryFilteringOptionsGrid).GetField("_filterer", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo FiltersField = typeof(UIBestiaryFilteringOptionsGrid).GetField("_filterButtons", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo FiltersTestsField = typeof(UIBestiaryFilteringOptionsGrid).GetField("_filterAvailabilityTests", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo FilterAvailableMethod = typeof(UIBestiaryFilteringOptionsGrid).GetMethod("GetIsFilterAvailableForEntries", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo GroupOptionColorField = typeof(GroupOptionButton<int>).GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic)!;
}