using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SpikysLib;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ContextID = Terraria.UI.ItemSlot.Context;

// BUG items not counted when manually placing items into guide slots
namespace BetterInventory.ItemSearch;


public sealed class GuideGuideTile : ModPlayer {

    public override void Load() {
        IL_Recipe.FindRecipes += static il => {
            if (!il.ApplyTo(ILGuideTileRecipes, Configs.BetterGuide.GuideTile)) Configs.UnloadedItemSearch.Value.guideTile = true;
        };
        _guideTileFilters = new(() => Configs.BetterGuide.GuideTile && !guideTile.IsAir, r => CheckGuideTileFilter(r));
        GuideRecipeFiltering.AddFilter(_guideTileFilters);
    }

    public override void Unload() {
        guideTile = new();
        CraftingStationsItems.Clear();
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

    public override void SaveData(TagCompound tag) {
        if (!guideTile.IsAir) tag[GuideTileTag] = guideTile;
    }
    public override void LoadData(TagCompound tag) {
        if (tag.TryGet(GuideTileTag, out Item guide)) _tempGuideTile = guide;
        else _tempGuideTile = new();
    }
    public override void OnEnterWorld() {
        if (_tempGuideTile is not null) guideTile = _tempGuideTile;
    }

    public const string GuideTileTag = "guideTile";



    private static void ILGuideTileRecipes(ILContext il){
        ILCursor cursor = new(il);
        cursor.GotoNext(i => i.MatchCall(Reflection.Recipe.CollectGuideRecipes));
        cursor.GotoPrev(MoveType.After, i => i.MatchCallvirt(Reflection.Item.IsAir.GetMethod!));
        cursor.EmitDelegate((bool isAir) => isAir && (!Configs.BetterGuide.GuideTile || guideTile.IsAir));
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(Reflection.Item.Name.GetMethod!));
        cursor.EmitDelegate((string name) => Configs.BetterGuide.GuideTile && guideTile.Name != "" ? guideTile.Name : name);
    }

    internal static void DrawGuideTile(int inventoryX, int inventoryY) {
        float x = inventoryX + TextureAssets.InventoryBack.Width() * Main.inventoryScale * (1 + GuideRequiredObjectsDisplay.TileSpacingRatio);
        float y = inventoryY;
        Main.inventoryScale *= GuideRequiredObjectsDisplay.TileScale;
        Item[] items = Guide.GuideItems;

        // Handle Mouse hover
        Rectangle hitbox = new((int)x, (int)y, (int)(TextureAssets.InventoryBack.Width() * Main.inventoryScale), (int)(TextureAssets.InventoryBack.Height() * Main.inventoryScale));
        if (!Main.player[Main.myPlayer].mouseInterface && hitbox.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
            Main.player[Main.myPlayer].mouseInterface = true;
            Main.craftingHide = true;
            ItemSlot.OverrideHover(items, ContextID.GuideItem, 1);
            ItemSlot.LeftClick(items, ContextID.GuideItem, 1);
            Guide.GuideItems = items; // Update items if changed
            if (Main.mouseLeftRelease && Main.mouseLeft) Recipe.FindRecipes();
            ItemSlot.RightClick(items, ContextID.GuideItem, 1);
            ItemSlot.MouseHover(items, ContextID.GuideItem, 1);
            Guide.GuideItems = items; // Update items if changed
        }
        ItemSlot.Draw(Main.spriteBatch, items, ContextID.GuideItem, 1, hitbox.TopLeft());
        Main.inventoryScale /= GuideRequiredObjectsDisplay.TileScale;
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

    public static void dropGuideTileCheck(Player self) {
        if (Main.InGuideCraftMenu || guideTile.IsAir) return;
        if (guideTile.IsAPlaceholder()) guideTile.TurnToAir();
        else self.GetDropItem(ref guideTile);
    }

    public static Item guideTile = new();
    private static GuideRecipeFilterGroup _guideTileFilters = null!;


    public static bool FitsCraftingTile(Item item) => IsCraftingStation(item) || PlaceholderItem.ConditionItems.ContainsValue(item.type);
    public static bool IsCraftingStation(Item item) => CraftingStationsItems.ContainsKey(item.createTile) || item.IsAPlaceholder();

    public static readonly Dictionary<int, int> CraftingStationsItems = []; // tile -> item
    private Item? _tempGuideTile;
}
