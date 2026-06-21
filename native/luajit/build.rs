use std::path::{Path, PathBuf};

const FLAVOR: &str = "luajit";
const CSHARP_NAMESPACE: &str = "NuLua.Interop.LuaJit";
const CSHARP_CLASS: &str = "NativeMethods";
const CSHARP_PROJECT: &str = "NuLua.Interop.LuaJit";

fn main() {
    let manifest_dir = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    let workspace_root = manifest_dir
        .parent()
        .unwrap()
        .parent()
        .unwrap()
        .to_path_buf();
    let header_dir = workspace_root.join("submodules").join("luajit").join("src");
    let shim = workspace_root.join("native").join("shim");
    let out_dir = PathBuf::from(std::env::var("OUT_DIR").unwrap());

    let bindings_rs = out_dir.join(format!("{FLAVOR}.rs"));
    generate_rust_bindings(&header_dir, &shim, &bindings_rs);

    let cs_out = workspace_root
        .join("src")
        .join(CSHARP_PROJECT)
        .join("Generated")
        .join(format!("{CSHARP_CLASS}.g.cs"));
    let ffi_rs = out_dir.join(format!("{FLAVOR}_ffi.rs"));
    generate_csharp_bindings(&bindings_rs, &ffi_rs, &cs_out);

    println!("cargo:rerun-if-changed=build.rs");
    println!("cargo:rerun-if-changed={}", header_dir.display());
    println!("cargo:rerun-if-changed={}", shim.display());
}

fn generate_rust_bindings(header_dir: &Path, shim: &Path, out: &Path) {
    let bindings = bindgen::Builder::default()
        .header(header_dir.join("lua.h").to_string_lossy())
        .header(header_dir.join("lualib.h").to_string_lossy())
        .header(header_dir.join("lauxlib.h").to_string_lossy())
        .header(header_dir.join("luajit.h").to_string_lossy())
        .header(shim.join("nulua_shim.h").to_string_lossy())
        .clang_arg(format!("-I{}", header_dir.display()))
        .allowlist_function("lua_.*")
        .allowlist_function("luaL_.*")
        .allowlist_function("luaopen_.*")
        .allowlist_function("luaJIT_.*")
        .allowlist_function("nulua_.*")
        .allowlist_type("lua_.*")
        .allowlist_type("luaL_.*")
        .allowlist_var("LUA_.*")
        .allowlist_var("LUAL_.*")
        .allowlist_var("LUAJIT_.*")
        .layout_tests(false)
        .generate()
        .expect("bindgen failed");
    bindings.write_to_file(out).expect("write bindings.rs");
}

fn generate_csharp_bindings(bindings_rs: &Path, ffi_rs: &Path, out_cs: &Path) {
    std::fs::create_dir_all(out_cs.parent().unwrap()).expect("mkdir Generated/");
    csbindgen::Builder::default()
        .input_bindgen_file(bindings_rs)
        .rust_file_header(format!("use crate::{FLAVOR}::*;"))
        .csharp_class_accessibility("public")
        .csharp_file_header("using NuLua.Polyfills;")
        .csharp_dll_name(FLAVOR)
        .csharp_namespace(CSHARP_NAMESPACE)
        .csharp_class_name(CSHARP_CLASS)
        .csharp_use_function_pointer(false)
        .csharp_entry_point_prefix("")
        .csharp_generate_const_filter(|name| name.starts_with("LUA_"))
        .generate_to_file(ffi_rs.to_str().unwrap(), out_cs.to_str().unwrap())
        .expect("csbindgen failed");
}
