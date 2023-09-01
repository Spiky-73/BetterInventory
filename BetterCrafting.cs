using System.Collections.Generic;
using System.IO;
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
using Terraria.UI;

namespace BetterInventory;

public static class BetterCrafting {
    public static void Load(){
        IL_Main.DrawInventory += IlDrawInventory;
        On_Main.DrawInterface_36_Cursor += HookDrawCursor;

        FindItemRecipes = KeybindLoader.RegisterKeybind(BetterInventory.Instance, "FindRecipes", Microsoft.Xna.Framework.Input.Keys.N);
        Main.OnPostDraw += UpdateHoveredItem;

        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;

        On_Player.dropItemCheck += OndropItems;
        On_Player.SaveTemporaryItemSlotContents += HookSaveTemporaryItemSlotContents;

        On_Main.HoverOverCraftingItemButton += HookHoverOverCraftingItemButton;

        On_Recipe.ClearAvailableRecipes += HookClearRecipes;
        On_Recipe.AddToAvailableRecipes += HookAddAvailableRecipe;
        IL_Recipe.FindRecipes += ILFindRecipes;

        // On_Recipe.FindRecipes += HookFindRecipes;
        // IL_Recipe.CollectGuideRecipes += ILGuideRecipes;

        On_Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
        IL_Main.CraftItem += ILCraftItem;
        IL_Recipe.Create += ILCreateRecipe;

        _inventoryBack4 = TextureAssets.InventoryBack4;
        _ownedMaterials = (Dictionary<int, int>)OwnedItemsField.GetValue(null)!;
    }

    private static void HookAddAvailableRecipe(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
        AvailableRecipesInfo.Add(new(_canCraft, GetRecipeState(recipeIndex)));
        orig(recipeIndex);
    }

    private static void HookClearRecipes(On_Recipe.orig_ClearAvailableRecipes orig) {
        AvailableRecipesInfo.Clear();
        orig();
    }

