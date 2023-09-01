using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.Crafting;

public sealed class RecipeDefinition {
    public RecipeDefinition(string mod, List<string> items, List<int> stacks, List<string> tiles) {
        this.mod = mod;
        this.items = items;
        this.stacks = stacks;
        this.tiles = tiles;
    }

    public RecipeDefinition(Recipe recipe) {
        mod = recipe.Mod?.Name ?? "Terraria";
        items = new();
        stacks = new();
        AddItem(recipe.createItem);
        foreach (Item item in recipe.requiredItem) AddItem(item);
        tiles = new();
        foreach (int tile in recipe.requiredTile) AddTile(tile);
    }

    public Recipe? GetRecipe(){
        (int type, int stack) = (ItemID.Search.GetId(items[0]), stacks[0]);
        Dictionary<int,int> requiredItem = new();
        for (int i = 1; i < items.Count; i++) requiredItem[ItemID.Search.GetId(items[i])] = stacks[i];

        HashSet<int> requiredTile = new();
        foreach(string tile in tiles) requiredTile.Add(TileID.Search.GetId(tile));

        for (int r = 0; r < Recipe.numRecipes; r++) {
            Recipe recipe = Main.recipe[r];
            if ((recipe.Mod?.Name ?? "Terraria") != mod) continue;
            if(recipe.createItem.type != type || recipe.createItem.stack != stack) continue;
            foreach(Item material in recipe.requiredItem){
                if(!requiredItem.TryGetValue(material.type, out int s) || material.stack != s) goto next;
            }
            foreach(int tile in recipe.requiredTile){
                if(!requiredTile.Contains(tile)) goto next;
            }
            return recipe;
        next:;
        }
        return null;
    }
    
    public string mod;
    public List<string> items;
    public List<int> stacks;
    public List<string> tiles;

    private void AddItem(Item item) {
        items.Add(ItemID.Search.GetName(item.type));
        stacks.Add(item.stack);
    }
    private void AddTile(int tile) => tiles.Add(TileID.Search.GetName(tile));

}
public sealed class RecipeSerialiser : TagSerializer<RecipeDefinition, TagCompound> {
    public override TagCompound Serialize(RecipeDefinition value) => new() {
        ["mod"] = value.mod,
        ["items"] = value.items, ["stacks"] = value.stacks,
        ["tiles"] = value.tiles,
    };

    public override RecipeDefinition Deserialize(TagCompound tag) => new(
        tag.GetString("mod"),
        tag.Get<List<string>>("items"), tag.Get<List<int>>("stacks"),
        tag.Get<List<string>>("tiles")
    );
}

public sealed class RecipeFilters {

    public bool ShowAllRecipes => Main.guideItem.IsAir ? ShowAllRecipesNoGuide : ShowAllRecipesGuide;

    public bool ShowAllRecipesNoGuide { get; set; }
    public bool ShowAllRecipesGuide { get; set; } = true;

    public readonly Dictionary<int, FavoriteState> FavoriteRecipes = new();

    public void Save(TagCompound tag) {
        byte all = 0;
        if(ShowAllRecipesNoGuide) all |= 0b01;
        if(ShowAllRecipesGuide)   all |= 0b10;
        if(all != 0b10) tag["all"] = all;
        
        List<RecipeDefinition> recipes = new();
        List<byte> favorites = new();
        foreach ((int index, FavoriteState state) in FavoriteRecipes){
            recipes.Add(new(Main.recipe[index]));
            favorites.Add((byte)state);
        }
        foreach ((RecipeDefinition recipe, byte state) in _missingFavoriteRecipes){
            recipes.Add(recipe);
            favorites.Add(state);
        }

        if(recipes.Count != 0){
            tag["recipes"] = recipes;
            tag["favorites"] = favorites;
        }
    }

