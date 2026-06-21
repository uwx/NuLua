#include "luau_shim.h"

#include <cstdlib>

#include "lua.h"
#include "lualib.h"

#include "../../shim/nulua_shim.c"

extern "C" void luau_free(void* p) {
    std::free(p);
}

extern "C" int luau_error(void* L, const char* msg) {
    auto* state = static_cast<lua_State*>(L);
    lua_pushstring(state, msg);
    lua_error(state);
    // Unreachable: lua_error longjmps. Keep a return for type-correctness.
    return 0;
}
