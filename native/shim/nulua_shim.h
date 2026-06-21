#pragma once

#ifndef NULUA_SHIM_API
#define NULUA_SHIM_API extern
#endif

#ifdef __cplusplus
extern "C" {
#endif

NULUA_SHIM_API int nulua_pgettable(void* L, int index);
NULUA_SHIM_API int nulua_pgetfield(void* L, int index, const char* name);
NULUA_SHIM_API int nulua_psettable(void* L, int index);
NULUA_SHIM_API int nulua_psetfield(void* L, int index, const char* name);
NULUA_SHIM_API int nulua_pgetglobal(void* L, const char* name);
NULUA_SHIM_API int nulua_psetglobal(void* L, const char* name);
NULUA_SHIM_API int nulua_plen(void* L, int index);
NULUA_SHIM_API int nulua_pconcat(void* L, int count);
NULUA_SHIM_API int nulua_parith(void* L, int op, int operand_count);
NULUA_SHIM_API int nulua_pcompare(void* L, int op);
NULUA_SHIM_API int nulua_pnext(void* L, int index, int* has_next);

#ifdef __cplusplus
}
#endif
