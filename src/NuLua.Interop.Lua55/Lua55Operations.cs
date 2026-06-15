#pragma warning disable IDE1006

using static NuLua.Interop.Lua55.NativeMethods;

namespace NuLua.Interop.Lua55;

public sealed unsafe class Lua55Operations : ILuaOperations
{
    public static readonly Lua55Operations Default = new();

    Lua55Operations() { }

    public void* luaL_newstate() => NativeMethods.luaL_newstate();

    public void* lua_newstate(delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> allocator, void* userData)
        => NativeMethods.lua_newstate(allocator, userData, 0);

    public void lua_close(void* state) => NativeMethods.lua_close((lua_State*)state);

    public void* lua_newthread(void* state) => NativeMethods.lua_newthread((lua_State*)state);

    public delegate* unmanaged[Cdecl]<void*, int> lua_atpanic(void* state, delegate* unmanaged[Cdecl]<void*, int> panicFunction)
        => (delegate* unmanaged[Cdecl]<void*, int>)NativeMethods.lua_atpanic((lua_State*)state, (delegate* unmanaged[Cdecl]<lua_State*, int>)panicFunction);

    public int lua_absindex(void* state, int index) => NativeMethods.lua_absindex((lua_State*)state, index);
    public int lua_gettop(void* state) => NativeMethods.lua_gettop((lua_State*)state);
    public void lua_settop(void* state, int index) => NativeMethods.lua_settop((lua_State*)state, index);
    public void lua_pushvalue(void* state, int index) => NativeMethods.lua_pushvalue((lua_State*)state, index);
    public void lua_rotate(void* state, int index, int count) => NativeMethods.lua_rotate((lua_State*)state, index, count);
    public void lua_copy(void* state, int fromIndex, int toIndex) => NativeMethods.lua_copy((lua_State*)state, fromIndex, toIndex);

    public void lua_remove(void* state, int index)
    {
        var L = (lua_State*)state;
        NativeMethods.lua_rotate(L, index, -1);
        NativeMethods.lua_settop(L, -2);
    }

    public void lua_insert(void* state, int index) => NativeMethods.lua_rotate((lua_State*)state, index, 1);

    public void lua_replace(void* state, int index)
    {
        var L = (lua_State*)state;
        NativeMethods.lua_copy(L, -1, index);
        NativeMethods.lua_settop(L, -2);
    }

    public int lua_checkstack(void* state, int size) => NativeMethods.lua_checkstack((lua_State*)state, size);
    public void lua_xmove(void* fromState, void* toState, int count) => NativeMethods.lua_xmove((lua_State*)fromState, (lua_State*)toState, count);

    public int lua_isnumber(void* state, int index) => NativeMethods.lua_isnumber((lua_State*)state, index);
    public int lua_isstring(void* state, int index) => NativeMethods.lua_isstring((lua_State*)state, index);
    public int lua_iscfunction(void* state, int index) => NativeMethods.lua_iscfunction((lua_State*)state, index);
    public int lua_isinteger(void* state, int index) => NativeMethods.lua_isinteger((lua_State*)state, index);
    public int lua_isuserdata(void* state, int index) => NativeMethods.lua_isuserdata((lua_State*)state, index);
    public int lua_type(void* state, int index) => NativeMethods.lua_type((lua_State*)state, index);
    public byte* lua_typename(void* state, int type) => NativeMethods.lua_typename((lua_State*)state, type);

    public int lua_equal(void* state, int index1, int index2) => NativeMethods.lua_compare((lua_State*)state, index1, index2, (int)LUA_OPEQ);
    public int lua_rawequal(void* state, int index1, int index2) => NativeMethods.lua_rawequal((lua_State*)state, index1, index2);
    public int lua_lessthan(void* state, int index1, int index2) => NativeMethods.lua_compare((lua_State*)state, index1, index2, (int)LUA_OPLT);
    public int lua_compare(void* state, int index1, int index2, int operation) => NativeMethods.lua_compare((lua_State*)state, index1, index2, operation);

