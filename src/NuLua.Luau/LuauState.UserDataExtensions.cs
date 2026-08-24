using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Interop.Luau;

namespace NuLua.Luau;

/// <summary>
/// Exposes a managed object implementing <see cref="ILuaUserData{T}"/> to Luau as a tagged userdata
/// whose payload holds a strong <see cref="GCHandle"/> to the object.
///
/// Per (state, type) a single tag + metatable + GC destructor are registered once and cached, so
/// every instance of the same type shares one metatable (the fork's per-tag metatable registry).
/// When Luau GCs a userdata, the native dtor frees the GCHandle.
/// </summary>
public static class LuauStateUserDataExtensions
{
    // Per-state: ILuaUserData type → userdata tag, plus bookkeeping for one-time registration.
    static readonly ConditionalWeakTable<LuauState, UserDataTagTable> tagTables = new();

    sealed class UserDataTagTable
    {
        public readonly ConcurrentDictionary<Type, int> Tags = new();
        public int NextTag; // 0 => first allocated tag is 1 (tag 0 is reserved for raw userdata)
    }

    /// <summary>
    /// Wraps <paramref name="value"/> as a Luau userdata with the per-type metatable for
    /// <c>value.GetType()</c>, and returns a <see cref="LuaUserData"/> wrapper (the userdata is
    /// also left on top of the stack). The metatable + GC destructor are registered once per
    /// (state, type); the GCHandle is freed automatically when Luau collects the userdata.
    /// </summary>
    public static unsafe LuaUserData CreateUserData<T>(this LuauState state, T value) where T : ILuaUserData<T>
    {
        ArgumentNullException.ThrowIfNull(value);

        var table = tagTables.GetValue(state, static _ => new UserDataTagTable());
        var tag = table.Tags.GetOrAdd(typeof(T), static (type, t) =>
        {
            var (table, state, value) = t;
            var tag = AllocateTag(table);

            using var metatable = BuildMetatable(state, type, value.SupportedMetamethods);
            state.PushValue(metatable.Reference);
            state.SetUserDataMetatable(tag);
            state.SetManagedUserDataDtor(tag);
            
            return tag;
        }, (table, state, value));

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var data = state.NewUserDataTaggedWithMetatable(
                sizeof(ManagedUserData),
                tag
            );
            Unsafe.Write(data, ManagedUserData.Create(value));
        }
        else
        {
            var data = state.NewUserDataTaggedWithMetatable(
                Unsafe.SizeOf<T>(),
                tag
            );
            Unsafe.Write(data, value);
        }

        return new LuaUserData(state, state.Ref());