    public void Load(TagCompound tag) {
        FavoriteRecipes.Clear();
        _missingFavoriteRecipes.Clear();
        if (tag.TryGet("all", out byte all)) {
            ShowAllRecipesNoGuide = (all & 0b01) != 0;
            ShowAllRecipesGuide = (all & 0b10) != 0;
        }
        if(tag.TryGet("recipes", out List<RecipeDefinition> recipes)){
            List<byte> favorites = tag.Get<List<byte>>("favorites");
            for (int r = 0; r < recipes.Count; r++) {
                Recipe? recipe = recipes[r].GetRecipe();
                if(recipe is null) {
                    _missingFavoriteRecipes.Add((recipes[r], favorites[r]));
                    continue;
                }
                FavoriteRecipes[recipe.RecipeIndex] = (FavoriteState)favorites[r];
            }
        }

    }

    private readonly List<(RecipeDefinition, byte)> _missingFavoriteRecipes = new();
}

public enum FavoriteState { Default, Blacklisted, Favorited }

public static class RecipeFiltering {

    public static bool Enabled => Configs.ClientConfig.Instance.betterCrafting;

    public static RecipeFilters LocalFilters => Main.LocalPlayer.GetModPlayer<Globals.BetterPlayer>().RecipeFilters;


    public static void Load(){

        On_Main.HoverOverCraftingItemButton += HookHoverOverCraftingItemButton;

        IL_Main.DrawInventory += IlDrawInventory;

        On_Recipe.FindRecipes += HookFindRecipes;

        _inventoryBack4 = TextureAssets.InventoryBack4;
    }

    public static void PostAddRecipes() => CraftableRecipes = new bool[Recipe.maxRecipes];

    private static void HookHoverOverCraftingItemButton(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (!Enabled) {
            orig(recipeIndex);
            return;
        }

        bool cancel = OverrideRecipeHover(recipeIndex);
        if(cancel) Recipe.FindRecipes(true);
                
        cancel |= recipeIndex == Main.focusRecipe && !CraftableRecipes[recipeIndex];
        (bool lstate, bool rstate) = (Main.mouseLeft, Main.mouseRight);
        if (cancel) (Main.mouseLeft, Main.mouseRight) = (false, false);
        orig(recipeIndex);
        if (cancel) (Main.mouseLeft, Main.mouseRight) = (lstate, rstate);
    }

    public static bool OverrideRecipeHover(int recipeIndex){
        bool click = Main.mouseLeft && Main.mouseLeftRelease;
        Dictionary<int, FavoriteState> favorites = LocalFilters.FavoriteRecipes;
        if (Main.keyState.IsKeyDown(Main.FavoriteKey)) {
            Main.cursorOverride = CursorOverrideID.FavoriteStar;
            if (click) {
                FavoriteState state = favorites.GetValueOrDefault(Main.availableRecipe[recipeIndex]);
                if (state == FavoriteState.Favorited) favorites.Remove(Main.availableRecipe[recipeIndex]);
                else favorites[Main.availableRecipe[recipeIndex]] = FavoriteState.Favorited;
                return true;
            }
        } else if (ItemSlot.ControlInUse) {
            Main.cursorOverride = CursorOverrideID.TrashCan;
            if (click) {
                FavoriteState state = favorites.GetValueOrDefault(Main.availableRecipe[recipeIndex]);
                if(state == FavoriteState.Blacklisted) favorites.Remove(Main.availableRecipe[recipeIndex]);
                else favorites[Main.availableRecipe[recipeIndex]] = FavoriteState.Blacklisted;
                return true;
            }
        }
        return false;
    }

