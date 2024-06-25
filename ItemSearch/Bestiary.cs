using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using SpikysLib.Extensions;
using SpikysLib;

namespace BetterInventory.ItemSearch;


public sealed class Bestiary : ILoadable {

    public void Load(Mod mod) {
        On_FewFromOptionsNotScaledWithLuckDropRule.ReportDroprates += HookFixDropRates;
        
        On_UIBestiaryInfoItemLine.ctor += HookShowBagContent;
        On_ItemDropBestiaryInfoElement.GetSearchString += HookSearchBagText;

        On_Filters.ByUnlockState.GetDisplayNameKey += HookCustomUnlockFilterName;
        On_Filters.ByUnlockState.FitsFilter += HookCustomUnlockFilter;

        On_UIBestiaryEntryButton.ctor += HookDarkenEntryButton;

        On_UIBestiaryEntryInfoPage.AddInfoToList += HookDarkenEntryPage;

        On_UIBestiaryFilteringOptionsGrid.UpdateButtonSelections += HookDarkenFilters;

        On_UIBestiaryTest.FilterEntries += HookBestiaryFilterRemoveHiddenEntries;

        IL_Filters.BySearch.FitsFilter += il => {
            if (!il.ApplyTo(ILSearchAddEntries, Configs.BetterBestiary.DisplayedInfo)) Configs.UnloadedItemSearch.Value.bestiaryDisplayedInfo = true;
        };
        IL_UIBestiaryEntryIcon.Update += il => {
            if(!il.ApplyTo(ILIconUpdateFakeUnlock, Configs.BetterBestiary.Unlock)) Configs.UnloadedItemSearch.Value.BestiaryUnlock = true;
        };
        IL_UIBestiaryEntryIcon.DrawSelf += il => {
            if(!il.ApplyTo(ILIconDrawFakeUnlock, Configs.BetterBestiary.UnknownDisplay)) Configs.UnloadedItemSearch.Value.bestiaryUnknown = true;
        };
        IL_UIBestiaryEntryInfoPage.AddInfoToList += il => {
            if(!il.ApplyTo(IlEntryPageFakeUnlock, Configs.BetterBestiary.Unlock)) Configs.UnloadedItemSearch.Value.BestiaryUnlock = true;
        };
        IL_UIBestiaryFilteringOptionsGrid.UpdateAvailability += il => {
            if(!il.ApplyTo(ILFakeUnlockFilters, Configs.BetterBestiary.UnknownDisplay)) Configs.UnloadedItemSearch.Value.bestiaryUnknown = true;
            if(!il.ApplyTo(ILFixPosition, Configs.BetterBestiary.UnknownDisplay)) Configs.UnloadedItemSearch.Value.bestiaryUnknown = true;
        };
    }
    public void Unload() => _bossBagSearch.Clear();

    private static void HookFixDropRates(On_FewFromOptionsNotScaledWithLuckDropRule.orig_ReportDroprates orig, FewFromOptionsNotScaledWithLuckDropRule self, List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
        if (!Configs.BetterBestiary.Enabled) {
            orig(self, drops, ratesInfo);
            return;
        }
        float personalDroprate = Math.Min(1, self.chanceNumerator / (float)self.chanceDenominator);
        float globalDroprate = personalDroprate * ratesInfo.parentDroprateChance;
        float dropRate = 1f / self.dropIds.Length * self.amount * globalDroprate;
        for (int i = 0; i < self.dropIds.Length; i++) drops.Add(new DropRateInfo(self.dropIds[i], 1, 1, dropRate, ratesInfo.conditions));
        Chains.ReportDroprates(self.ChainedRules, personalDroprate, drops, ratesInfo);
    }
    
