#pragma warning disable IDE1006

using static NuLua.Interop.Luau.NativeMethods;

namespace NuLua.Interop.Luau;

public sealed unsafe class LuauOperations : ILuaOperations
{
    public static readonly LuauOperations Default = new();

    const int LUA_REGISTRYINDEX = -10000;
    const int LUA_GLOBALSINDEX = -10002;

    LuauOperations() { }

    public void* luaL_newstate() => NativeMethods.luaL_newstate();
    public void* lua_newstate(delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> allocator, void* userData) => NativeMethods.lua_newstate(allocator, userData);
    public void lua_close(void* state) => NativeMethods.lua_close((lua_State*)state);
    public void* lua_newthread(void* state) => NativeMethods.lua_newthread((lua_State*)state);

    public delegate* unmanaged[Cdecl]<void*, int> lua_atpanic(void* state, delegate* unmanaged[Cdecl]<void*, int> panicFunction) => throw new NotSupportedException("lua_atpanic is not supported in Luau.");

    public int lua_absindex(void* state, int index) => NativeMethods.lua_absindex((lua_State*)state, index);
    public int lua_gettop(void* state) => NativeMethods.lua_gettop((lua_State*)state);
    public void lua_settop(void* state, int index) => NativeMethods.lua_settop((lua_State*)state, index);
    public void lua_pushvalue(void* state, int index) => NativeMethods.lua_pushvalue((lua_State*)state, index);
    public void lua_rotate(void* state, int index, int count) => throw new NotSupportedException("lua_rotate is not supported in Luau.");
    public void lua_copy(void* state, int fromIndex, int toIndex) => throw new NotSupportedException("lua_copy is not supported in Luau.");
    public void lua_remove(void* state, int index) => NativeMethods.lua_remove((lua_State*)state, index);
    public void lua_insert(void* state, int index) => NativeMethods.lua_insert((lua_State*)state, index);
    public void lua_replace(void* state, int index) => NativeMethods.lua_replace((lua_State*)state, index);
    public int lua_checkstack(void* state, int size) => NativeMethods.lua_checkstack((lua_State*)state, size);
    public void lua_xmove(void* fromState, void* toState, int count) => NativeMethods.lua_xmove((lua_State*)fromState, (lua_State*)toState, count);

    public int lua_isnumber(void* state, int index) => NativeMethods.lua_isnumber((lua_State*)state, index);
    public int lua_isstring(void* state, int index) => NativeMethods.lua_isstring((lua_State*)state, index);
    public int lua_iscfunction(void* state, int index) => NativeMethods.lua_iscfunction((lua_State*)state, index);
    public int lua_isinteger(void* state, int index) => NativeMethods.lua_isinteger64((lua_State*)state, index);
    public int lua_isuserdata(void* state, int index) => NativeMethods.lua_isuserdata((lua_State*)state, index);
    public int lua_type(void* state, int index) => NativeMethods.lua_type((lua_State*)state, index);
    public byte* lua_typename(void* state, int type) => NativeMethods.lua_typename((lua_State*)state, type);

    public int lua_equal(void* state, int index1, int index2) => NativeMethods.lua_equal((lua_State*)state, index1, index2);
    public int lua_rawequal(void* state, int index1, int index2) => NativeMethods.lua_rawequal((lua_State*)state, index1, index2);
    public int lua_lessthan(void* state, int index1, int index2) => NativeMethods.lua_lessthan((lua_State*)state, index1, index2);
    public int lua_compare(void* state, int index1, int index2, int operation) => throw new NotSupportedException("lua_compare is not supported in Luau.");

