using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using SpikysLib.Collections;

namespace BetterInventory.VisualChanges.GrabBagContent;

public sealed class GrabBagBestiary : ILoadable {

    public bool IsLoadingEnabled(Mod mod) => Compatibility.LoadDisabledFeatures || VisualChangesConfig.GrabBagContent;
    public void Load(Mod mod) {
        On_UIBestiaryInfoItemLine.ctor += HookShowBagContent;
        On_ItemDropBestiaryInfoElement.GetSearchString += HookSearchBagText;
    }
    public void Unload() => _bossBagSearch.Clear();

    private static void HookShowBagContent(On_UIBestiaryInfoItemLine.orig_ctor orig, UIBestiaryInfoItemLine self, DropRateInfo info, BestiaryUICollectionInfo uiinfo, float textScale) {
        orig(self, info, uiinfo, textScale);
        if (!VisualChangesConfig.GrabBagContent || !GrabBagContentConfig.Instance.bestiary || !ItemID.Sets.BossBag[info.itemId]) return;
        
        UIList uIList = new() {
            Left = StyleDimension.FromPixelsAndPercent(-1, 0f),
            Width = StyleDimension.FromPixelsAndPercent(0, 1f),
            Height = StyleDimension.FromPixelsAndPercent(0, 1f),
        };
        uIList.SetPadding(0);
        uIList.PaddingBottom = uIList.ListPadding = 4;
        uIList.Top.Set(self.Height.Pixels + uIList.PaddingTop, 0);
        self.Append(uIList);

        List<DropRateInfo> drops = [];
        DropRateInfoChainFeed ratesInfo = new(1f);
        foreach (IItemDropRule itemDropRule in Main.ItemDropsDB.GetRulesForItemID(info.itemId)) itemDropRule.ReportDroprates(drops, ratesInfo);
        foreach (DropRateInfo drop in drops) {
            if (ItemID.CopperCoin <= drop.itemId && drop.itemId <= ItemID.PlatinumCoin) continue;
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
    private static string HookSearchBagText(On_ItemDropBestiaryInfoElement.orig_GetSearchString orig, ItemDropBestiaryInfoElement self, ref BestiaryUICollectionInfo info) {
        string s = orig(self, ref info);
        if (!VisualChangesConfig.GrabBagContent || !GrabBagContentConfig.Instance.bestiary) return s;
        if (!ItemDropBestiaryInfoElement.ShouldShowItem(ref self._droprateInfo)) return s;
        if (!ItemID.Sets.BossBag[self._droprateInfo.itemId]) return s;
        return $"{s}|{GetBossBagSearch(self._droprateInfo)}";
    }

    public static string GetBossBagSearch(DropRateInfo bossBag) => _bossBagSearch.GetOrAdd(bossBag.itemId, () => {
        List<DropRateInfo> drops = [];
        DropRateInfoChainFeed ratesInfo = new(1f);
        List<string> names = [];
        foreach (IItemDropRule itemDropRule in Main.ItemDropsDB.GetRulesForItemID(bossBag.itemId)) itemDropRule.ReportDroprates(drops, ratesInfo);
        foreach (DropRateInfo drop in drops) {
            if (drop.itemId < ItemID.CopperCoin || ItemID.PlatinumCoin < drop.itemId) names.Add(Lang.GetItemNameValue(drop.itemId));
        }
        return string.Join('|', names);
    });

    private static readonly Dictionary<int, string> _bossBagSearch = [];
}