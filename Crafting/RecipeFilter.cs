using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;

namespace BetterInventory.Crafting;

public interface IRecipeFilter : IEntryFilter<Item> {
    Asset<Texture2D> GetSource();
    Asset<Texture2D> GetSourceGray();
    Rectangle GetSourceFrame();
}
