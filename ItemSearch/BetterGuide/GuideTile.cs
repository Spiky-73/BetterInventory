using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BetterInventory.Default.Catalogues;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch.BetterGuide;

public sealed class GuideTileFakeItemContext : IFakeItemContext {
    public bool IsFake(Item item) => item.IsAPlaceholder();

    public bool IsHovered(Item[] inv, int context, int slot) => Configs.BetterGuide.GuideTile && context == ContextID.GuideItem && slot == 1;

    public bool WouldMoveToContext(Item[] inv, int context, int slot, [MaybeNullWhen(false)] out Item destination) {
        destination = null;
        if(!Configs.BetterGuide.GuideTile || !Main.InGuideCraftMenu || Main.cursorOverride != CursorOverrideID.InventoryToChest) return false;
        destination = GuideTilePlayer.GetGuideContextDestination(inv[slot], out var guideSlot);
        return guideSlot == 1;
    }
}

public sealed class GuideTilePlayer : ModPlayer {

    public static ref Item GetGuideContextDestination(Item item, out int guideSlot) {
        if (item.IsAir) {
            guideSlot = 0;
            return ref Main.guideItem;
        }
        guideSlot = GuideTile.IsCraftingStation(item) || (!Configs.BetterGuide.MoreRecipes && PlaceholderItem.ConditionItems.ContainsValue(item.type)) ? 1 : 0;
        Item[] guideItems = GuideTile.GuideItems;
        if (ItemSlot.PickItemMovementAction(guideItems, ContextID.GuideItem, 0, item) != -1 && ItemSlot.PickItemMovementAction(guideItems, ContextID.GuideItem, 1, item) != -1) {
            if(guideSlot == 0 && PlaceholderHelper.AreSame(item, Main.guideItem)) guideSlot = 1;
            else if(guideSlot == 1 && PlaceholderHelper.AreSame(item, GuideTile.guideTile)) guideSlot = 0;
        }
        if (guideSlot == 0) return ref Main.guideItem;
        return ref GuideTile.guideTile;
    }

    public override void Load() {
        On_ItemSlot.OverrideLeftClick += HookOverrideTileClick;
        On_ItemSlot.PickItemMovementAction += HookPickItemMovementAction;
        On_Player.dropItemCheck += HookDropItems;

        PlaceholderItem.AddFakeItemContext(new GuideTileFakeItemContext());
    }

    public override void SaveData(TagCompound tag) {
        if (!GuideTile.guideTile.IsAir) tag[GuideTileTag] = GuideTile.guideTile;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(GuideTileTag, out Item guide)) _tempGuideTile = guide;
        else _tempGuideTile = new();
    }
    public override void OnEnterWorld() {
        GuideTile.guideTile = _tempGuideTile ?? new();
    }

    private int HookPickItemMovementAction(On_ItemSlot.orig_PickItemMovementAction orig, Item[] inv, int context, int slot, Item checkItem) {
        if(!Configs.BetterGuide.GuideTile || context != ContextID.GuideItem || slot != 1) return orig(inv, context, slot, checkItem);
        return checkItem.IsAir || FitsGuideTile(checkItem) ? 0 : -1;
    }

    public override bool HoverSlot(Item[] inventory, int context, int slot) {
        if(!Configs.BetterGuide.GuideTile || !ItemSlot.ShiftInUse) return false;
        if(context == 0 && Main.InGuideCraftMenu && FitsGuideTile(inventory[slot])) {
            Main.cursorOverride = CursorOverrideID.InventoryToChest;
            return true;
        }
        return false;
    }

    private static bool HookOverrideTileClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if(!Configs.BetterGuide.GuideTile) return orig(inv, context, slot);
        if(context == ContextID.GuideItem && slot == 1 && GuideTile.guideTile.IsAir && (Main.mouseItem.IsAir || ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == -1)) {
            inv[slot] = PlaceholderItem.FromTile(PlaceholderItem.ByHandTile);
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) {
            ref Item destination = ref GetGuideContextDestination(inv[slot], out var guideSlot);
            if (RecipeList.Instance.Enabled && FitsGuideTile(inv[slot])) {
                var items = GuideTile.GuideItems;
                void HandleDouble(int checkSlot) {
                    if (!RecipeList.searchedFromGuide && PlaceholderHelper.AreSame(inv[slot], items[checkSlot])) RecipeList.SearchPrevious(items, ContextID.GuideItem, checkSlot, true);
                }
                if (guideSlot == 1) HandleDouble(0);
                else if (guideSlot == 0) HandleDouble(1);
                GuideTile.GuideItems = items; // Update items if changed
            }
            if (guideSlot == 1) {
                (inv[slot], destination) = (destination, inv[slot]);
                SoundEngine.PlaySound(SoundID.Grab);
                Recipe.FindRecipes();
                return true;
            }
        }
        return orig(inv, context, slot);
    }

    internal const string GuideTileTag = "guideTile";

    internal static void DrawGuideTile(int inventoryX, int inventoryY) {
        float x = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + RequiredObjectsDisplay.TileSpacingRatio);
        float y = inventoryY;
        Main.inventoryScale *= RequiredObjectsDisplay.TileScale;
        Item[] items = GuideTile.GuideItems;

        // Handle Mouse hover
        Rectangle hitbox = new((int)x, (int)y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
        if (!Main.player[Main.myPlayer].mouseInterface && hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.player[Main.myPlayer].mouseInterface = true;
            Main.craftingHide = true;
            ItemSlot.OverrideHover(items, ContextID.GuideItem, 1);
            ItemSlot.LeftClick(items, ContextID.GuideItem, 1);
            GuideTile.GuideItems = items; // Update items if changed
            if (Main.mouseLeftRelease && Main.mouseLeft) Recipe.FindRecipes();
            ItemSlot.RightClick(items, ContextID.GuideItem, 1);
            ItemSlot.MouseHover(items, ContextID.GuideItem, 1);
            GuideTile.GuideItems = items; // Update items if changed
        }
        ItemSlot.Draw(Main.spriteBatch, items, ContextID.GuideItem, 1, hitbox.TopLeft());
        Main.inventoryScale /= RequiredObjectsDisplay.TileScale;
    }

    private static void HookDropItems(On_Player.orig_dropItemCheck orig, Player self) {
        bool old = Main.InGuideCraftMenu;
        if (RecipeList.Instance.Enabled) Main.InGuideCraftMenu = true;
        orig(self);
        dropGuideTileCheck(self);
        Main.InGuideCraftMenu = old;
    }
    public static void dropGuideTileCheck(Player self) {
        if (Main.InGuideCraftMenu || GuideTile.guideTile.IsAir) return;
        if (GuideTile.guideTile.IsAPlaceholder()) GuideTile.guideTile.TurnToAir();
        else self.GetDropItem(ref GuideTile.guideTile);
    }

    public static bool FitsGuideTile(Item item) => GuideTile.IsCraftingStation(item) || PlaceholderItem.ConditionItems.ContainsValue(item.type);

    internal Item? _tempGuideTile;
}

