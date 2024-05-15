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
using SpikysLib.Extensions;
using SpikysLib.UI;
using Terraria.Localization;
using Microsoft.Xna.Framework.Graphics;

namespace BetterInventory.ItemActions;

public sealed class BetterPlayer : ModPlayer {

    public static BetterPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<BetterPlayer>();

    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;
    public static readonly List<(BuilderToggle? toggle, ModKeybind kb)> BuilderTogglesKb = new();
    public static readonly List<BuilderToggle> WireDisplayToggles = new();

    public override void Load() {
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(Mod, "FavoritedQuickBuff", Microsoft.Xna.Framework.Input.Keys.B);
        On_ItemSlot.TryOpenContainer += HookTryOpenContainer;
        On_Player.DropItemFromExtractinator += HookFastExtractinator;

        On_ItemSlot.PickupItemIntoMouse += HookNoPickupMouse;
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
            BuilderTogglesKb.Add((toggle, KeybindLoader.RegisterKeybind(Mod, toggle.Name.Replace("BuilderToggle", ""), Microsoft.Xna.Framework.Input.Keys.None)));
        }
    }

    public override void OnEnterWorld() {
        RecipeFilters ??= new();
        VisibilityFilters ??= new();
        if (Configs.BetterGuide.AvailableRecipes) Guide.FindGuideRecipes();

        DisplayUpdate();
        DisplayCompatibility();
    }

    public void DisplayUpdate() {
        bool download;
        if (Configs.Version.Instance.lastPlayedVersion.Length == 0) download = true;
        else if (Mod.Version > new System.Version(Configs.Version.Instance.lastPlayedVersion)) download = false;
        else return;

        List<ITextLine> lines = new();
        if (download) lines.Add(new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.Download")));
        else lines.Add(new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.Update")));
        lines.Add(new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.Bug")));
        LocalizedLine important = new(Language.GetText($"{Localization.Keys.Chat}.Important"), Colors.RarityAmber);
        if (!download && important.Value.Length != 0) lines.Add(important);
        InGameNotificationsTracker.AddNotification(new InGameNotification(ModContent.Request<Texture2D>($"BetterInventory/icon"), lines.ToArray()) { timeLeft = 15 * 60 });

        Configs.Version.Instance.lastPlayedVersion = Mod.Version.ToString();
        Configs.Version.Instance.Save();
    }

    public static void DisplayCompatibility() {
        bool failed;
        if (Hooks.FailedILs > Configs.Compatibility.Instance.failedILs) failed = true;
        else if (Hooks.FailedILs < Configs.Compatibility.Instance.failedILs) failed = false;
        else return;
        Configs.Compatibility.Instance.failedILs = Hooks.FailedILs;
        Configs.Compatibility.Instance.Save();

        List<ITextLine> lines = new() { failed ? new LocalizedLine(Language.GetText($"{Localization.Keys.Chat}.UnloadedMore"), Colors.RarityAmber) : new(Language.GetText($"{Localization.Keys.Chat}.UnloadedLess"), Colors.RarityGreen) };
        InGameNotificationsTracker.AddNotification(new InGameNotification(ModContent.Request<Texture2D>($"BetterInventory/icon"), lines.ToArray()));
    }

    public override void SetControls() {
        if (Configs.ItemActions.FastContainerOpening && Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if(Configs.QuickMove.Enabled) QuickMove.ProcessTriggers();
        if(Configs.QuickList.Enabled) SearchItem.ProcessSearchTap();
        if (Configs.ItemActions.FavoritedBuff && FavoritedBuffKb.JustPressed) FavoritedBuff(Player);
        if (Configs.ItemActions.BuilderAccs) BuilderKeys();
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        QuickMove.HoverItem(inventory, context, slot);
        if (SearchItem.OverrideHover(inventory, context, slot)) return true;
        if (Guide.OverrideHover(inventory, context, slot)) return true;
        if (ClickOverrides.OverrideHover(inventory, context, slot)) return true;
        return false;
    }

    public override bool PreItemCheck() {
        if (Main.myPlayer == Player.whoAmI && Configs.ItemRightClick.Enabled && Player.controlUseTile && Player.releaseUseItem && !Player.controlUseItem && !Player.tileInteractionHappened
                && !Player.mouseInterface && !Terraria.Graphics.Capture.CaptureManager.Instance.Active && !Main.HoveringOverAnNPC && !Main.SmartInteractShowingGenuine
                && Main.HoverItem.IsAir && Player.altFunctionUse == 0 && Player.selectedItem < 10) {
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
        List<Item> items = new();
        Item? mat;
        if((mat = Guide.GetGuideMaterials()) != null) items.Add(mat);
        if((mat = Crafting.Crafting.GetMouseMaterial()) != null) items.Add(mat);
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
    public static void FavoritedBuff(Player player) => ItemExtensions.RunWithHiddenItems(player.inventory, player.QuickBuff, i => !i.favorited);
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
        if (!Guide.guideTile.IsAir) tag[GuideTileTag] = Guide.guideTile;
        tag[VisibilityTag] = VisibilityFilters;
        tag[RecipesTag] = RecipeFilters;
        tag[FavoritedInBanksTag] = new FavoritedInBanks(Player);
    }

    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(GuideTileTag, out Item guide)) Guide.guideTile = guide;
        if (tag.TryGet(VisibilityTag, out VisibilityFilters visibility)) VisibilityFilters = visibility;
        if (tag.TryGet(RecipesTag, out RecipeFilters recipe)) RecipeFilters = recipe;
        if (Configs.InventoryManagement.FavoriteInBanks && tag.TryGet(FavoritedInBanksTag, out FavoritedInBanks favorited)) favorited.Apply(Player);
    }

    public RecipeFilters RecipeFilters { get; set; } = null!;
    public VisibilityFilters VisibilityFilters { get; set; } = new();

    private static bool s_noMousePickup;

    public const string VisibilityTag = "visibility";
    public const string RecipesTag = "recipes";
    public const string GuideTileTag = "guideTile";
    public const string FavoritedInBanksTag = "favorited";
}
