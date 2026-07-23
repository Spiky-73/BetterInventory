using System;
using System.Collections.Generic;
using System.Linq;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using Terraria.UI;
using SpikysLib.IL;
using SpikysLib;

namespace BetterInventory.BetterBestiary;


public sealed class UnknownNPCs : ILoadable {

    public void Load(Mod mod) {
        On_UIBestiaryEntryButton.ctor += HookDarkenEntryButton;
        On_UIBestiaryEntryInfoPage.AddInfoToList += HookDarkenEntryPage;
        On_UIBestiaryFilteringOptionsGrid.UpdateButtonSelections += HookDarkenFilters;

        On_UIBestiaryTest.FilterEntries += HookBestiaryFilterRemoveHiddenEntries;

        IL_UIBestiaryEntryIcon.Update += il => il.TryEdit(ILIconUpdateFakeUnlock, ref UnloadedBetterBestiaryConfig.Instance.unknownNPCs);
        IL_UIBestiaryEntryIcon.DrawSelf += il => il.TryEdit(ILIconDrawFakeUnlock, ref UnloadedBetterBestiaryConfig.Instance.unknownNPCs);
        IL_UIBestiaryEntryInfoPage.AddInfoToList += il => il.TryEdit(IlEntryPageFakeUnlock, ref UnloadedBetterBestiaryConfig.Instance.unknownNPCs);
        IL_UIBestiaryFilteringOptionsGrid.UpdateAvailability += il => il.TryEdit(ILFakeUnlockFilters, ref UnloadedBetterBestiaryConfig.Instance.unknownNPCs);
    }

    public void Unload() { }