public sealed class GuideTile : ModSystem {

    public override void Load() {
        IL_Recipe.FindRecipes += static il => {
            if (!il.ApplyTo(ILGuideTileRecipes, Configs.BetterGuide.GuideTile)) Configs.UnloadedItemSearch.Value.guideTile = true;
        };

        _guideTileFilters = new(() => Configs.BetterGuide.GuideTile && !guideTile.IsAir, CheckGuideTileFilter);
        RecipeFiltering.AddFilter(_guideTileFilters);
    }
    public override void PostAddRecipes() {
        // Gather all used crafting stations
        for (int r = 0; r < Recipe.numRecipes; r++) {
            foreach (int tile in Main.recipe[r].requiredTile) CraftingStationsItems[tile] = 0;
        }

        // Try to map an item to each crafting station
        for (int type = 0; type < ItemLoader.ItemCount; type++) {
            Item item = new(type);
            if (CraftingStationsItems.TryGetValue(item.createTile, out int value) && value == ItemID.None) CraftingStationsItems[item.createTile] = item.type;
        }
    }

    public override void Unload() {
        guideTile = new();
        CraftingStationsItems.Clear();
    }

    public static bool IsCraftingStation(Item item) => CraftingStationsItems.ContainsKey(item.createTile) || item.IsAPlaceholder();

    private static void ILGuideTileRecipes(ILContext il) {
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.MatchCall(Reflection.Recipe.CollectGuideRecipes));
        cursor.GotoPrev(MoveType.After, i => i.MatchCallvirt(Reflection.Item.IsAir.GetMethod!));
        cursor.EmitDelegate((bool isAir) => isAir && (!Configs.BetterGuide.GuideTile || guideTile.IsAir));
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.Item.Name.GetMethod!));
        cursor.EmitDelegate((string name) => Configs.BetterGuide.GuideTile && guideTile.Name != "" ? guideTile.Name : name);
    }

    private static bool CheckGuideTileFilter(Recipe recipe) {
        if (guideTile.IsAir) return true;
        if (guideTile.TryGetGlobalItem(out PlaceholderItem placeholder)) {
            if (placeholder.tile == PlaceholderItem.ByHandTile) return recipe.requiredTile.Count == 0;
            if (placeholder.tile >= 0) return recipe.requiredTile.Contains(placeholder.tile);
            if (placeholder.condition is not null) return recipe.Conditions.Exists(c => c.Description.Key == placeholder.condition);
        }

        return CraftingStationsItems.ContainsKey(guideTile.createTile) ?
            recipe.requiredTile.Contains(guideTile.createTile) :
            recipe.Conditions.Exists(c => PlaceholderItem.ConditionItems.TryGetValue(c.Description.Key, out int type) && type == guideTile.type);
    }

    public static Item[] GuideItems {
        get {
            (s_guideItems[0], s_guideItems[1]) = (Main.guideItem, guideTile);
            return s_guideItems;
        }
        set => (Main.guideItem, guideTile) = (value[0], value[1]);
    }
    private static readonly Item[] s_guideItems = new Item[2];
    public static Item guideTile = new();

    public static readonly Dictionary<int, int> CraftingStationsItems = []; // tile -> item

    private static GuideRecipeFilterGroup _guideTileFilters = null!;
}