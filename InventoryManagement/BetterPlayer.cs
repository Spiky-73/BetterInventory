using System;
using System.Collections.Generic;
using System.Linq;
using BetterInventory.Configs.UI;
using BetterInventory.ItemSearch;
using MonoMod.Cil;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.InventoryManagement;

public sealed record class BuilderAccToggle {
    public BuilderAccToggle(string name, Predicate<Player> available, int number) : this(name, available, player => BetterPlayer.CycleAccState(player, number)) {}
    public BuilderAccToggle(string name, Predicate<Player> available, Action<Player> toggle) {
        Name = name;
        IsAvailable = available;
        Toggle = toggle;
        Keybind = null!;
    }

    public void AddKeybind(Mod mod) => Keybind = KeybindLoader.RegisterKeybind(mod, Name, Microsoft.Xna.Framework.Input.Keys.None);
    public void UnloadKeybind() => Keybind = null!;

    public void Process(Player player){
        if (Keybind.JustPressed && IsAvailable(player)) Toggle(player);
    }

    public string Name { get; }
    public ModKeybind Keybind { get; private set; }
    public Predicate<Player> IsAvailable { get; }
    public Action<Player> Toggle { get; }
}

public sealed class BetterPlayer : ModPlayer {

    public static Configs.InventoryManagement Config => Configs.InventoryManagement.Instance;

    public static BetterPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<BetterPlayer>();

    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;
    public static readonly BuilderAccToggle[] BuilderAccToggles = new BuilderAccToggle[] {
        new("RulerLine", player => player.rulerLine, 0),
        new("RulerGrid", player => player.rulerGrid, 1),
        new("AutoActuator", player => player.autoActuator, 2),
        new("AutoPaint", player => player.autoPaint, 3),
        new("WireDisplay", player => player.InfoAccMechShowWires, player => {
            CycleAccState(player, 4, 3);
            for (int i = 5; i < 8; i++) player.builderAccStatus[i] = player.builderAccStatus[4];
            player.builderAccStatus[9] = player.builderAccStatus[4];
        }),
        new("ForcedWires", player => player.InfoAccMechShowWires, 8),
        new("BlockSwap", player => true, 10),
        new("BiomeTorches", player => player.unlockedBiomeTorches, 11)
    };

    public override void Load() {
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(Mod, "FavoritedQuickBuff", Microsoft.Xna.Framework.Input.Keys.N);
        foreach (BuilderAccToggle bat in BuilderAccToggles) bat.AddKeybind(Mod);
        On_ItemSlot.TryOpenContainer += HookTryOpenContainer;
        On_Player.DropItemFromExtractinator += HookFastExtractinator;

        On_Player.OpenChest += HookOpenChest;
        On_Player.GetItem += HookGetItem;

        On_ChestUI.LootAll += HookLootAll;
        On_ChestUI.Restock += HookRestock;
        IL_ItemSlot.LeftClick_ItemArray_int_int += ILKeepFavoriteInChest;
    }

    public override void Unload() {
        FavoritedBuffKb = null!;
        foreach (BuilderAccToggle bat in BuilderAccToggles) bat.UnloadKeybind();
    }

    public override void OnEnterWorld() {
        RecipeFilters ??= new();
        VisibilityFilters ??= new();

        List<NotificationLine> lines = new();
        if (Configs.Version.Instance.lastPlayedVersion.Length == 0) {
            lines.Add(NotificationLine.Download);
            lines.Add(NotificationLine.Bug);
        } else if (Mod.Version > new Version(Configs.Version.Instance.lastPlayedVersion)) {
            lines.Add(NotificationLine.Update);
            lines.Add(NotificationLine.Bug);
            var important = NotificationLine.Important;
            if (important.Text.Length != 0) lines.Add(important);
        } else return;

        InGameNotificationsTracker.AddNotification(new UpdateNotification(lines));
        Configs.Version.Instance.lastPlayedVersion = Mod.Version.ToString();
        Configs.Version.Instance.SaveConfig();
    }