    private static void ILIconUpdateFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // this._collectionInfo = this._entry.UIInfoProvider.GetEntryUICollectionInfo();
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt((IBestiaryUICollectionInfoProvider i) => i.GetEntryUICollectionInfo));

        // ++ <fakeUnlock> 
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (BetterBestiaryConfig.UnknownNPCs && UnknownNPCsConfig.Instance.unknownDisplay == UnknownDisplay.Known && info.UnlockState < BestiaryEntryUnlockState.CanShowPortraitOnly_1) info.UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1;
            return info;
        });

        // ...
    }
    private static void ILIconDrawFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // bool unlockState = this._entry.Icon.GetUnlockState(this._collectionInfo);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt((IEntryIcon i) => i.GetUnlockState));

        // ++ <changeVisibleState>
        cursor.EmitDelegate((bool unlocked) => unlocked || (BetterBestiaryConfig.UnknownNPCs && UnknownNPCsConfig.Instance.unknownDisplay == UnknownDisplay.Known));
    }
    private static void HookDarkenEntryButton(On_UIBestiaryEntryButton.orig_ctor orig, UIBestiaryEntryButton self, BestiaryEntry entry, bool isAPrettyPortrait) {
        orig(self, entry, isAPrettyPortrait);
        if (!BetterBestiaryConfig.UnknownNPCs || self.Entry.Icon.GetUnlockState(self.Entry.UIInfoProvider.GetEntryUICollectionInfo())) return;
        ((UIImage)self.Children.First().Children.First()).Color.ApplyRGB(IconDark);
        self._borders.Color.ApplyRGB(IconDark);
        self._bordersGlow.Color.ApplyRGB(IconDark);
    }

    private static void IlEntryPageFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // BestiaryUICollectionInfo uICollectionInfo = this.GetUICollectionInfo(entry, extraInfo);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall((UIBestiaryEntryInfoPage i) => i.GetUICollectionInfo));

        // ++ <fakeUnlock>
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (BetterBestiaryConfig.UnknownNPCs && UnknownNPCsConfig.Instance.unknownDisplay == UnknownDisplay.Known && info.UnlockState < BestiaryEntryUnlockState.CanShowPortraitOnly_1) info.UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1;
            return info;
        });
        // ...
    }
    private static void HookDarkenEntryPage(On_UIBestiaryEntryInfoPage.orig_AddInfoToList orig, UIBestiaryEntryInfoPage self, BestiaryEntry entry, ExtraBestiaryInfoPageInformation extraInfo) {
        orig(self, entry, extraInfo);
        if (!BetterBestiaryConfig.UnknownNPCs) return;
        if (s_darkPage != (entry.UIInfoProvider.GetEntryUICollectionInfo().UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0)) {
            DarkenElement(self, s_darkPage ? (1 / PageDark) : PageDark, 1);
            s_darkPage = !s_darkPage;
        }
        if (s_darkPage) DarkenElement(self._list, PageDark);
    }

    private static void ILFakeUnlockFilters(ILContext il) {
        ILCursor cursor = new(il);
        cursor.EmitDelegate(() => { s_ilSkipped = 0; });

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld((EntryFilterer<BestiaryEntry, IBestiaryEntryFilter> i) => i.AvailableFilters));
        cursor.GotoNextLoc(out int filter, i => true, 13);
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld((UIBestiaryFilteringOptionsGrid i) => i._filterAvailabilityTests));
        cursor.GotoNextLoc(out int entries, i => true, 14);
        // ...
        // for (<filter>) {
        //     ...
        //     bool b = this.GetIsFilterAvailableForEntries(bestiaryEntryFilter, entries);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall((UIBestiaryFilteringOptionsGrid i) => i.GetIsFilterAvailableForEntries));

        ILLabel? cont = null;
        cursor.FindNext(out _, i => i.MatchBr(out cont));

        //     ++ <fakeUnlock> 
        cursor.EmitLdloc(filter);
        cursor.EmitLdloc(entries);
        cursor.EmitDelegate((bool on, IBestiaryEntryFilter filter, List<BestiaryEntry> entries) => {
            s_ilOn = on;
            if (!BetterBestiaryConfig.UnknownNPCs || on || filter.ForcedDisplay.HasValue) return false;
            if (UnknownNPCsConfig.Instance.unknownDisplay == UnknownDisplay.Known) {
                s_ilOn = true;
                return false;
            }
            if (UnknownNPCsConfig.Instance.unknownDisplay == UnknownDisplay.Hidden) {
                s_ilSkipped++;
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(cont!);
        cursor.EmitDelegate(() => s_ilOn);
    }
    private static void ILFixPosition(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(i => i.MatchLdcI4(0));
        cursor.GotoNextLoc(out int index, i => i.Next.MatchBr(out _), 10);

        cursor.GotoNext(i => i.MatchStloc(out _) && i.Previous.MatchDiv()).GotoPrev(MoveType.After, i => i.MatchLdloc(index));
        cursor.EmitDelegate((int i) => i - s_ilSkipped);
        cursor.GotoNext(i => i.MatchStloc(out _) && i.Previous.MatchRem()).GotoPrev(MoveType.After, i => i.MatchLdloc(index));
        cursor.EmitDelegate((int i) => i - s_ilSkipped);

        cursor.GotoNext(MoveType.Before, i => i.MatchRet());
        cursor.EmitLdarg0();
        cursor.EmitDelegate((UIBestiaryFilteringOptionsGrid self) => {
            int widthWithSpacing = 32 + 2;
            int perRow = 12;
            int howManyRows = (int)Math.Ceiling((self._filterer.AvailableFilters.Count - s_ilSkipped) / (float)perRow);
            if (self._container.Children.Count() < perRow) {
                self._container.Width = new(self._container.Children.Count() * widthWithSpacing + 10, 0f);
                self._container.Height = new(1 * widthWithSpacing + 10, 0f);
            } else {
                self._container.Width = new(perRow * widthWithSpacing + 10, 0f);
                self._container.Height = new(howManyRows * widthWithSpacing + 10, 0f);
            }
        });

    }
    private static void HookDarkenFilters(On_UIBestiaryFilteringOptionsGrid.orig_UpdateButtonSelections orig, UIBestiaryFilteringOptionsGrid self) {
        orig(self);
        if (!BetterBestiaryConfig.UnknownNPCs) return;
        foreach (GroupOptionButton<int> filter in self._filterButtons) {
            if (filter.OptionValue < 0 || !self.GetIsFilterAvailableForEntries(self._filterer.AvailableFilters[filter.OptionValue], self._filterAvailabilityTests[filter.OptionValue])) DarkenElement(filter, IconDark);
        }
    }

    private static void HookBestiaryFilterRemoveHiddenEntries(On_UIBestiaryTest.orig_FilterEntries orig, UIBestiaryTest self) {
        orig(self);
        if (!BetterBestiaryConfig.UnknownNPCs || UnknownNPCsConfig.Instance.unknownDisplay != UnknownDisplay.Hidden) return;
        var entries = Main.BestiaryUI._workingSetEntries;
        for (int i = entries.Count - 1; i >= 0; i--) {
            if (entries[i].UIInfoProvider.GetEntryUICollectionInfo().UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0) entries.RemoveAt(i);
        }
    }

    public static void DarkenElement(UIElement element, float dark, int depth = -1) {
        if (element is UIHorizontalSeparator sep) sep.Color.ApplyRGB(dark);
        else if (element is UIBestiaryNPCEntryPortrait portrait) ((UIImage)portrait.Children.Last()).Color.ApplyRGB(dark);
        else if (element is GroupOptionButton<int> button) {
            button._color.ApplyRGB(IconDark);
        } else if (element is UIPanel panel) {
            panel.BorderColor.ApplyRGB(dark);
            panel.BackgroundColor.ApplyRGB(dark);

            if (element is UIBestiaryInfoItemLine item) {
                item.OnMouseOver += (_, _) => item.BorderColor.ApplyRGB(dark);
                item.OnMouseOut += (_, _) => item.BorderColor.ApplyRGB(dark);
            }
        }

        if (depth != 0) {
            depth--;
            if (element is UIList list) foreach (UIElement e in list) DarkenElement(e, dark, depth);
            else foreach (UIElement e in element.Children) DarkenElement(e, dark, depth);
        }
    }

    public const float PageDark = 0.7f;
    public const float IconDark = 0.5f;

    private static bool s_darkPage = false;
    private static int s_ilSkipped = 0;
    private static bool s_ilOn = false;
}