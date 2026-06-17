// This crate exists only as a host for build.rs (bindgen + csbindgen).
// The LuaJIT shared library is produced by `make -C native luajit`
// (which delegates to submodules/luajit/src/Makefile), and C# calls
// LuaJIT's C API directly via P/Invoke.
