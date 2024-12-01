using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BetterInventory.Default.Catalogues;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public string? condition;
    public bool IsAPlaceholder => tile != -1 || condition is not null;

    public override void Load() {
        On_ItemSlot.DrawItemIcon += HookDrawPlaceholder;
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.ModifyTooltips)), HookPlaceholderTooltip);
        On_ItemSlot.LeftClick_ItemArray_int_int += HookFakeItemClick;

        ConditionItems["Conditions.NearWater"] = ItemID.WaterBucket;
        ConditionItems["Conditions.NearLava"] = ItemID.LavaBucket;
        ConditionItems["Conditions.NearHoney"] = ItemID.HoneyBucket;
        ConditionItems["Conditions.InGraveyard"] = ItemID.Gravestone;
        ConditionItems["Conditions.InSnow"] = ItemID.SnowBlock;
    }
    public override void Unload() {
        ConditionItems.Clear();
        _fakeContexts.Clear();
    }

    public sealed override bool CanStack(Item destination, Item source) => CanPlaceholderStack(source);
    public sealed override bool CanStackInWorld(Item destination, Item source) => CanPlaceholderStack(source);
    public bool CanPlaceholderStack(Item source) => !IsAPlaceholder && !source.IsAPlaceholder();

    public sealed override bool InstancePerEntity => true;
    public sealed override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == FakeType;

    public override void SaveData(Item item, TagCompound tag) {
        if (tile != -1) tag[TileTag] = tile;
        else if (condition is not null) tag[ConditionTag] = condition;
    }
    public override void LoadData(Item item, TagCompound tag) {
        if (tag.TryGet(TileTag, out int t)) tile = t;
        else if (tag.TryGet(ConditionTag, out string c)) condition = c;
    }
    public const string TileTag = "tile";
    public const string ConditionTag = "condition";

    private static float HookDrawPlaceholder(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
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
        if(GuideUnknownDisplayPlayer.IsUnknown(item)) name = Language.GetTextValue($"{Localization.Keys.UI}.Unknown");
        if (!item.IsAir && name is null && item.TryGetGlobalItem(out PlaceholderItem placeholder)) {
            if (placeholder.tile == ByHandTile) name = Language.GetTextValue($"{Localization.Keys.UI}.ByHand");
            else if (placeholder.tile >= 0) name = Lang.GetMapObjectName(MapHelper.TileToLookup(placeholder.tile, 0));
            else if (placeholder.condition is not null) name = Language.GetTextValue(placeholder.condition);
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
        if (GuideGuideTile.CraftingStationsItems.TryGetValue(tile, out int type) && type != ItemID.None) return new(type);
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
        placeholder.GetGlobalItem<PlaceholderItem>().condition = condition.Description.Key;
        return placeholder;
    }

    public const int FakeType = ItemID.IronPickaxe;
    public const int ByHandTile = -2;
    public static readonly Dictionary<string, int> ConditionItems = []; // description -> id

    public static bool OverrideHover(Item[] inv, int context, int slot) {
        if(inv[slot].IsAir) return false;
        if(!_fakeContexts.Exists(f => f.IsHovered(inv, context, slot) && f.IsFake(inv[slot]))) return false;
        if (Main.mouseItem.IsAir || ItemSlot.ShiftInUse || ItemSlot.ControlInUse) Main.cursorOverride = CursorOverrideID.TrashCan;
        if (ItemSlot.PickItemMovementAction(inv, context, slot, Main.mouseItem) == -1) Main.cursorOverride = CursorOverrideID.TrashCan;
        return true;
    }

    private void HookFakeItemClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        if (inv[slot].IsAir || !(Main.mouseLeftRelease && Main.mouseLeft || (Main.mouseRight && !(RecipeList.Instance.Enabled && Configs.QuickSearch.RightClick)))) {
            orig(inv, context, slot);
            return;
        }
        if (_fakeContexts.Exists(f => f.IsHovered(inv, context, slot) && f.IsFake(inv[slot]))) {
            inv[slot].TurnToAir();
            if(Main.cursorOverride > CursorOverrideID.DefaultCursor) {
                SoundEngine.PlaySound(SoundID.Grab);
                Recipe.FindRecipes();
                return;
            }
        } else {
            foreach (var fakeContext in _fakeContexts) {
                if (!fakeContext.WouldMoveToContext(inv, context, slot, out Item? destination) || destination.IsAir || !fakeContext.IsFake(destination)) continue;
                destination.TurnToAir();
                break;
            }
        }
        orig(inv, context, slot);
    }

    public static void AddFakeItemContext(IFakeItemContext context) => _fakeContexts.Add(context);
    private readonly static List<IFakeItemContext> _fakeContexts = [];
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

public interface IFakeItemContext {
    bool IsHovered(Item[] inv, int context, int slot);
    bool WouldMoveToContext(Item[] inv, int context, int slot, [MaybeNullWhen(false)] out Item destination);
    bool IsFake(Item item);
}