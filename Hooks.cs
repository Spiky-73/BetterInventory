using System;
using System.Runtime.CompilerServices;
using BetterInventory.Configs.UI;
using BetterInventory.Crafting;
using BetterInventory.InventoryManagement;
using BetterInventory.ItemSearch;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;

namespace BetterInventory;

public class Hooks : ILoadable {
    public static Mod Mod { get; private set; } = null!;
    public void Load(Mod mod) {
        Mod = mod;
        UnloadedCrafting = new();
        UnloadedInventoryManagement = new();
        UnloadedItemSearch = new();
        IL_Player.GetItem += ILGetItem;
        IL_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += ILPickAndConsumeBait;
        
        IL_Main.DrawInventory += ILDrawInventory;
        IL_Main.CraftItem += ILCraftItem;
        IL_Main.HoverOverCraftingItemButton += ILHoverOverCraftingItemButton;

        IL_Recipe.FindRecipes += ILFindRecipes;
        IL_Recipe.CollectGuideRecipes += ILOverrideGuideRecipes;
        IL_Recipe.ConsumeForCraft += ILConsumeForCraft;

        IL_ItemSlot.LeftClick_ItemArray_int_int += ILLeftClick;
        IL_ItemSlot.HandleShopSlot += ILHandleShopSlot;
        IL_ItemSlot.SellOrTrash += ILSellOrTrash;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += ILDrawSlot;

        MonoModHooks.Modify(Reflection.ConfigElement.DrawSelf, IlDrawSelf);

        IL_Filters.BySearch.FitsFilter += ILFilters_BySearch;
        IL_UIBestiaryEntryIcon.Update += ILEntryIcon_Update;
        IL_UIBestiaryEntryIcon.DrawSelf += ILEntryIcon_DrawSelf;
        IL_UIBestiaryEntryInfoPage.AddInfoToList += IlEntryPage_AddInfoToList;
        IL_UIBestiaryFilteringOptionsGrid.UpdateAvailability += ILFilteringOptionsGrid_UpdateAvailability;
    }
    public void Unload() {
        Mod = null!;
    }

    private void ILGetItem(ILContext il) {
        if (!ApplyIL(il, SmartPickup.ILSmartPickup, Configs.SmartPickup.Enabled())) UnloadedInventoryManagement.smartPickup = true;
        if (!ApplyIL(il, SmartPickup.ILAutoEquip, Configs.InventoryManagement.AutoEquip)) UnloadedInventoryManagement.autoEquip = true;
    }
    private void ILPickAndConsumeBait(ILContext il){
        if (!ApplyIL(il, SmartConsumptionItem.ILOnConsumeBait, Configs.SmartConsumption.Baits)) UnloadedInventoryManagement.baits = true;
    }