    public double lua_tonumber(void* state, int index) => NativeMethods.lua_tonumberx((lua_State*)state, index, null);
    public double lua_tonumberx(void* state, int index, int* isNumber) => NativeMethods.lua_tonumberx((lua_State*)state, index, isNumber);
    public long lua_tointeger(void* state, int index) => NativeMethods.lua_tointeger64((lua_State*)state, index, null);
    public long lua_tointegerx(void* state, int index, int* isNumber) => NativeMethods.lua_tointeger64((lua_State*)state, index, isNumber);
    public int lua_toboolean(void* state, int index) => NativeMethods.lua_toboolean((lua_State*)state, index);
    public byte* lua_tolstring(void* state, int index, nuint* length) => NativeMethods.lua_tolstring((lua_State*)state, index, length);
    public nuint lua_objlen(void* state, int index) => (nuint)NativeMethods.lua_objlen((lua_State*)state, index);
    public nuint lua_rawlen(void* state, int index) => (nuint)NativeMethods.lua_objlen((lua_State*)state, index);

    public delegate* unmanaged[Cdecl]<void*, int> lua_tocfunction(void* state, int index)
        => (delegate* unmanaged[Cdecl]<void*, int>)NativeMethods.lua_tocfunction((lua_State*)state, index);

    public void* lua_touserdata(void* state, int index) => NativeMethods.lua_touserdata((lua_State*)state, index);
    public void* lua_tothread(void* state, int index) => NativeMethods.lua_tothread((lua_State*)state, index);
    public void* lua_topointer(void* state, int index) => NativeMethods.lua_topointer((lua_State*)state, index);

    public void lua_arith(void* state, int operation) => throw new NotSupportedException("lua_arith is not supported in Luau.");
    public void lua_len(void* state, int index) => throw new NotSupportedException("lua_len is not supported in Luau.");
    public nuint lua_stringtonumber(void* state, byte* value) => throw new NotSupportedException("lua_stringtonumber is not supported in Luau.");

    public void lua_pushnil(void* state) => NativeMethods.lua_pushnil((lua_State*)state);
    public void lua_pushnumber(void* state, double value) => NativeMethods.lua_pushnumber((lua_State*)state, value);
    public void lua_pushinteger(void* state, long value) => NativeMethods.lua_pushinteger64((lua_State*)state, value);

    public byte* lua_pushlstring(void* state, byte* value, nuint length)
    {
        var L = (lua_State*)state;
        NativeMethods.lua_pushlstring(L, value, length);
        return NativeMethods.lua_tolstring(L, -1, null);
    }

    public byte* lua_pushstring(void* state, byte* value)
    {
        var L = (lua_State*)state;
        NativeMethods.lua_pushstring(L, value);
        return NativeMethods.lua_tolstring(L, -1, null);
    }

    public byte* lua_pushvfstring(void* state, byte* format, byte* argp) => NativeMethods.lua_pushvfstring((lua_State*)state, format, argp);
    public byte* lua_pushfstring(void* state, byte* format) => NativeMethods.lua_pushfstringL((lua_State*)state, format);

    public void lua_pushcclosure(void* state, delegate* unmanaged[Cdecl]<void*, int> function, int upvalueCount)
        => NativeMethods.lua_pushcclosurek((lua_State*)state, (delegate* unmanaged[Cdecl]<lua_State*, int>)function, null, upvalueCount, null);

    public void lua_pushboolean(void* state, int value) => NativeMethods.lua_pushboolean((lua_State*)state, value);
    public void lua_pushlightuserdata(void* state, void* pointer) => NativeMethods.lua_pushlightuserdatatagged((lua_State*)state, pointer, 0);
    public int lua_pushthread(void* state) => NativeMethods.lua_pushthread((lua_State*)state);

