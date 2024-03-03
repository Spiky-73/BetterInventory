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
    public void Load(Mod mod) {
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
    public void Unload() {}

    private void ILGetItem(ILContext il) {
        SmartPickup.ILSmartPickup(il);
        SmartPickup.ILAutoEquip(il);
    }
    private void ILPickAndConsumeBait(ILContext il){
        SmartConsumptionItem.ILOnConsumeBait(il);
    }

    private void ILDrawInventory(ILContext il) {
        RecipeFiltering.ILDrawFilters(il);
        FixedUI.ILFastScroll(il);
        FixedUI.ILListScrollFix(il);
        FixedUI.ILMaterialWrapping(il);
        Crafting.Crafting.ILCraftOnList(il);
        Guide.ILForceGuideDisplay(il);
        Guide.ILDrawVisibility(il);
        Guide.ILCustomDrawCreateItem(il);
        Guide.ILCustomDrawMaterials(il);
        Guide.ILCustomDrawRecipeList(il);
    }
    private void ILHoverOverCraftingItemButton(ILContext il) {
        Guide.ILFavoriteRecipe(il);
        Guide.ILCraftInGuideMenu(il);
        ClickOverrides.ILCraftCursorOverride(il);
    }
    private void ILCraftItem(ILContext il){
        ClickOverrides.ILShiftCraft(il);
        ClickOverrides.ILCraftStack(il);
        Guide.IlUnfavoriteOnCraft(il);
        ClickOverrides.ILRestoreRecipe(il);
        ClickOverrides.ILFixCraftMouseText(il);
    }

    private void ILFindRecipes(ILContext il){
        Guide.ILSkipGuideRecipes(il);
        Guide.ILUpdateOwnedItems(il);
    }
    private void ILOverrideGuideRecipes(ILContext il){
        Guide.ILOverrideGuideRecipes(il);
    }
    private void ILConsumeForCraft(ILContext il) {
        SmartConsumptionItem.ILOnConsumedMaterial(il);
    }


    private void ILLeftClick(ILContext il) {
        ClickOverrides.ILKeepFavoriteInChest(il);
    }
    private void ILHandleShopSlot(ILContext il){
        ClickOverrides.ILPreventChainBuy(il);
        ClickOverrides.ILBuyStack(il);
    }
    private void ILSellOrTrash(ILContext il){
        ClickOverrides.ILStackStrash(il);
    }
    private void ILDrawSlot(ILContext il) {
        SmartPickup.ILDrawMarks(il);
        QuickMove.ILHighlightSlot(il);
        QuickMove.ILDisplayHotkey(il);
    }

    private void ILFilters_BySearch(ILContext il) => Bestiary.ILSearchAddEntries(il);
    private void ILEntryIcon_Update(ILContext il) => Bestiary.ILIconUpdateFakeUnlock(il);
    private void ILEntryIcon_DrawSelf(ILContext il) => Bestiary.ILIconDrawFakeUnlock(il);
    private void IlEntryPage_AddInfoToList(ILContext il) => Bestiary.IlEntryPageFakeUnlock(il);
    private void ILFilteringOptionsGrid_UpdateAvailability(ILContext il) => Bestiary.ILFakeUnlockFilters(il);

    private void IlDrawSelf(ILContext il) => Text.ILTextColors(il);
}