    private void ILDrawInventory(ILContext il) {
        if (!ApplyIL(il, RecipeFiltering.ILDrawFilters, Configs.RecipeFiltering.Enabled)) UnloadedCrafting.recipeFiltering = true;
        if (!ApplyIL(il, FixedUI.ILFastScroll, Configs.FixedUI.FastScroll)) UnloadedCrafting.fastScroll = true;
        if (!ApplyIL(il, FixedUI.ILListScrollFix, Configs.FixedUI.ListScroll)) UnloadedCrafting.listScroll = true;
        if (!ApplyIL(il, FixedUI.ILMaterialWrapping, Configs.FixedUI.Wrapping)) UnloadedCrafting.wrapping = true;
        if (!ApplyIL(il, Crafting.Crafting.ILCraftOnList, Configs.CraftOnList.Enabled)) UnloadedCrafting.craftOnList = true;
        if (!ApplyIL(il, SearchItem.ILForceGuideDisplay, Configs.ItemSearch.SearchRecipes)) UnloadedItemSearch.searchRecipes = true;
        if (!ApplyIL(il, Guide.ILDrawVisibility, Configs.BetterGuide.CraftInMenu)) UnloadedItemSearch.guideCraftInMenu = true;
        if (!ApplyIL(il, Guide.ILCustomDrawCreateItem, Configs.BetterGuide.AvailablesRecipes)) UnloadedItemSearch.GuideAvailablesRecipes = true;
        if (!ApplyIL(il, Guide.ILCustomDrawMaterials, Configs.BetterGuide.AvailablesRecipes)) UnloadedItemSearch.GuideAvailablesRecipes = true;
        if (!ApplyIL(il, Guide.ILCustomDrawRecipeList, Configs.BetterGuide.AvailablesRecipes)) UnloadedItemSearch.GuideAvailablesRecipes = true;
    }
    private void ILHoverOverCraftingItemButton(ILContext il) {
        if (!ApplyIL(il, Guide.ILFavoriteRecipe, Configs.BetterGuide.AvailablesRecipes)) UnloadedItemSearch.GuideAvailablesRecipes = true;
        if (!ApplyIL(il, Guide.ILCraftInGuideMenu, Configs.BetterGuide.CraftInMenu)) UnloadedItemSearch.guideCraftInMenu = true;
        if (!ApplyIL(il, ClickOverrides.ILShiftRightCursorOverride, Configs.InventoryManagement.ShiftRight)) UnloadedInventoryManagement.shiftRight = true;
    }
    private void ILCraftItem(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILShiftCraft, Configs.InventoryManagement.ShiftRight)) UnloadedInventoryManagement.shiftRight = true;
        if (!ApplyIL(il, ClickOverrides.ILCraftStack, Configs.InventoryManagement.ClickOverrides)) UnloadedInventoryManagement.ClickOverrides = true;
        if (!ApplyIL(il, Guide.IlUnfavoriteOnCraft, Configs.FavoriteRecipes.UnfavoriteOnCraft)) UnloadedItemSearch.guideUnfavoriteOnCraft = true;
        if (!ApplyIL(il, ClickOverrides.ILFixCraftMouseText, Configs.InventoryManagement.ClickOverrides)) UnloadedInventoryManagement.ClickOverrides = true;
        if (!ApplyIL(il, ClickOverrides.ILFixCraftMouseText, Configs.CraftStack.Enabled)) UnloadedInventoryManagement.craftStack = true;
    }

    private void ILFindRecipes(ILContext il){
        if (!ApplyIL(il, Guide.ILSkipGuideRecipes, Configs.BetterGuide.AvailablesRecipes)) UnloadedItemSearch.GuideAvailablesRecipes = true;
        if (!ApplyIL(il, Guide.ILUpdateOwnedItems, Configs.BetterGuide.AvailablesRecipes)) UnloadedItemSearch.GuideAvailablesRecipes = true;
    }
    private void ILOverrideGuideRecipes(ILContext il){
        if (!ApplyIL(il, Guide.ILMoreGuideRecipes, Configs.BetterGuide.AnyItem)) UnloadedItemSearch.guideAnyItem = true;
        if (!ApplyIL(il, Guide.ILForceAddToAvailable, Configs.BetterGuide.AvailablesRecipes || Configs.BetterGuide.Tile || Configs.RecipeFiltering.Enabled)) UnloadedItemSearch.GuideAvailablesRecipes = UnloadedItemSearch.guideAnyItem = UnloadedCrafting.recipeFiltering = true;
        if (!ApplyIL(il, Guide.ILGuideRecipeOrder, Configs.BetterGuide.AvailablesRecipes)) UnloadedItemSearch.GuideAvailablesRecipes = true;
    }
    private void ILConsumeForCraft(ILContext il) {
        if (!ApplyIL(il, SmartConsumptionItem.ILOnConsumedMaterial, Configs.SmartConsumption.Materials)) UnloadedInventoryManagement.materials = true;
    }


    private void ILLeftClick(ILContext il) {
        if (!ApplyIL(il, ClickOverrides.ILKeepFavoriteInBanks, Configs.InventoryManagement.FavoriteInBanks)) UnloadedInventoryManagement.favoriteInBanks = true;
    }
    private void ILHandleShopSlot(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILPreventChainBuy, Configs.CraftStack.Enabled)) UnloadedInventoryManagement.craftStack = true;
        if (!ApplyIL(il, ClickOverrides.ILBuyStack, Configs.InventoryManagement.ClickOverrides)) UnloadedInventoryManagement.ClickOverrides = true;
    }
    private void ILSellOrTrash(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILStackTrash, Configs.InventoryManagement.StackTrash)) UnloadedInventoryManagement.stackTrash = true;
    }
    private void ILDrawSlot(ILContext il) {
        if (!ApplyIL(il, SmartPickup.ILDrawMarks, Configs.SmartPickup.Marks)) UnloadedInventoryManagement.marks = true;
        if (!ApplyIL(il, QuickMove.ILHighlightSlot, Configs.QuickMove.Hightlight)) UnloadedInventoryManagement.quickMoveHightlight = true;
        if (!ApplyIL(il, QuickMove.ILDisplayHotkey, Configs.QuickMove.DisplayHotkeys)) UnloadedInventoryManagement.quickMoveDisplay = true;
    }

    private void ILFilters_BySearch(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILSearchAddEntries, Configs.BetterBestiary.DisplayedUnlock)) UnloadedItemSearch.bestiaryDisplayedUnlock = true;
    }
    private void ILEntryIcon_Update(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILIconUpdateFakeUnlock, Configs.BetterBestiary.Unlock)) UnloadedItemSearch.BestiaryUnlock = true;
    }
    private void ILEntryIcon_DrawSelf(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILIconDrawFakeUnlock, Configs.BetterBestiary.Progression)) UnloadedItemSearch.bestiaryProgression = true;
    }
    private void IlEntryPage_AddInfoToList(ILContext il) {
        if (!ApplyIL(il, Bestiary.IlEntryPageFakeUnlock, Configs.BetterBestiary.Unlock)) UnloadedItemSearch.BestiaryUnlock = true;
    }
    private void ILFilteringOptionsGrid_UpdateAvailability(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILFakeUnlockFilters, Configs.BetterBestiary.Progression)) UnloadedItemSearch.bestiaryProgression = true;
    }

    private void IlDrawSelf(ILContext il) => ApplyIL(il, Text.ILTextColors, true);

    public static bool ApplyIL(ILContext context, Action<ILContext> ilEdit, bool enabled, [CallerArgumentExpression(nameof(ilEdit))] string name = "") {
        if (Configs.Compatibility.CompatibilityMode && !enabled) {
            Mod.Logger.Info($"{name} was not loaded. Related features will be disabled until reload");
            return false;
        }
        try { 
            ilEdit(context); 
        } catch (Exception) {
            MonoModHooks.DumpIL(Mod, context);
            Mod.Logger.Warn($"{name} failled to load. Related features will be disabled until reload");
            return false;
        }
        return true;
    }

    public static UnloadedCrafting UnloadedCrafting {get; private set; } = new();
    public static UnloadedInventoryManagement UnloadedInventoryManagement {get; private set; } = new();
    public static UnloadedItemSearch UnloadedItemSearch {get; private set; } = new();
}