    public int lua_getglobal(void* state, byte* name) => NativeMethods.lua_getfield((lua_State*)state, LUA_GLOBALSINDEX, name);
    public int lua_gettable(void* state, int index) => NativeMethods.lua_gettable((lua_State*)state, index);
    public int lua_getfield(void* state, int index, byte* key) => NativeMethods.lua_getfield((lua_State*)state, index, key);
    public int lua_geti(void* state, int index, long key) => throw new NotSupportedException("lua_geti is not supported in Luau.");
    public int lua_rawget(void* state, int index) => NativeMethods.lua_rawget((lua_State*)state, index);
    public int lua_rawgeti(void* state, int index, long key) => NativeMethods.lua_rawgeti((lua_State*)state, index, (int)key);
    public int lua_rawgetp(void* state, int index, void* pointer) => NativeMethods.lua_rawgetptagged((lua_State*)state, index, pointer, 0);
    public void lua_createtable(void* state, int arrayCount, int recordCount) => NativeMethods.lua_createtable((lua_State*)state, arrayCount, recordCount);
    public void* lua_newuserdata(void* state, nuint size) => NativeMethods.lua_newuserdatatagged((lua_State*)state, size, 0);
    public void* lua_newuserdatauv(void* state, nuint size, int userValueCount) => throw new NotSupportedException("lua_newuserdatauv is not supported in Luau.");
    public int lua_getmetatable(void* state, int objectIndex) => NativeMethods.lua_getmetatable((lua_State*)state, objectIndex);
    public void lua_getfenv(void* state, int index) => NativeMethods.lua_getfenv((lua_State*)state, index);
    public int lua_getuservalue(void* state, int index) => throw new NotSupportedException("lua_getuservalue is not supported in Luau.");
    public int lua_getiuservalue(void* state, int index, int userValueIndex) => throw new NotSupportedException("lua_getiuservalue is not supported in Luau.");

    public void lua_setglobal(void* state, byte* name) => NativeMethods.lua_setfield((lua_State*)state, LUA_GLOBALSINDEX, name);
    public void lua_settable(void* state, int index) => NativeMethods.lua_settable((lua_State*)state, index);
    public void lua_setfield(void* state, int index, byte* key) => NativeMethods.lua_setfield((lua_State*)state, index, key);
    public void lua_seti(void* state, int index, long key) => throw new NotSupportedException("lua_seti is not supported in Luau.");
    public void lua_rawset(void* state, int index) => NativeMethods.lua_rawset((lua_State*)state, index);
    public void lua_rawseti(void* state, int index, long key) => NativeMethods.lua_rawseti((lua_State*)state, index, (int)key);
    public void lua_rawsetp(void* state, int index, void* pointer) => NativeMethods.lua_rawsetptagged((lua_State*)state, index, pointer, 0);
    public int lua_setmetatable(void* state, int objectIndex) => NativeMethods.lua_setmetatable((lua_State*)state, objectIndex);
    public int lua_setfenv(void* state, int index) => NativeMethods.lua_setfenv((lua_State*)state, index);
    public void lua_setuservalue(void* state, int index) => throw new NotSupportedException("lua_setuservalue is not supported in Luau.");
    public int lua_setiuservalue(void* state, int index, int userValueIndex) => throw new NotSupportedException("lua_setiuservalue is not supported in Luau.");

    public void lua_call(void* state, int argumentCount, int resultCount) => NativeMethods.lua_call((lua_State*)state, argumentCount, resultCount);

    public void lua_callk(void* state, int argumentCount, int resultCount, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation)
    {
        if (continuation != null) throw new NotSupportedException("lua_callk continuation is not supported in Luau.");
        NativeMethods.lua_call((lua_State*)state, argumentCount, resultCount);
    }

    public int lua_pcall(void* state, int argumentCount, int resultCount, int errorFunction) => NativeMethods.lua_pcall((lua_State*)state, argumentCount, resultCount, errorFunction);

    public int lua_pcallk(void* state, int argumentCount, int resultCount, int errorFunction, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation)
    {
        if (continuation != null) throw new NotSupportedException("lua_pcallk continuation is not supported in Luau.");
        return NativeMethods.lua_pcall((lua_State*)state, argumentCount, resultCount, errorFunction);
    }

    public int lua_cpcall(void* state, delegate* unmanaged[Cdecl]<void*, int> function, void* userData)
        => NativeMethods.lua_cpcall((lua_State*)state, (delegate* unmanaged[Cdecl]<lua_State*, int>)function, userData);

    public int lua_load(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint*, byte*> reader, void* data, byte* chunkName, byte* mode)
        => throw new NotSupportedException("lua_load is not supported in Luau; use luau_load with precompiled bytecode.");

    public int lua_dump(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint, void*, int> writer, void* data, int strip)
        => throw new NotSupportedException("lua_dump is not supported in Luau.");

    public int lua_yield(void* state, int resultCount) => NativeMethods.lua_yield((lua_State*)state, resultCount);

