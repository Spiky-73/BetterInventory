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
using Terraria.UI;

namespace BetterInventory;

public class Hooks : ILoadable {
    public static Mod Mod { get; private set; } = null!;
    public void Load(Mod mod) {
        Mod = mod;
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
        if (!ApplyIL(il, SmartPickup.ILSmartPickup, Configs.SmartPickup.Enabled())) Configs.UnloadedInventoryManagement.Value.smartPickup = true;
        if (!ApplyIL(il, SmartPickup.ILAutoEquip, Configs.InventoryManagement.AutoEquip)) Configs.UnloadedInventoryManagement.Value.autoEquip = true;
    }
    private void ILPickAndConsumeBait(ILContext il){
        if (!ApplyIL(il, SmartConsumptionItem.ILOnConsumeBait, Configs.SmartConsumption.Baits)) Configs.UnloadedInventoryManagement.Value.baits = true;
    }

    private void ILDrawInventory(ILContext il) {
        if (!ApplyIL(il, RecipeFiltering.ILDrawFilters, Configs.RecipeFilters.Enabled)) Configs.UnloadedCrafting.Value.recipeFilters = true;
        if (!ApplyIL(il, FixedUI.ILFastScroll, Configs.FixedUI.FastScroll)) Configs.UnloadedCrafting.Value.fastScroll = true;
        if (!ApplyIL(il, FixedUI.ILListScrollFix, Configs.FixedUI.ListScroll)) Configs.UnloadedCrafting.Value.listScroll = true;
        if (!ApplyIL(il, FixedUI.ILMaterialWrapping, Configs.FixedUI.Wrapping)) Configs.UnloadedCrafting.Value.wrapping = true;
        if (!ApplyIL(il, Crafting.Crafting.ILCraftOnList, Configs.CraftOnList.Enabled)) Configs.UnloadedCrafting.Value.craftOnList = true;
        if (!ApplyIL(il, SearchItem.ILForceGuideDisplay, Configs.SearchItems.Recipes)) Configs.UnloadedItemSearch.Value.searchRecipes = true;
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
        if (!ApplyIL(il, ClickOverrides.ILCraftStack, Configs.InventoryManagement.ClickOverrides)) Configs.UnloadedInventoryManagement.Value.ClickOverrides = true;
        if (!ApplyIL(il, Guide.IlUnfavoriteOnCraft, Configs.FavoriteRecipes.UnfavoriteOnCraft)) Configs.UnloadedItemSearch.Value.guideUnfavoriteOnCraft = true;
        if (!ApplyIL(il, ClickOverrides.ILFixCraftMouseText, Configs.InventoryManagement.ClickOverrides)) Configs.UnloadedInventoryManagement.Value.ClickOverrides = true;
        if (!ApplyIL(il, ClickOverrides.ILFixCraftMouseText, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
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


    private void ILLeftClick(ILContext il) {
        if (!ApplyIL(il, ClickOverrides.ILKeepFavoriteInBanks, Configs.InventoryManagement.FavoriteInBanks)) Configs.UnloadedInventoryManagement.Value.favoriteInBanks = true;
    }
    private void ILHandleShopSlot(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILPreventChainBuy, Configs.CraftStack.Enabled)) Configs.UnloadedInventoryManagement.Value.craftStack = true;
        if (!ApplyIL(il, ClickOverrides.ILBuyStack, Configs.InventoryManagement.ClickOverrides)) Configs.UnloadedInventoryManagement.Value.ClickOverrides = true;
    }
    private void ILSellOrTrash(ILContext il){
        if (!ApplyIL(il, ClickOverrides.ILStackTrash, Configs.InventoryManagement.StackTrash)) Configs.UnloadedInventoryManagement.Value.stackTrash = true;
    }
    private void ILDrawSlot(ILContext il) {
        if (!ApplyIL(il, SmartPickup.ILDrawMarks, Configs.SmartPickup.Marks)) Configs.UnloadedInventoryManagement.Value.marks = true;
        if (!ApplyIL(il, QuickMove.ILHighlightSlot, Configs.QuickMove.Highlight)) Configs.UnloadedInventoryManagement.Value.quickMoveHighlight = true;
        if (!ApplyIL(il, QuickMove.ILDisplayHotkey, Configs.QuickMove.DisplayHotkeys)) Configs.UnloadedInventoryManagement.Value.quickMoveHotkeys = true;
    }

    private void ILFilters_BySearch(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILSearchAddEntries, Configs.BetterBestiary.DisplayedUnlock)) Configs.UnloadedItemSearch.Value.bestiaryDisplayedUnlock = true;
    }
    private void ILEntryIcon_Update(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILIconUpdateFakeUnlock, Configs.BetterBestiary.Unlock)) Configs.UnloadedItemSearch.Value.BestiaryUnlock = true;
    }
    private void ILEntryIcon_DrawSelf(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILIconDrawFakeUnlock, Configs.BetterBestiary.Progression)) Configs.UnloadedItemSearch.Value.bestiaryProgression = true;
    }
    private void IlEntryPage_AddInfoToList(ILContext il) {
        if (!ApplyIL(il, Bestiary.IlEntryPageFakeUnlock, Configs.BetterBestiary.Unlock)) Configs.UnloadedItemSearch.Value.BestiaryUnlock = true;
    }
    private void ILFilteringOptionsGrid_UpdateAvailability(ILContext il) {
        if (!ApplyIL(il, Bestiary.ILFakeUnlockFilters, Configs.BetterBestiary.Progression)) Configs.UnloadedItemSearch.Value.bestiaryProgression = true;
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
            FailedILs++;
            Mod.Logger.Warn($"{name} failled to load. Related features will be disabled until reload");
            return false;
        }
        return true;
    }

    public static int FailedILs { get; private set; }
}