    public override void SetControls() {
        if (Config.fastContainerOpening && Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        QuickMove.RecordSelectedSlot();
        SearchItem.ProcessSearchTap();
        if (FavoritedBuffKb.JustPressed) FavoritedBuff(Player);
        if(Config.builderKeys) foreach (BuilderAccToggle bat in BuilderAccToggles) bat.Process(Player);
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        QuickMove.HoverItem(inventory, context, slot);
        return false;
    }

    public override bool PreItemCheck() {
        if (Config.itemRightClick && Player.controlUseTile && Player.releaseUseItem && !Player.controlUseItem && !Player.tileInteractionHappened
                && !Player.mouseInterface && !Terraria.Graphics.Capture.CaptureManager.Instance.Active && !Main.HoveringOverAnNPC && !Main.SmartInteractShowingGenuine
                && Main.HoverItem.IsAir && Player.altFunctionUse == 0 && Player.selectedItem < 10) {
            if(Main.stackSplit == 1) Main.stackSplit = 31;
            ItemSlot.RightClick(Player.inventory, ItemSlot.Context.InventoryItem, Player.selectedItem);
            return false;
        }
        return true;
    }

    public override IEnumerable<Item> AddMaterialsForCrafting(out ItemConsumedCallback itemConsumedCallback) {
        return Guide.AddMaterials(out itemConsumedCallback);
    }

    private static void HookTryOpenContainer(On_ItemSlot.orig_TryOpenContainer orig, Item item, Player player) {
        int split = Main.stackSplit;
        orig(item, player);
        if (Config.fastContainerOpening) {
            Main.stackSplit = split == 31 ? 1 : split;
            ItemSlot.RefreshStackSplitCooldown();
        }
    }
    private void HookFastExtractinator(On_Player.orig_DropItemFromExtractinator orig, Player self, int itemType, int stack) {
        orig(self, itemType, stack);
        if (Config.fastContainerOpening) self.itemTime = self.itemTimeMax = self.itemTime/5;
    }

    private static void HookOpenChest(On_Player.orig_OpenChest orig, Player self, int x, int y, int newChest) {
        foreach (Item item in self.Chest(newChest)) if (!item.IsAir) self.GetModPlayer<BetterPlayer>().VisibilityFilters.AddOwnedItems(item);
        orig(self, x, y, newChest);
    }

    private static void ILKeepFavoriteInChest(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.MatchStfld(Reflection.Item.favorited));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdarg2();
        cursor.EmitDelegate((bool fav, Item[] inv, int context, int slot) => {
            if(context == ItemSlot.Context.BankItem) fav = inv[slot].favorited;
            return fav;
        });
    }
    private static void HookRestock(On_ChestUI.orig_Restock orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && Config.favoriteInBanks) Utility.RunWithHiddenItems(items, i => i.favorited, () => orig());
        else orig();
    }
    private static void HookLootAll(On_ChestUI.orig_LootAll orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (!sync && Config.favoriteInBanks) Utility.RunWithHiddenItems(items, i => i.favorited, () => orig());
        else orig();
    }


    public static Item GetItem_Inner(Player self, int plr, Item newItem, GetItemSettings settings) {
        innerGetItem = true;
        Item i = self.GetItem(plr, newItem, settings);
        innerGetItem = false;
        return i;
    }
    private static Item HookGetItem(On_Player.orig_GetItem orig, Player self, int plr, Item newItem, GetItemSettings settings) {
        if (innerGetItem) return orig(self, plr, newItem, settings);

        if (Config.smartPickup != Configs.InventoryManagement.SmartPickupLevel.Off) {
            newItem = SmartPickup.SmartGetItem(self, newItem, settings);
            if (newItem.IsAir) return new();
        }

        self.GetModPlayer<BetterPlayer>().VisibilityFilters.AddOwnedItems(newItem);
         if (!settings.NoText && Config.autoEquip != Configs.InventoryManagement.AutoEquipLevel.Off) {
            foreach (ModSubInventory slots in InventoryLoader.GetInventories(newItem, Config.autoEquip == Configs.InventoryManagement.AutoEquipLevel.MainSlots ? SubInventoryType.RightClickTarget : SubInventoryType.WithCondition).ToArray()) {
                newItem = slots.GetItem(self, newItem, settings);
                if (newItem.IsAir) return new();
            }
        }
        return orig(self, plr, newItem, settings);
    }


    public static void CycleAccState(Player player, int index, int cycle = 2) => player.builderAccStatus[index] = (player.builderAccStatus[index] + 1) % cycle;
    public static void FavoritedBuff(Player player) => Utility.RunWithHiddenItems(player.inventory, i => !i.favorited, player.QuickBuff);

    public override void SaveData(TagCompound tag) {
        tag[VisibilityTag] = VisibilityFilters;
        tag[RecipesTag] = RecipeFilters;
        if (!Guide.guideTile.IsAir) tag[GuideTileTag] = Guide.guideTile;
    }

    public override void LoadData(TagCompound tag) {
        VisibilityFilters = tag.Get<VisibilityFilters>(VisibilityTag);
        if (tag.TryGet(GuideTileTag, out Item guide)) Guide.guideTile = guide;
        RecipeFilters = tag.Get<Crafting.RecipeFilters>(RecipesTag);
    }

    public Crafting.RecipeFilters RecipeFilters { get; set; } = null!;
    public VisibilityFilters VisibilityFilters { get; set; } = new();

    private static bool innerGetItem;

    public const string VisibilityTag = "visibility";
    public const string RecipesTag = "recipes";
    public const string GuideTileTag = "guideTile";
}