    public double lua_tonumber(void* state, int index) => NativeMethods.lua_tonumberx((lua_State*)state, index, null);
    public double lua_tonumberx(void* state, int index, int* isNumber) => NativeMethods.lua_tonumberx((lua_State*)state, index, isNumber);
    public long lua_tointeger(void* state, int index) => NativeMethods.lua_tointegerx((lua_State*)state, index, null);
    public long lua_tointegerx(void* state, int index, int* isNumber) => NativeMethods.lua_tointegerx((lua_State*)state, index, isNumber);
    public int lua_toboolean(void* state, int index) => NativeMethods.lua_toboolean((lua_State*)state, index);
    public byte* lua_tolstring(void* state, int index, nuint* length) => NativeMethods.lua_tolstring((lua_State*)state, index, length);
    public nuint lua_objlen(void* state, int index) => (nuint)NativeMethods.lua_rawlen((lua_State*)state, index);
    public nuint lua_rawlen(void* state, int index) => (nuint)NativeMethods.lua_rawlen((lua_State*)state, index);

    public delegate* unmanaged[Cdecl]<void*, int> lua_tocfunction(void* state, int index)
        => (delegate* unmanaged[Cdecl]<void*, int>)NativeMethods.lua_tocfunction((lua_State*)state, index);

    public void* lua_touserdata(void* state, int index) => NativeMethods.lua_touserdata((lua_State*)state, index);
    public void* lua_tothread(void* state, int index) => NativeMethods.lua_tothread((lua_State*)state, index);
    public void* lua_topointer(void* state, int index) => NativeMethods.lua_topointer((lua_State*)state, index);

    public void lua_arith(void* state, int operation) => NativeMethods.lua_arith((lua_State*)state, operation);
    public void lua_len(void* state, int index) => NativeMethods.lua_len((lua_State*)state, index);
    public nuint lua_stringtonumber(void* state, byte* value) => NativeMethods.lua_stringtonumber((lua_State*)state, value);

    public void lua_pushnil(void* state) => NativeMethods.lua_pushnil((lua_State*)state);
    public void lua_pushnumber(void* state, double value) => NativeMethods.lua_pushnumber((lua_State*)state, value);
    public void lua_pushinteger(void* state, long value) => NativeMethods.lua_pushinteger((lua_State*)state, value);
    public byte* lua_pushlstring(void* state, byte* value, nuint length) => NativeMethods.lua_pushlstring((lua_State*)state, value, length);
    public byte* lua_pushstring(void* state, byte* value) => NativeMethods.lua_pushstring((lua_State*)state, value);
    public byte* lua_pushvfstring(void* state, byte* format, byte* argp) => NativeMethods.lua_pushvfstring((lua_State*)state, format, argp);
    public byte* lua_pushfstring(void* state, byte* format) => NativeMethods.lua_pushfstring((lua_State*)state, format);

    public void lua_pushcclosure(void* state, delegate* unmanaged[Cdecl]<void*, int> function, int upvalueCount)
        => NativeMethods.lua_pushcclosure((lua_State*)state, (delegate* unmanaged[Cdecl]<lua_State*, int>)function, upvalueCount);

    public void lua_pushboolean(void* state, int value) => NativeMethods.lua_pushboolean((lua_State*)state, value);
    public void lua_pushlightuserdata(void* state, void* pointer) => NativeMethods.lua_pushlightuserdata((lua_State*)state, pointer);
    public int lua_pushthread(void* state) => NativeMethods.lua_pushthread((lua_State*)state);

    public int lua_getglobal(void* state, byte* name) => NativeMethods.lua_getglobal((lua_State*)state, name);
    public int lua_gettable(void* state, int index) => NativeMethods.lua_gettable((lua_State*)state, index);
    public int lua_getfield(void* state, int index, byte* key) => NativeMethods.lua_getfield((lua_State*)state, index, key);
    public int lua_geti(void* state, int index, long key) => NativeMethods.lua_geti((lua_State*)state, index, key);
    public int lua_rawget(void* state, int index) => NativeMethods.lua_rawget((lua_State*)state, index);
    public int lua_rawgeti(void* state, int index, long key) => NativeMethods.lua_rawgeti((lua_State*)state, index, key);
    public int lua_rawgetp(void* state, int index, void* pointer) => NativeMethods.lua_rawgetp((lua_State*)state, index, pointer);
    public void lua_createtable(void* state, int arrayCount, int recordCount) => NativeMethods.lua_createtable((lua_State*)state, arrayCount, recordCount);
    public void* lua_newuserdata(void* state, nuint size) => NativeMethods.lua_newuserdatauv((lua_State*)state, size, 1);
    public void* lua_newuserdatauv(void* state, nuint size, int userValueCount) => NativeMethods.lua_newuserdatauv((lua_State*)state, size, userValueCount);
    public int lua_getmetatable(void* state, int objectIndex) => NativeMethods.lua_getmetatable((lua_State*)state, objectIndex);

