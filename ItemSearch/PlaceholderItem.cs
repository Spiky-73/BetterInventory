using System.Collections.Generic;
using BetterInventory.Default.Catalogues;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SpikysLib;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BetterInventory.ItemSearch;

public sealed class PlaceholderItem : GlobalItem {
    
    public int tile = -1;
    public LocalizedText? condition;
    public bool IsAPlaceholder => tile != -1 || condition is not null;

    public override void Load() {
        On_ItemSlot.DrawItemIcon += HookDrawPlaceholder;
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookPlaceholderTooltip);
        On_ItemSlot.Draw_SpriteBatch_refItem_int_Vector2_Color += HookHideItem;
        On_ItemSlot.OverrideLeftClick += HookFakeItemLeftClick;

        ConditionItems["Conditions.NearWater"] = ItemID.WaterBucket;
        ConditionItems["Conditions.NearLava"] = ItemID.LavaBucket;
        ConditionItems["Conditions.NearHoney"] = ItemID.HoneyBucket;
        ConditionItems["Conditions.InGraveyard"] = ItemID.Gravestone;
        ConditionItems["Conditions.InSnow"] = ItemID.SnowBlock;

        s_unknownTexture = Mod.Assets.Request<Texture2D>($"Assets/Unknown_Item");

    }
    public override void Unload() {
        ConditionItems.Clear();
    }

    public sealed override bool CanStack(Item destination, Item source) => CanPlaceholderStack(source);
    public sealed override bool CanStackInWorld(Item destination, Item source) => CanPlaceholderStack(source);
    public bool CanPlaceholderStack(Item source) => !IsAPlaceholder && !source.IsAPlaceholder();

    public sealed override bool InstancePerEntity => true;
    public sealed override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == FakeType;

    public override void SaveData(Item item, TagCompound tag) {
        if (tile != -1) tag[TileTag] = tile;
        else if (condition is not null) tag[ConditionTag] = condition.Key;
    }
    public override void LoadData(Item item, TagCompound tag) {
        if (tag.TryGet(TileTag, out int t)) tile = t;
        else if (tag.TryGet(ConditionTag, out string c)) condition = Language.GetText(c);
    }
    public const string TileTag = "tile";
    public const string ConditionTag = "condition";

    private static void HookHideItem(On_ItemSlot.orig_Draw_SpriteBatch_refItem_int_Vector2_Color orig, SpriteBatch spriteBatch, ref Item inv, int context, Vector2 position, Color lightColor) {
        if (hideNextItem) {
            Item item = new(FakeType);
            orig(spriteBatch, ref item, context, position, lightColor);
            hideNextItem = false;
        } else {
            orig(spriteBatch, ref inv, context, position, lightColor);
        }
    }

    private static float HookDrawPlaceholder(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
        if (hideNextItem) {
            return spriteBatch.DrawTexture(s_unknownTexture.Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit);
        }
        if(!item.IsAir && item.TryGetGlobalItem(out PlaceholderItem placeholder)) {
            if(placeholder.tile == ByHandTile) {
                Main.instance.LoadItem(ItemID.BoneGlove);
                return spriteBatch.DrawTexture(TextureAssets.Item[ItemID.BoneGlove].Value, Color.White, screenPositionForItemCenter, ref scale, sizeLimit);
            } else if (placeholder.tile >= 0) {
                GraphicsHelper.DrawTileFrame(spriteBatch, placeholder.tile, screenPositionForItemCenter, new Vector2(0.5f, 0.5f), scale);
                return scale;
            }
        }
        return orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
    }

    private static List<TooltipLine> HookPlaceholderTooltip(Reflection.ItemLoader.ModifyTooltipsFn orig, Item item, ref int numTooltips, string[] names, ref string[] text, ref bool[] modifier, ref bool[] badModifier, ref int oneDropLogo, out Color?[] overrideColor, int prefixlineIndex) {
        string? name = null;
        if (hideTooltip) name = Language.GetTextValue($"{Localization.Keys.UI}.Unknown");

        if (!item.IsAir && name is null && item.TryGetGlobalItem(out PlaceholderItem placeholder)) {
            if(placeholder.tile == ByHandTile) name = Language.GetTextValue($"{Localization.Keys.UI}.ByHand");
            else if (placeholder.tile >= 0) name = Lang.GetMapObjectName(MapHelper.TileToLookup(placeholder.tile, 0));
            else if (placeholder.condition is not null) name = placeholder.condition.Value;
        }
        if (name is null) return orig.Invoke(item, ref numTooltips, names, ref text, ref modifier, ref badModifier, ref oneDropLogo, out overrideColor, prefixlineIndex);

        List<TooltipLine> tooltips = [new(BetterInventory.Instance, names[0], name)];
        numTooltips = 1;
        text = [tooltips[0].Text];
        modifier = [tooltips[0].IsModifier];
        badModifier = [tooltips[0].IsModifierBad];
        oneDropLogo = -1;
        overrideColor = [null];
        return tooltips;
    }

    public static Item FromTile(int tile) {
        if (Guide.CraftingStationsItems.TryGetValue(tile, out int type) && type != ItemID.None) return new(type);
        Item item = new(FakeType);
        item.GetGlobalItem<PlaceholderItem>().tile = tile;
        return item;
    }
    public static Item FromCondition(Condition condition) {
        if (ConditionItems.TryGetValue(condition.Description.Key, out int type)) {
            Item item = new(type);
            item.SetNameOverride(condition.Description.Value);
            return item;
        }
        Item placeholder = new(FakeType);
        placeholder.GetGlobalItem<PlaceholderItem>().condition = condition.Description;
        return placeholder;
    }

    public const int FakeType = ItemID.Lens;
    public const int ByHandTile = -2;
    public static readonly Dictionary<string, int> ConditionItems = []; // description -> id

    public static bool hideNextItem;
    public static bool hideTooltip;
    private static Asset<Texture2D> s_unknownTexture = null!;

    public static bool IsFakeItem(Item[] inv, int context, int slot) => context == ItemSlot.Context.GuideItem && !inv[slot].IsAir && (RecipeList.Instance.Enabled || inv[slot].IsAPlaceholder());
    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if(!IsFakeItem(inv, context, slot)) return false;
        if (Main.mouseItem.IsAir || ItemSlot.ShiftInUse || ItemSlot.ControlInUse) Main.cursorOverride = CursorOverrideID.TrashCan;
        if (ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == -1) Main.cursorOverride = CursorOverrideID.TrashCan;
        return true;
    }

    private static bool HookFakeItemLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot) {
        if (!IsFakeItem(inv, context, slot)) return orig(inv, context, slot);
        inv[slot].TurnToAir();
        Recipe.FindRecipes();
        SoundEngine.PlaySound(SoundID.Grab);
        return Main.cursorOverride > CursorOverrideID.DefaultCursor;
    }
}

public static class PlaceholderHelper {
    public static bool IsAPlaceholder(this Item item) => item.type == PlaceholderItem.FakeType && item.TryGetGlobalItem(out PlaceholderItem placeholder) && placeholder.IsAPlaceholder;
    
    public static bool AreSame(Item item, Item other) {
        if (item.IsAir && other.IsAir) return true;
        if (item.type != other.type || item.IsAir) return false;
        if (!item.TryGetGlobalItem(out PlaceholderItem a) || !other.TryGetGlobalItem(out PlaceholderItem b)) return true;
        if (a.tile != -1) return a.tile == b.tile;
        if (a.condition is not null) return a.condition == b.condition;
        return true;
    }
}