    public int lua_yieldk(void* state, int resultCount, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation)
    {
        if (continuation != null) throw new NotSupportedException("lua_yieldk continuation is not supported in Luau.");
        return NativeMethods.lua_yield((lua_State*)state, resultCount);
    }

    public int lua_resume(void* state, void* fromState, int argumentCount, int* resultCount) => NativeMethods.lua_resume((lua_State*)state, (lua_State*)fromState, argumentCount);
    public int lua_status(void* state) => NativeMethods.lua_status((lua_State*)state);
    public int lua_isyieldable(void* state) => NativeMethods.lua_isyieldable((lua_State*)state);
    public int lua_error(void* state) => throw new NotSupportedException("lua_error is not supported in Luau.");

    public int lua_gc(void* state, int what, int data) => NativeMethods.lua_gc((lua_State*)state, what, data);
    public int lua_next(void* state, int index) => NativeMethods.lua_next((lua_State*)state, index);
    public void lua_concat(void* state, int count) => NativeMethods.lua_concat((lua_State*)state, count);

    public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> lua_getallocf(void* state, void** userData)
        => NativeMethods.lua_getallocf((lua_State*)state, userData);

    public void lua_setallocf(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> allocator, void* userData)
        => throw new NotSupportedException("lua_setallocf is not supported in Luau.");

    public int lua_getstack(void* state, int level, void* debug) => throw new NotSupportedException("lua_getstack is not supported in Luau.");
    public int lua_getinfo(void* state, byte* what, void* debug) => NativeMethods.lua_getinfo((lua_State*)state, 0, what, (lua_Debug*)debug);
    public byte* lua_getlocal(void* state, void* debug, int index) => throw new NotSupportedException("lua_getlocal signature is incompatible with Luau.");
    public byte* lua_setlocal(void* state, void* debug, int index) => throw new NotSupportedException("lua_setlocal signature is incompatible with Luau.");
    public byte* lua_getupvalue(void* state, int functionIndex, int index) => NativeMethods.lua_getupvalue((lua_State*)state, functionIndex, index);
    public byte* lua_setupvalue(void* state, int functionIndex, int index) => NativeMethods.lua_setupvalue((lua_State*)state, functionIndex, index);
    public void* lua_upvalueid(void* state, int functionIndex, int index) => throw new NotSupportedException("lua_upvalueid is not supported in Luau.");
    public void lua_upvaluejoin(void* state, int functionIndex1, int index1, int functionIndex2, int index2) => throw new NotSupportedException("lua_upvaluejoin is not supported in Luau.");

    public int lua_sethook(void* state, delegate* unmanaged[Cdecl]<void*, void*, void> hook, int mask, int count) => throw new NotSupportedException("lua_sethook is not supported in Luau.");
    public delegate* unmanaged[Cdecl]<void*, void*, void> lua_gethook(void* state) => throw new NotSupportedException("lua_gethook is not supported in Luau.");
    public int lua_gethookmask(void* state) => throw new NotSupportedException("lua_gethookmask is not supported in Luau.");
    public int lua_gethookcount(void* state) => throw new NotSupportedException("lua_gethookcount is not supported in Luau.");

    public int luaopen_base(void* state) => NativeMethods.luaopen_base((lua_State*)state);
    public int luaopen_coroutine(void* state) => NativeMethods.luaopen_coroutine((lua_State*)state);
    public int luaopen_table(void* state) => NativeMethods.luaopen_table((lua_State*)state);
    public int luaopen_io(void* state) => throw new NotSupportedException("luaopen_io is not supported in Luau.");
    public int luaopen_os(void* state) => NativeMethods.luaopen_os((lua_State*)state);
    public int luaopen_string(void* state) => NativeMethods.luaopen_string((lua_State*)state);
    public int luaopen_utf8(void* state) => NativeMethods.luaopen_utf8((lua_State*)state);
    public int luaopen_bit32(void* state) => NativeMethods.luaopen_bit32((lua_State*)state);
    public int luaopen_math(void* state) => NativeMethods.luaopen_math((lua_State*)state);
    public int luaopen_debug(void* state) => NativeMethods.luaopen_debug((lua_State*)state);
    public int luaopen_package(void* state) => throw new NotSupportedException("luaopen_package is not supported in Luau.");

