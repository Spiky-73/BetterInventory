using BetterInventory.Features.RecipeFiltering.UI.Elements;
using Microsoft.Xna.Framework;
using SpikysLib.UI.Elements;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BetterInventory.Features.RecipeFiltering.UI.States;

public sealed class UIRecipeFiltering : UIState {

    public UIRecipeFiltering() {
        OnUpdate += UpdateSelf;
    }

    public override void OnInitialize() {
        Append(_container);
        _sort.Initialize();
        _filters.Initialize();
        _search.Initialize();
    }

    public override void OnActivate() => Rebuild();
    private void UpdateSelf(UIElement affectedElement) {
        if (_container.IsMouseHovering) Main.LocalPlayer.mouseInterface = true;

        if (_pendingRebuild) Rebuild(true);

        if (_pendingRecalculate) Recalculate();
    }

    public override void Recalculate() {
        _pendingRecalculate = false;
        if (_container is not null) {
            _container.Left.Pixels = _topLeft.X;
            _container.Top.Pixels = _topLeft.Y;
        }
        base.Recalculate(); // FIXME needing 3 calls is definitely not normal
        base.Recalculate();
        base.Recalculate();
    }

    public void Rebuild(bool immediate = false) {
        _pendingRebuild = !immediate;
        if (_pendingRebuild) return;

        _container.Clear();
        _toggleIcons.Clear();
        _toggleIcons.Add(_craftingToggle);
        _container.Add(_toggleIcons);

        if (Configs.RecipeFiltering.Sort) {
            _container.Add(_sort);
            _sort.Rebuild(immediate);
        }
        if (Configs.RecipeFiltering.Search) {
            _container.Add(_search);
            _search.Rebuild(immediate);
        }
        if (Configs.RecipeFiltering.Filters) {
            _container.Add(_filters);
            _filters.Rebuild(immediate);
        }
        Recalculate();
    }

    public void Reposition(int hammerX, int hammerY) {
        Point p = new(hammerX - TextureAssets.InfoIcon[0].Width() - 1, hammerY - TextureAssets.InfoIcon[0].Width() - 1);
        if (p != _topLeft) {
            _topLeft = p;
            Recalculate();
        }
    }

    public static void NeedsRecalculate() => _pendingRecalculate = true;

    private Point _topLeft;
    private bool _pendingRebuild = false;
    private static bool _pendingRecalculate = false;

    private readonly UIFlexGrid _container = new(1) { ListPadding = 6 };
    private readonly UIFlexGrid _toggleIcons = new(2) { ListPadding = 4, FlexHeight = false, Height = new(30, 0) };
    private readonly UIImage _craftingToggle = new(TextureAssets.CraftToggle[0]) { Color = Color.Transparent }; // Take to space of the Crafting toggle button;

    public UIRecipeSort _sort = new();
    public UIRecipeSearch _search = new();
    public UIRecipeFilters _filters = new();
}
