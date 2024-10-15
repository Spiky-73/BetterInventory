using System;
using System.Collections.Generic;
using BetterInventory.Default.Catalogues;
using BetterInventory.InventoryManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
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

        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItem;
        On_ItemSlot.DrawItemIcon += HookDrawPlaceholder;
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookHideTooltip);

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
            if (!il.ApplyTo(ILMoreGuideRecipes, Configs.BetterGuide.AvailableRecipes || Configs.BetterGuide.MoreRecipes || Configs.BetterGuide.GuideTile))
                Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = Configs.UnloadedItemSearch.Value.guideMoreRecipes = Configs.BetterGuide.Value.craftingStation = true;
            if (!il.ApplyTo(ILGuideRecipeOrder, Configs.BetterGuide.RecipeOrdering)) Configs.UnloadedItemSearch.Value.GuideRecipeOrdering = true;
        };

        s_inventoryBack4 = TextureAssets.InventoryBack4;
        s_defaultTextures = new(TextureAssets.InventoryBack4, TextureAssets.InventoryBack14);
        s_favoriteTextures = new(TextureAssets.InventoryBack10, TextureAssets.InventoryBack17);
        s_blacklistedTextures = new(TextureAssets.InventoryBack5, TextureAssets.InventoryBack11);
        s_tileTextures = new(TextureAssets.InventoryBack3, TextureAssets.InventoryBack6);
        s_conditionTextures = new(TextureAssets.InventoryBack12, TextureAssets.InventoryBack8);
        s_inventoryTickBorder = Mod.Assets.Request<Texture2D>($"Assets/Inventory_Tick_Border");
        s_unknownTexture = Mod.Assets.Request<Texture2D>($"Assets/Unknown_Item");
    }

    public override void Unload() {
        s_inventoryBack4 = null!;
        CraftingStationsItems.Clear();
        ConditionItems.Clear();
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

        ConditionItems["Conditions.NearWater"] = ItemID.WaterBucket;
        ConditionItems["Conditions.NearLava"] = ItemID.LavaBucket;
        ConditionItems["Conditions.NearHoney"] = ItemID.HoneyBucket;
        ConditionItems["Conditions.InGraveyard"] = ItemID.Gravestone;
        ConditionItems["Conditions.InSnow"] = ItemID.SnowBlock;
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

        if (Configs.BetterGuide.CraftInMenu) HandleVisibility(inventoryX, inventoryY);

        if (Configs.BetterGuide.ConditionsDisplay) DrawRequiredTiles(inventoryX, inventoryY);
        else orig(adjY, craftingTipColor, out inventoryX, out inventoryY);

        if (Configs.BetterGuide.GuideTile) DrawGuideTile(inventoryX, inventoryY);
    }

    public static bool OverrideHover(Item[] inv, int context, int slot) {
        // Trash placeholder instead of moving them
        if (GetPlaceholderType(inv[slot]) != PlaceholderType.None
        && (ItemSlot.ShiftInUse || ItemSlot.ControlInUse || Main.mouseItem.IsAir || ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == -1)) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            return true;
        }

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

        if (context != ContextID.GuideItem) return orig(inv, context, slot);

        // Trash item if needed
        if ((Configs.BetterGuide.Enabled || RecipeList.Instance.Enabled) && Main.cursorOverride == CursorOverrideID.TrashCan) {
            inv[slot].TurnToAir();
            Recipe.FindRecipes();
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }

        if (!Configs.BetterGuide.Enabled) return orig(inv, context, slot);

        // Auto open recipe list
        if (!Main.mouseItem.IsAir && ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == 0) Main.recBigList = true;

        // Prevent placeholder pickup
        if (GetPlaceholderType(inv[slot]) != PlaceholderType.None) inv[slot].TurnToAir();
        // Allow by hand when clicking on guideTile
        else if (Configs.BetterGuide.GuideTile && slot == 1 && inv[slot].IsAir && !FitsCraftingTile(Main.mouseItem)) {
            inv[slot] = ByHandPlaceholder;
            Recipe.FindRecipes();
            SoundEngine.PlaySound(SoundID.Grab);
            return true;
        }
        return orig(inv, context, slot);
    }

    private static float HookDrawPlaceholder(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        if (s_hideNextItem) {
            return DrawTexture(spriteBatch, s_unknownTexture.Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit, environmentColor);
        }
        switch (GetPlaceholderType(item)) {
        case PlaceholderType.ByHand:
            Main.instance.LoadItem(ItemID.BoneGlove);
            return DrawTexture(spriteBatch, TextureAssets.Item[ItemID.BoneGlove].Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit, environmentColor);
        case PlaceholderType.Tile:
            GraphicsHelper.DrawTileFrame(spriteBatch, item.createTile, screenPositionForItemCenter, new Vector2(0.5f, 0.5f), scale);
            return scale;
        }
        return orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
    }
    private static float DrawTexture(SpriteBatch spriteBatch, Texture2D value, Color alpha, Vector2 screenPositionForItemCenter, ref float scale, float sizeLimit, Color environmentColor) {
        Rectangle frame = value.Frame(1, 1, 0, 0, 0, 0);
        if (frame.Width > sizeLimit || frame.Height > sizeLimit) scale *= (frame.Width <= frame.Height) ? (sizeLimit / frame.Height) : (sizeLimit / frame.Width);
        spriteBatch.Draw(value, screenPositionForItemCenter, new Rectangle?(frame), alpha, 0f, frame.Size() / 2f, scale, 0, 0f);
        return scale;
    }

    public static readonly Item Placeholder = new(ItemID.Lens);
    public const string ConditionMark = "@BI:";
    public const int ByHandCreateTile = -2;
    public static Item ByHandPlaceholder => new(Placeholder.type) { createTile = ByHandCreateTile };
    public static Item ConditionPlaceholder(Condition condition) => new(Placeholder.type) { BestiaryNotes = ConditionMark + condition.Description.Key };
    public static Item TilePlaceholder(int type) => new(Placeholder.type) { createTile = type };
    public static PlaceholderType GetPlaceholderType(Item item) {
        if (item.type != Placeholder.type || item.stack != 1) return PlaceholderType.None;
        if (item.createTile == ByHandCreateTile) return PlaceholderType.ByHand;
        if (item.createTile != -1) return PlaceholderType.Tile;
        if (item.BestiaryNotes?.StartsWith(ConditionMark) == true) return PlaceholderType.Condition;
        return PlaceholderType.None;
    }
    public static bool FitsCraftingTile(Item item) => IsCraftingStation(item) || ConditionItems.ContainsValue(item.type);
    public static bool IsCraftingStation(Item item) => CraftingStationsItems.ContainsKey(item.createTile) || GetPlaceholderType(item) != PlaceholderType.None;
    public static bool AreSame(Item item, Item other) {
        PlaceholderType a = GetPlaceholderType(item);
        PlaceholderType b = GetPlaceholderType(other);
        return a == b && (a switch {
            PlaceholderType.ByHand => true,
            PlaceholderType.Tile => item.createTile == other.createTile,
            PlaceholderType.Condition => item.BestiaryNotes == other.BestiaryNotes,
            PlaceholderType.None or _ => item.type == other.type,
        });
    }

    public static Item[] GuideItems {
        get {
            (s_guideItems[0], s_guideItems[1]) = (Main.guideItem, guideTile);
            return s_guideItems;
        }
        set => (Main.guideItem, guideTile) = (value[0], value[1]);
    }
    private static readonly Item[] s_guideItems = new Item[2];

    public static readonly Dictionary<int, int> CraftingStationsItems = []; // tile -> item
    public static readonly Dictionary<string, int> ConditionItems = []; // description -> id
}

public enum PlaceholderType { None, ByHand, Tile, Condition }

public record class TextureHighlight(Asset<Texture2D> Default, Asset<Texture2D> Highlight);