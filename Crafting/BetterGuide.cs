using System.IO;
using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.Crafting;


public static class BetterGuide {

    public static bool Enabled => Configs.ClientConfig.Instance.betterCrafting;

    public static ModKeybind SearchItem { get; private set; } = null!;
    private static int _searchItemTimer = 0;
    private static int _searchItemClicks = 0;

    public static void Load(){
        IL_Main.DrawInventory += IlDrawInventory;

        On_Main.DrawInterface_36_Cursor += HookDrawCursor;

        IL_Recipe.CollectGuideRecipes += ILGuideRecipes;

        SearchItem = KeybindLoader.RegisterKeybind(BetterInventory.Instance, "SearchItem", Microsoft.Xna.Framework.Input.Keys.N);

        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;

        On_Player.dropItemCheck += OndropItems;
        On_Player.SaveTemporaryItemSlotContents += HookSaveTemporaryItemSlotContents;
    }

    private static void HookDrawCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        if(Enabled) ProcessSearchItem();
        orig();
    }

    private static void ILGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);
        ILLabel? endLoop = null;
        // for (...) {
        //     ...
        //     if (recipe.Disabled) continue;
        cursor.GotoNext(i => i.MatchCallvirt(typeof(Recipe).GetProperty(nameof(Recipe.Disabled))!.GetMethod!));
        cursor.GotoNext(i => i.MatchBrtrue(out endLoop));

        //     ++ if(<extraRecipe>) {
        //        <addRecipe>
        //        continue;
        //    }
        cursor.GotoNext(MoveType.AfterLabel);
        cursor.EmitLdloc1();
        cursor.EmitDelegate<System.Func<int, bool>>(i => {
            if (Enabled && Main.recipe[i].createItem.type == Main.guideItem.type) {
                Utility.AddToAvailableRecipesMethod.Invoke(null, new object[] { i });
                return true;
            }
            return false;
        });
        cursor.EmitBrtrue(endLoop!);
        // }
    }

    private static void IlDrawInventory(ILContext il) {
        ILCursor cursor = new(il);

        // if(Main.InReforgeMenu){
        //     ...
        // } else if(Main.InGuideCraftMenu) {
        //     if(<closeGuideUI>) ...
        //     else {
        ILLabel? endGuide = null;
        cursor.GotoNext(i => i.MatchCall(typeof(Main), "DrawGuideCraftText"));
        cursor.GotoPrev(MoveType.After, i => i.MatchBr(out endGuide));

        //         ++ guide:
        ILLabel guide = cursor.DefineLabel();
        cursor.MarkLabel(guide);

        //         <draw guide item slot>
        cursor.GotoLabel(endGuide!, MoveType.Before);

        //         ++ if(!Main.InGuideCraftMenu) goto recipe;
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitCall(AlternateGuideItemProp.GetMethod!);
        cursor.EmitBrtrue(recipe);
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);

        //     ++ if(!Main.InGuideCraftMenu) goto guide;
        cursor.EmitCall(AlternateGuideItemProp.GetMethod!);
        cursor.EmitBrtrue(guide);

        //     ++ recipe:
        cursor.MarkLabel(recipe);
        //     ...
        // }
    }

    public static void UpdateMouseItem() {
        if (!Enabled) Main.guideItem.TurnToAir();
        else if (!Main.guideItem.IsAir) {
            Main.LocalPlayer.GetDropItem(ref Main.guideItem);
            Main.guideItem = new(Main.guideItem.type);
        }
    }

    public static void ProcessClickSearch(){
        TryDelaySetBestiaryItem();
        _searchItemTimer++;
        if (SearchItem.JustReleased) {
            if (_searchItemTimer <= 15) {
                if (Main.InGameUI.CurrentState == Main.BestiaryUI) {
                    ToggleBestiary();
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _searchItemClicks = -1;
                } else {
                    if (_searchItemClicks % 2 == 0) {
                        ToggleRecipeList();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    } else {
                        ToggleRecipeList(false);
                        ToggleBestiary();
                    }
                }
                _searchItemClicks++;
            } else _searchItemClicks = 0;
            _searchItemTimer = 0;
        }

        if (SearchItem.JustPressed) {
            if (_searchItemTimer > 15) _searchItemClicks = 0;
            _searchItemTimer = 0;
        }
    }

    public static void ProcessSearchItem() {
        if (SearchItem.Current) {
            if (Main.HoverItem.IsAir) {
                if(Main.cursorOverride == CursorOverrideID.Magnifiers) Main.cursorOverride = -1;
                return;
            }
            Main.cursorOverride = CursorOverrideID.Magnifiers;
            if (Main.mouseLeft && Main.mouseLeftRelease) {
                _searchItemTimer = 30;
                SetGuideItem(Main.HoverItem.type);
                ToggleRecipeList(true);
            } else if (Main.mouseRight && Main.mouseRightRelease) {
                ToggleBestiary(true);
                SetBestiaryItem(Main.HoverItem.type, true);
                _searchItemTimer = 30;
            }
            return;
        } else if (Main.cursorOverride == CursorOverrideID.Magnifiers) Main.cursorOverride = -1;
    }

    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled) {
            orig(inv, context, slot);
            return;
        }
        if (SearchItem.Current) return;
        if (context == ItemSlot.Context.GuideItem && ((!Main.guideItem.IsAir && Main.mouseItem.IsAir) || ItemSlot.ShiftInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        else if (Main.InGuideCraftMenu && ItemSlot.ShiftInUse) Main.cursorOverride = CursorOverrideID.Magnifiers;
        else orig(inv, context, slot);
    }

    private static bool HookOverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!Enabled) return orig(inv, context, slot);
        if (SearchItem.Current) return true;

        if (context == ItemSlot.Context.GuideItem) {
            if (Main.cursorOverride == CursorOverrideID.TrashCan) {
                SetGuideItem(ItemID.None);
                ToggleRecipeList(false);
            } else {
                SetGuideItem(Main.mouseItem.type);
                ToggleRecipeList(true);
                Main.LocalPlayer.GetDropItem(ref Main.mouseItem);
            }
            return true;
        }
        if (Main.InGuideCraftMenu && Main.cursorOverride == CursorOverrideID.Magnifiers) {
            SetGuideItem(inv[slot].type);
            ToggleRecipeList(true);
            return true;
        }
        return orig(inv, context, slot);
    }

    private static void HookRightClick(On_ItemSlot.orig_RightClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (SearchItem.Current) return;
        if (Enabled && context == ItemSlot.Context.GuideItem && Main.mouseRight && Main.mouseRightRelease) {
            SetGuideItem(ItemID.None);
            ToggleRecipeList(false);
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

    public static void ToggleRecipeList(bool? enabled = null) {
        if (Main.playerInventory && Main.recBigList) {
            if(enabled == true) return;
            Main.recBigList = false;
        } else {
            if(enabled == false) return;
            if(!Main.playerInventory) {
                Main.LocalPlayer.ToggleInv();
                if(!Main.playerInventory) Main.LocalPlayer.ToggleInv();
            } else {
                Main.CreativeMenu.CloseMenu();
                Main.LocalPlayer.tileEntityAnchor.Clear();
                Main.LocalPlayer.SetTalkNPC(-1);
            }
            Main.recBigList = Main.numAvailableRecipes > 0;
        }
    }

    public static void ToggleBestiary(bool? enabled = null){
        if(Main.InGameUI.CurrentState == Main.BestiaryUI) {
            if(enabled == true) return;
            // ? Main.BestiaryUI.Click_GoBack(null, null);
            IngameFancyUI.Close();
        } else { // TODO sound
            if(enabled == false) return;
            // ? Reset Filters
            Main.LocalPlayer.SetTalkNPC(-1, false);
            Main.npcChatCornerItem = 0;
            Main.npcChatText = "";
            Main.mouseLeftRelease = false;
            IngameFancyUI.OpenUIState(Main.BestiaryUI);
            Main.BestiaryUI.OnOpenPage();
        }
    }
    
    public static void SetGuideItem(int type) {
        if (type != Main.guideItem.type) {
            Main.guideItem.SetDefaults(type);
            Recipe.FindRecipes(false);
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }

    public static void SetBestiaryItem(int type, bool delayed = false) {
        if (delayed) {
            _delay = 2;
            _bestiaryDelayedType = type;
            return;
        }
        static void PlayNoise(string content) => SoundEngine.PlaySound(SoundID.Grab);
        UISearchBar searchBar = (UISearchBar)BestiarySearchBarField.GetValue(Main.BestiaryUI)!;
        searchBar.OnContentsChanged += PlayNoise;        
        searchBar.SetContents(Lang.GetItemNameValue(type), true);
        if(searchBar.IsWritingText) searchBar.ToggleTakingText();
        searchBar.OnContentsChanged -= PlayNoise;
        UIBestiaryEntryGrid grid = (UIBestiaryEntryGrid)BestiaryGridField.GetValue(Main.BestiaryUI)!;
        foreach(UIElement element in grid.Children){
            if(element is not UIBestiaryEntryButton button) continue;
            SelectEntryButtonMethod.Invoke(Main.BestiaryUI, new object[] { button });
            break;
        }
    }

    public static void TryDelaySetBestiaryItem(){
        _delay--;
        if(!_bestiaryDelayedType.HasValue || _delay > 0) return;
        SetBestiaryItem(_bestiaryDelayedType.Value);
        _bestiaryDelayedType = null;
    }

    private static int _delay;
    private static int? _bestiaryDelayedType;

    public static bool AlternateGuideItem => Enabled && !Main.InGuideCraftMenu;

    public static readonly PropertyInfo AlternateGuideItemProp = typeof(BetterGuide).GetProperty(nameof(AlternateGuideItem), BindingFlags.Static | BindingFlags.Public)!;
    public static readonly MethodInfo SetBestiaryTextMethod = typeof(UIBestiaryTest).GetMethod("OnFinishedSettingName", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiarySearchBarField = typeof(UIBestiaryTest).GetField("_searchBar", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly FieldInfo BestiaryGridField = typeof(UIBestiaryTest).GetField("_entryGrid", BindingFlags.Instance | BindingFlags.NonPublic)!;
    public static readonly MethodInfo SelectEntryButtonMethod = typeof(UIBestiaryTest).GetMethod("SelectEntryButton", BindingFlags.Instance | BindingFlags.NonPublic)!;

}
