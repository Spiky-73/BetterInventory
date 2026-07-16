using System;
using System.ComponentModel;
using System.Reflection;
using BetterInventory.BetterBestiary;
using BetterInventory.BetterInventoryManagement;
using BetterInventory.BetterItemInformationDisplay;
using BetterInventory.BetterItemPickup;
using BetterInventory.BetterKeybinds;
using BetterInventory.BetterRecipeList;
using BetterInventory.BetterTooltips;
using Microsoft.Xna.Framework;
using SpikysLib;
using SpikysLib.Collections;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;

namespace BetterInventory;

public sealed class BetterInventoryConfig : ModConfig {

    [DefaultValue(true)] public bool betterTooltips = true;
    [DefaultValue(true)] public bool betterBestiary = true;
    [DefaultValue(true)] public bool betterRecipeList = true;
    [DefaultValue(true)] public bool betterItemInformationDisplay = true;
    [DefaultValue(true)] public bool betterItemPickup = true;
    [DefaultValue(true)] public bool betterInventoryManagement = true;
    [DefaultValue(true)] public bool betterKeybinds = true;

    // TODO add "load disable configs"
    [ReloadRequired, DefaultValue(true)] public bool loadDisabledModule = true;

    public UnloadedBetterTooltipsConfig unloadedBetterTooltips = new();
    public UnloadedBetterBestiaryConfig unloadedBetterBestiary = new();
    public UnloadedBetterRecipeListConfig unloadedBetterRecipeList = new();
    public UnloadedBetterItemInformationDisplayConfig unloadedBetterItemInformationDisplay = new();
    public UnloadedBetterItemPickupConfig unloadedBetterItemPickup = new();
    public UnloadedBetterInventoryManagementConfig unloadedBetterInventoryManagement = new();
    public UnloadedBetterKeybindsConfig unloadedBetterKeybinds = new();

    public static BetterInventoryConfig Instance = null!;
    public static bool BetterTooltips => Instance.betterTooltips;
    public static bool BetterBestiary => Instance.betterBestiary;
    public static bool BetterRecipeList => Instance.betterRecipeList;
    public static bool BetterItemInformationDisplay => Instance.betterItemInformationDisplay;
    public static bool BetterItemPickup => Instance.betterItemPickup;
    public static bool BetterInventoryManagement => Instance.betterInventoryManagement;
    public static bool BetterKeybinds => Instance.betterKeybinds;

    public sealed override ConfigScope Mode => ConfigScope.ClientSide;
}

// TODO modify the label and tooltip
[AttributeUsage(AttributeTargets.Field)]
public class UnloadableAttribute : BackgroundColorAttribute {
    public UnloadableAttribute(Type config, string field) : this(UICommon.DefaultUIBlue, GetLoadedState(config, field)) { }
    private UnloadableAttribute(Color color, LoadedState loadedState) : this(GetUnloadedColor(color, loadedState)) { }
    private UnloadableAttribute(Color color) : base(color.R, color.G, color.B, color.A) { }

    public static LoadedState GetLoadedState(Type configType, string fieldName) => GetLoadedState(configType.Retrieve("Instance")!.Retrieve(fieldName)!);
    public static LoadedState GetLoadedState(object value) => value switch {
        bool b => b ? LoadedState.Unloaded : LoadedState.Loaded,
        IKeyValuePair kvp => (bool)kvp.Key! ? LoadedState.Unloaded : GetLoadedState(kvp.Value!),
        _ => value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Exist(f => GetLoadedState(f.GetValue(value)!) != LoadedState.Loaded) ? LoadedState.PartiallyLoaded : LoadedState.Loaded
    };

    public static Color GetUnloadedColor(Color color, LoadedState state) => Color.Lerp(color, Color.DarkSlateGray, state switch {
        LoadedState.Unloaded => 0.8f,
        LoadedState.PartiallyLoaded => 0.5f,
        _ => 0,
    });

    public enum LoadedState {
        Loaded,
        PartiallyLoaded,
        Unloaded
    }
}
public class UnloadableAttribute<T>(string field) : UnloadableAttribute(typeof(T), field) { }
