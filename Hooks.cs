using System;
using System.Runtime.CompilerServices;
using BetterInventory.Crafting;
using BetterInventory.InventoryManagement;
using BetterInventory.ItemSearch;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory;

public class Hooks : ILoadable {
    public static Mod Mod { get; private set; } = null!;
    public void Load(Mod mod) {
        Mod = mod;
        IL_Player.GetItem += ILGetItem;
        IL_Player.ItemCheck_CheckFishingBobber_PickAndConsumeBait += ILPickAndConsumeBait;
        IL_Player.PayCurrency += ILPayCurrency;
        
        IL_Main.DrawInventory += ILDrawInventory;
        IL_Main.CraftItem += ILCraftItem;
        IL_Main.HoverOverCraftingItemButton += ILHoverOverCraftingItemButton;

        IL_Recipe.FindRecipes += ILFindRecipes;
        IL_Recipe.CollectGuideRecipes += ILOverrideGuideRecipes;
        IL_Recipe.ConsumeForCraft += ILConsumeForCraft;
        IL_Recipe.Create += ILCreate;

        IL_ItemSlot.LeftClick_ItemArray_int_int += ILLeftClick;
        IL_ItemSlot.HandleShopSlot += ILHandleShopSlot;
        IL_ItemSlot.SellOrTrash += ILSellOrTrash;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += ILDrawSlot;

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
        if (!ApplyIL(il, SmartPickup.ILSmartPickup, Configs.SmartPickup.Enabled)) Configs.UnloadedInventoryManagement.Value.smartPickup = true;
        if (!ApplyIL(il, SmartPickup.ILAutoEquip, Configs.AutoEquip.Enabled)) Configs.UnloadedInventoryManagement.Value.autoEquip = true;
        if (!ApplyIL(il, SmartPickup.ILHotbarLast, Configs.InventoryManagement.HotbarLast)) Configs.UnloadedInventoryManagement.Value.hotbarLast = true;
    }
    private void ILPickAndConsumeBait(ILContext il){
        if (!ApplyIL(il, SmartConsumptionItem.ILOnConsumeBait, Configs.SmartConsumption.Baits)) Configs.UnloadedInventoryManagement.Value.baits = true;
    }
    private void ILPayCurrency(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILPayStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
    }