    public void lua_getfenv(void* state, int index) => throw new NotSupportedException("lua_getfenv is not supported in Lua 5.5.");
    public int lua_getuservalue(void* state, int index) => NativeMethods.lua_getiuservalue((lua_State*)state, index, 1);
    public int lua_getiuservalue(void* state, int index, int userValueIndex) => NativeMethods.lua_getiuservalue((lua_State*)state, index, userValueIndex);

    public void lua_setglobal(void* state, byte* name) => NativeMethods.lua_setglobal((lua_State*)state, name);
    public void lua_settable(void* state, int index) => NativeMethods.lua_settable((lua_State*)state, index);
    public void lua_setfield(void* state, int index, byte* key) => NativeMethods.lua_setfield((lua_State*)state, index, key);
    public void lua_seti(void* state, int index, long key) => NativeMethods.lua_seti((lua_State*)state, index, key);
    public void lua_rawset(void* state, int index) => NativeMethods.lua_rawset((lua_State*)state, index);
    public void lua_rawseti(void* state, int index, long key) => NativeMethods.lua_rawseti((lua_State*)state, index, key);
    public void lua_rawsetp(void* state, int index, void* pointer) => NativeMethods.lua_rawsetp((lua_State*)state, index, pointer);
    public int lua_setmetatable(void* state, int objectIndex) => NativeMethods.lua_setmetatable((lua_State*)state, objectIndex);
    public int lua_setfenv(void* state, int index) => throw new NotSupportedException("lua_setfenv is not supported in Lua 5.5.");
    public void lua_setuservalue(void* state, int index) => NativeMethods.lua_setiuservalue((lua_State*)state, index, 1);
    public int lua_setiuservalue(void* state, int index, int userValueIndex) => NativeMethods.lua_setiuservalue((lua_State*)state, index, userValueIndex);

    public void lua_call(void* state, int argumentCount, int resultCount) => NativeMethods.lua_callk((lua_State*)state, argumentCount, resultCount, 0, null);

    public void lua_callk(void* state, int argumentCount, int resultCount, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation)
        => NativeMethods.lua_callk((lua_State*)state, argumentCount, resultCount, context, (delegate* unmanaged[Cdecl]<lua_State*, int, nint, int>)continuation);

    public int lua_pcall(void* state, int argumentCount, int resultCount, int errorFunction) => NativeMethods.lua_pcallk((lua_State*)state, argumentCount, resultCount, errorFunction, 0, null);

    public int lua_pcallk(void* state, int argumentCount, int resultCount, int errorFunction, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation)
        => NativeMethods.lua_pcallk((lua_State*)state, argumentCount, resultCount, errorFunction, context, (delegate* unmanaged[Cdecl]<lua_State*, int, nint, int>)continuation);

    public int lua_cpcall(void* state, delegate* unmanaged[Cdecl]<void*, int> function, void* userData) => throw new NotSupportedException("lua_cpcall is not supported in Lua 5.5.");

    public int lua_load(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint*, byte*> reader, void* data, byte* chunkName, byte* mode)
        => NativeMethods.lua_load((lua_State*)state, (delegate* unmanaged[Cdecl]<lua_State*, void*, nuint*, byte*>)reader, data, chunkName, mode);

    public int lua_dump(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint, void*, int> writer, void* data, int strip)
        => NativeMethods.lua_dump((lua_State*)state, (delegate* unmanaged[Cdecl]<lua_State*, void*, nuint, void*, int>)writer, data, strip);

