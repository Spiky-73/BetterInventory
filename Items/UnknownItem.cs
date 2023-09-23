using Terraria;
using Terraria.ModLoader;

namespace BetterInventory.Items;

public sealed class UnknownItem : ModItem {
    public static Item Instance { get; private set; } = null!;

    public override void SetStaticDefaults() {
        Instance = new(ModContent.ItemType<UnknownItem>());
    }
    public override void Unload() {
        Instance = null!;
    }
}

/*
Recipe list
 - sprite
 - hover

Material
 - hite

CreateItem
 - sprite
 - hover

tiles and condtions
*/