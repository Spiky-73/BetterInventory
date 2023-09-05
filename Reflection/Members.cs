using System;
using System.Reflection;

namespace BetterInventory.Reflection;


public abstract class Member<T, TMemberInfo> where TMemberInfo : MemberInfo {
    public Member(TMemberInfo? info) {
        if(info is null) throw new ArgumentNullException(nameof(info));
        if(info.DeclaringType != typeof(T)) throw new ArgumentException("MemberInfo.DeclaringType != typeof(T)");
        ValidateMemberInfo(info);
        MemberInfo = info;
    }

    public string Name => MemberInfo.Name;
    public TMemberInfo MemberInfo { get; }

    protected abstract void ValidateMemberInfo(TMemberInfo info);

    public static implicit operator TMemberInfo(Member<T, TMemberInfo> member) => member.MemberInfo;

    public const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}

public sealed class Field<T, TField> : Member<T, FieldInfo>{

    public Field(FieldInfo? info) : base(info) {}
    public Field(string name) : this(typeof(T).GetField(name, Flags)) {}

    public TField GetValue(T? self) => (TField)MemberInfo.GetValue(self)!;
    public void SetValue(T? self, TField value) => MemberInfo.SetValue(self, value);

    protected override void ValidateMemberInfo(FieldInfo info) {
        if(info.FieldType != typeof(TField)) throw new MissingFieldException(info.DeclaringType!.FullName, info.Name);
    }
}

public sealed class Property<T, TProperty> : Member<T, PropertyInfo> {
    public Property(PropertyInfo? info) : base(info) { }
    public Property(string name) : this(typeof(T).GetProperty(name, Flags)) { }

    public TProperty GetValue(T? self) => (TProperty)MemberInfo.GetValue(self)!;
    public void SetValue(T? self, TProperty value) => MemberInfo.SetValue(self, value);
    
    public Method<T, TProperty>? GetMethod => MemberInfo.CanRead ? new(MemberInfo.GetMethod!) : null;
    public Method<T, TProperty, object?>? SetMethod => MemberInfo.CanWrite ? new(MemberInfo.SetMethod!) : null;
    
    protected sealed override void ValidateMemberInfo(PropertyInfo info) {
        if (info.PropertyType != typeof(TProperty)) throw new MissingFieldException(info.DeclaringType!.FullName, info.Name);
    }
}

public abstract class MethodBase<T, TResult> : Member<T, MethodInfo> {
    protected MethodBase(MethodInfo? info) : base(info) {}
    protected MethodBase(string name, Type[] argsType) : this(typeof(T).GetMethod(name, Flags, argsType)) {}

    protected sealed override void ValidateMemberInfo(MethodInfo info) {
        if ((typeof(TResult) != typeof(object) || info.ReturnType != typeof(void)) && info.ReturnType != typeof(TResult)) throw new MissingMethodException(info.DeclaringType!.FullName, info.Name);
    }
}

public sealed class Method<T, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo? info) : base(info) {}
    public Method(string name) : base(name, Array.Empty<Type>()) {}
    
    public TResult Invoke(T? self) => (TResult)MemberInfo.Invoke(self, null)!;
}
public sealed class Method<T, T1, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo? info) : base(info) {}
    public Method(string name) : base(name, new Type[] { typeof(T1) }) {}

    public TResult Invoke(T? self, T1 arg) => (TResult)MemberInfo.Invoke(self, new object?[] { arg })!;
}
public sealed class Method<T, T1, T2, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo? info) : base(info) {}
    public Method(string name) : base(name, new Type[] { typeof(T1), typeof(T2) }) { }

    public TResult Invoke(T? self, T1 arg1, T2 arg2) => (TResult)MemberInfo.Invoke(self, new object?[] { arg1, arg2 })!;
}
public sealed class Method<T, T1, T2, T3, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name, new Type[] { typeof(T1), typeof(T2), typeof(T3) }) {}

    public T1 Invoke(T? self, T1 arg1, T2 arg2, T3 arg3) => (T1)MemberInfo.Invoke(self, new object?[] { arg1, arg2, arg3 })!;
}
public sealed class Method<T, T1, T2, T3, T4, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }) {}

    public T1 Invoke(T? self, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => (T1)MemberInfo.Invoke(self, new object?[] { arg1, arg2, arg3, arg4 })!;
}
public sealed class Method<T, T1, T2, T3, T4, T5, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }) {}

    public T1 Invoke(T? self, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => (T1)MemberInfo.Invoke(self, new object?[] { arg1, arg2, arg3, arg4, arg5 })!;
}


public abstract class StaticMember<TMemberInfo> where TMemberInfo : MemberInfo {
    public StaticMember(TMemberInfo? info) {
        if(info is null) throw new ArgumentNullException(nameof(info));
        ValidateMemberInfo(info);
        MemberInfo = info;
    }

