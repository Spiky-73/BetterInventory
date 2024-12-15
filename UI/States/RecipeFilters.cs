using System;
using BetterInventory.Crafting;
using BetterInventory.Default.Catalogues;
using BetterInventory.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpikysLib.UI.Elements;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BetterInventory.UI.States;

public sealed class RecipeFilters : UIState {
    public UIFlexList container = null!;
    public UIPanel searchPanel = null!;
    public UISearchBar searchBar = null!;
    public UIImageButton searchCancel = null!;
    public UIFlexGrid filters = null!;

    public sealed override void OnInitialize() {
        container = new() { ListPadding = 6 };
        InitSearchBar();
        InitFilters();

        RebuildList();
        Append(container);
    }

    public void RebuildList () {
        container.Clear();

        if (Configs.RecipeSearchBar.Enabled) container.Add(searchPanel);
        if (Configs.RecipeFilters.Enabled) container.Add(filters);
    }

    public override void Update(GameTime gameTime) {
        base.Update(gameTime);
        if (!filters.IsMouseHovering) Reflection.PlayerInput.MouseInModdedUI.GetValue().Remove("ModLoader/UIList");
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);
        if (searchPanel.IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
        if (filters.IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
    }

    public override void RecalculateChildren() {
        base.RecalculateChildren();

        if (Configs.RecipeSearchBar.Enabled && searchPanel is not null) {
            if (Configs.RecipeSearchBar.Value.expand && Main.recBigList) searchPanel.Width.Pixels = 220;
            else searchPanel.Width.Pixels = Math.Max(filters.Width.Pixels, Configs.RecipeSearchBar.Value.minWidth);
        }
    }

    private void InitFilters() {
        filters = new() { ListPadding = 0.1f }; // Cannot be set to 0 for some reason
    }

    public void RebuildRecipeGrid() {
        static void OnFilterChanges() {
            Recipe.FindRecipes();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        this.filters.Clear();

        var filters = RecipeUI.Filterer;
        for (int i = 0; i < filters.AvailableFilters.Count; i++) {
            IRecipeFilter filter = filters.AvailableFilters[i];
            int recipeCount = RecipeUI.RecipesPerFilter[i];
            bool active = filters.IsFilterActive(i);
            bool available = recipeCount > 0;
            if (!active && !available && Configs.RecipeFilters.Value.hideUnavailable) continue;
            
            UIRecipeFilterIcon icon = new(filter, recipeCount, active);
            icon.OnLeftClick += (_, _) => {
                bool keepOn = !active || filters.ActiveFilters.Count > 1;
                filters.ActiveFilters.Clear();
                if (keepOn) filters.ActiveFilters.Add(filter);
                OnFilterChanges();
            };
            icon.OnRightClick += (_, _) => {
                if (!filters.ActiveFilters.Remove(filter)) filters.ActiveFilters.Add(filter);
                OnFilterChanges();
            };
            this.filters.Add(icon);
        }
        Recalculate();
    }

    private void InitSearchBar() {
        searchPanel = new() {
            Height = new StyleDimension(24f, 0),
            BackgroundColor = new Color(35, 40, 83),
            BorderColor = new Color(15, 20, 40)
        };
        searchPanel.SetPadding(0f);

        searchBar = new(Language.GetText("UI.PlayerNameSlot"), 0.8f) {
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(0f, 1f),
            HAlign = 0f,
            VAlign = 0.5f,
            Left = new StyleDimension(-4, 0f),
            IgnoresMouseInteraction = true
        };

        searchBar.OnStartTakingInput += () => searchPanel.BorderColor = Main.OurFavoriteColor;
        searchBar.OnEndTakingInput += () => searchPanel.BorderColor = new Color(15, 20, 40);
        searchPanel.OnLeftClick += (evt, _) => {
            if (evt.Target.Parent != searchPanel) searchBar.ToggleTakingText();
        };
        searchBar.OnContentsChanged += OnSearchContentChange;
        searchBar.OnCanceledTakingInput += () => OnSearchContentChange(null);
        searchBar.SetContents(null, true);
        searchPanel.Append(searchBar);

        searchCancel = new(Main.Assets.Request<Texture2D>("Images/UI/SearchCancel")) {
            HAlign = 1f,
            VAlign = 0.5f,
            Left = new StyleDimension(-2f, 0f)
        };
        searchCancel.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
        searchCancel.OnLeftClick += (_, _) => {
            RecipeList.HookSearchRecipe_Cancel(searchBar);
            searchBar.SetContents(null, true);
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        searchPanel.Append(searchCancel);
        RecipeList.OnSearchBarInit(searchBar);
    }

    private void OnSearchContentChange(string? content) {
        if (Main.gameMenu) return;
        RecipeUI.Filterer.SetSearchFilter(content);
        Recipe.FindRecipes();
    }
}