    public int lua_yield(void* state, int resultCount) => NativeMethods.lua_yieldk((lua_State*)state, resultCount, 0, null);

    public int lua_yieldk(void* state, int resultCount, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation)
        => NativeMethods.lua_yieldk((lua_State*)state, resultCount, context, (delegate* unmanaged[Cdecl]<lua_State*, int, nint, int>)continuation);

    public int lua_resume(void* state, void* fromState, int argumentCount, int* resultCount) => NativeMethods.lua_resume((lua_State*)state, (lua_State*)fromState, argumentCount, resultCount);
    public int lua_status(void* state) => NativeMethods.lua_status((lua_State*)state);
    public int lua_isyieldable(void* state) => NativeMethods.lua_isyieldable((lua_State*)state);
    public int lua_error(void* state) => NativeMethods.lua_error((lua_State*)state);

    public int lua_gc(void* state, int what, int data) => NativeMethods.lua_gc((lua_State*)state, what);
    public int lua_next(void* state, int index) => NativeMethods.lua_next((lua_State*)state, index);
    public void lua_concat(void* state, int count) => NativeMethods.lua_concat((lua_State*)state, count);

    public delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> lua_getallocf(void* state, void** userData)
        => NativeMethods.lua_getallocf((lua_State*)state, userData);

    public void lua_setallocf(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> allocator, void* userData)
        => NativeMethods.lua_setallocf((lua_State*)state, allocator, userData);

    public int lua_getstack(void* state, int level, void* debug) => NativeMethods.lua_getstack((lua_State*)state, level, (lua_Debug*)debug);
    public int lua_getinfo(void* state, byte* what, void* debug) => NativeMethods.lua_getinfo((lua_State*)state, what, (lua_Debug*)debug);
    public byte* lua_getlocal(void* state, void* debug, int index) => NativeMethods.lua_getlocal((lua_State*)state, (lua_Debug*)debug, index);
    public byte* lua_setlocal(void* state, void* debug, int index) => NativeMethods.lua_setlocal((lua_State*)state, (lua_Debug*)debug, index);
    public byte* lua_getupvalue(void* state, int functionIndex, int index) => NativeMethods.lua_getupvalue((lua_State*)state, functionIndex, index);
    public byte* lua_setupvalue(void* state, int functionIndex, int index) => NativeMethods.lua_setupvalue((lua_State*)state, functionIndex, index);
    public void* lua_upvalueid(void* state, int functionIndex, int index) => NativeMethods.lua_upvalueid((lua_State*)state, functionIndex, index);
    public void lua_upvaluejoin(void* state, int functionIndex1, int index1, int functionIndex2, int index2) => NativeMethods.lua_upvaluejoin((lua_State*)state, functionIndex1, index1, functionIndex2, index2);

    public int lua_sethook(void* state, delegate* unmanaged[Cdecl]<void*, void*, void> hook, int mask, int count)
    {
        NativeMethods.lua_sethook((lua_State*)state, (delegate* unmanaged[Cdecl]<lua_State*, lua_Debug*, void>)hook, mask, count);
        return 0;
    }

    public delegate* unmanaged[Cdecl]<void*, void*, void> lua_gethook(void* state)
        => (delegate* unmanaged[Cdecl]<void*, void*, void>)NativeMethods.lua_gethook((lua_State*)state);

    public int lua_gethookmask(void* state) => NativeMethods.lua_gethookmask((lua_State*)state);
    public int lua_gethookcount(void* state) => NativeMethods.lua_gethookcount((lua_State*)state);

    public int luaopen_base(void* state) => NativeMethods.luaopen_base((lua_State*)state);
    public int luaopen_coroutine(void* state) => NativeMethods.luaopen_coroutine((lua_State*)state);
    public int luaopen_table(void* state) => NativeMethods.luaopen_table((lua_State*)state);
    public int luaopen_io(void* state) => NativeMethods.luaopen_io((lua_State*)state);
    public int luaopen_os(void* state) => NativeMethods.luaopen_os((lua_State*)state);
    public int luaopen_string(void* state) => NativeMethods.luaopen_string((lua_State*)state);
    public int luaopen_utf8(void* state) => NativeMethods.luaopen_utf8((lua_State*)state);
    public int luaopen_bit32(void* state) => throw new NotSupportedException("luaopen_bit32 is not supported in Lua 5.5.");
    public int luaopen_math(void* state) => NativeMethods.luaopen_math((lua_State*)state);
    public int luaopen_debug(void* state) => NativeMethods.luaopen_debug((lua_State*)state);
    public int luaopen_package(void* state) => NativeMethods.luaopen_package((lua_State*)state);