    private static void HookDrawCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        if(!Enabled || Main.cursorOverride != CraftCursorID) {
            orig();
            return;
        }
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        Main.spriteBatch.Draw(CursorCraft.Value, new Vector2(Main.mouseX, Main.mouseY), null, Color.White, 0, default, Main.cursorScale, 0, 0f);
        return;
    }

    internal static void OnStateChanged() {
        if (!Enabled) Main.guideItem.TurnToAir();
        else if (!Main.guideItem.IsAir) {
            Main.LocalPlayer.GetDropItem(ref Main.guideItem);
            Main.guideItem = new(Main.guideItem.type);
        }
    }


    private static void IlDrawInventory(ILContext il) {
        // if(Main.InReforgeMenu){
        //     ...
        // } else if(Main.InGuideCraftMenu) {
        //     if(<closeGuideUI>) {
        //         ...
        //     } else {
        //         ++ guide:
        //         ...
        //         ++ if(!Main.InGuideCraftMenu) goto recipe;
        //     }
        // }
        // ...
        // if(<showRecipes>){
        //     ++ if(!Main.InGuideCraftMenu) goto guide;
        //     ++ recipe:
        //     ...
        // }

        ILCursor cursor = new(il);

        // Keybind
        cursor.EmitDelegate(CheckFindRecipes);

        // Mark guide
        ILLabel? endGuide = null;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(MoveType.After, i => i.MatchBr(out endGuide));
        ILLabel guide = cursor.DefineLabel();
        cursor.MarkLabel(guide);

        // Apply recipe
        cursor.GotoLabel(endGuide!, MoveType.Before);
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitCall(AlternateGuideItemProp.GetMethod!);
        cursor.EmitBrtrue(recipe);

        // Apply guide + Mark recipe
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);
        cursor.EmitCall(AlternateGuideItemProp.GetMethod!);
        cursor.EmitBrtrue(guide);
        cursor.MarkLabel(recipe);

        // Background of createdItem
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "HoverOverCraftingItemButton"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(124);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            TextureAssets.InventoryBack4 = GetRecipeTexture(AvailableRecipesInfo[i], ItemSlot.DrawGoldBGForCraftingMaterial);
            ItemSlot.DrawGoldBGForCraftingMaterial = false;

            if (!AvailableRecipesInfo[i].CanCraft) {
                byte alpha = Main.inventoryBack.A;
                Main.inventoryBack *= 0.5f;
                Main.inventoryBack.A = alpha;
            }
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });

        // Background of materials
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "SetRecipeMaterialDisplayName"));
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(130);
        cursor.EmitDelegate((int matI) => {
            if (!Enabled || Main.guideItem.type == ItemID.None ||  AvailableRecipesInfo[Main.focusRecipe].CanCraft) return;
            Item material = Main.recipe[Main.availableRecipe[Main.focusRecipe]].requiredItem[matI];
            if (_ownedMaterials.GetValueOrDefault(material.type, 0) >= material.stack) return;
            byte alpha = Main.inventoryBack.A;
            Main.inventoryBack *= 0.5f;
            Main.inventoryBack.A = alpha;
        });

        // Show all recipes Toggle
        cursor.GotoNext(MoveType.After, i => i.MatchStsfld(typeof(Main), nameof(Main.hidePlayerCraftingMenu)));
        cursor.EmitLdloc(13);
        cursor.EmitDelegate((int yOffset) => {
            if (!Enabled) return;
            int x = 94;
            int y = 450 + yOffset + TextureAssets.CraftToggle[0].Height();
            Vector2 hibBox = TextureAssets.CraftToggle[0].Size() * 0.45f;
            Asset<Texture2D> eye = ShowAllRecipes ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff;
            Main.spriteBatch.Draw(eye.Value, new Vector2(x, y), null, Color.White, 0f, eye.Value.Size() / 2, 1f, 0, 0f);
            if (Main.mouseX > x - hibBox.X && Main.mouseX < x + hibBox.X && Main.mouseY > y - hibBox.Y && Main.mouseY < y + hibBox.Y && !PlayerInput.IgnoreMouseInterface) {
                Main.instance.MouseText(Language.GetTextValue("Mods.BetterInventory.UI.ShowRecipes"));
                Main.spriteBatch.Draw(EyeBorder.Value, new Vector2(x, y), null, Color.White, 0f, EyeBorder.Value.Size() / 2, 1f, 0, 0f);
                Main.player[Main.myPlayer].mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease) {
                    if(Main.guideItem.IsAir) showAllRecipes = !ShowAllRecipes;
                    else  showAllRecipesWithGuide = !ShowAllRecipes;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    Recipe.FindRecipes();
                }
            }
        });

        // Background of recipes
        cursor.GotoNext(i => i.MatchCall(typeof(Main), nameof(Main.LockCraftingForThisCraftClickDuration)));
        cursor.GotoNext(i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.MouseHover)));
        cursor.EmitDelegate<System.Action>(() => Main.recBigList |= Enabled);
        cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.Draw)));
        cursor.EmitLdloc(153);
        cursor.EmitDelegate((int i) => {
            if (!Enabled) return;
            TextureAssets.InventoryBack4 = GetRecipeTexture(AvailableRecipesInfo[i], false);
            if (!AvailableRecipesInfo[i].CanCraft) {
                byte alpha = Main.inventoryBack.A;
                Main.inventoryBack *= 0.5f;
                Main.inventoryBack.A = alpha;
            }
        });
        cursor.GotoNext(MoveType.After, i => true);
        cursor.EmitDelegate(() => { TextureAssets.InventoryBack4 = _inventoryBack4; });
    }

    private static Asset<Texture2D> GetRecipeTexture(RecipeInfo info, bool selected) => selected ?
        info.State switch {
            RecipeState.Blacklisted => TextureAssets.InventoryBack11,
            RecipeState.Favorited => TextureAssets.InventoryBack17,
            _ => TextureAssets.InventoryBack14,
        } :
        info.State switch {
            RecipeState.Blacklisted => TextureAssets.InventoryBack5,
            RecipeState.Favorited => TextureAssets.InventoryBack10,
            _ => TextureAssets.InventoryBack4,
        };


    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled) {
            orig(inv, context, slot);
            return;
        }
        if (FindItemRecipes.Current) return;
        if (context == ItemSlot.Context.GuideItem && ((Main.guideItem.type > ItemID.None && Main.mouseItem.type == ItemID.None) || ItemSlot.ShiftInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        else if (Main.InGuideCraftMenu && ItemSlot.ShiftInUse) Main.cursorOverride = CursorOverrideID.Magnifiers;
        else orig(inv, context, slot);
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!Enabled) return orig(inv, context, slot);
        if (FindItemRecipes.Current) return true;
        if (context == ItemSlot.Context.GuideItem) {
            if (Main.cursorOverride == CursorOverrideID.TrashCan) FindRecipes(ItemID.None);
            else {
                FindRecipes(Main.mouseItem.type);
                Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            }
            return true;
        }
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.Magnifiers) {
            FindRecipes(inv[slot].type);
            return true;
        }
        return orig(inv, context, slot);
    }

    private static void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (Enabled && context == ItemSlot.Context.GuideItem && Main.mouseRight && Main.mouseRightRelease) {
            FindRecipes(ItemID.None);
            return;
        }
        orig(inv, context, slot);
    }


    private static void OndropItems(On_Player.orig_dropItemCheck orig, Player self) {
        if (!Enabled) {
            orig(self);
            return;
        }
        bool old = Main.InGuideCraftMenu;
        Main.InGuideCraftMenu = true;
        orig(self);
        Main.InGuideCraftMenu = old;
    }

    private static void HookSaveTemporaryItemSlotContents(On_Player.orig_SaveTemporaryItemSlotContents orig, Player self, BinaryWriter writer) {
        if (!Enabled || Main.guideItem.IsAir) {
            orig(self, writer);
            return;
        }
        Main.guideItem.stack = 0;
        orig(self, writer);
        Main.guideItem.stack = 1;
    }


    private static void HookHoverOverCraftingItemButton(On_Main.orig_HoverOverCraftingItemButton orig, int recipeIndex) {
        if (!Enabled) {
            orig(recipeIndex);
            return;
        }
        _inHoverCrafting = true;

        if (Main.cursorOverride == -1) {
            if(recipeIndex == Main.focusRecipe && ItemSlot.ShiftInUse) Main.cursorOverride = CraftCursorID;
            else if(ItemSlot.ControlInUse) Main.cursorOverride = CursorOverrideID.TrashCan;
            else if(Main.keyState.IsKeyDown(Main.FavoriteKey)) Main.cursorOverride = CursorOverrideID.FavoriteStar;
        }
        
        
        if(Main.cursorOverride == CursorOverrideID.TrashCan && Main.mouseLeft && Main.mouseLeftRelease){
            FavoritedRecipes.Remove(Main.availableRecipe[recipeIndex]);
            if(AvailableRecipesInfo[recipeIndex].Blacklisted) BlacklistedRecipes.Remove(Main.availableRecipe[recipeIndex]);
            else BlacklistedRecipes.Add(Main.availableRecipe[recipeIndex]);
        }
        else if(Main.cursorOverride == CursorOverrideID.FavoriteStar && Main.mouseLeft && Main.mouseLeftRelease){
            BlacklistedRecipes.Remove(Main.availableRecipe[recipeIndex]);
            if(AvailableRecipesInfo[recipeIndex].Favorited) FavoritedRecipes.Remove(Main.availableRecipe[recipeIndex]);
            else FavoritedRecipes.Add(Main.availableRecipe[recipeIndex]);
        }
        if (Main.cursorOverride != -1 && Main.cursorOverride != CraftCursorID) {
            bool state = Main.mouseLeft;
            Main.mouseLeft = false;
            orig(recipeIndex);
            Main.mouseLeft = state;
            return;
        }
        if (Main.guideItem.IsAir || !AvailableRecipesInfo[recipeIndex].CanCraft) {
            orig(recipeIndex);
            return;
        }
        Main.guideItem.stack = 0;
        orig(recipeIndex);
        Main.guideItem.stack = 1;
        _inHoverCrafting = false;
    }


    private static void ILFindRecipes(ILContext il) {
        // ...
        // Recipe.ClearAvailableRecipes();
        // if (Enabled) goto skipGuide
        // <guideRecipes>
        // skipGuide:
        // ...
        // for(...) {
        //     ...
        //     if(recipe.Disabled) continue;
        //     ++ if(Enabled && !<match guide & journey filters>) continue;
        //     <requirementsMet code>
        //     ++ canCraft = false;
        //     ++ if(Enabled && ShowAllRecipes) Recipe.AddToAvailableRecipes(i);
        //     if (!requirementsMet) continue;
        //     if (!Recipe.AddToAvailableRecipes(i)) continue;
        //     ++ canCraft = true;
        //     ++ if(Enabled && ShowAllRecipes) continue;
        //     Recipe.AddToAvailableRecipes(i);
        // }
        // ...

        ILCursor cursor = new(il);

        // Skip guide code
        ILLabel skipGuide = cursor.DefineLabel();
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(Recipe), nameof(Recipe.ClearAvailableRecipes)));
        cursor.EmitCall(EnabledField.GetMethod!);
        cursor.EmitBrtrue(skipGuide);
        cursor.GotoNext(i => i.MatchCall(typeof(Recipe), "CollectItemsToCraftWithFrom"));
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipGuide);

        // Filters
        ILLabel? endLoop = null;
        cursor.GotoNext(i => i.MatchCallvirt(DisabledProp.GetMethod!));
        cursor.GotoNext(MoveType.After, i => i.MatchBrtrue(out endLoop));
        cursor.EmitLdloc(4);
        cursor.EmitDelegate((Recipe r) => Enabled && !MatchFilters(r));
        cursor.EmitBrtrue(endLoop!);

        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdloc(5));
        cursor.EmitLdloc3();
        cursor.EmitDelegate((int i) => {
            _canCraft = false;
            if (Enabled && ShowAllRecipes) AddToAvailableRecipesMethod.Invoke(null, new object[] { i });
        });

        // Show All filter
        ILLabel skipAdd = cursor.DefineLabel();
        cursor.GotoNext(i => i.MatchCall(AddToAvailableRecipesMethod));
        cursor.GotoPrev(MoveType.AfterLabel);
        cursor.EmitDelegate(() => {
            if(!Enabled) return false;
            if(ShowAllRecipes) AvailableRecipesInfo[^1].CanCraft = true;
            else _canCraft = true;
            return ShowAllRecipes;
        });
        cursor.EmitBrtrue(skipAdd!);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(AddToAvailableRecipesMethod));
        cursor.MarkLabel(skipAdd);
    }

    public static bool MatchFilters(Recipe recipe){
        return MatchGuideItem(recipe);
    }

    public static bool MatchGuideItem(Recipe recipe){
        if (Main.guideItem.IsAir) return true;
        int type = Main.guideItem.type;
        if (recipe.createItem.type == type) return true;
        foreach (Item item in recipe.requiredItem) {
            if (Main.guideItem.type == item.type || (bool)useWoodMethod.Invoke(recipe, new object[] { type, item.type })! || (bool)useSandMethod.Invoke(recipe, new object[] { type, item.type })! || (bool)useIronBarMethod.Invoke(recipe, new object[] { type, item.type })! || (bool)useFragmentMethod.Invoke(recipe, new object[] { type, item.type })! || recipe.AcceptedByItemGroups(type, item.type) || (bool)usePressurePlateMethod.Invoke(recipe, new object[] { type, item.type })!)
                return true;
        }
        return false;
    }

    private static void UpdateHoveredItem(GameTime time) {
        if (HoverItemInfo.Type == Main.HoverItem.type) return;
        bool canBeCrafted = false;
        for (int i = 0; i < Recipe.maxRecipes; i++) {
            if (Main.recipe[i].Disabled || Main.recipe[i].createItem.type != Main.HoverItem.type) continue;
            canBeCrafted = true;
            break;
        }
        HoverItemInfo = new(Main.HoverItem.type, Main.HoverItem.material, canBeCrafted);
    }

    private static void CheckFindRecipes() {
        if (!Enabled) return;
        if (!FindItemRecipes.Current) {
            if (FindItemRecipes.JustReleased && _findRecipesFrames <= 15) {
                if (FocusRecipes() || !Main.recBigList) {
                    if(TryFocusRecipeList()) SoundEngine.PlaySound(SoundID.MenuTick);
                } else if (Main.recBigList) {
                    Main.recBigList = false;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            return;
        }
        if (FindItemRecipes.JustPressed) _findRecipesFrames = 0;
        _findRecipesFrames++;

        if (HoverItemInfo.Type <= 0) return;
        if (!HoverItemInfo.HasAnyRecipe) return;
        Main.cursorOverride = CursorOverrideID.Magnifiers;

        if (!Main.mouseLeft || !Main.mouseLeftRelease) return;
        _findRecipesFrames = 20;
        FocusRecipes();
        FindRecipes(HoverItemInfo.Type);
    }

    public static bool FocusRecipes(){
        Player player = Main.LocalPlayer;
        if (Main.CreativeMenu.Enabled) Main.CreativeMenu.CloseMenu();
        else if (player.tileEntityAnchor.InUse) player.tileEntityAnchor.Clear();
        else if (Main.InReforgeMenu) player.SetTalkNPC(-1);
        else return false;
        return true;
    }

    public static bool TryFocusRecipeList() => Main.recBigList = Main.numAvailableRecipes > 0;

    public static void FindRecipes(int type) {
        SoundEngine.PlaySound(SoundID.Grab);
        if (type != Main.guideItem.type) {
            Main.guideItem.SetDefaults(type);
            Recipe.FindRecipes(false);
        }
        if(HoverItemInfo.Type > ItemID.None) TryFocusRecipeList();
    }


    private static bool HookTryAllowingToCraftRecipe(On_Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        if(!Enabled) return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Enabled, out movedAnItemToAllowCrafting);
        movedAnItemToAllowCrafting = false;
        if(Main.mouseLeft && !Main.mouseLeftRelease) return false;
        if(ItemSlot.ShiftInUse && Main.mouseLeft) return Main.LocalPlayer.ItemSpace(currentRecipe.createItem).CanTakeItem;
        return orig(currentRecipe, tryFittingItemInInventoryToAllowCrafting || Enabled, out movedAnItemToAllowCrafting);
    }

    private static void ILCraftItem(ILContext il) {
        ILCursor cursor = new(il);

        ILLabel vanillaCheck = cursor.DefineLabel();
        ILLabel skipCheck = cursor.DefineLabel();

        // Shift Click check
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Enabled && ItemSlot.ShiftInUse && Main.mouseLeft);
        cursor.EmitBrfalse(vanillaCheck);
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => Main.LocalPlayer.ItemSpace(r.createItem).CanTakeItem);
        cursor.EmitBrtrue(skipCheck);
        cursor.EmitRet();
        cursor.MarkLabel(vanillaCheck);

        // Vanilla check
        cursor.GotoNext(i => i.MatchCallOrCallvirt(typeof(Recipe), nameof(Recipe.Create)));
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Recipe r) => {
            craftMultiplier = 1;
            if(!Enabled || !Main.mouseLeft) return;
            int amount = GetMaxCraftAmount(r);
            if(ItemSlot.ShiftInUse) craftMultiplier = System.Math.Min(amount, GetMaxPickupAmount(r.createItem) / r.createItem.stack);
            else craftMultiplier = System.Math.Min(amount, (r.createItem.maxStack - Main.mouseItem.stack) / r.createItem.stack);
        });
        cursor.GotoPrev(MoveType.After, i => i.MatchRet());
        cursor.MarkLabel(skipCheck);

        // Shift Click grab
        ILLabel normalCraftItemCode = cursor.DefineLabel();
        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt(typeof(RecipeLoader), nameof(RecipeLoader.OnCraft)));
        cursor.EmitLdloc0();
        cursor.EmitDelegate((Item crafted) => {
            crafted.stack *= craftMultiplier;
            if (!Enabled || !ItemSlot.ShiftInUse || !Main.mouseLeft) return false;
            craftMultiplier = 1;
            Main.LocalPlayer.GetItem(Main.myPlayer, crafted, GetItemSettings.InventoryUIToInventorySettingsShowAsNew);
            return true;
        });
        cursor.EmitBrfalse(normalCraftItemCode);
        cursor.EmitRet();
        cursor.MarkLabel(normalCraftItemCode);

        // Mouse text correction
        cursor.GotoNext(i => i.MatchCall(typeof(PopupText), nameof(PopupText.NewText)));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld(typeof(Item), nameof(Item.stack)));
        cursor.EmitDelegate((int stack) => {
            int c = stack * craftMultiplier;
            craftMultiplier = 1;
            return c;
        });

    }


    private static void ILCreateRecipe(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(RecipeLoader), nameof(RecipeLoader.ConsumeItem)));
        cursor.EmitLdloca(4);
        cursor.EmitDelegate((ref int consumed) => { consumed *= craftMultiplier; });
    }

    public static int GetMaxCraftAmount(Recipe recipe){
        Dictionary<int, int> groupItems = new();
        foreach(int id in recipe.acceptedGroups){
            RecipeGroup group = RecipeGroup.recipeGroups[id];
            groupItems.Add(group.IconicItemId, group.GetGroupFakeItemId());
        }

        int amount = 0;
        foreach(Item material in recipe.requiredItem){
            int a = _ownedMaterials[groupItems.GetValueOrDefault(material.type, material.type)] / material.stack;
            if(amount == 0 || a < amount) amount = a;
        }
        return amount;
    }
    
    public static int GetMaxPickupAmount(Item item, int max = -1){
        if(max == -1) max = item.maxStack;
        int free = GetFreeSpace(Main.LocalPlayer.inventory, item, 58);
        if(Main.LocalPlayer.InChest(out Item[]? chest)) free += GetFreeSpace(chest, item);
        if(Main.LocalPlayer.useVoidBag() && Main.LocalPlayer.chest != -5) free += GetFreeSpace(Main.LocalPlayer.bank4.item, item);
        return System.Math.Min(max, free);
    }

    public static int GetFreeSpace(Item[] inv, Item item, params int[] ignored){
        int free = 0;
        for (int i = 0; i < inv.Length; i++) {
            if(System.Array.IndexOf(ignored, i) != -1) continue;
            Item slot = inv[i];
            if (slot.IsAir) free += item.maxStack;
            if (slot.type == item.type) free += item.maxStack - slot.stack;
        }
        return free;
    }

    public static RecipeState GetRecipeState(int recipe) => FavoritedRecipes.Contains(recipe) ? RecipeState.Favorited : BlacklistedRecipes.Contains(recipe) ? RecipeState.Blacklisted : RecipeState.Default;

    public static Asset<Texture2D> EyeBorder => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Border");
    public static Asset<Texture2D> EyeForced => ModContent.Request<Texture2D>($"BetterInventory/Assets/Inventory_Tick_Forced");
    public static Asset<Texture2D> CursorCraft => ModContent.Request<Texture2D>($"BetterInventory/Assets/Cursor_Craft");

    public const int CraftCursorID = 22;

    public static bool Enabled => Configs.ClientConfig.Instance.betterCrafting;

    public static readonly List<RecipeInfo> AvailableRecipesInfo = new();

    public static bool ShowAllRecipes => Main.guideItem.IsAir ? showAllRecipes : showAllRecipesWithGuide;
    public static bool showAllRecipesWithGuide = true;
    public static bool showAllRecipes = false;
    public static bool AlternateGuideItem => Enabled && !Main.InGuideCraftMenu;

    public static int craftMultiplier = 1;

    public static HoverItemCache HoverItemInfo { get; private set; }

    public static ModKeybind FindItemRecipes { get; private set; } = null!;
    private static int _findRecipesFrames = 0;
    private static Asset<Texture2D> _inventoryBack4 = null!;
    private static Dictionary<int, int> _ownedMaterials = null!;
    private static bool _inHoverCrafting;
    private static bool _canCraft;
    public static readonly List<int> FavoritedRecipes = new();
    public static readonly List<int> BlacklistedRecipes = new();

    public static readonly PropertyInfo EnabledField = typeof(BetterCrafting).GetProperty(nameof(Enabled), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly PropertyInfo AlternateGuideItemProp = typeof(BetterCrafting).GetProperty(nameof(AlternateGuideItem), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly PropertyInfo DisabledProp = typeof(Recipe).GetProperty(nameof(Recipe.Disabled), BindingFlags.Instance | BindingFlags.Public)!;
    public static readonly FieldInfo RecBigListField = typeof(Main).GetField(nameof(Main.recBigList), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly FieldInfo InGuideCraftMenuField = typeof(Main).GetField(nameof(Main.InGuideCraftMenu), BindingFlags.Static | BindingFlags.Public)!;
    
    public static readonly MethodInfo useWoodMethod = typeof(Recipe).GetMethod("useWood", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useIronBarMethod = typeof(Recipe).GetMethod("useIronBar", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useSandMethod = typeof(Recipe).GetMethod("useSand", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo useFragmentMethod = typeof(Recipe).GetMethod("useFragment", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo usePressurePlateMethod = typeof(Recipe).GetMethod("usePressurePlate", BindingFlags.Instance | BindingFlags.NonPublic)!;
    
    public static readonly FieldInfo OwnedItemsField = typeof(Recipe).GetField("_ownedItems", BindingFlags.Static | BindingFlags.NonPublic)!;
    // public static readonly MethodInfo CollectGuideRecipesMethod = typeof(Recipe).GetMethod("CollectGuideRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo AddToAvailableRecipesMethod = typeof(Recipe).GetMethod("AddToAvailableRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo TryRefocusingRecipeMethod = typeof(Recipe).GetMethod("TryRefocusingRecipe", BindingFlags.Static | BindingFlags.NonPublic)!;
    public static readonly MethodInfo VisuallyRepositionRecipesMethod = typeof(Recipe).GetMethod("VisuallyRepositionRecipes", BindingFlags.Static | BindingFlags.NonPublic)!;
}

public readonly record struct HoverItemCache(int Type, bool Material, bool CanBeCrafted) {
    public bool HasAnyRecipe => Material || CanBeCrafted;
}

public enum RecipeState {Default, Blacklisted, Favorited }


public class RecipeInfo{
    public RecipeInfo(bool canCraft, RecipeState state){
        State = state;
        CanCraft = canCraft;
    }
    public RecipeState State { get; }
    public bool CanCraft { get; internal set; }
    public bool Favorited => State == RecipeState.Favorited;
    public bool Blacklisted => State == RecipeState.Blacklisted;
}