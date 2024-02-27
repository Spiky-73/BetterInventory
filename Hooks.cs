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

        IL_ItemSlot.LeftClick_ItemArray_int_int += ILLeftClick;
        IL_ItemSlot.HandleShopSlot += ILHandleShopSlot;
        IL_ItemSlot.SellOrTrash += ILSellOrTrash;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += ILDrawSlot;

        MonoModHooks.Modify(Reflection.ConfigElement.DrawSelf, IlDrawSelf);

        IL_Filters.BySearch.FitsFilter += ILSearchAddEntries;
        IL_UIBestiaryEntryIcon.Update += ILIconUpdateFakeUnlock;
        IL_UIBestiaryEntryIcon.DrawSelf += ILIconDrawFakeUnlock;
        IL_UIBestiaryEntryInfoPage.AddInfoToList += IlEntryPageFakeUnlock;
        IL_UIBestiaryFilteringOptionsGrid.UpdateAvailability += ILFakeUnlockFilters;
    }
    public void Unload() {}

    private void ILGetItem(ILContext il) {
        SmartPickup.ILSmartPickup(il);
        BetterPlayer.ILAutoEquip(il);
    }
    private void ILPickAndConsumeBait(ILContext il){
        SmartConsumptionItem.ILSmartBait(il);
    }

    private void ILDrawInventory(ILContext il) {
        RecipeFiltering.ILDrawFilters(il);
        Tweeks.ILFastScroll(il);
        Tweeks.ILListScrollFix(il);
        Tweeks.ILMaterialWrapping(il);
        Guide.ILForceGuideDisplay(il);
        Guide.ILDrawVisibility(il);
        Guide.ILCustomDrawCreateItem(il);
        Guide.ILCustomDrawMaterials(il);
        Guide.ILCustomDrawRecipeList(il);
    }
    private void ILHoverOverCraftingItemButton(ILContext il) {
        Guide.ILFavoriteRecipe(il);
        Guide.ILCraftInGuideMenu(il);
        ClickOverride.ILCraftCursorOverride(il);
    }
    private void ILCraftItem(ILContext il){
        ClickOverride.ILShiftCraft(il);
        ClickOverride.ILCraftStack(il);
        ClickOverride.ILRestoreRecipe(il);
        ClickOverride.ILFixCraftMouseText(il);
    }

    private void ILFindRecipes(ILContext il){
        Guide.ILSkipGuideRecipes(il);
        Guide.ILUpdateOwnedItems(il);
    }
    private void ILOverrideGuideRecipes(ILContext il){
        Guide.ILOverrideGuideRecipes(il);
    }


    private void ILLeftClick(ILContext il) {
        BetterPlayer.ILKeepFavoriteInChest(il);
    }
    private void ILHandleShopSlot(ILContext il){
        ClickOverride.ILBuyStack(il);
    }
    private void ILSellOrTrash(ILContext il){
        ClickOverride.ILStackStrash(il);
    }
    private void ILDrawSlot(ILContext il) {
        SmartPickup.ILDrawMarks(il);
        QuickMove.ILHighlightSlot(il);
        QuickMove.ILDisplayHotkey(il);
    }

    private void ILFakeUnlockFilters(ILContext il) => Bestiary.ILSearchAddEntries(il);
    private void IlEntryPageFakeUnlock(ILContext il) => Bestiary.ILIconUpdateFakeUnlock(il);
    private void ILIconDrawFakeUnlock(ILContext il) => Bestiary.ILIconDrawFakeUnlock(il);
    private void ILIconUpdateFakeUnlock(ILContext il) => Bestiary.IlEntryPageFakeUnlock(il);
    private void ILSearchAddEntries(ILContext il) => Bestiary.ILFakeUnlockFilters(il);

    private void IlDrawSelf(ILContext il) => Text.ILTextColors(il);
}