    public void luaL_openlibs(void* state) => NativeMethods.luaL_openselectedlibs((lua_State*)state, -1, -1);
    public void luaL_openlib(void* state, byte* libraryName, void* registrations, int upvalueCount) => throw new NotSupportedException("luaL_openlib is not supported in Lua 5.5.");
    public void luaL_register(void* state, byte* libraryName, void* registrations) => throw new NotSupportedException("luaL_register is not supported in Lua 5.5.");
    public void luaL_checkversion_(void* state, double version, nuint size) => NativeMethods.luaL_checkversion_((lua_State*)state, version, size);
    public int luaL_getmetafield(void* state, int objectIndex, byte* eventName) => NativeMethods.luaL_getmetafield((lua_State*)state, objectIndex, eventName);
    public int luaL_callmeta(void* state, int objectIndex, byte* eventName) => NativeMethods.luaL_callmeta((lua_State*)state, objectIndex, eventName);
    public byte* luaL_tolstring(void* state, int index, nuint* length) => NativeMethods.luaL_tolstring((lua_State*)state, index, length);
    public int luaL_typerror(void* state, int argument, byte* typeName) => NativeMethods.luaL_typeerror((lua_State*)state, argument, typeName);
    public int luaL_argerror(void* state, int argument, byte* extraMessage) => NativeMethods.luaL_argerror((lua_State*)state, argument, extraMessage);
    public byte* luaL_checklstring(void* state, int argument, nuint* length) => NativeMethods.luaL_checklstring((lua_State*)state, argument, length);
    public byte* luaL_optlstring(void* state, int argument, byte* defaultValue, nuint* length) => NativeMethods.luaL_optlstring((lua_State*)state, argument, defaultValue, length);
    public double luaL_checknumber(void* state, int argument) => NativeMethods.luaL_checknumber((lua_State*)state, argument);
    public double luaL_optnumber(void* state, int argument, double defaultValue) => NativeMethods.luaL_optnumber((lua_State*)state, argument, defaultValue);
    public long luaL_checkinteger(void* state, int argument) => NativeMethods.luaL_checkinteger((lua_State*)state, argument);
    public long luaL_optinteger(void* state, int argument, long defaultValue) => NativeMethods.luaL_optinteger((lua_State*)state, argument, defaultValue);
    public void luaL_checkstack(void* state, int size, byte* message) => NativeMethods.luaL_checkstack((lua_State*)state, size, message);
    public void luaL_checktype(void* state, int argument, int type) => NativeMethods.luaL_checktype((lua_State*)state, argument, type);
    public void luaL_checkany(void* state, int argument) => NativeMethods.luaL_checkany((lua_State*)state, argument);
    public int luaL_newmetatable(void* state, byte* typeName) => NativeMethods.luaL_newmetatable((lua_State*)state, typeName);
    public void luaL_setmetatable(void* state, byte* typeName) => NativeMethods.luaL_setmetatable((lua_State*)state, typeName);
    public void* luaL_testudata(void* state, int userDataIndex, byte* typeName) => NativeMethods.luaL_testudata((lua_State*)state, userDataIndex, typeName);
    public void* luaL_checkudata(void* state, int userDataIndex, byte* typeName) => NativeMethods.luaL_checkudata((lua_State*)state, userDataIndex, typeName);
    public void luaL_where(void* state, int level) => NativeMethods.luaL_where((lua_State*)state, level);
    public int luaL_error(void* state, byte* format) => NativeMethods.luaL_error((lua_State*)state, format);
    public int luaL_checkoption(void* state, int argument, byte* defaultValue, byte** list) => NativeMethods.luaL_checkoption((lua_State*)state, argument, defaultValue, list);
    public int luaL_fileresult(void* state, int status, byte* fileName) => NativeMethods.luaL_fileresult((lua_State*)state, status, fileName);
    public int luaL_execresult(void* state, int status) => NativeMethods.luaL_execresult((lua_State*)state, status);
    public int luaL_ref(void* state, int tableIndex) => NativeMethods.luaL_ref((lua_State*)state, tableIndex);
    public void luaL_unref(void* state, int tableIndex, int reference) => NativeMethods.luaL_unref((lua_State*)state, tableIndex, reference);
    public int luaL_loadfile(void* state, byte* fileName) => NativeMethods.luaL_loadfilex((lua_State*)state, fileName, null);
    public int luaL_loadfilex(void* state, byte* fileName, byte* mode) => NativeMethods.luaL_loadfilex((lua_State*)state, fileName, mode);
    public int luaL_loadbuffer(void* state, byte* buffer, nuint size, byte* name) => NativeMethods.luaL_loadbufferx((lua_State*)state, buffer, size, name, null);
    public int luaL_loadbufferx(void* state, byte* buffer, nuint size, byte* name, byte* mode) => NativeMethods.luaL_loadbufferx((lua_State*)state, buffer, size, name, mode);
    public int luaL_loadstring(void* state, byte* source) => NativeMethods.luaL_loadstring((lua_State*)state, source);
    public long luaL_len(void* state, int index) => NativeMethods.luaL_len((lua_State*)state, index);
    public byte* luaL_gsub(void* state, byte* source, byte* pattern, byte* replacement) => NativeMethods.luaL_gsub((lua_State*)state, source, pattern, replacement);
    public byte* luaL_findtable(void* state, int index, byte* fieldName, int sizeHint) => throw new NotSupportedException("luaL_findtable is not supported in Lua 5.5.");
    public void luaL_setfuncs(void* state, void* registrations, int upvalueCount) => NativeMethods.luaL_setfuncs((lua_State*)state, (luaL_Reg*)registrations, upvalueCount);
    public int luaL_getsubtable(void* state, int index, byte* fieldName) => NativeMethods.luaL_getsubtable((lua_State*)state, index, fieldName);
    public void luaL_traceback(void* state, void* sourceState, byte* message, int level) => NativeMethods.luaL_traceback((lua_State*)state, (lua_State*)sourceState, message, level);

