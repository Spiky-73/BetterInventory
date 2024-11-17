using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpikysLib;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BetterInventory.ItemActions;

public sealed class GrabBagTooltipItem : GlobalItem {

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!Configs.GrabBagTooltip.Enabled) return;
        List<IItemDropRule> itemDropRules = Main.ItemDropsDB.GetRulesForItemID(item.type);
        if (itemDropRules.Count == 0) return;
        tooltips.AddRange(GetGrabBagContent(item.type));
    }
    
    public static List<TooltipLine> GetGrabBagContent(int itemType) {
        if (_bagContentItemType != itemType) UpdateGrabBagContent(itemType);
        List<TooltipLine> tooltips = [new(BetterInventory.Instance, $"BagContent0", Language.GetTextValue($"{Localization.Keys.UI}.BagContent"))];
        for (int i = _bagContentCurrencies.Count - 1; i >= 0; i--) {
            var (currency, min, max) = _bagContentCurrencies[i];

            string priceText = CurrencyHelper.PriceText(currency, min);
            var match = _coinRegex.Match(priceText);
            string priceTextMax = CurrencyHelper.PriceText(currency, max);
            if (min != max) {
                if (match.Success) {
                    Regex regexMax = new($"""^{Regex.Escape(match.Groups[1].Value)}(\d+){Regex.Escape(match.Groups[3].Value)}{Regex.Escape(match.Groups[4].Value)}$""");
                    var matchMax = regexMax.Match(priceTextMax);

                    if (match.Success) priceText = $"{match.Groups[1]}{match.Groups[2]}-{matchMax.Groups[1]}{match.Groups[3]}{match.Groups[4]}";
                    else priceText += $" - {priceTextMax}";
                } else priceText += $" - {priceTextMax}";
            }
            tooltips.Add(new(BetterInventory.Instance, $"BagContentCurrency{i}", priceText));
        }
        tooltips.AddRange(_bagContentTooltips);
        return tooltips;
    }
    private static void UpdateGrabBagContent(int itemType) {
        _bagContentItemType = itemType;
        _bagContentTooltips.Clear();
        _bagContentCurrencies.Clear();

        List<IItemDropRule> itemDropRules = Main.ItemDropsDB.GetRulesForItemID(itemType);
        if (itemDropRules.Count > 0) {
            foreach (IItemDropRule itemDropRule in itemDropRules) {
                List<DropRateInfo> drops = [];
                DropRateInfoChainFeed ratesInfo = new(1f);
                itemDropRule.ReportDroprates(drops, ratesInfo);
                drops.RemoveAll(dri => !Reflection.ItemDropBestiaryInfoElement.ShouldShowItem.Invoke(dri));
                if (!Configs.GrabBagTooltip.Value.compact) AddGrabBagContent(_bagContentTooltips, drops);
                else AddGrabBagContent_Compact(_bagContentTooltips, drops);
            }
        }
        _bagContentCurrencies.Sort((a, b) => a.currency.CompareTo(b.currency));
    }
    private static void AddGrabBagContent(List<TooltipLine> tooltips, List<DropRateInfo> drops) {
        for (int i = 0; i < drops.Count; i++) {
            DropRateInfo drop = drops[i];
            if (drop.dropRate == 1 && CurrencyHelper.IsPartOfACurrency(drop.itemId, out int currency)) AddGrabBagContent_Currency(drop, currency);
            else tooltips.Add(new(BetterInventory.Instance, $"BagContent{tooltips.Count}", $"[i:{drop.itemId}] {Lang.GetItemName(drop.itemId)} {GetDropRate(drop)}"));
        }
    }
    private static void AddGrabBagContent_Compact(List<TooltipLine> tooltips, List<DropRateInfo> drops) {
        string dropRate = string.Empty;
        List<int> items = [];
        void AddLine() {
            if (items.Count == 0) return;
            string sprites = items.Count == 1 ? $"[i:{items[0]}] {Lang.GetItemName(items[0])}" : string.Join(string.Empty, items.Select(i => $"[i:{i}]"));
            tooltips.Add(new(BetterInventory.Instance, $"BagContent{tooltips.Count}", $"{sprites} {dropRate}"));
            items.Clear();
        }
        for (int i = 0; i < drops.Count; i++) {
            DropRateInfo drop = drops[i];
            Item item = new(drop.itemId);
            if (drop.dropRate == 1 && CurrencyHelper.IsPartOfACurrency(drop.itemId, out int currency)) {
                AddGrabBagContent_Currency(drop, currency);
                continue;
            }
            string s = GetDropRate(drop);
            if (s != dropRate) {
                AddLine();
                dropRate = s;
            }
            items.Add(drop.itemId);
        }
        AddLine();
    }
    private static void AddGrabBagContent_Currency(DropRateInfo drop, int currency) {
        int value = CurrencyHelper.CurrencyValue(drop.itemId);
        _bagContentCurrencies.Add((currency, drop.stackMin * value, drop.stackMax * value));
    }

    // Adapted From UIBestiaryInfoItemLine.cs
    public static string GetDropRate(DropRateInfo dropRateInfo) {
        string str = string.Empty;
        if (dropRateInfo.stackMin != dropRateInfo.stackMax) str += $"({dropRateInfo.stackMin}-{dropRateInfo.stackMax}) ";
        else if (dropRateInfo.stackMin != 1) str += $"({dropRateInfo.stackMin}) ";

        string originalFormat = dropRateInfo.dropRate >= 0.001 ? "P" : "P4";
        str += dropRateInfo.dropRate != 1f ? Utils.PrettifyPercentDisplay(dropRateInfo.dropRate, originalFormat) : "100%";
        return str;
    }

    private static int _bagContentItemType;
    private static readonly List<(int currency, int min, int max)> _bagContentCurrencies = [];
    private static readonly List<TooltipLine> _bagContentTooltips = [];

    private static readonly Regex _coinRegex = new("""^(\[c\/[0-9a-fA-F]{6}:)?(\d+)( [a-zA-Z ]+)(\])?$""");
}