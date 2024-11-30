using BetterInventory.Crafting.UI;
using BetterInventory.Default.Catalogues;
using BetterInventory.InventoryManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

namespace BetterInventory.ItemSearch;


public sealed partial class Guide : ModSystem {

    // TODO redo
    public static void FindGuideRecipes() => Recipe.FindRecipes();

    internal static GameTime _lastUpdateUiGameTime = null!;
    internal static UserInterface recipeInterface = null!;
    internal static RecipeUI recipeUI = null!;

    public override void UpdateUI(GameTime gameTime) {
        _lastUpdateUiGameTime = gameTime;
        recipeInterface.Update(gameTime);
    }

    public override void Load() {
        On_Recipe.CollectGuideRecipes += HookCollectGuideRecipes;

        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;

        IL_Recipe.CollectGuideRecipes += static il => {
            if (!il.ApplyTo(ILGuideRecipeOrder, Configs.BetterGuide.RecipeOrdering)) Configs.UnloadedItemSearch.Value.GuideRecipeOrdering = true;
        };

        recipeUI = new();
        recipeUI.Activate();
        recipeInterface = new();
        recipeInterface.SetState(recipeUI);
    }

    public override void ClearWorld() {
        SmartPickup.ClearMarks();
    }

    public override void PostAddRecipes() {
        Default.Catalogues.Bestiary.HooksBestiaryUI();
        GuideGuideTile.FindCraftingStations();
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        // Replace ShiftClick by a LeftClick
        if ((Configs.BetterGuide.Enabled || RecipeList.Instance.Enabled) && Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.InventoryToChest) {
            (Item mouse, Main.mouseItem, inv[slot]) = (Main.mouseItem, inv[slot], new());
            (int cursor, Main.cursorOverride) = (Main.cursorOverride, 0);

            Item[] items = GuideItems;
            ItemSlot.LeftClick(items, ContextID.GuideItem, Configs.BetterGuide.GuideTile && GuideGuideTile.IsCraftingStation(Main.mouseItem) ? 1 : 0);
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
        if (res || !Configs.BetterGuide.GuideTile || slot != 1 || !inv[slot].IsAir || GuideGuideTile.FitsCraftingTile(Main.mouseItem)) return res;

        // Allow by hand when clicking on guideTile
        inv[slot] = PlaceholderItem.FromTile(PlaceholderItem.ByHandTile);
        Recipe.FindRecipes();
        SoundEngine.PlaySound(SoundID.Grab);
        Main.recBigList = true;
        return true;
    }

    public static Item[] GuideItems {
        get {
            (s_guideItems[0], s_guideItems[1]) = (Main.guideItem, GuideGuideTile.guideTile);
            return s_guideItems;
        }
        set => (Main.guideItem, GuideGuideTile.guideTile) = (value[0], value[1]);
    }
    private static readonly Item[] s_guideItems = new Item[2];
}

public enum PlaceholderType { None, ByHand, Tile, Condition }

public record class TextureHighlight(Asset<Texture2D> Default, Asset<Texture2D> Highlight);