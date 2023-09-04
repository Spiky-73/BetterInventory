using System;
using System.Reflection;

namespace BetterInventory.Reflection;

public abstract class Member<T, TMemberInfo> where TMemberInfo : MemberInfo {
    public Member(string name) {
        DeclaringType = typeof(T);
        Name = name;
        TMemberInfo? info = GetMemberInfo() ?? throw new MissingMemberException(DeclaringType.FullName, Name);
        ValidateMemberInfo(info);
        MemberInfo = info;
    }
    public Member(TMemberInfo info) {
        if(info.DeclaringType != typeof(T)) throw new ArgumentException("MemberInfo.DeclaringType != typeof(T)");
        DeclaringType = typeof(T);
        Name = info.Name;
        ValidateMemberInfo(info);
        MemberInfo = info;
    }

    public Type DeclaringType { get; }
    public string Name { get; }
    public TMemberInfo MemberInfo { get; }

    protected abstract TMemberInfo? GetMemberInfo();
    protected abstract void ValidateMemberInfo(TMemberInfo info);

    public static implicit operator TMemberInfo(Member<T, TMemberInfo> member) => member.MemberInfo;

    public const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}


public sealed class Field<T, TField> : Member<T, FieldInfo>{
    public Field(FieldInfo info) : base(info) {}
    public Field(string name) : base(name) {}

    protected sealed override FieldInfo? GetMemberInfo() => DeclaringType.GetField(Name, Flags);
    protected override void ValidateMemberInfo(FieldInfo info) {
        if(info.FieldType != typeof(TField)) throw new MissingFieldException(DeclaringType.FullName, Name);
    }

    public TField GetValue(T? self) => (TField)MemberInfo.GetValue(self)!;
    public void SetValue(T? self, TField value) => MemberInfo.SetValue(self, value);
}

public sealed class Property<T, TProperty> : Member<T, PropertyInfo> {
    public Property(PropertyInfo info) : base(info) { }
    public Property(string name) : base(name) { }

    protected sealed override PropertyInfo? GetMemberInfo() => DeclaringType.GetProperty(Name, Flags);
    protected sealed override void ValidateMemberInfo(PropertyInfo info) {
        if (info.PropertyType != typeof(TProperty)) throw new MissingFieldException(DeclaringType.FullName, Name);
    }

    public Method<T, TProperty>? GetMethod => MemberInfo.CanRead ? new(MemberInfo.GetMethod!) : null;
    public Method<T, TProperty, object?>? SetMethod => MemberInfo.CanWrite ? new(MemberInfo.SetMethod!) : null;

    public TProperty GetValue(T? self) => (TProperty)MemberInfo.GetValue(self)!;
    public void SetValue(T? self, TProperty value) => MemberInfo.SetValue(self, value);
}


public abstract class MethodBase<T, TResult> : Member<T, MethodInfo> {
    protected MethodBase(MethodInfo info) : base(info) {}
    protected MethodBase(string name) : base(name) {}

    protected sealed override MethodInfo? GetMemberInfo() => DeclaringType.GetMethod(Name, Flags, ArgsType);
    protected sealed override void ValidateMemberInfo(MethodInfo info) {
        if ((typeof(TResult) != typeof(object) || info.ReturnType != typeof(void)) && info.ReturnType != typeof(TResult)) throw new MissingMethodException(DeclaringType.FullName, Name);
    }

    protected abstract Type[] ArgsType { get; }
}

public sealed class Method<T, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name) {}

    protected sealed override Type[] ArgsType => Array.Empty<Type>();

    public TResult Invoke(T? self) => (TResult)MemberInfo.Invoke(self, null)!;
}
public sealed class Method<T, T1, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name) {}

    protected sealed override Type[] ArgsType => new Type[] { typeof(T1) };

    public TResult Invoke(T? self, T1 arg) => (TResult)MemberInfo.Invoke(self, new object?[] { arg })!;
}
public sealed class Method<T, T1, T2, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name) {}

    protected sealed override Type[] ArgsType => new Type[] { typeof(T1), typeof(T2) };
    public TResult Invoke(T? self, T1 arg1, T2 arg2) => (TResult)MemberInfo.Invoke(self, new object?[] { arg1, arg2 })!;
}
public sealed class Method<T, T1, T2, T3, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name) {}

    protected sealed override Type[] ArgsType => new Type[] { typeof(T1), typeof(T2), typeof(T3) };

    public T1 Invoke(T? self, T1 arg1, T2 arg2, T3 arg3) => (T1)MemberInfo.Invoke(self, new object?[] { arg1, arg2, arg3 })!;
}
public sealed class Method<T, T1, T2, T3, T4, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name) {}

    protected sealed override Type[] ArgsType => new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };

    public T1 Invoke(T? self, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => (T1)MemberInfo.Invoke(self, new object?[] { arg1, arg2, arg3, arg4 })!;
}
public sealed class Method<T, T1, T2, T3, T4, T5, TResult> : MethodBase<T, TResult> {
    public Method(MethodInfo info) : base(info) {}
    public Method(string name) : base(name) {}

    protected sealed override Type[] ArgsType => new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };

    public T1 Invoke(T? self, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => (T1)MemberInfo.Invoke(self, new object?[] { arg1, arg2, arg3, arg4, arg5 })!;
}