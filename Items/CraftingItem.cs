using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.UI;

namespace BetterInventory.Items;

public sealed class CraftingItem : ModItem {
    public static int ID => ModContent.ItemType<CraftingItem>();

    public Condition? condition = null;

    public static Item WithTile(int tile, int style) => new(ID) { createTile = tile, placeStyle = style };
    public static Item WithCondition(Condition condition) {
        Item item =  new(ID);
        (item.ModItem as CraftingItem)!.condition = condition;
        return item;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
        if (Item.createTile != -1) Utility.DrawTileFrame(spriteBatch, Item.createTile, position, origin/frame.Size(), scale);
        else if (condition is not null) ItemSlot.DrawItemIcon(_conditionItem, ItemSlot.Context.CraftingMaterial, spriteBatch, position, Main.inventoryScale, 32f, Color.White);
        else return true;
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips) {
        for (int i = 1; i < tooltips.Count; i++) tooltips[i].Hide();
        if(Item.createTile != -1) tooltips[0].Text = Lang.GetMapObjectName(MapHelper.TileToLookup(Item.createTile, Item.placeStyle));
        else if(condition is not null) tooltips[0].Text = condition.Description.Value;
    }

    private static readonly Item _conditionItem = new(ItemID.SoulofNight);
}