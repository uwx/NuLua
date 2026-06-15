#pragma warning disable IDE1006

namespace NuLua;

public unsafe interface ILuaOperations
{
    void* luaL_newstate();
    void* lua_newstate(delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> allocator, void* userData);
    void lua_close(void* state);
    void* lua_newthread(void* state);
    delegate* unmanaged[Cdecl]<void*, int> lua_atpanic(void* state, delegate* unmanaged[Cdecl]<void*, int> panicFunction);

    int lua_absindex(void* state, int index);
    int lua_gettop(void* state);
    void lua_settop(void* state, int index);
    void lua_pushvalue(void* state, int index);
    void lua_rotate(void* state, int index, int count);
    void lua_copy(void* state, int fromIndex, int toIndex);
    void lua_remove(void* state, int index);
    void lua_insert(void* state, int index);
    void lua_replace(void* state, int index);
    int lua_checkstack(void* state, int size);
    void lua_xmove(void* fromState, void* toState, int count);

    int lua_isnumber(void* state, int index);
    int lua_isstring(void* state, int index);
    int lua_iscfunction(void* state, int index);
    int lua_isinteger(void* state, int index);
    int lua_isuserdata(void* state, int index);
    int lua_type(void* state, int index);
    byte* lua_typename(void* state, int type);
    int lua_equal(void* state, int index1, int index2);
    int lua_rawequal(void* state, int index1, int index2);
    int lua_lessthan(void* state, int index1, int index2);
    int lua_compare(void* state, int index1, int index2, int operation);

    double lua_tonumber(void* state, int index);
    double lua_tonumberx(void* state, int index, int* isNumber);
    long lua_tointeger(void* state, int index);
    long lua_tointegerx(void* state, int index, int* isNumber);
    int lua_toboolean(void* state, int index);
    byte* lua_tolstring(void* state, int index, nuint* length);
    nuint lua_objlen(void* state, int index);
    nuint lua_rawlen(void* state, int index);
    delegate* unmanaged[Cdecl]<void*, int> lua_tocfunction(void* state, int index);
    void* lua_touserdata(void* state, int index);
    void* lua_tothread(void* state, int index);
    void* lua_topointer(void* state, int index);

    void lua_arith(void* state, int operation);
    void lua_len(void* state, int index);
    nuint lua_stringtonumber(void* state, byte* value);

    void lua_pushnil(void* state);
    void lua_pushnumber(void* state, double value);
    void lua_pushinteger(void* state, long value);
    byte* lua_pushlstring(void* state, byte* value, nuint length);
    byte* lua_pushstring(void* state, byte* value);
    byte* lua_pushvfstring(void* state, byte* format, byte* argp);
    byte* lua_pushfstring(void* state, byte* format);
    void lua_pushcclosure(void* state, delegate* unmanaged[Cdecl]<void*, int> function, int upvalueCount);
    void lua_pushboolean(void* state, int value);
    void lua_pushlightuserdata(void* state, void* pointer);
    int lua_pushthread(void* state);

    int lua_getglobal(void* state, byte* name);
    int lua_gettable(void* state, int index);
    int lua_getfield(void* state, int index, byte* key);
    int lua_geti(void* state, int index, long key);
    int lua_rawget(void* state, int index);
    int lua_rawgeti(void* state, int index, long key);
    int lua_rawgetp(void* state, int index, void* pointer);
    void lua_createtable(void* state, int arrayCount, int recordCount);
    void* lua_newuserdata(void* state, nuint size);
    void* lua_newuserdatauv(void* state, nuint size, int userValueCount);
    int lua_getmetatable(void* state, int objectIndex);
    void lua_getfenv(void* state, int index);
    int lua_getuservalue(void* state, int index);
    int lua_getiuservalue(void* state, int index, int userValueIndex);

    void lua_setglobal(void* state, byte* name);
    void lua_settable(void* state, int index);
    void lua_setfield(void* state, int index, byte* key);
    void lua_seti(void* state, int index, long key);
    void lua_rawset(void* state, int index);
    void lua_rawseti(void* state, int index, long key);
    void lua_rawsetp(void* state, int index, void* pointer);
    int lua_setmetatable(void* state, int objectIndex);
    int lua_setfenv(void* state, int index);
    void lua_setuservalue(void* state, int index);
    int lua_setiuservalue(void* state, int index, int userValueIndex);

    void lua_call(void* state, int argumentCount, int resultCount);
    void lua_callk(void* state, int argumentCount, int resultCount, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation);
    int lua_pcall(void* state, int argumentCount, int resultCount, int errorFunction);
    int lua_pcallk(void* state, int argumentCount, int resultCount, int errorFunction, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation);
    int lua_cpcall(void* state, delegate* unmanaged[Cdecl]<void*, int> function, void* userData);
    int lua_load(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint*, byte*> reader, void* data, byte* chunkName, byte* mode);
    int lua_dump(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint, void*, int> writer, void* data, int strip);
    int lua_yield(void* state, int resultCount);
    int lua_yieldk(void* state, int resultCount, nint context, delegate* unmanaged[Cdecl]<void*, int, nint, int> continuation);
    int lua_resume(void* state, void* fromState, int argumentCount, int* resultCount);
    int lua_status(void* state);
    int lua_isyieldable(void* state);
    int lua_error(void* state);