        static LuaTable BuildMetatable(
            LuauState state,
            Type type,
            LuaUserDataMetamethods methods
        )
        {
            var metatable = state.CreateTable(0, 8);

            if (methods.HasFlag(LuaUserDataMetamethods.Index))
            {
                metatable["__index"] = LuaValue.FromFunction(state.CreateFunction(IndexMetamethod));
            }

            if (methods.HasFlag(LuaUserDataMetamethods.NewIndex))
            {
                metatable["__newindex"] = LuaValue.FromFunction(
                    state.CreateFunction(NewIndexMetamethod)
                );
            }

            if (methods.HasFlag(LuaUserDataMetamethods.Length))
            {
                metatable["__len"] = LuaValue.FromFunction(state.CreateFunction(LenMetamethod));
            }

            if (methods.HasFlag(LuaUserDataMetamethods.ToString))
            {
                metatable["__tostring"] = LuaValue.FromFunction(
                    state.CreateFunction(ToStringMetamethod)
                );
            }

            if (methods.HasFlag(LuaUserDataMetamethods.Iter))
            {
                metatable["__iter"] = LuaValue.FromFunction(state.CreateFunction(IterMetamethod));
            }
            
            // Relational operators
            if (methods.HasFlag(LuaUserDataMetamethods.Eq))
            {
                metatable["__eq"] = LuaValue.FromFunction(state.CreateFunction(EqualsMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Lt))
            {
                metatable["__lt"] = LuaValue.FromFunction(state.CreateFunction(LessThanMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Le))
            {
                metatable["__le"] = LuaValue.FromFunction(state.CreateFunction(LessThanOrEqualMetamethod));
            }
    
            // Arithmetic operators
            if (methods.HasFlag(LuaUserDataMetamethods.Unm))
            {
                metatable["__unm"] = LuaValue.FromFunction(state.CreateFunction(UnaryMinusMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Add))
            {
                metatable["__add"] = LuaValue.FromFunction(state.CreateFunction(AddMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Sub))
            {
                metatable["__sub"] = LuaValue.FromFunction(state.CreateFunction(SubtractMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Mul))
            {
                metatable["__mul"] = LuaValue.FromFunction(state.CreateFunction(MultiplyMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Div))
            {
                metatable["__div"] = LuaValue.FromFunction(state.CreateFunction(DivideMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Idiv))
            {
                metatable["__idiv"] = LuaValue.FromFunction(state.CreateFunction(FloorDivideMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Mod))
            {
                metatable["__mod"] = LuaValue.FromFunction(state.CreateFunction(ModulusMetamethod));
            }
            if (methods.HasFlag(LuaUserDataMetamethods.Pow))
            {
                metatable["__pow"] = LuaValue.FromFunction(state.CreateFunction(PowerMetamethod));
            }

            // __type → nicer type()/error output via lua_getuserdataname.
            metatable["__type"] = LuaValue.FromString(type.Name);

            return metatable;
        }
        
        static unsafe T GetSelfAtPosition(LuauState state, int index)
        {
            var data = state.ToUserDataPointer(index);
            
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ref var header = ref Unsafe.AsRef<ManagedUserData>((void*)data);
                if (header.Handle == 0)
                {
                    throw new InvalidOperationException("Value is not a managed userdata.");
                }

                if (GCHandle.FromIntPtr(header.Handle).Target is not T userData)
                {
                    throw new InvalidOperationException(
                        $"Managed userdata does not implement {typeof(T)}."
                    );
                }
                
                return userData;
            }
            else
            {
                ref var @struct = ref Unsafe.AsRef<T>((void*)data);
                return @struct;
            }
        }

        // Reads the managed object from the userdata at stack index 1 (metamethod self arg).
        static T GetSelf(LuauState state)
        {
            return GetSelfAtPosition(state, 1);
        }

        static int IndexMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            using var key = state.ToLuaValue(2);
            if (self.TryGetIndex(state, key, out var value))
            {
                state.Push(value);
                value.Dispose();
                return 1;
            }

            state.PushNil();
            return 1;
        }

        static int NewIndexMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            using var key = state.ToLuaValue(2);
            using var value = state.ToLuaValue(3);
            self.TrySetIndex(state, key, value);
            return 0;
        }

        static int LenMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            if (self.Length is { } len)
            {
                state.PushNumber(len);
                return 1;
            }

            return 0;
        }

        static int ToStringMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            state.PushString(self.ToLuaString() ?? self.ToString() ?? self.GetType().Name);
            return 1;
        }

        // `__iter`: returns `(iteratorFunction, nil, nil)` where the iterator closure
        // captures <see cref="ILuaUserData.GetIterator"/>'s enumerator. Luau calls it repeatedly for
        // `for k, v in ud do`.
        static int IterMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var enumerator = self.GetIterator(state);
            if (enumerator is null)
            {
                state.PushNil();
                state.PushNil();
                state.PushNil();
                return 3;
            }

            var iterator = state.CreateFunction(
                (s, _) =>
                {
                    if (!enumerator.MoveNext())
                    {
                        return 0;
                    }

                    var pair = enumerator.Current;
                    s.Push(pair.Key);
                    s.Push(pair.Value);
                    return 2;
                }
            );

            state.Push(LuaValue.FromFunction(iterator));
            state.PushNil();
            state.PushNil();
            return 3;
        }
        
        static int EqualsMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            var result = T.Equals(state, self, other);
            state.PushBoolean(result);
            return 1;
        }
        
        static int LessThanMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            var result = T.LessThan(state, self, other);
            state.PushBoolean(result);
            return 1;
        }
        
        static int LessThanOrEqualMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            var result = T.LessThanOrEqual(state, self, other);
            state.PushBoolean(result);
            return 1;
        }
        
        static int UnaryMinusMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            using var result = T.UnaryMinus(state, self);
            state.Push(result);
            return 1;
        }
        
        static int AddMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            using var result = T.Add(state, self, other);
            state.Push(result);
            return 1;
        }
        
        static int SubtractMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            using var result = T.Subtract(state, self, other);
            state.Push(result);
            return 1;
        }
        
        static int MultiplyMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            using var result = T.Multiply(state, self, other);
            state.Push(result);
            return 1;
        }
        
        static int DivideMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            using var result = T.Divide(state, self, other);
            state.Push(result);
            return 1;
        }
        
        static int FloorDivideMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            using var result = T.FloorDivide(state, self, other);
            state.Push(result);
            return 1;
        }
        
        static int ModulusMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            using var result = T.Modulus(state, self, other);
            state.Push(result);
            return 1;
        }
        
        static int PowerMetamethod(LuauState state, LuaFuncArguments args)
        {
            var self = GetSelf(state);
            var other = GetSelfAtPosition(state, 2);
            using var result = T.Power(state, self, other);
            state.Push(result);
            return 1;
        }
    }

    static int AllocateTag(UserDataTagTable table)
    {
        var tag = Interlocked.Increment(ref table.NextTag);
        if (tag >= (int)NativeMethods.LUA_UTAG_LIMIT)
        {
            throw new InvalidOperationException(
                $"Exceeded the userdata tag limit ({NativeMethods.LUA_UTAG_LIMIT}); too many ILuaUserData types for one state."
            );
        }

        return tag;
    }
}
