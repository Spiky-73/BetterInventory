using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpikysLib;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace BetterInventory.VisualChanges.GrabBagContent;

public sealed class GrabBagTooltipItem : GlobalItem {

    public override bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || VisualChangesConfig.GrabBagContent;

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!VisualChangesConfig.GrabBagContent || !GrabBagContentConfig.Instance.tooltip) return;
        tooltips.AddRange(GrabBagTooltip.GetContentTooltips(item.type));
    }
}

public static class GrabBagTooltip {
    public static List<TooltipLine> GetContentTooltips(int type) {
        if (_type == type && _compact == GrabBagContentConfig.Instance.compact) return _tooltips;
        _type = type;
        _compact = GrabBagContentConfig.Instance.compact;
        _tooltips.Clear();

        List<IItemDropRule> itemDropRules = Main.ItemDropsDB.GetRulesForItemID(type);
        if (itemDropRules.Count == 0) return _tooltips;
        DropAttemptInfo info = new() {
            player = Main.LocalPlayer,
            item = type,
            IsExpertMode = Main.expertMode,
            IsMasterMode = Main.masterMode,
            IsInSimulation = true,
            rng = Main.rand
        };

        List<TooltipLine> dropLines = [];
        SortedList<int, TooltipLine> currencyLines = [];
        foreach (IItemDropRule itemDropRule in itemDropRules) {
            if (!itemDropRule.CanDrop(info)) continue;
            List<DropRateInfo> drops = [];
            DropRateInfoChainFeed ratesInfo = new(1f);
            itemDropRule.ReportDroprates(drops, ratesInfo);
            drops.RemoveAll(dri => !ItemDropBestiaryInfoElement.ShouldShowItem(ref dri));
            if (!_compact) AddDropRuleLines(drops, dropLines, currencyLines);
            else AddDropRuleLines_Compact(drops, dropLines, currencyLines);
        }
        _tooltips.AddRange(currencyLines.Values);
        _tooltips.AddRange(dropLines);
        return _tooltips;
    }

    private static void AddDropRuleLines(List<DropRateInfo> drops, List<TooltipLine> dropLines, SortedList<int, TooltipLine> currencyLines) {
        foreach (DropRateInfo drop in drops) {
            if (!TryAddCurrencyLine(drop, currencyLines)) dropLines.Add(new(BetterInventory.Instance, $"BagContent{dropLines.Count}", $"[i:{drop.itemId}] {Lang.GetItemName(drop.itemId)} {GetDropRate(drop)}"));
        }
    }
    private static void AddDropRuleLines_Compact(List<DropRateInfo> drops, List<TooltipLine> dropLines, SortedList<int, TooltipLine> currencyLines) {
        string dropRate = string.Empty;
        List<int> items = [];
        void AddLine() {
            string sprites = items.Count == 1 ? $"[i:{items[0]}] {Lang.GetItemName(items[0])}" : string.Join(string.Empty, items.Select(i => $"[i:{i}]"));
            dropLines.Add(new(BetterInventory.Instance, $"BagContent{dropLines.Count}", $"{sprites} {dropRate}"));
            items.Clear();
        }
        foreach (DropRateInfo drop in drops) {
            if (TryAddCurrencyLine(drop, currencyLines)) continue;
            string s = GetDropRate(drop);
            if (s != dropRate) {
                if (!string.IsNullOrEmpty(dropRate)) AddLine();
                dropRate = s;
            }
            items.Add(drop.itemId);
        }
        AddLine();
    }

    private static bool TryAddCurrencyLine(DropRateInfo drop, SortedList<int, TooltipLine> currencyLines) {
        if (drop.dropRate != 1 || !CurrencyHelper.IsPartOfACurrency(drop.itemId, out var currency)) return false;
        var value = CurrencyHelper.CurrencyValue(drop.itemId);
        var (min, max) = (drop.stackMin * value, drop.stackMax * value);

        string priceText = CurrencyHelper.PriceText(currency, min);
        if (min != max) {
            string priceTextMax = CurrencyHelper.PriceText(currency, max);
            var match = _coinRegex.Match(priceText);
            if (match.Success) {
                Regex regexMax = new($"""^{Regex.Escape(match.Groups[1].Value)}(\d+){Regex.Escape(match.Groups[3].Value)}{Regex.Escape(match.Groups[4].Value)}$""");
                var matchMax = regexMax.Match(priceTextMax);

                if (match.Success) priceText = $"{match.Groups[1]}{match.Groups[2]}-{matchMax.Groups[1]}{match.Groups[3]}{match.Groups[4]}";
                else priceText += $" - {priceTextMax}";
            } else priceText += $" - {priceTextMax}";
        }

        currencyLines.Add(currency, new(BetterInventory.Instance, $"BagContentCurrency{currency}", priceText));
        return true;
    }

    // Adapted From UIBestiaryInfoItemLine.cs
    public static string GetDropRate(DropRateInfo dropRateInfo) {
        string str = string.Empty;
        if (dropRateInfo.stackMin != dropRateInfo.stackMax) str += $"({dropRateInfo.stackMin}-{dropRateInfo.stackMax}) ";
        else if (dropRateInfo.stackMin != 1) str += $"({dropRateInfo.stackMin}) ";
        if (dropRateInfo.dropRate == 1f) str += "100%";
        else str += Utils.PrettifyPercentDisplay(dropRateInfo.dropRate, dropRateInfo.dropRate >= 0.001 ? "P" : "P4");
        return str.ToString();
    }

    private static int _type;
    private static bool _compact;
    private static readonly List<TooltipLine> _tooltips = [];

    private static readonly Regex _coinRegex = new("""^(\[c\/[0-9a-fA-F]{6}:)?(\d+)( [a-zA-Z ]+)(\])?$""");
}