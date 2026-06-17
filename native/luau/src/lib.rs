// This crate exists only as a host for build.rs (bindgen + csbindgen).
// The Luau shared library is produced by `make -C native luau`,
// and C# calls Luau's C API directly via P/Invoke.