public sealed class UnloadedCrafting {
    public bool fastScroll = false;
    public bool listScroll = false;
    public bool wrapping = false;
    public bool recipeFiltering = false;
    public bool craftOnList = false;
}

public sealed class UnloadedInventoryManagement {
    public bool autoEquip = false;
    public bool favoriteInBanks = false;
    public bool shiftRight = false;
    public bool stackTrash = false;
    public bool baits = false;
    public bool materials = false;
    public bool smartPickup = false;
    public bool marks = false;
    public bool quickMoveDisplay = false;
    public bool quickMoveHightlight = false;
    public bool craftStack = false;

    public bool ClickOverrides {
        set { craftStack = value; shiftRight = value;}
    }
}

public sealed class UnloadedItemSearch {
    public bool searchRecipes = false;
    public bool searchDrops = false;
    public bool guideAnyItem = false;
    public bool guideFavorite = false;
    public bool guideUnfavoriteOnCraft = false;
    public bool guideCraftInMenu = false;
    public bool guideProgression = false;
    public bool bestiaryProgression = false;
    public bool bestiaryDisplayedUnlock = false;

    public bool BestiaryUnlock {
        set { bestiaryProgression = value; bestiaryDisplayedUnlock = value; }
    }

    public bool GuideAvailablesRecipes {
        set { guideFavorite = value; guideCraftInMenu = value; guideProgression = value; }
    }
}