    private void ILDrawInventory(ILContext il) {
        if (!ApplyIL(il, RecipeFiltering.ILDrawFilters, Configs.RecipeFilters.Enabled)) Configs.UnloadedCrafting.Value.recipeFilters = true;
        if (!ApplyIL(il, FixedUI.ILFastScroll, Configs.FixedUI.FastScroll)) Configs.UnloadedCrafting.Value.fastScroll = true;
        if (!ApplyIL(il, FixedUI.ILListScrollFix, Configs.FixedUI.ListScroll)) Configs.UnloadedCrafting.Value.listScroll = true;
        if (!ApplyIL(il, FixedUI.ILMaterialWrapping, Configs.FixedUI.Wrapping)) Configs.UnloadedCrafting.Value.wrapping = true;
        if (!ApplyIL(il, Crafting.Crafting.ILCraftOnList, Configs.CraftOnList.Enabled)) Configs.UnloadedCrafting.Value.craftOnList = true;
        if (!ApplyIL(il, Default.SearchProviders.RecipeList.ILForceGuideDisplay, Default.SearchProviders.RecipeList.Instance.Enabled)) Configs.UnloadedItemSearch.Value.searchRecipes = true;
        if (!ApplyIL(il, Guide.ILDrawVisibility, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        if (!ApplyIL(il, Guide.ILCustomDrawCreateItem, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        if (!ApplyIL(il, Guide.ILCustomDrawMaterials, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        if (!ApplyIL(il, Guide.ILCustomDrawRecipeList, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
    }
    private void ILHoverOverCraftingItemButton(ILContext il) {
        if (!ApplyIL(il, Guide.ILFavoriteRecipe, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        if (!ApplyIL(il, Guide.ILCraftInGuideMenu, Configs.BetterGuide.CraftInMenu)) Configs.UnloadedItemSearch.Value.guideCraftInMenu = true;
        if (!ApplyIL(il, ClickOverrides.ILShiftRightCursorOverride, Configs.InventoryManagement.ShiftRight)) Configs.UnloadedInventoryManagement.Value.shiftRight = true;
    }
    private void ILCraftItem(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILShiftCraft, Configs.InventoryManagement.ShiftRight)) Configs.UnloadedInventoryManagement.Value.shiftRight = true;
        if (!ApplyIL(il, ClickOverrides.ILCraftMultiplier, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        if (!ApplyIL(il, ClickOverrides.ILCraftStackAndPickup, Configs.CraftStack.Enabled || Configs.InventoryManagement.ShiftRight)) Configs.UnloadedInventoryManagement.Value.craftStack = Configs.UnloadedInventoryManagement.Value.shiftRight = true;
        if (!ApplyIL(il, ClickOverrides.ILCraftFixMouseText, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        if (!ApplyIL(il, Guide.IlUnfavoriteOnCraft, Configs.FavoriteRecipes.UnfavoriteOnCraft)) Configs.UnloadedItemSearch.Value.guideUnfavoriteOnCraft = true;
    }

    private void ILFindRecipes(ILContext il){
        if (!ApplyIL(il, Guide.ILSkipGuideRecipes, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
        if (!ApplyIL(il, Guide.ILUpdateOwnedItems, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
    }
    private void ILOverrideGuideRecipes(ILContext il){
        if (!ApplyIL(il, Guide.ILMoreGuideRecipes, Configs.BetterGuide.MoreRecipes)) Configs.UnloadedItemSearch.Value.guideMoreRecipes = true;
        if (!ApplyIL(il, Guide.ILForceAddToAvailable, Configs.BetterGuide.AvailableRecipes || Configs.BetterGuide.Tile || Configs.RecipeFilters.Enabled)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = Configs.UnloadedItemSearch.Value.guideMoreRecipes = Configs.UnloadedCrafting.Value.recipeFilters = true;
        if (!ApplyIL(il, Guide.ILGuideRecipeOrder, Configs.BetterGuide.AvailableRecipes)) Configs.UnloadedItemSearch.Value.GuideAvailableRecipes = true;
    }
    private void ILConsumeForCraft(ILContext il) {
        if (!ApplyIL(il, SmartConsumptionItem.ILOnConsumedMaterial, Configs.SmartConsumption.Materials)) Configs.UnloadedInventoryManagement.Value.materials = true;
    }
    private void ILCreate(ILContext il) {
        if (!ApplyIL(il, ClickOverrides.ILRecipeConsumeStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
    }

    private void ILLeftClick(ILContext il) {
        if (!ApplyIL(il, ClickOverrides.ILKeepFavoriteInBanks, Configs.InventoryManagement.FavoriteInBanks)) Configs.UnloadedInventoryManagement.Value.favoriteInBanks = true;
    }
    private void ILHandleShopSlot(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILPreventChainBuy, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        if (!ApplyIL(il, ClickOverrides.ILBuyMultiplier, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        if (!ApplyIL(il, ClickOverrides.ILBuyStack, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        if (!ApplyIL(il, ClickOverrides.ILRestoreShopItem, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
    }
    private void ILSellOrTrash(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILStackTrash, Configs.InventoryManagement.StackTrash)) Configs.UnloadedInventoryManagement.Value.stackTrash = true;
    }
    private void ILDrawSlot(ILContext il) {
        if (!ApplyIL(il, SmartPickup.ILDrawFakeItem, Configs.MarksDisplay.FakeItem)) Configs.UnloadedInventoryManagement.Value.fakeItem = true;
        if (!ApplyIL(il, SmartPickup.ILDrawIcon, Configs.MarksDisplay.Icon)) Configs.UnloadedInventoryManagement.Value.marksIcon = true;
        if (!ApplyIL(il, QuickMove.ILHighlightSlot, Configs.QuickMove.Highlight)) Configs.UnloadedInventoryManagement.Value.quickMoveHighlight = true;
        if (!ApplyIL(il, QuickMove.ILDisplayHotkey, Configs.QuickMove.DisplayHotkeys)) Configs.UnloadedInventoryManagement.Value.quickMoveHotkeys = true;
        if (!ApplyIL(il, ClickOverrides.ILFavoritedBankBackground, Configs.InventoryManagement.FavoriteInBanks)) Configs.UnloadedInventoryManagement.Value.favoriteInBanks = true;
    }

    private void ILFilters_BySearch(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILSearchAddEntries, Configs.BetterBestiary.DisplayedUnlock)) Configs.UnloadedItemSearch.Value.bestiaryDisplayedUnlock = true;
    }
    private void ILEntryIcon_Update(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILIconUpdateFakeUnlock, Configs.BetterBestiary.Unlock)) Configs.UnloadedItemSearch.Value.BestiaryUnlock = true;
    }
    private void ILEntryIcon_DrawSelf(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILIconDrawFakeUnlock, Configs.BetterBestiary.UnknownDisplay)) Configs.UnloadedItemSearch.Value.bestiaryUnknown = true;
    }
    private void IlEntryPage_AddInfoToList(ILContext il) {
        if (!ApplyIL(il, Bestiary.IlEntryPageFakeUnlock, Configs.BetterBestiary.Unlock)) Configs.UnloadedItemSearch.Value.BestiaryUnlock = true;
    }
    private void ILFilteringOptionsGrid_UpdateAvailability(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILFakeUnlockFilters, Configs.BetterBestiary.UnknownDisplay)) Configs.UnloadedItemSearch.Value.bestiaryUnknown = true;
        if (!ApplyIL(il, Bestiary.ILFixPosition, Configs.BetterBestiary.UnknownDisplay)) Configs.UnloadedItemSearch.Value.bestiaryUnknown = true;
    }

    public static bool ApplyIL(ILContext context, Action<ILContext> ilEdit, bool enabled, [CallerArgumentExpression("ilEdit")] string name = "") {
        if (Configs.Compatibility.CompatibilityMode && !enabled) {
            Mod.Logger.Info($"{name} was not loaded. Related features will be disabled until reload");
            return false;
        }
        try { 
            ilEdit(context); 
        } catch (Exception e) {
            MonoModHooks.DumpIL(Mod, context);
            FailedILs++;
            Mod.Logger.Warn($"{name} failed to load. Related features will be disabled until reload", e);

            return false;
        }
        return true;
    }

    public static int FailedILs { get; private set; }
}
