using System.IO;
using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterInventory.ItemSearch;


public sealed class BetterGuide : ILoadable {

    public static bool Enabled => Configs.ClientConfig.Instance.betterGuide;

    public void Load(Mod mod){
        SearchItem = KeybindLoader.RegisterKeybind(mod, "SearchItem", Microsoft.Xna.Framework.Input.Keys.N);
        On_Main.DrawInterface_36_Cursor += HookDrawCursor;

        On_ItemSlot.OverrideHover_ItemArray_int_int += HookOverrideHover;
        On_ItemSlot.OverrideLeftClick += HookOverrideLeftClick;
        On_ItemSlot.RightClick_ItemArray_int_int += HookRightClick;
        
        On_Player.dropItemCheck += OndropItems;
        On_Player.SaveTemporaryItemSlotContents += HookSaveTemporaryItemSlotContents;
        
        IL_Recipe.CollectGuideRecipes += ILGuideRecipes;
        
        IL_Main.DrawInventory += IlDrawInventory;
    }
    public void Unload(){}


    private static void HookDrawCursor(On_Main.orig_DrawInterface_36_Cursor orig) {
        if(!Enabled && !Bestiary.Enabled){
            orig();
            return;
        }
        if (SearchItem.Current) {
            if (Main.HoverItem.IsAir) {
                if (Main.cursorOverride == CursorOverrideID.Magnifiers) Main.cursorOverride = -1;
            } else {
                Main.cursorOverride = CursorOverrideID.Magnifiers;
                if (Enabled && Main.mouseLeft && Main.mouseLeftRelease) {
                    s_searchItemTimer = 30;
                    bool? rec = Main.HoverItem.type == Main.guideItem.type ? Main.recBigList : null;
                    SetGuideItem(Main.HoverItem.type);
                    ToggleRecipeList(true);
                    if(rec.HasValue && rec != Main.recBigList) SoundEngine.PlaySound(SoundID.Grab);
                } else if (Bestiary.Enabled && Main.mouseRight && Main.mouseRightRelease) {
                    bool delay = Main.InGameUI.CurrentState != Main.BestiaryUI;
                    Bestiary.ToggleBestiary(true);
                    Bestiary.SetBestiaryItem(Main.HoverItem.type, delay);
                    s_searchItemTimer = 30;
                }
            }
        } else if (!Main.playerInventory && Main.cursorOverride == CursorOverrideID.Magnifiers) Main.cursorOverride = -1;
        orig();
    }
    public static void ProcessSearchTap() {
        if (!Enabled && !Bestiary.Enabled) return;
        s_searchItemTimer++;
        if (SearchItem.JustReleased) {
            if (s_searchItemTimer <= 15) {
                if (Main.InGameUI.CurrentState == Main.BestiaryUI) {
                        Bestiary.ToggleBestiary();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        s_searchItemTaps = -1;
                } else {
                    if (s_searchItemTaps % 2 == 0) {
                        ToggleRecipeList();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    } else {
                        ToggleRecipeList(false);
                        Bestiary.ToggleBestiary();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                }
                s_searchItemTaps++;
            } else s_searchItemTaps = 0;
            s_searchItemTimer = 0;
        }

        if (SearchItem.JustPressed) {
            if (s_searchItemTimer > 15) s_searchItemTaps = 0;
            s_searchItemTimer = 0;
        }
    }


    private static void HookOverrideHover(On_ItemSlot.orig_OverrideHover_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (!Enabled) {
            orig(inv, context, slot);
            return;
        }
        if (SearchItem.Current) return;
        if (context == ItemSlot.Context.GuideItem && ((!Main.guideItem.IsAir && Main.mouseItem.IsAir) || ItemSlot.ShiftInUse)) Main.cursorOverride = CursorOverrideID.TrashCan;
        else if (Main.InGuideCraftMenu && ItemSlot.ShiftInUse && !inv[slot].IsAir) Main.cursorOverride = CursorOverrideID.Magnifiers;
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
    public static void UpdateMouseItem() {
        if (!Enabled) Main.guideItem.TurnToAir();
        else if (!Main.guideItem.IsAir) {
            Main.LocalPlayer.GetDropItem(ref Main.guideItem);
            Main.guideItem = new(Main.guideItem.type);
        }
    }


    private static void ILGuideRecipes(ILContext il) {
        ILCursor cursor = new(il);
        ILLabel? endLoop = null;
        // for (<recipeIndex>) {
        //     ...
        //     if (recipe.Disabled) continue;
        cursor.GotoNext(i => i.MatchCallvirt(typeof(Recipe).GetProperty(nameof(Recipe.Disabled))!.GetMethod!));
        cursor.GotoNext(i => i.MatchBrtrue(out endLoop));
        cursor.GotoNext(MoveType.AfterLabel);

        //     ++ if(<extraRecipe>) {
        //     ++     <addRecipe>
        //     ++     continue;
        //     ++ }
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

        //         ++ if(<alternateGuideDraw>) goto recipe;
        ILLabel recipe = cursor.DefineLabel();
        cursor.EmitDelegate(() => Enabled && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(recipe);
        //     }
        // }

        // ...
        // if(<showRecipes>){
        cursor.GotoNext(i => i.MatchStsfld(typeof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts), nameof(Terraria.UI.Gamepad.UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig)));
        cursor.GotoPrev(MoveType.AfterLabel);

        //     ++ if(<alternateGuideDraw>) goto guide;
        cursor.EmitDelegate(() => Enabled && !Main.InGuideCraftMenu);
        cursor.EmitBrtrue(guide);

        //     ++ recipe:
        cursor.MarkLabel(recipe);
        //     ...
        // }
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
                if(!Main.InGuideCraftMenu) Main.LocalPlayer.SetTalkNPC(-1); // Not if talking to guide
            }
            Main.recBigList = Main.numAvailableRecipes > 0;
        }
    }
    public static void SetGuideItem(int type) {
        if (type != Main.guideItem.type) {
            Main.guideItem.SetDefaults(type);
            Recipe.FindRecipes(false);
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }


    public static ModKeybind SearchItem { get; private set; } = null!;
    private static int s_searchItemTimer = 0, s_searchItemTaps = 0;

    public static readonly MethodInfo SetBestiaryTextMethod = typeof(UIBestiaryTest).GetMethod("OnFinishedSettingName", BindingFlags.Instance | BindingFlags.NonPublic)!;
}
