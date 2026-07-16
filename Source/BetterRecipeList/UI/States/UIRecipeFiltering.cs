using BetterInventory.BetterRecipeList.UI.Elements;
using Microsoft.Xna.Framework;
using SpikysLib.UI.Elements;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace BetterInventory.BetterRecipeList.UI.States;

public sealed class UIRecipeFiltering : UIState {

    public UIRecipeFiltering() {
        OnUpdate += UpdateSelf;
    }

    public override void OnInitialize() {
        Append(_container);
        _container.Add(_toggleIcons);
        _toggleIcons.Add(_craftingToggle);
        _toggleIcons.Add(_sort);
        _container.Add(_search);
        _container.Add(_filters);
    }

    private void UpdateSelf(UIElement affectedElement) {
        if (_container.IsMouseHovering) Main.LocalPlayer.mouseInterface = true;

        if (Main.recBigList != _expanded) {
            _expanded = Main.recBigList;
            Recalculate();
        }
    }

    public override void Recalculate() {
        _container.Left.Pixels = _topLeft.X;
        _container.Top.Pixels = _topLeft.Y;
        _container.Width.Pixels = RecipeFiltersConfig.Instance.expand && _expanded ? 220 : RecipeFiltersConfig.Instance.minWidth;
        base.Recalculate();
    }

    public void Reposition(int hammerX, int hammerY) {
        Point p = new(hammerX - TextureAssets.InfoIcon[0].Width() - 1, hammerY - TextureAssets.InfoIcon[0].Width() - 1);
        if (p != _topLeft) {
            _topLeft = p;
            Recalculate();
        }
    }

    private Point _topLeft;
    private readonly UIFlexList _container = new() { ListPadding = 6, FlexWidth = false };
    // private readonly UIFlexList _container = new() { ListPadding = 6, Flex = FlexDirection.Horizontal };
    private readonly UIGrid _toggleIcons = new() { ListPadding = 4, Width = new(0, 1), Height = new(30, 0) };
    // private readonly UIFlexGrid _toggleIcons = new() { ListPadding = 4, FlexHeight = false, Width = new(0,1), Height = new(30, 0) };
    private readonly UIImage _craftingToggle = new(TextureAssets.CraftToggle[0]) { Color = Color.Transparent }; // Take to space of the Crafting toggle button;

    public UIRecipeSort _sort = new();
    public UIRecipeSearch _search = new() { Width = new(0, 1) };
    public UIRecipeFilters _filters = new() { Width = new(0, 1) };
    private bool _expanded;
}
