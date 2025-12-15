using System;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SpikysLib;
using SpikysLib.Collections;
using SpikysLib.Configs;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;

namespace BetterInventory.Configs;

public sealed class CompatibilityConfig : ModConfig {

    [ReloadRequired, DefaultValue(true)] public bool loadDisabledFeatures = true;

    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedVanillaPatchesConfig unloadedVanillaPatches = new();
    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedFeaturesConfig unloadedFeatures = new();
    [JsonIgnore, ShowDespiteJsonIgnore] public UnloadedImprovementsConfig unloadedImprovements = new();

    public static CompatibilityConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;
}

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
