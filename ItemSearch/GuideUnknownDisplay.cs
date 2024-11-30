using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
namespace BetterInventory.ItemSearch;

public sealed class GuideUnknownDisplay : ILoadable {
    public void Load(Mod mod) {
        On_Recipe.CollectItemsToCraftWithFrom += HookUpdatedOwnedItems;
        On_Recipe.CollectGuideRecipes += HookRefreshUnknownRecipes;

        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILHideMaterials, Configs.BetterGuide.UnknownDisplay)) Configs.UnloadedItemSearch.Value.guideUnknown = true;
        };

        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItem;
        On_ItemSlot.DrawItemIcon += HookHideItem;

        _unknownFilters = new(() => Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay != Configs.UnknownDisplay.Known, r => {
            if(!GuideCraftInMenu.LocalFilters.IsRecipeKnown(r)) {
                if(Configs.BetterGuide.Value.unknownDisplay == Configs.UnknownDisplay.Hidden) return false;
                s_unknownCreateItems.Add(r.createItem.UniqueId());
            }
            return true;
        });
        GuideRecipeFiltering.AddFilter(_unknownFilters);

        s_unknownTexture = mod.Assets.Request<Texture2D>($"Assets/Unknown_Item");
    }
    public void Unload() { }

    private static void HookUpdatedOwnedItems(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player) {
        orig(player);
        if (player.whoAmI != Main.myPlayer) return;
        foreach (var key in PlayerHelper.OwnedItems.Keys) {
            if (key < 1000000 && !GuideCraftInMenu.LocalFilters.HasOwnedItem(key)) GuideCraftInMenu.LocalFilters.AddOwnedItem(new(key));
        }
    }

    private static void HookRefreshUnknownRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
        s_unknownCreateItems.Clear();
        orig();
    }

    private static void HookHideItem(On_ItemSlot.orig_Draw_SpriteBatch_refItem_int_Vector2_Color orig, SpriteBatch spriteBatch, ref Item inv, int context, Vector2 position, Color lightColor) {
        if(context != ItemSlot.Context.CraftingMaterial || !Configs.BetterGuide.UnknownDisplay || !IsUnknown(inv)) {
            orig(spriteBatch, ref inv, context, position, lightColor);
            return;
        }
        _hideItem = true;
        Item item = new(PlaceholderItem.FakeType);
        orig(spriteBatch, ref item, context, position, lightColor);
        _hideItem = false;
    }
    private static float HookHideItem(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        if (_hideItem) {
            return spriteBatch.DrawTexture(s_unknownTexture.Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit);
        }
        return orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
    }

    private static void ILHideMaterials(ILContext il) {
        ILCursor cursor = new(il);

        //     if (++<known> && Main.numAvailableRecipes > 0) {
        cursor.GotoNext(i => i.MatchStsfld(Reflection.UILinkPointNavigator.CRAFT_CurrentIngredientsCount));
        cursor.GotoPrev(MoveType.After, i => i.MatchLdsfld(Reflection.Main.numAvailableRecipes));
        cursor.EmitDelegate((int num) => num == 0 || Configs.BetterGuide.UnknownDisplay && IsUnknown(Main.recipe[Main.availableRecipe[Main.focusRecipe]].createItem) ? 0 : num);
    }

    public static bool IsUnknown(Item createItem) => !createItem.IsAir && s_unknownCreateItems.Contains(createItem.UniqueId());

    private static GuideRecipeFilterGroup _unknownFilters = null!;
    private static readonly HashSet<Guid> s_unknownCreateItems = [];

    private static bool _hideItem;
    private static Asset<Texture2D> s_unknownTexture = null!;
}