    private static void ILSearchAddEntries(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // BestiaryUICollectionInfo info = entry.UIInfoProvider.GetEntryUICollectionInfo();
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(IBestiaryUICollectionInfoProvider), nameof(IBestiaryUICollectionInfoProvider.GetEntryUICollectionInfo)));

        // ++ <fakeUnlock> 
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (Configs.BetterBestiary.DisplayedInfo) info.UnlockState = GetDisplayedUnlockLevel(info.UnlockState);
            return info;
        });
        // ...
    }


    private static void HookShowBagContent(On_UIBestiaryInfoItemLine.orig_ctor orig, UIBestiaryInfoItemLine self, DropRateInfo info, BestiaryUICollectionInfo uiinfo, float textScale) {
        orig(self, info, uiinfo, textScale);
        if (Configs.BetterBestiary.ShowBagContent && ItemID.Sets.BossBag[info.itemId]) {
            UIList uIList = new() {
                Left = StyleDimension.FromPixelsAndPercent(-1, 0f),
                Width = StyleDimension.FromPixelsAndPercent(0, 1f),
                Height = StyleDimension.FromPixelsAndPercent(0, 1f),
            };
            uIList.SetPadding(0);
            uIList.PaddingBottom = 4;
            uIList.PaddingBottom = 4;
            uIList.ListPadding = 4;
            uIList.Top.Set(self.Height.Pixels + uIList.PaddingTop, 0);
            self.Append(uIList);

            List<DropRateInfo> drops = new();
            DropRateInfoChainFeed ratesInfo = new(1f);
            foreach (IItemDropRule itemDropRule in Main.ItemDropsDB.GetRulesForItemID(info.itemId)) itemDropRule.ReportDroprates(drops, ratesInfo);
            foreach (DropRateInfo drop in drops) {
                if (MathX.InRange(drop.itemId, ItemID.CopperCoin, ItemID.PlatinumCoin)) continue;
                ItemDropBestiaryInfoElement element = new(drop);
                UIElement? dropLine = element.ProvideUIElement(uiinfo);
                if (dropLine is null) continue;
                dropLine.Left.Set(0, 0);
                dropLine.Width.Set(0, 1);
                dropLine.PaddingLeft /= 2;
                dropLine.PaddingRight /= 2;
                uIList.Add(dropLine);
            }
            uIList.Recalculate();
            self.Height.Pixels += uIList.GetTotalHeight() + uIList.PaddingBottom;
        }
    }
    private static string HookSearchBagText(On_ItemDropBestiaryInfoElement.orig_GetSearchString orig, ItemDropBestiaryInfoElement self, ref BestiaryUICollectionInfo info) {
        string s = orig(self, ref info);
        if (!Configs.BetterBestiary.ShowBagContent) return s;
        DropRateInfo dropRateInfo = Reflection.ItemDropBestiaryInfoElement._droprateInfo.GetValue(self);
        if (!ItemID.Sets.BossBag[dropRateInfo.itemId]) return s;
        return $"{s}|{GetBossBagSearch(dropRateInfo)}";
    }

    private static string HookCustomUnlockFilterName(On_Filters.ByUnlockState.orig_GetDisplayNameKey orig, Filters.ByUnlockState self) => Configs.BetterBestiary.UnlockFilter ? $"{Localization.Keys.UI}.FullUnlock" : orig(self);
    private static bool HookCustomUnlockFilter(On_Filters.ByUnlockState.orig_FitsFilter orig, Filters.ByUnlockState self, BestiaryEntry entry) => Configs.BetterBestiary.UnlockFilter ? entry.UIInfoProvider.GetEntryUICollectionInfo().UnlockState != BestiaryEntryUnlockState.CanShowDropsWithDropRates_4 : orig(self, entry);


    private static void ILIconUpdateFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // this._collectionInfo = this._entry.UIInfoProvider.GetEntryUICollectionInfo();
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(IBestiaryUICollectionInfoProvider), nameof(IBestiaryUICollectionInfoProvider.GetEntryUICollectionInfo)));

        // ++ <fakeUnlock> 
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (Configs.BetterBestiary.UnknownDisplay && Configs.BetterBestiary.Value.unknownDisplay == Configs.UnknownDisplay.Known && info.UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0) info.UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1;
            if (Configs.BetterBestiary.DisplayedInfo && info.UnlockState > BestiaryEntryUnlockState.NotKnownAtAll_0) info.UnlockState = GetDisplayedUnlockLevel(info.UnlockState);
            return info;
        });
        // ...
    }
    private static void ILIconDrawFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // ...
        // bool unlockState = this._entry.Icon.GetUnlockState(this._collectionInfo);
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(IEntryIcon), nameof(IEntryIcon.GetUnlockState)));

        // ++ <changeVisibleState>
        cursor.EmitDelegate((bool unlocked) => unlocked || (Configs.BetterBestiary.UnknownDisplay && Configs.BetterBestiary.Value.unknownDisplay == Configs.UnknownDisplay.Known));
    }
    private static void HookDarkenEntryButton(On_UIBestiaryEntryButton.orig_ctor orig, UIBestiaryEntryButton self, BestiaryEntry entry, bool isAPrettyPortrait) {
        orig(self, entry, isAPrettyPortrait);
        if (!Configs.BetterBestiary.UnknownDisplay || self.Entry.Icon.GetUnlockState(self.Entry.UIInfoProvider.GetEntryUICollectionInfo())) return;
        ((UIImage)self.Children.First().Children.First()).Color.ApplyRGB(IconDark);
        Reflection.UIBestiaryEntryButton._borders.GetValue(self).Color.ApplyRGB(IconDark);
        Reflection.UIBestiaryEntryButton._bordersGlow.GetValue(self).Color.ApplyRGB(IconDark);
    }

    private static void IlEntryPageFakeUnlock(ILContext il) {
        ILCursor cursor = new(il);

        // BestiaryUICollectionInfo uICollectionInfo = this.GetUICollectionInfo(entry, extraInfo);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(UIBestiaryEntryInfoPage), "GetUICollectionInfo"));

        // ++ <fakeUnlock>
        cursor.EmitDelegate((BestiaryUICollectionInfo info) => {
            if (Configs.BetterBestiary.UnknownDisplay && Configs.BetterBestiary.Value.unknownDisplay == Configs.UnknownDisplay.Known && info.UnlockState <= BestiaryEntryUnlockState.NotKnownAtAll_0) info.UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1;
            if (Configs.BetterBestiary.DisplayedInfo && info.UnlockState > BestiaryEntryUnlockState.NotKnownAtAll_0) info.UnlockState = GetDisplayedUnlockLevel(info.UnlockState);
            return info;
        });
        // ...
    }
    private static void HookDarkenEntryPage(On_UIBestiaryEntryInfoPage.orig_AddInfoToList orig, UIBestiaryEntryInfoPage self, BestiaryEntry entry, ExtraBestiaryInfoPageInformation extraInfo) {
        orig(self, entry, extraInfo);
        if(!Configs.BetterBestiary.UnknownDisplay) return;
        if(s_darkPage != (entry.UIInfoProvider.GetEntryUICollectionInfo().UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0)) {
            DarkenElement(self, s_darkPage ? (1 / PageDark) : PageDark, 1);
            s_darkPage = !s_darkPage;
        }
        if (s_darkPage) DarkenElement(Reflection.UIBestiaryEntryInfoPage._list.GetValue(self), PageDark);
    }

    private static void ILFakeUnlockFilters(ILContext il) {
        ILCursor cursor = new(il);
        cursor.EmitDelegate(() => { s_ilSkipped = 0; });

        // ...
        // for (<filter>) {
        //     ...
        //     bool b = this.GetIsFilterAvailableForEntries(bestiaryEntryFilter, entries);
        cursor.GotoNext(MoveType.After, i => i.SaferMatchCall(typeof(UIBestiaryFilteringOptionsGrid), "GetIsFilterAvailableForEntries"));

        ILLabel? cont = null; 
        cursor.FindNext(out _, i => i.MatchBr(out cont));

        //     ++ <fakeUnlock> 
        cursor.EmitLdloc(13);
        cursor.EmitLdloc(14);
        cursor.EmitDelegate((bool on, IBestiaryEntryFilter filter, List<BestiaryEntry> entries) => {
            s_ilOn = on;
            if(!Configs.BetterBestiary.UnknownDisplay || on || filter.ForcedDisplay.HasValue) return false;
            if (Configs.BetterBestiary.Value.unknownDisplay == Configs.UnknownDisplay.Known) {
                s_ilOn = true;
                return false;
            }
            if (Configs.BetterBestiary.Value.unknownDisplay == Configs.UnknownDisplay.Hidden) {
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

        cursor.GotoNext(i => i.MatchStloc(11)).GotoPrev(MoveType.After, i => i.MatchLdloc(10));
        cursor.EmitDelegate((int i) => i-s_ilSkipped);
        cursor.GotoNext(i => i.MatchStloc(12)).GotoPrev(MoveType.After, i => i.MatchLdloc(10));
        cursor.EmitDelegate((int i) => i-s_ilSkipped);

        cursor.GotoNext(MoveType.Before, i => i.MatchRet());
        cursor.EmitLdarg0();
        cursor.EmitDelegate((UIBestiaryFilteringOptionsGrid self) => {
            UIPanel p = (UIPanel)Reflection.UIBestiaryFilteringOptionsGrid._container.GetValue(self);
            EntryFilterer<BestiaryEntry, IBestiaryEntryFilter> filterer = Reflection.UIBestiaryFilteringOptionsGrid._filterer.GetValue(self);
            int widthWithSpacing = 32 + 2;
            int perRow = 12;
            int howManyRows = (int)Math.Ceiling((filterer.AvailableFilters.Count - s_ilSkipped) / (float)perRow);
            if (p.Children.Count() < perRow) {
                p.Width = new(p.Children.Count() * widthWithSpacing + 10, 0f);
                p.Height = new(1 * widthWithSpacing + 10, 0f);
            } else {
                p.Width = new(perRow * widthWithSpacing + 10, 0f);
                p.Height = new(howManyRows * widthWithSpacing + 10, 0f);
            }
        }); 

    }
    private static void HookDarkenFilters(On_UIBestiaryFilteringOptionsGrid.orig_UpdateButtonSelections orig, UIBestiaryFilteringOptionsGrid self) {
        orig(self);
        if (!Configs.BetterBestiary.UnknownDisplay) return;
        EntryFilterer<BestiaryEntry, IBestiaryEntryFilter> filterer = Reflection.UIBestiaryFilteringOptionsGrid._filterer.GetValue(self);
        List<GroupOptionButton<int>> filters = Reflection.UIBestiaryFilteringOptionsGrid._filterButtons.GetValue(self);
        List<List<BestiaryEntry>> test = Reflection.UIBestiaryFilteringOptionsGrid._filterAvailabilityTests.GetValue(self);
        foreach (GroupOptionButton<int> filter in filters) {
            if (filter.OptionValue < 0 || !Reflection.UIBestiaryFilteringOptionsGrid.GetIsFilterAvailableForEntries.Invoke(self, filterer.AvailableFilters[filter.OptionValue], test[filter.OptionValue])) DarkenElement(filter, IconDark);
        }
    }

    private static void HookBestiaryFilterRemoveHiddenEntries(On_UIBestiaryTest.orig_FilterEntries orig, UIBestiaryTest self) {
        orig(self);
        if (!Configs.BetterBestiary.UnknownDisplay || Configs.BetterBestiary.Value.unknownDisplay != Configs.UnknownDisplay.Hidden) return;
        List<BestiaryEntry> entries = Reflection.UIBestiaryTest._workingSetEntries.GetValue(Main.BestiaryUI);
        for (int i = entries.Count - 1; i >= 0; i--) {
            if (entries[i].UIInfoProvider.GetEntryUICollectionInfo().UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0) entries.RemoveAt(i);
        }
    }


    public static BestiaryEntryUnlockState GetDisplayedUnlockLevel(BestiaryEntryUnlockState state) => state < (BestiaryEntryUnlockState)Configs.BetterBestiary.Value.displayedInfo ? (BestiaryEntryUnlockState)Configs.BetterBestiary.Value.displayedInfo : state;

    public static void DarkenElement(UIElement element, float dark, int depth = -1){
        if (element is UIHorizontalSeparator sep) sep.Color.ApplyRGB(dark);
        else if (element is UIBestiaryNPCEntryPortrait portrait) ((UIImage)portrait.Children.Last()).Color.ApplyRGB(dark);
        else if (element is GroupOptionButton<int> button) {
            Color color = Reflection.GroupOptionButton<int>._color.GetValue(button);
            color.ApplyRGB(IconDark);
            Reflection.GroupOptionButton<int>._color.SetValue(button, color);
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
            else foreach(UIElement e in element.Children) DarkenElement(e, dark, depth);
        }
    }

    public static string GetBossBagSearch(DropRateInfo bossBag){
        if(_bossBagSearch.TryGetValue(bossBag.itemId, out string? s)) return s;
        List<DropRateInfo> drops = new();
        DropRateInfoChainFeed ratesInfo = new(1f);
        List<string> names = new();
        foreach (IItemDropRule itemDropRule in Main.ItemDropsDB.GetRulesForItemID(bossBag.itemId)) itemDropRule.ReportDroprates(drops, ratesInfo);
        foreach (DropRateInfo drop in drops) {
            if (!MathX.InRange(drop.itemId, ItemID.CopperCoin, ItemID.PlatinumCoin)) names.Add(Lang.GetItemNameValue(drop.itemId));
        }
        return _bossBagSearch[bossBag.itemId] = string.Join('|', names);
    }

    public const float PageDark = 0.7f;
    public const float IconDark = 0.5f;

    private static bool s_darkPage = false;
    private static int s_ilSkipped = 0;
    private static bool s_ilOn = false;

    private static readonly Dictionary<int, string> _bossBagSearch = new();
}