using System.Collections.Generic;
using BetterInventory.ItemSearch;
using BetterInventory.Crafting;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using BetterInventory.InventoryManagement;
using Terraria.Audio;
using Terraria.ID;
using SpikysLib.UI;
using Terraria.Localization;
using SpikysLib.CrossMod;
using SpikysLib.Constants;
using SpikysLib.Configs;
using SpikysLib;
using Terraria.GameContent.ItemDropRules;
using System.Linq;
using System.Text.RegularExpressions;

namespace BetterInventory.ItemActions;

public sealed class BetterPlayer : ModPlayer {

    public static BetterPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<BetterPlayer>();

    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;
    public static readonly List<(BuilderToggle? toggle, ModKeybind kb)> BuilderTogglesKb = [];
    public static readonly List<BuilderToggle> WireDisplayToggles = [];

    public override void Load() {
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(Mod, "FavoritedQuickBuff", Microsoft.Xna.Framework.Input.Keys.B);
        On_ItemSlot.TryOpenContainer += HookTryOpenContainer;
        On_Player.DropItemFromExtractinator += HookFastExtractinator;

        On_ItemSlot.PickupItemIntoMouse += HookNoPickupMouse;

        On_ItemSlot.DyeSwap += HookDyeSwapFavorited;
        On_ItemSlot.ArmorSwap += HookArmorSwapFavorited;
        On_ItemSlot.EquipSwap += HookEquipSwapFavorited;
    }

    public override void Unload() {
        FavoritedBuffKb = null!;
        BuilderTogglesKb.Clear();
        WireDisplayToggles.Clear();
    }
    public override void SetStaticDefaults() {
        foreach (BuilderToggle toggle in Reflection.BuilderToggleLoader.BuilderToggles.GetValue()) {
            if (toggle is WireVisibilityBuilderToggle wv && wv.NumberOfStates == 3) {
                if (WireDisplayToggles.Count == 0) BuilderTogglesKb.Add((null, KeybindLoader.RegisterKeybind(Mod, "WireDisplay", Microsoft.Xna.Framework.Input.Keys.None)));
                WireDisplayToggles.Add(toggle);
                continue;
            }
            BuilderTogglesKb.Add((toggle, KeybindLoader.RegisterKeybind(Mod, toggle.Name.Replace("BuilderToggle",string.Empty), Microsoft.Xna.Framework.Input.Keys.None)));
        }
    }

    public override void OnEnterWorld() {
        RecipeFilters ??= new();
        VisibilityFilters ??= new();
        if (Configs.BetterGuide.AvailableRecipes) Guide.FindGuideRecipes();

        DisplayUpdate();
        DisplayCompatibility();
        DisplaySpicWarning();

        Guide.SetGuideItem(this);
    }

    public override void ResetEffects() {
        Guide.forcedTooltip = null;
    }