    int lua_gc(void* state, int what, int data);
    int lua_next(void* state, int index);
    void lua_concat(void* state, int count);
    delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> lua_getallocf(void* state, void** userData);
    void lua_setallocf(void* state, delegate* unmanaged[Cdecl]<void*, void*, nuint, nuint, void*> allocator, void* userData);

    int lua_getstack(void* state, int level, void* debug);
    int lua_getinfo(void* state, byte* what, void* debug);
    byte* lua_getlocal(void* state, void* debug, int index);
    byte* lua_setlocal(void* state, void* debug, int index);
    byte* lua_getupvalue(void* state, int functionIndex, int index);
    byte* lua_setupvalue(void* state, int functionIndex, int index);
    void* lua_upvalueid(void* state, int functionIndex, int index);
    void lua_upvaluejoin(void* state, int functionIndex1, int index1, int functionIndex2, int index2);
    int lua_sethook(void* state, delegate* unmanaged[Cdecl]<void*, void*, void> hook, int mask, int count);
    delegate* unmanaged[Cdecl]<void*, void*, void> lua_gethook(void* state);
    int lua_gethookmask(void* state);
    int lua_gethookcount(void* state);

    int luaopen_base(void* state);
    int luaopen_coroutine(void* state);
    int luaopen_table(void* state);
    int luaopen_io(void* state);
    int luaopen_os(void* state);
    int luaopen_string(void* state);
    int luaopen_utf8(void* state);
    int luaopen_bit32(void* state);
    int luaopen_math(void* state);
    int luaopen_debug(void* state);
    int luaopen_package(void* state);

    void luaL_openlibs(void* state);
    void luaL_openlib(void* state, byte* libraryName, void* registrations, int upvalueCount);
    void luaL_register(void* state, byte* libraryName, void* registrations);
    void luaL_checkversion_(void* state, double version, nuint size);
    int luaL_getmetafield(void* state, int objectIndex, byte* eventName);
    int luaL_callmeta(void* state, int objectIndex, byte* eventName);
    byte* luaL_tolstring(void* state, int index, nuint* length);
    int luaL_typerror(void* state, int argument, byte* typeName);
    int luaL_argerror(void* state, int argument, byte* extraMessage);
    byte* luaL_checklstring(void* state, int argument, nuint* length);
    byte* luaL_optlstring(void* state, int argument, byte* defaultValue, nuint* length);
    double luaL_checknumber(void* state, int argument);
    double luaL_optnumber(void* state, int argument, double defaultValue);
    long luaL_checkinteger(void* state, int argument);
    long luaL_optinteger(void* state, int argument, long defaultValue);
    void luaL_checkstack(void* state, int size, byte* message);
    void luaL_checktype(void* state, int argument, int type);
    void luaL_checkany(void* state, int argument);
    int luaL_newmetatable(void* state, byte* typeName);
    void luaL_setmetatable(void* state, byte* typeName);
    void* luaL_testudata(void* state, int userDataIndex, byte* typeName);
    void* luaL_checkudata(void* state, int userDataIndex, byte* typeName);
    void luaL_where(void* state, int level);
    int luaL_error(void* state, byte* format);
    int luaL_checkoption(void* state, int argument, byte* defaultValue, byte** list);
    int luaL_fileresult(void* state, int status, byte* fileName);
    int luaL_execresult(void* state, int status);
    int luaL_ref(void* state, int tableIndex);
    void luaL_unref(void* state, int tableIndex, int reference);
    int luaL_loadfile(void* state, byte* fileName);
    int luaL_loadfilex(void* state, byte* fileName, byte* mode);
    int luaL_loadbuffer(void* state, byte* buffer, nuint size, byte* name);
    int luaL_loadbufferx(void* state, byte* buffer, nuint size, byte* name, byte* mode);
    int luaL_loadstring(void* state, byte* source);
    long luaL_len(void* state, int index);
    byte* luaL_gsub(void* state, byte* source, byte* pattern, byte* replacement);
    byte* luaL_findtable(void* state, int index, byte* fieldName, int sizeHint);
    void luaL_setfuncs(void* state, void* registrations, int upvalueCount);
    int luaL_getsubtable(void* state, int index, byte* fieldName);
    void luaL_traceback(void* state, void* sourceState, byte* message, int level);
    void luaL_requiref(void* state, byte* moduleName, delegate* unmanaged[Cdecl]<void*, int> openFunction, int global);
    void luaL_buffinit(void* state, void* buffer);
    byte* luaL_prepbuffer(void* buffer);
    byte* luaL_prepbuffsize(void* buffer, nuint size);
    byte* luaL_buffinitsize(void* state, void* buffer, nuint size);
    void luaL_addlstring(void* buffer, byte* value, nuint length);
    void luaL_addstring(void* buffer, byte* value);
    void luaL_addvalue(void* buffer);
    void luaL_pushresult(void* buffer);
    void luaL_pushresultsize(void* buffer, nuint size);
}

#pragma warning restore IDE1006