    public void luaL_openlibs(void* state) => NativeMethods.luaL_openlibs((lua_State*)state);
    public void luaL_openlib(void* state, byte* libraryName, void* registrations, int upvalueCount) => throw new NotSupportedException("luaL_openlib is not supported in Luau.");
    public void luaL_register(void* state, byte* libraryName, void* registrations) => NativeMethods.luaL_register((lua_State*)state, libraryName, (luaL_Reg*)registrations);
    public void luaL_checkversion_(void* state, double version, nuint size) => throw new NotSupportedException("luaL_checkversion_ is not supported in Luau.");
    public int luaL_getmetafield(void* state, int objectIndex, byte* eventName) => NativeMethods.luaL_getmetafield((lua_State*)state, objectIndex, eventName);
    public int luaL_callmeta(void* state, int objectIndex, byte* eventName) => NativeMethods.luaL_callmeta((lua_State*)state, objectIndex, eventName);
    public byte* luaL_tolstring(void* state, int index, nuint* length) => NativeMethods.luaL_tolstring((lua_State*)state, index, length);
    public int luaL_typerror(void* state, int argument, byte* typeName) => throw new NotSupportedException("luaL_typerror is not supported in Luau.");
    public int luaL_argerror(void* state, int argument, byte* extraMessage) => throw new NotSupportedException("luaL_argerror is not supported in Luau.");
    public byte* luaL_checklstring(void* state, int argument, nuint* length) => NativeMethods.luaL_checklstring((lua_State*)state, argument, length);
    public byte* luaL_optlstring(void* state, int argument, byte* defaultValue, nuint* length) => NativeMethods.luaL_optlstring((lua_State*)state, argument, defaultValue, length);
    public double luaL_checknumber(void* state, int argument) => NativeMethods.luaL_checknumber((lua_State*)state, argument);
    public double luaL_optnumber(void* state, int argument, double defaultValue) => NativeMethods.luaL_optnumber((lua_State*)state, argument, defaultValue);
    public long luaL_checkinteger(void* state, int argument) => NativeMethods.luaL_checkinteger64((lua_State*)state, argument);
    public long luaL_optinteger(void* state, int argument, long defaultValue) => NativeMethods.luaL_optinteger64((lua_State*)state, argument, defaultValue);
    public void luaL_checkstack(void* state, int size, byte* message) => NativeMethods.luaL_checkstack((lua_State*)state, size, message);
    public void luaL_checktype(void* state, int argument, int type) => NativeMethods.luaL_checktype((lua_State*)state, argument, type);
    public void luaL_checkany(void* state, int argument) => NativeMethods.luaL_checkany((lua_State*)state, argument);
    public int luaL_newmetatable(void* state, byte* typeName) => NativeMethods.luaL_newmetatable((lua_State*)state, typeName);
    public void luaL_setmetatable(void* state, byte* typeName) => throw new NotSupportedException("luaL_setmetatable is not supported in Luau.");
    public void* luaL_testudata(void* state, int userDataIndex, byte* typeName) => throw new NotSupportedException("luaL_testudata is not supported in Luau.");
    public void* luaL_checkudata(void* state, int userDataIndex, byte* typeName) => NativeMethods.luaL_checkudata((lua_State*)state, userDataIndex, typeName);
    public void luaL_where(void* state, int level) => NativeMethods.luaL_where((lua_State*)state, level);
    public int luaL_error(void* state, byte* format) => throw new NotSupportedException("luaL_error is not supported in Luau.");
    public int luaL_checkoption(void* state, int argument, byte* defaultValue, byte** list) => NativeMethods.luaL_checkoption((lua_State*)state, argument, defaultValue, list);
    public int luaL_fileresult(void* state, int status, byte* fileName) => throw new NotSupportedException("luaL_fileresult is not supported in Luau.");
    public int luaL_execresult(void* state, int status) => throw new NotSupportedException("luaL_execresult is not supported in Luau.");

