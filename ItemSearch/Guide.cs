using System;
using System.Collections.Generic;
using BetterInventory.Default.Catalogues;
using BetterInventory.InventoryManagement;
using BetterInventory.ItemActions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;


public sealed partial class Guide : ModSystem {

    public override void Load() {
        On_Main.DrawGuideCraftText += HookGuideCraftText;
        On_Main.HoverOverCraftingItemButton += HookDisableWhenNonAvailable;

        On_Recipe.ClearAvailableRecipes += HookClearRecipes;
        On_Recipe.FindRecipes += HookFindRecipes;
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;

        On_Player.AdjTiles += HookGuideTileAdj;
        On_ItemSlot.PickItemMovementAction += HookAllowGuideItem;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;

        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILDrawVisibility, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
            if (!il.ApplyTo(ILCustomDrawCreateItem, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
            if (!il.ApplyTo(ILCustomDrawMaterials, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
            if (!il.ApplyTo(ILCustomDrawRecipeList, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        };
        IL_Main.CraftItem += static il => {
            if (!il.ApplyTo(IlUnfavoriteOnCraft, Configs.FavoritedRecipes.UnfavoriteOnCraft)) Configs.UnloadedItemSearch.Value.guideUnfavoriteOnCraft = true;
        };
        IL_Main.HoverOverCraftingItemButton += static il => {
            if (!il.ApplyTo(ILFavoriteRecipe, Configs.BetterGuide.RecipeOrdering)) Configs.UnloadedItemSearch.Value.GuideRecipeOrdering = true;
            if (!il.ApplyTo(ILCraftInGuideMenu, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        };
        IL_Recipe.FindRecipes += static il => {
            if (!il.ApplyTo(ILSkipGuideRecipes, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        };
        IL_Recipe.CollectGuideRecipes += static il => {
            if (!il.ApplyTo(ILGuideRecipeOrder, Configs.BetterGuide.RecipeOrdering)) Configs.UnloadedItemSearch.Value.GuideRecipeOrdering = true;
        };

        s_inventoryBack4 = TextureAssets.InventoryBack4;
        s_defaultTextures = new(TextureAssets.InventoryBack4, TextureAssets.InventoryBack14);
        s_favoriteTextures = new(TextureAssets.InventoryBack10, TextureAssets.InventoryBack17);
        s_blacklistedTextures = new(TextureAssets.InventoryBack5, TextureAssets.InventoryBack11);
        s_tileTextures = new(TextureAssets.InventoryBack3, TextureAssets.InventoryBack6);
        s_conditionTextures = new(TextureAssets.InventoryBack12, TextureAssets.InventoryBack8);
        s_inventoryTickBorder = Mod.Assets.Request<Texture2D>($"Assets/Inventory_Tick_Border");

        recipeUI = new();
        recipeUI.Activate();
        recipeInterface = new();
        recipeInterface.SetState(recipeUI);

        _guideTileFilters = new(() => Configs.BetterGuide.GuideTile && !guideTile.IsAir, r => CheckGuideTileFilter(r));
        GuideRecipeFiltering.AddFilter(_guideTileFilters);
        
        GuideRecipeFiltering.AddGuideItemFilter(r => Configs.BetterGuide.MoreRecipes && r.HasResult(Main.guideItem.type));

        _availableRecipesFilters = new(() => Configs.BetterGuide.AvailableRecipes && !ShowAllRecipes(), r => IsAvailable(r.RecipeIndex), r => LocalFilters.IsFavorited(r.RecipeIndex));
        GuideRecipeFiltering.AddFilter(_availableRecipesFilters);
    }

    public override void Unload() {
        s_inventoryBack4 = null!;
        CraftingStationsItems.Clear();
        guideTile = new();
        s_dispGuide = new();
        s_dispTile = new();
    }

    public override void ClearWorld() {
        SmartPickup.ClearMarks();
    }

    public override void PostAddRecipes() {
        Default.Catalogues.Bestiary.HooksBestiaryUI();
        FindCraftingStations();
    }
    public static void FindCraftingStations() {
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

    private static void HookGuideCraftText(On_Main.orig_DrawGuideCraftText orig, int adjY, Color craftingTipColor, out int inventoryX, out int inventoryY) {
        // Main.guideItem's positions
        inventoryX = 73;
        inventoryY = 331 + adjY;

        if (Configs.RecipeTooltip.Enabled) RequiredTooltipItem.OnDrawGuideCraftText();

        if (Configs.BetterGuide.CraftInMenu) HandleVisibility(inventoryX, inventoryY);

        if (Configs.BetterGuide.ConditionsDisplay) DrawRequiredTiles(inventoryX, inventoryY);
        else orig(adjY, craftingTipColor, out inventoryX, out inventoryY);

        if (Configs.BetterGuide.GuideTile) DrawGuideTile(inventoryX, inventoryY);
    }

    public static bool OverrideHover(Item[] inv, int context, int slot) {
        // Allow any item into guideItem -including from ammo / coin- slots if it could be placed in it
        if (Configs.BetterGuide.MoreRecipes && ItemSlot.ShiftInUse && !inv[slot].favorited
        && Main.InGuideCraftMenu && Array.IndexOf(PlayerHelper.InventoryContexts, context) != -1 && !inv[slot].IsAir
        && ItemSlot.PickItemMovementAction(inv, ContextID.GuideItem, 0, inv[slot]) == 0) {
            Main.cursorOverride = CursorOverrideID.InventoryToChest;
            return true;
        }
        return false;
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        // Replace ShiftClick by a LeftClick
        if ((Configs.BetterGuide.Enabled || RecipeList.Instance.Enabled) && Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) {
            (Item mouse, Main.mouseItem, inv[slot]) = (Main.mouseItem, inv[slot], new());
            (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);

            Item[] items = GuideItems;
            ItemSlot.LeftClick(items, ContextID.GuideItem, Configs.BetterGuide.GuideTile && IsCraftingStation(Main.mouseItem) ? 1 : 0);
            GuideItems = items;

            // Emulates ShiftClick swap
            if (Configs.BetterGuide.Enabled && !RecipeList.Instance.Enabled) Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            else inv[slot] = Main.mouseItem;

            Main.mouseItem = mouse;
            Main.cursorOverride = cursor;
            return true;
        }

        if (context != ContextID.GuideItem || !Configs.BetterGuide.Enabled) return orig(inv, context, slot);

        // Auto open recipe list
        if (!Main.mouseItem.IsAir && ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == 0) Main.recBigList = true;

        bool res = orig(inv, context, slot);
        if (res || !Configs.BetterGuide.GuideTile || slot != 1 || !inv[slot].IsAir || FitsCraftingTile(Main.mouseItem)) return res;

        // Allow by hand when clicking on guideTile
        inv[slot] = PlaceholderItem.FromTile(PlaceholderItem.ByHandTile);
        Recipe.FindRecipes();
        SoundEngine.PlaySound(SoundID.Grab);
        Main.recBigList = true;
        return true;
    }

    public static bool FitsCraftingTile(Item item) => IsCraftingStation(item) || PlaceholderItem.ConditionItems.ContainsValue(item.type);
    public static bool IsCraftingStation(Item item) => CraftingStationsItems.ContainsKey(item.createTile) || item.IsAPlaceholder();

    public static void SaveData(BetterPlayer player, TagCompound tag) {
        if (!guideTile.IsAir) tag[GuideTileTag] = guideTile;
    }
    public static void LoadData(BetterPlayer player, TagCompound tag) {
        if (tag.TryGet(GuideTileTag, out Item guide)) player._tempGuideTile = guide;
        else player._tempGuideTile = new();
    }
    internal static void SetGuideItem(BetterPlayer player) {
        if (player._tempGuideTile is not null) guideTile = player._tempGuideTile;
    }

    public const string GuideTileTag = "guideTile";


    public static Item[] GuideItems {
        get {
            (s_guideItems[0], s_guideItems[1]) = (Main.guideItem, guideTile);
            return s_guideItems;
        }
        set => (Main.guideItem, guideTile) = (value[0], value[1]);
    }
    private static readonly Item[] s_guideItems = new Item[2];

    public static readonly Dictionary<int, int> CraftingStationsItems = []; // tile -> item
}

public enum PlaceholderType { None, ByHand, Tile, Condition }

public record class TextureHighlight(Asset<Texture2D> Default, Asset<Texture2D> Highlight);