    public string Name => MemberInfo.Name;
    public TMemberInfo MemberInfo { get; }

    protected abstract void ValidateMemberInfo(TMemberInfo info);

    public static implicit operator TMemberInfo(StaticMember<TMemberInfo> member) => member.MemberInfo;

    public const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}

public sealed class StaticField<TField> : StaticMember<FieldInfo>{

    public StaticField(FieldInfo? info) : base(info) {}
    public StaticField(Type type, string name) : this(type.GetField(name, Flags)) {}

    public TField GetValue() => (TField)MemberInfo.GetValue(null)!;
    public void SetValue(TField value) => MemberInfo.SetValue(null, value);

    protected override void ValidateMemberInfo(FieldInfo info) {
        if(info.FieldType != typeof(TField)) throw new MissingFieldException(info.DeclaringType!.FullName, info.Name);
    }
}

public sealed class StaticProperty<TProperty> : StaticMember<PropertyInfo> {
    public StaticProperty(PropertyInfo? info) : base(info) { }
    public StaticProperty(Type type, string name) : this(type.GetProperty(name, Flags)) { }

    public TProperty GetValue() => (TProperty)MemberInfo.GetValue(null)!;
    public void SetValue(TProperty value) => MemberInfo.SetValue(null, value);
    
    public StaticMethod<TProperty>? GetMethod => MemberInfo.CanRead ? new(MemberInfo.GetMethod!) : null;
    public StaticMethod<TProperty, object?>? SetMethod => MemberInfo.CanWrite ? new(MemberInfo.SetMethod!) : null;
    
    protected sealed override void ValidateMemberInfo(PropertyInfo info) {
        if (info.PropertyType != typeof(TProperty)) throw new MissingFieldException(info.DeclaringType!.FullName, info.Name);
    }
}

public abstract class StaticMethodBase<TResult> : StaticMember<MethodInfo> {
    protected StaticMethodBase(MethodInfo? info) : base(info) {}
    protected StaticMethodBase(Type type, string name, Type[] argsType) : this(type.GetMethod(name, Flags, argsType)) {}

    protected sealed override void ValidateMemberInfo(MethodInfo info) {
        if ((typeof(TResult) != typeof(object) || info.ReturnType != typeof(void)) && info.ReturnType != typeof(TResult)) throw new MissingMethodException(info.DeclaringType!.FullName, info.Name);
    }
}

public sealed class StaticMethod<TResult> : StaticMethodBase<TResult> {
    public StaticMethod(MethodInfo? info) : base(info) {}
    public StaticMethod(Type type, string name) : base(type, name, Array.Empty<Type>()) {}
    
    public TResult Invoke() => (TResult)MemberInfo.Invoke(null, null)!;
}
public sealed class StaticMethod<T1, TResult> : StaticMethodBase<TResult> {
    public StaticMethod(MethodInfo? info) : base(info) {}
    public StaticMethod(Type type, string name) : base(type, name, new Type[] { typeof(T1) }) {}

    public TResult Invoke(T1 arg) => (TResult)MemberInfo.Invoke(null, new object?[] { arg })!;
}
public sealed class StaticMethod<T1, T2, TResult> : StaticMethodBase<TResult> {
    public StaticMethod(MethodInfo? info) : base(info) {}
    public StaticMethod(Type type, string name) : base(type, name, new Type[] { typeof(T1), typeof(T2) }) { }

    public TResult Invoke(T1 arg1, T2 arg2) => (TResult)MemberInfo.Invoke(null, new object?[] { arg1, arg2 })!;
}
public sealed class StaticMethod<T1, T2, T3, TResult> : StaticMethodBase<TResult> {
    public StaticMethod(MethodInfo info) : base(info) {}
    public StaticMethod(Type type, string name) : base(type, name, new Type[] { typeof(T1), typeof(T2), typeof(T3) }) {}

    public T1 Invoke(T1 arg1, T2 arg2, T3 arg3) => (T1)MemberInfo.Invoke(null, new object?[] { arg1, arg2, arg3 })!;
}
public sealed class StaticMethod<T1, T2, T3, T4, TResult> : StaticMethodBase<TResult> {
    public StaticMethod(MethodInfo info) : base(info) {}
    public StaticMethod(Type type, string name) : base(type, name, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }) {}

    public T1 Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => (T1)MemberInfo.Invoke(null, new object?[] { arg1, arg2, arg3, arg4 })!;
}
public sealed class StaticMethod<T1, T2, T3, T4, T5, TResult> : StaticMethodBase<TResult> {
    public StaticMethod(MethodInfo info) : base(info) {}
    public StaticMethod(Type type, string name) : base(type, name, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }) {}

    public T1 Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => (T1)MemberInfo.Invoke(null, new object?[] { arg1, arg2, arg3, arg4, arg5 })!;
}