    public void luaL_requiref(void* state, byte* moduleName, delegate* unmanaged[Cdecl]<void*, int> openFunction, int global)
        => NativeMethods.luaL_requiref((lua_State*)state, moduleName, (delegate* unmanaged[Cdecl]<lua_State*, int>)openFunction, global);

    public void luaL_buffinit(void* state, void* buffer) => NativeMethods.luaL_buffinit((lua_State*)state, (luaL_Buffer*)buffer);
    public byte* luaL_prepbuffer(void* buffer) => NativeMethods.luaL_prepbuffsize((luaL_Buffer*)buffer, 8192);
    public byte* luaL_prepbuffsize(void* buffer, nuint size) => NativeMethods.luaL_prepbuffsize((luaL_Buffer*)buffer, size);
    public byte* luaL_buffinitsize(void* state, void* buffer, nuint size) => NativeMethods.luaL_buffinitsize((lua_State*)state, (luaL_Buffer*)buffer, size);
    public void luaL_addlstring(void* buffer, byte* value, nuint length) => NativeMethods.luaL_addlstring((luaL_Buffer*)buffer, value, length);
    public void luaL_addstring(void* buffer, byte* value) => NativeMethods.luaL_addstring((luaL_Buffer*)buffer, value);
    public void luaL_addvalue(void* buffer) => NativeMethods.luaL_addvalue((luaL_Buffer*)buffer);
    public void luaL_pushresult(void* buffer) => NativeMethods.luaL_pushresult((luaL_Buffer*)buffer);
    public void luaL_pushresultsize(void* buffer, nuint size) => NativeMethods.luaL_pushresultsize((luaL_Buffer*)buffer, size);
}

#pragma warning restore IDE1006
