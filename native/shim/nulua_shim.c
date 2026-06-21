#include "nulua_shim.h"

#include "lua.h"

static int nulua_gettable(lua_State* L) {
    lua_pushvalue(L, 2);
    lua_gettable(L, 1);
    return 1;
}

static int nulua_settable(lua_State* L) {
    lua_pushvalue(L, 2);
    lua_pushvalue(L, 3);
    lua_settable(L, 1);
    return 0;
}

static int nulua_getglobal(lua_State* L) {
    lua_getglobal(L, lua_tostring(L, 1));
    return 1;
}

static int nulua_setglobal(lua_State* L) {
    lua_pushvalue(L, 2);
    lua_setglobal(L, lua_tostring(L, 1));
    return 0;
}

static int nulua_len(lua_State* L) {
#if defined(NULUA_LUAU) || !defined(LUA_VERSION_NUM) || LUA_VERSION_NUM < 502
    lua_pushinteger(L, (lua_Integer)lua_objlen(L, 1));
#else
    lua_len(L, 1);
#endif
    return 1;
}

static int nulua_concat(lua_State* L) {
    lua_concat(L, lua_gettop(L));
    return 1;
}

#if !defined(NULUA_LUAU) && defined(LUA_VERSION_NUM) && LUA_VERSION_NUM >= 502
static int nulua_arith(lua_State* L) {
    int op = (int)lua_tointeger(L, -1);
    lua_pop(L, 1);
    lua_arith(L, op);
    return 1;
}
#endif

static int nulua_compare(lua_State* L) {
    int op = (int)lua_tointeger(L, 3);
    int result;
#if defined(NULUA_LUAU) || !defined(LUA_VERSION_NUM) || LUA_VERSION_NUM < 502
    if (op == 0)
        result = lua_equal(L, 1, 2);
    else if (op == 1)
        result = lua_lessthan(L, 1, 2);
    else
        result = !lua_lessthan(L, 2, 1);
#else
    result = lua_compare(L, 1, 2, op);
#endif
    lua_pushboolean(L, result);
    return 1;
}

static int nulua_next(lua_State* L) {
    lua_pushvalue(L, 2);
    return lua_next(L, 1) ? 2 : 0;
}

static int nulua_absindex(lua_State* L, int index) {
    return index > 0 || index <= LUA_REGISTRYINDEX ? index : lua_gettop(L) + index + 1;
}

int nulua_pgettable(void* state, int index) {
    lua_State* L = (lua_State*)state;
    int table = nulua_absindex(L, index);
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_gettable, "nulua_gettable");
#else
    lua_pushcclosure(L, nulua_gettable, 0);
#endif
    lua_pushvalue(L, table);
    lua_pushvalue(L, -3);
    lua_remove(L, -4);
    return lua_pcall(L, 2, 1, 0);
}

int nulua_pgetfield(void* state, int index, const char* name) {
    lua_State* L = (lua_State*)state;
    int table = nulua_absindex(L, index);
    lua_pushstring(L, name);
    return nulua_pgettable(L, table);
}

int nulua_psettable(void* state, int index) {
    lua_State* L = (lua_State*)state;
    int table = nulua_absindex(L, index);
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_settable, "nulua_settable");
#else
    lua_pushcclosure(L, nulua_settable, 0);
#endif
    lua_pushvalue(L, table);
    lua_pushvalue(L, -4);
    lua_pushvalue(L, -4);
    lua_remove(L, -5);
    lua_remove(L, -5);
    return lua_pcall(L, 3, 0, 0);
}

int nulua_psetfield(void* state, int index, const char* name) {
    lua_State* L = (lua_State*)state;
    int table = nulua_absindex(L, index);
    lua_pushstring(L, name);
    lua_insert(L, -2);
    return nulua_psettable(L, table);
}

int nulua_pgetglobal(void* state, const char* name) {
    lua_State* L = (lua_State*)state;
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_getglobal, "nulua_getglobal");
#else
    lua_pushcclosure(L, nulua_getglobal, 0);
#endif
    lua_pushstring(L, name);
    return lua_pcall(L, 1, 1, 0);
}

int nulua_psetglobal(void* state, const char* name) {
    lua_State* L = (lua_State*)state;
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_setglobal, "nulua_setglobal");
#else
    lua_pushcclosure(L, nulua_setglobal, 0);
#endif
    lua_pushstring(L, name);
    lua_pushvalue(L, -3);
    lua_remove(L, -4);
    return lua_pcall(L, 2, 0, 0);
}

int nulua_plen(void* state, int index) {
    lua_State* L = (lua_State*)state;
    int value = nulua_absindex(L, index);
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_len, "nulua_len");
#else
    lua_pushcclosure(L, nulua_len, 0);
#endif
    lua_pushvalue(L, value);
    return lua_pcall(L, 1, 1, 0);
}

int nulua_pconcat(void* state, int count) {
    lua_State* L = (lua_State*)state;
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_concat, "nulua_concat");
#else
    lua_pushcclosure(L, nulua_concat, 0);
#endif
    lua_insert(L, -1 - count);
    return lua_pcall(L, count, 1, 0);
}

int nulua_parith(void* state, int op, int operand_count) {
#if !defined(NULUA_LUAU) && defined(LUA_VERSION_NUM) && LUA_VERSION_NUM >= 502
    lua_State* L = (lua_State*)state;
    lua_pushcclosure(L, nulua_arith, 0);
    lua_insert(L, -1 - operand_count);
    lua_pushinteger(L, op);
    return lua_pcall(L, operand_count + 1, 1, 0);
#else
    (void)state;
    (void)op;
    (void)operand_count;
    return -1;
#endif
}

int nulua_pcompare(void* state, int op) {
    lua_State* L = (lua_State*)state;
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_compare, "nulua_compare");
#else
    lua_pushcclosure(L, nulua_compare, 0);
#endif
    lua_insert(L, -3);
    lua_pushinteger(L, op);
    return lua_pcall(L, 3, 1, 0);
}

int nulua_pnext(void* state, int index, int* has_next) {
    lua_State* L = (lua_State*)state;
    int table = nulua_absindex(L, index);
#ifdef NULUA_LUAU
    lua_pushcfunction(L, nulua_next, "nulua_next");
#else
    lua_pushcclosure(L, nulua_next, 0);
#endif
    lua_pushvalue(L, table);
    lua_pushvalue(L, -3);
    lua_remove(L, -4);
    int base = lua_gettop(L) - 3;
    int status = lua_pcall(L, 2, LUA_MULTRET, 0);
    *has_next = status == 0 && lua_gettop(L) - base == 2;
    return status;
}