    public void DisplayUpdate() {
        LocalizedLine line;
        if (Configs.Version.Instance.lastPlayedVersion.Length == 0) line = new(Language.GetText($"{Localization.Keys.Chat}.Download"));
        else if (Mod.Version > new System.Version(Configs.Version.Instance.lastPlayedVersion)) line = new(Language.GetText($"{Localization.Keys.Chat}.Update"));
        else return;
        Configs.Version.Instance.lastPlayedVersion = Mod.Version.ToString();
        Configs.Version.Instance.Save();

        if (Language.GetText($"{Localization.Keys.Chat}.Summary").Value.Length != 0) {
            InGameNotificationsTracker.AddNotification(new InGameNotification(Mod, line, new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.Bug"), Colors.RarityAmber)) { timeLeft = 15 * 60 });
        }
    }

    public void DisplayCompatibility() {
        LocalizedLine line;
        if (Utility.FailedILs > Configs.Compatibility.Instance.failedILs) line = new(Language.GetText($"{Localization.Keys.Chat}.UnloadedMore"), Colors.RarityAmber);
        else if (Utility.FailedILs < Configs.Compatibility.Instance.failedILs) line = new(Language.GetText(Utility.FailedILs == 0 ? $"{Localization.Keys.Chat}.UnloadedNone" : $"{Localization.Keys.Chat}.UnloadedLess"), Colors.RarityGreen);
        else return;
        Configs.Compatibility.Instance.failedILs = Utility.FailedILs;
        Configs.Compatibility.Instance.Save();

        InGameNotificationsTracker.AddNotification(new InGameNotification(Mod, line));
    }

    public void DisplaySpicWarning() {
        if (!Configs.CraftStack.Enabled || Configs.CraftStack.Value.maxItems.Key.Choice != nameof(Configs.MaxCraftAmount.spicRequirement) || SpysInfiniteConsumables.Enabled) return;
        InGameNotificationsTracker.AddNotification(new InGameNotification(Mod, new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.SPICWarning"), Colors.RarityAmber)));
    }

    public override void SetControls() {
        if (Configs.ItemActions.FastContainerOpening && Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        QuickMove.ProcessTriggers();
        QuickSearch.ProcessTriggers();
        if (Configs.ItemActions.FavoritedBuff && FavoritedBuffKb.JustPressed) FavoritedBuff(Player);
        if (Configs.ItemActions.BuilderAccs) BuilderKeys();
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        QuickMove.HoverItem(inventory, context, slot);
        if (Default.Catalogues.RecipeList.OverrideHover(inventory, context, slot)) return true;
        if (Guide.OverrideHover(inventory, context, slot)) return true;
        if (ClickOverrides.OverrideHover(inventory, context, slot)) return true;
        return false;
    }

    public override bool PreItemCheck() {
        if (Main.myPlayer == Player.whoAmI && Configs.ItemRightClick.Enabled && Player.controlUseTile && Player.releaseUseItem && !Player.controlUseItem && !Player.tileInteractionHappened
                && !Player.mouseInterface && !Terraria.Graphics.Capture.CaptureManager.Instance.Active && !Main.HoveringOverAnNPC && !Main.SmartInteractShowingGenuine
                && Main.HoverItem.IsAir && Player.altFunctionUse == 0 && Player.selectedItem < InventorySlots.Hotbar.End) {
            Player.itemAnimation--;
            if(Main.stackSplit == 1) Player.itemAnimation = 0;
            if (!Configs.ItemRightClick.Value.stackableItems) s_noMousePickup = true;
            ItemSlot.RightClick(Player.inventory, ItemSlot.Context.InventoryItem, Player.selectedItem);
            s_noMousePickup = false;
            if (!Main.mouseItem.IsAir) Player.DropSelectedItem();
            return false;
        }
        return true;
    }
    private static void HookNoPickupMouse(On_ItemSlot.orig_PickupItemIntoMouse orig, Item[] inv, int context, int slot, Player player) {
        if (!Configs.ItemRightClick.Enabled || !s_noMousePickup) orig(inv, context, slot, player);
    }


    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback itemConsumedCallback) {
        List<Item> items = [];
        Item? mat;
        if((mat = Guide.GetGuideMaterials()) != null) items.Add(mat);
        if(Main.myPlayer == Player.whoAmI && (mat = Crafting.Crafting.GetMouseMaterial()) != null) items.Add(mat);
        itemConsumedCallback = (item, index) => {
            if (item == Main.mouseItem) item.stack -= Reflection.RecipeLoader.ConsumedItems.GetValue()[^1].stack; // FIXME seems hacky
            return;
        };
        return items;
    }

    private static void HookTryOpenContainer(On_ItemSlot.orig_TryOpenContainer orig, Item item, Player player) {
        if (!Configs.ItemActions.FastContainerOpening) {
            orig(item,player);
            return;
        }
        int split = Main.stackSplit;
        for (int i = 0; i < Main.superFastStack + 1; i++) orig(item,player);
        Main.stackSplit = split;
        ItemSlot.RefreshStackSplitCooldown();
    }
    private static void HookFastExtractinator(On_Player.orig_DropItemFromExtractinator orig, Player self, int itemType, int stack) {
        orig(self, itemType, stack);
        if (!Configs.ItemActions.FastExtractinator || self.ItemTimeIsZero) return;
        ItemSlot.RefreshStackSplitCooldown();
        self.itemTime = self.itemTimeMax = Main.stackSplit - 1;
        Main.preventStackSplitReset = true;
    }

    public static void CycleBuilderState(Player player, BuilderToggle toggle, int? state = null) => player.builderAccStatus[toggle.Type] = (state ?? (player.builderAccStatus[toggle.Type] + 1)) % toggle.NumberOfStates;
    public static void FavoritedBuff(Player player) => ItemHelper.RunWithHiddenItems(player.inventory, player.QuickBuff, i => !i.favorited);
    private void BuilderKeys() {
        foreach ((BuilderToggle? builder, ModKeybind kb) in BuilderTogglesKb) {
            if (!kb.JustPressed) continue;
            if (builder is null) {
                CycleBuilderState(Player, WireDisplayToggles[0]);
                for (int i = 1; i < WireDisplayToggles.Count; i++) CycleBuilderState(Player, WireDisplayToggles[i], WireDisplayToggles[i].CurrentState);
            } else {
                CycleBuilderState(Player, builder);
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }
    public override void SaveData(TagCompound tag) {
        Guide.SaveData(this, tag);
        tag[VisibilityTag] = VisibilityFilters;
        tag[RecipesTag] = RecipeFilters;
        tag[FavoritedInBanksTag] = new FavoritedInBanks(Player);
    }

    public override void LoadData(TagCompound tag) {
        Guide.LoadData(this, tag);
        if (tag.TryGet(VisibilityTag, out VisibilityFilters visibility)) VisibilityFilters = visibility;
        if (tag.TryGet(RecipesTag, out RecipeFilters recipe)) RecipeFilters = recipe;
        if (Configs.InventoryManagement.FavoriteInBanks && tag.TryGet(FavoritedInBanksTag, out FavoritedInBanks favorited)) favorited.Apply(Player);
    }

    public RecipeFilters RecipeFilters { get; set; } = null!;
    public VisibilityFilters VisibilityFilters { get; set; } = new();

    private static bool s_noMousePickup;

    private static Item HookEquipSwapFavorited(On_ItemSlot.orig_EquipSwap orig, Item item, Item[] inv, int slot, out bool success) => EquipSwapFavorited((out bool success) => orig(item, inv, slot, out success), item, out success);
    private static Item HookArmorSwapFavorited(On_ItemSlot.orig_ArmorSwap orig, Item item, out bool success) => EquipSwapFavorited((out bool success) => orig(item, out success), item, out success);
    private static Item HookDyeSwapFavorited(On_ItemSlot.orig_DyeSwap orig, Item item, out bool success) => EquipSwapFavorited((out bool success) => orig(item, out success), item, out success);

    private delegate Item EquipSwapFn(out bool success);
    private static Item EquipSwapFavorited(EquipSwapFn swap, Item item, out bool success) {
        bool favorited = item.favorited;
        Item swapped = swap(out success);
        if (success && favorited && Configs.ItemActions.KeepSwappedFavorited) swapped.favorited = true;
        return swapped;
    }

    public static void AddBagContentTooltips(Item item, List<TooltipLine> tooltips) {
        if (!Configs.ShowBagContent.Enabled) return;
        List<IItemDropRule> itemDropRules = Main.ItemDropsDB.GetRulesForItemID(item.type);
        if (itemDropRules.Count == 0) return;
        tooltips.AddRange(GetGrabBagContent(item.type));
    }

    public static List<TooltipLine> GetGrabBagContent(int itemType) {
        if (_bagContentItemType != itemType) UpdateGrabBagContent(itemType);
        List<TooltipLine> tooltips = [new(BetterInventory.Instance, $"bagContent#0", Language.GetTextValue($"{Localization.Keys.UI}.BagContent"))];
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
            tooltips.Add(new(BetterInventory.Instance, $"bagContentCurrency#{i}", priceText));
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
                if (!Configs.ShowBagContent.Value.compact) AddGrabBagContent(_bagContentTooltips, drops);
                else AddGrabBagContent_Compact(_bagContentTooltips, drops);
            }
        }
        _bagContentCurrencies.Sort((a, b) => a.currency.CompareTo(b.currency));
    }
    private static void AddGrabBagContent(List<TooltipLine> tooltips, List<DropRateInfo> drops) {
        for (int i = 0; i < drops.Count; i++) {
            DropRateInfo drop = drops[i];
            if (drop.dropRate == 1 && CurrencyHelper.IsPartOfACurrency(drop.itemId, out int currency)) AddGrabBagContent_Currency(drop, currency);
            else tooltips.Add(new(BetterInventory.Instance, $"bagContent#{tooltips.Count}", $"[i:{drop.itemId}] {Lang.GetItemName(drop.itemId)} {GetDropRate(drop)}"));
        }
    }
    private static void AddGrabBagContent_Compact(List<TooltipLine> tooltips, List<DropRateInfo> drops) {
        string dropRate = string.Empty;
        List<int> items = [];
        void AddLine() {
            if (items.Count == 0) return;
            string sprites = items.Count == 1 ? $"[i:{items[0]}] {Lang.GetItemName(items[0])}" : string.Join(string.Empty, items.Select(i => $"[i:{i}]"));
            tooltips.Add(new(BetterInventory.Instance, $"bagContent#{tooltips.Count}", $"{sprites} {dropRate}"));
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
        if (dropRateInfo.stackMin != dropRateInfo.stackMax) str+= $"({dropRateInfo.stackMin}-{dropRateInfo.stackMax}) ";
        else if (dropRateInfo.stackMin != 1) str+= $"({dropRateInfo.stackMin}) ";
        
        string originalFormat = dropRateInfo.dropRate >= 0.001 ? "P" : "P4";        
        str += dropRateInfo.dropRate != 1f ? Utils.PrettifyPercentDisplay(dropRateInfo.dropRate, originalFormat) : "100%";
        return str;
    }

    public const string VisibilityTag = "visibility";
    public const string RecipesTag = "recipes";
    public const string FavoritedInBanksTag = "favorited";

    private static int _bagContentItemType;
    private static List<(int currency, int min, int max)> _bagContentCurrencies = [];
    private readonly static List<TooltipLine> _bagContentTooltips = [];

    internal Item? _tempGuideTile;

    private static readonly Regex _coinRegex = new("""^(\[c\/[0-9a-fA-F]{6}:)?(\d+)( [a-zA-Z ]+)(\])?$""");
}
