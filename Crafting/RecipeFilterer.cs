using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;

namespace BetterInventory.Crafting;

public interface IRecipeFilter : IEntryFilter<Item> {
    Texture2D GetSource(bool available);
    Rectangle GetSourceFrame();
}
