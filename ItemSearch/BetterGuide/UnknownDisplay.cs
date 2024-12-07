using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using SpikysLib;
using SpikysLib.DataStructures;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.ItemSearch.BetterGuide;

public sealed class UnknownDisplayPlayer : ModPlayer {
    public static UnknownDisplayPlayer LocalPlayer => Main.LocalPlayer.GetModPlayer<UnknownDisplayPlayer>();

    public override void Load() {
        On_Recipe.CollectItemsToCraftWithFrom += HookUpdatedOwnedItems;
        On_Recipe.CollectGuideRecipes += HookRefreshUnknownRecipes;

        IL_Main.DrawInventory += static il => {
            if (!il.ApplyTo(ILHideMaterials, Configs.BetterGuide.UnknownDisplay)) Configs.UnloadedItemSearch.Value.guideUnknown = true;
        };

        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItem;
        On_ItemSlot.DrawItemIcon += HookHideItemIcon;

        _unknownFilters = new(() => Configs.BetterGuide.UnknownDisplay && Configs.BetterGuide.Value.unknownDisplay != Configs.UnknownDisplay.Known, r => {
            if(!LocalPlayer.IsRecipeKnown(r)) {
                if(Configs.BetterGuide.Value.unknownDisplay == Configs.UnknownDisplay.Hidden) return false;
                s_unknownCreateItems.Add(r.createItem.UniqueId());
            }
            return true;
        });
        RecipeFiltering.AddFilter(_unknownFilters);

        s_unknownTexture = Mod.Assets.Request<Texture2D>($"Assets/Unknown_Item");
    }

    private static void HookUpdatedOwnedItems(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player) {
        orig(player);
        if (player.whoAmI != Main.myPlayer) return;
        foreach (var key in PlayerHelper.OwnedItems.Keys) {
            if (key < 1000000 && !LocalPlayer.HasOwnedItem(key)) LocalPlayer.AddOwnedItem(new(key));
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
    private static float HookHideItemIcon(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
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

    public static bool IsUnknown(int recipe) => IsUnknown(Main.recipe[recipe].createItem);
    public static bool IsUnknown(Item createItem) => !createItem.IsAir && s_unknownCreateItems.Contains(createItem.UniqueId());

    public bool IsRecipeKnown(Recipe recipe) {
        if (HasOwnedItem(recipe.createItem) || recipe.requiredItem.Exists(i => HasOwnedItem(i))) return true;
        foreach (int group in recipe.acceptedGroups) {
            foreach (int type in RecipeGroup.recipeGroups[group].ValidItems) {
                if (HasOwnedItem(type)) return true;
            }
        }
        return false;
    }

    public bool HasOwnedItem(Item item) => ownedItems.TryGetValue(item.ModItem?.Mod.Name ?? "Terraria", out var items) && items.Contains(item.type);
    public bool HasOwnedItem(int type) {
        foreach (RangeSet set in ownedItems.Values) if (set.Contains(type)) return true;
        return false;
    }
    public bool AddOwnedItem(Item item) => AddOwnedItem(item.ModItem?.Mod.Name ?? "Terraria", item.type);
    public bool AddOwnedItem(string mod, int type) {
        if (!ownedItems.ContainsKey(mod)) ownedItems.Add(mod, []);
        return ownedItems[mod].Add(type);
    }

    public readonly Dictionary<string, RangeSet> ownedItems = [];
    public readonly List<ItemDefinition> unloadedItems = [];
    private const string OwnedTag = "owned";

    public override void SaveData(TagCompound tag) {
        List<ItemDefinition> owned = [];
        foreach ((string mod, RangeSet set) in ownedItems) {
            foreach (SpikysLib.DataStructures.Range range in set.Ranges) {
                owned.Add(new(range.Start));
                owned.Add(new(range.End - 1));
            }
        }
        owned.AddRange(unloadedItems);
        if (owned.Count != 0) tag[OwnedTag] = owned;

    }
    public override void LoadData(TagCompound tag) {
        if (!tag.TryGet(OwnedTag, out IList<ItemDefinition> owned)) return;
        for (int i = 0; i < owned.Count; i += 2) {
            if (owned[i].IsUnloaded) {
                unloadedItems.Add(owned[i]);
                unloadedItems.Add(owned[i + 1]);
            } else {
                if (!ownedItems.ContainsKey(owned[i].Mod)) ownedItems.Add(owned[i].Mod, []);
                ownedItems[owned[i].Mod].Add(new SpikysLib.DataStructures.Range(owned[i].Type, owned[i + 1].Type + 1));
            }
        }
    }

    private static GuideRecipeFilterGroup _unknownFilters = null!;
    private static readonly HashSet<Guid> s_unknownCreateItems = [];

    private static bool _hideItem;
    private static Asset<Texture2D> s_unknownTexture = null!;
}