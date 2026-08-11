using System.Collections.Generic;
using System.Linq;
using ReLogic.Reflection;
using SpikysLib;

namespace BetterInventory;

public static class InterfaceLoader {

    public static void Add(ModInterface @interface) {
        @interface.Type = _interfaces.Count;
        _interfaces.Add(@interface);
        Search.Add(@interface.FullName, @interface.Type);
    }

    public static IReadOnlyList<ModInterface> Interfaces => _interfaces.AsReadOnly();
    public static readonly IdDictionary Search = IdDictionary.Create(typeof(InterfaceLoader), typeof(object));

    private static readonly List<ModInterface> _interfaces = [];
}

public class InterfaceDefinition : EntityDefinition<InterfaceDefinition> {
    public InterfaceDefinition() : base() { }
    public InterfaceDefinition(string key) : base(key) { }
    public InterfaceDefinition(string mod, string name) : base(mod, name) { }
    public override int Type => InterfaceLoader.Search.TryGetId(ToString(), out var id) ? id : -1;
    public override bool IsUnloaded => Type < 0;

    public override InterfaceDefinition[] GetValues() => [.. InterfaceLoader.Interfaces.Select(i => new InterfaceDefinition(i.FullName))];
}