    public int luaL_ref(void* state, int tableIndex)
    {
        if (tableIndex != LUA_REGISTRYINDEX) throw new NotSupportedException("luaL_ref in Luau requires LUA_REGISTRYINDEX as tableIndex.");
        return NativeMethods.lua_ref((lua_State*)state, -1);
    }

    public void luaL_unref(void* state, int tableIndex, int reference)
    {
        if (tableIndex != LUA_REGISTRYINDEX) throw new NotSupportedException("luaL_unref in Luau requires LUA_REGISTRYINDEX as tableIndex.");
        NativeMethods.lua_unref((lua_State*)state, reference);
    }

    public int luaL_loadfile(void* state, byte* fileName) => throw new NotSupportedException("luaL_loadfile is not supported in Luau.");
    public int luaL_loadfilex(void* state, byte* fileName, byte* mode) => throw new NotSupportedException("luaL_loadfilex is not supported in Luau.");
    public int luaL_loadbuffer(void* state, byte* buffer, nuint size, byte* name) => throw new NotSupportedException("luaL_loadbuffer is not supported in Luau; use luau_load.");
    public int luaL_loadbufferx(void* state, byte* buffer, nuint size, byte* name, byte* mode) => throw new NotSupportedException("luaL_loadbufferx is not supported in Luau.");
    public int luaL_loadstring(void* state, byte* source) => throw new NotSupportedException("luaL_loadstring is not supported in Luau.");
    public long luaL_len(void* state, int index) => throw new NotSupportedException("luaL_len is not supported in Luau.");
    public byte* luaL_gsub(void* state, byte* source, byte* pattern, byte* replacement) => throw new NotSupportedException("luaL_gsub is not supported in Luau.");
    public byte* luaL_findtable(void* state, int index, byte* fieldName, int sizeHint) => NativeMethods.luaL_findtable((lua_State*)state, index, fieldName, sizeHint);
    public void luaL_setfuncs(void* state, void* registrations, int upvalueCount) => throw new NotSupportedException("luaL_setfuncs is not supported in Luau.");
    public int luaL_getsubtable(void* state, int index, byte* fieldName) => throw new NotSupportedException("luaL_getsubtable is not supported in Luau.");
    public void luaL_traceback(void* state, void* sourceState, byte* message, int level) => NativeMethods.luaL_traceback((lua_State*)state, (lua_State*)sourceState, message, level);
    public void luaL_requiref(void* state, byte* moduleName, delegate* unmanaged[Cdecl]<void*, int> openFunction, int global) => throw new NotSupportedException("luaL_requiref is not supported in Luau.");
    public void luaL_buffinit(void* state, void* buffer) => NativeMethods.luaL_buffinit((lua_State*)state, (luaL_Strbuf*)buffer);
    public byte* luaL_prepbuffer(void* buffer) => NativeMethods.luaL_prepbuffsize((luaL_Strbuf*)buffer, 8192);
    public byte* luaL_prepbuffsize(void* buffer, nuint size) => NativeMethods.luaL_prepbuffsize((luaL_Strbuf*)buffer, size);
    public byte* luaL_buffinitsize(void* state, void* buffer, nuint size) => NativeMethods.luaL_buffinitsize((lua_State*)state, (luaL_Strbuf*)buffer, size);
    public void luaL_addlstring(void* buffer, byte* value, nuint length) => NativeMethods.luaL_addlstring((luaL_Strbuf*)buffer, value, length);
    public void luaL_addstring(void* buffer, byte* value) => throw new NotSupportedException("luaL_addstring is not supported in Luau.");
    public void luaL_addvalue(void* buffer) => NativeMethods.luaL_addvalue((luaL_Strbuf*)buffer);
    public void luaL_pushresult(void* buffer) => NativeMethods.luaL_pushresult((luaL_Strbuf*)buffer);
    public void luaL_pushresultsize(void* buffer, nuint size) => NativeMethods.luaL_pushresultsize((luaL_Strbuf*)buffer, size);
}

#pragma warning restore IDE1006