    private static void IlDrawInventory(ILContext il) {
        ILCursor cursor = new(il);

        // Background of createdItem
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(124);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), ItemSlot.DrawGoldBGForCraftingMaterial, CraftableRecipes[i]);
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });

        // Background of materials
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "SetRecipeMaterialDisplayName"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int matI) => {
            if(!Enabled) return;
            bool canCraft = CraftableRecipes[Main.focusRecipe];
            FavoriteState state = LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[Main.focusRecipe]);
            if(!canCraft) {
                Item material = Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[matI];
                canCraft = Utility.OwnedItems.GetValueOrDefault(material.type, 0) >= material.stack;
            }
            if(state == FavoriteState.Favorited) state = FavoriteState.Default;
            OverrideRecipeTexture(state, false, canCraft);
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });

        // Show all recipes Toggle
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));
        cursor.EmitLdloc(13);
        cursor.EmitCall(typeof(RecipeFiltering).GetMethod(nameof(DrawFilters), BindingFlags.Static | BindingFlags.NonPublic)!);

        // revBigList scroll fix
        for (int i = 0; i < 2; i++) {
            cursor.GotoNext(i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
            cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));
            cursor.EmitDelegate(() => {
                if(!Enabled || !Main.mouseLeft) return;
                if (Main.mouseLeftRelease || _recDelay == 0) {
                    Main.mouseLeftRelease = true;
                    _recDelay = 1;
                } else _recDelay--;
            });
            cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(SpriteBatch), nameof(SpriteBatch.Draw)));
        }

        // Cursor override
        cursor.GotoNext(i => i.MatchCall(typeof(Main), nameof(Main.LockCraftingForThisCraftClickDuration)));
        cursor.GotoPrev(i => i.MatchLdsfld(typeof(Main), nameof(Main.mouseLeft)));
        ILLabel? noClick = null;
        cursor.GotoNext(i => i.MatchBrfalse(out noClick));
        cursor.GotoPrev(MoveType.After, i => i.MatchStfld(typeof(Player), nameof(Player.mouseInterface)));
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (!Enabled || !OverrideRecipeHover(i)) return false;
            Recipe.FindRecipes(true);
            return true;
        });
        cursor.EmitBrtrue(noClick!);


        // Force recBigList on
        cursor.GotoNext(i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.MouseHover)));
        cursor.EmitDelegate<System.Action>(() => Main.recBigList |= Enabled);
        
        // Background of recipes
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (Enabled) OverrideRecipeTexture(LocalFilters.FavoriteRecipes.GetValueOrDefault(Main.availableRecipe[i]), false, CraftableRecipes[i]);
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });
    }

    private static void DrawFilters(int yOffset){
        if (!Enabled) return;
        RecipeFilters filters = LocalFilters;
        int x = 94;
        int y = 450 + yOffset + TextureAssets.CraftToggle[0].Height();
        Vector2 hibBox = TextureAssets.CraftToggle[0].Size() * 0.45f;
        Asset<Texture2D> eye = filters.ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
        Main.spriteBatch.Draw(eye.Value, new Vector2(x, y), null, Color.White, 0f, eye.Value.Size() / 2, 1f, 0, 0f);
        if (Main.mouseX > x - hibBox.X && Main.mouseX < x + hibBox.X && Main.mouseY > y - hibBox.Y && Main.mouseY < y + hibBox.Y && !PlayerInput.IgnoreMouseInterface) {
            Main.instance.MouseText(Language.GetTextValue("Mods.BetterInventory.UI.ShowRecipes"));
            Main.spriteBatch.Draw(EyeBorder.Value, new Vector2(x, y), null, Color.White, 0f, EyeBorder.Value.Size() / 2, 1f, 0, 0f);
            Main.player[Main.myPlayer].mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease) {
                if (Main.guideItem.IsAir) filters.ShowAllRecipesNoGuide = !filters.ShowAllRecipesNoGuide;
                else filters.ShowAllRecipesGuide = !filters.ShowAllRecipesGuide;
                SoundEngine.PlaySound(SoundID.MenuTick);
                Recipe.FindRecipes();
            }
        }
    }


    public static void OverrideRecipeTexture(FavoriteState state, bool selected, bool canCraft){
        TextureAssets.InventoryBack4 = (selected ? SelectedRecipeTextures : DefaultRecipeTextures)[(int)state];
        if (!canCraft) {
            byte alpha = Main.inventoryBack.A;
            Main.inventoryBack *= 0.5f;
            Main.inventoryBack.A = alpha;
        }
    }

    public static readonly Asset<Texture2D>[] DefaultRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack4, TextureAssets.InventoryBack5, TextureAssets.InventoryBack10 };
    public static readonly Asset<Texture2D>[] SelectedRecipeTextures = new Asset<Texture2D>[] { TextureAssets.InventoryBack14, TextureAssets.InventoryBack11, TextureAssets.InventoryBack17 };

    public static IEnumerable<int> AllRecipesIndex() {
        for (int i = 0; i < Recipe.numRecipes; i++) yield return i;
    }

    private static void HookFindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if (canDelayCheck || !Enabled) {
            orig(canDelayCheck);
            return;
        }
        int oldRecipe = Main.availableRecipe[Main.focusRecipe];
        float focusY = Main.availableRecipeY[Main.focusRecipe];

        int type = Main.guideItem.type;
        Main.guideItem.type = ItemID.None;
        orig(canDelayCheck);
        int[] availableRecipes = Main.availableRecipe[..Main.numAvailableRecipes];
        Main.guideItem.type = type;

        IEnumerable<int> recipes;
        if(!Main.guideItem.IsAir) {
            Recipe.ClearAvailableRecipes();
            CollectGuideRecipesMethod.Invoke(null, null);
            int[] guideRecipes = Main.availableRecipe[..Main.numAvailableRecipes];
            recipes = guideRecipes;
        } else recipes = AllRecipesIndex();

        Recipe.ClearAvailableRecipes();
        AddFilteredRecipes(recipes, availableRecipes, FavoriteState.Favorited);
        AddFilteredRecipes(recipes, availableRecipes, FavoriteState.Default);
        if(LocalFilters.ShowAllRecipes) AddFilteredRecipes(recipes, availableRecipes, FavoriteState.Blacklisted);

        TryRefocusingRecipeMethod.Invoke(null, new object[] { oldRecipe });
        VisuallyRepositionRecipesMethod.Invoke(null, new object[] { focusY });
    }

    private static void AddFilteredRecipes(IEnumerable<int> recipes, int[] craftableRecipes, FavoriteState applyTo){
        int a = 0;
        foreach(int index in recipes){
            if(LocalFilters.FavoriteRecipes.GetValueOrDefault(index) != applyTo) continue;
            while(a < craftableRecipes.Length && craftableRecipes[a] < index) a++;
            Recipe recipe = Main.recipe[index];
            bool craftable = a < craftableRecipes.Length && index == craftableRecipes[a];
            if (!MatchVisibilityFilter(recipe, craftable)) continue;
            CraftableRecipes[Main.numAvailableRecipes] = craftable;
            Utility.AddToAvailableRecipesMethod.Invoke(null, new object[] { index });
        }
    }

    private static void HookClearRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        for (int i = 0; i < Main.numAvailableRecipes; i++) CraftableRecipes[i] = false;
        orig();
    }

    public static bool MatchVisibilityFilter(Recipe recipe, bool craftable) => LocalFilters.FavoriteRecipes.GetValueOrDefault(recipe.RecipeIndex) switch {
        FavoriteState.Favorited => true,
        FavoriteState.Blacklisted => LocalFilters.ShowAllRecipes,
        _ => LocalFilters.ShowAllRecipes || craftable,
    };


    public static bool[] CraftableRecipes = System.Array.Empty<bool>();

    private static Asset<Texture2D> _inventoryBack4 = null!;
    private static int _recDelay = 0;

    public static readonly PropertyInfo EnabledField = typeof(RecipeFiltering).GetProperty(nameof(Enabled), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly PropertyInfo DisabledProp = typeof(Recipe).GetProperty(nameof(Recipe.Disabled), BindingFlags.Instance | BindingFlags.Public)!;

    public static readonly MethodInfo useWoodMethod = typeof(Recipe).GetMethod("useWood", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useIronBarMethod = typeof(Recipe).GetMethod("useIronBar", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useSandMethod = typeof(Recipe).GetMethod("useSand", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useFragmentMethod = typeof(Recipe).GetMethod("useFragment", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo usePressurePlateMethod = typeof(Recipe).GetMethod("usePressurePlate", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo CollectGuideRecipesMethod = typeof(Recipe).GetMethod("CollectGuideRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;

    public static Asset<Texture2D> EyeBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> EyeForced => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Forced");

    public static readonly MethodInfo TryRefocusingRecipeMethod = typeof(Recipe).GetMethod("TryRefocusingRecipe", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo VisuallyRepositionRecipesMethod = typeof(Recipe).GetMethod("VisuallyRepositionRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;
}