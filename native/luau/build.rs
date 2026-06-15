use std::path::{Path, PathBuf};

const FLAVOR: &str = "luau";
const CSHARP_NAMESPACE: &str = "NuLua.Interop.Luau";
const CSHARP_CLASS: &str = "NativeMethods";
const CSHARP_PROJECT: &str = "NuLua.Interop.Luau";

// NOTE: Luau is a C++ codebase split across VM/, Compiler/, Ast/, etc. Its
// public C API lives in VM/include/. For this first milestone only the
// binding surface is generated; full cdylib compilation (cc-rs over the C++
// sources) will be wired in afterwards.
fn main() {
    let manifest_dir = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    let workspace_root = manifest_dir
        .parent()
        .unwrap()
        .parent()
        .unwrap()
        .to_path_buf();
    let luau_root = workspace_root.join("submodules").join("luau");
    let vm_include = luau_root.join("VM").join("include");

    let bindings_rs = "src/luau.rs";
    generate_rust_bindings(&vm_include, bindings_rs);

    let cs_out = workspace_root
        .join("src")
        .join(CSHARP_PROJECT)
        .join("Generated")
        .join(format!("{CSHARP_CLASS}.g.cs"));
    generate_csharp_bindings(bindings_rs, &cs_out);

    println!("cargo:rerun-if-changed=build.rs");
    println!("cargo:rerun-if-changed={}", vm_include.display());
}

fn generate_rust_bindings<T: AsRef<Path>>(vm_include: &Path, out: T) {
    let bindings = bindgen::Builder::default()
        .header(vm_include.join("lua.h").to_string_lossy())
        .header(vm_include.join("lualib.h").to_string_lossy())
        .clang_arg(format!("-I{}", vm_include.display()))
        // Luau headers use C++ extern "C" guards but compile cleanly as C.
        .clang_arg("-x")
        .clang_arg("c")
        .allowlist_function("lua_.*")
        .allowlist_function("luaL_.*")
        .allowlist_function("luaopen_.*")
        .allowlist_function("luau_.*")
        .allowlist_type("lua_.*")
        .allowlist_type("luaL_.*")
        .allowlist_var("LUA_.*")
        .allowlist_var("LUAL_.*")
        .layout_tests(false)
        .generate()
        .expect("bindgen failed");
    bindings.write_to_file(out).expect("write bindings.rs");
}

fn generate_csharp_bindings<T: AsRef<Path>>(bindings_rs: T, out_cs: &Path) {
    std::fs::create_dir_all(out_cs.parent().unwrap()).expect("mkdir Generated/");
    csbindgen::Builder::default()
        .input_bindgen_file(bindings_rs)
        .rust_file_header("use crate::luau::*;")
        .csharp_class_accessibility("public")
        .csharp_file_header("using NuLua.Polyfills;")
        .csharp_dll_name(FLAVOR)
        .csharp_namespace(CSHARP_NAMESPACE)
        .csharp_class_name(CSHARP_CLASS)
        .csharp_entry_point_prefix("csbindgen_")
        .generate_to_file("src/luau_ffi.rs", out_cs.to_str().unwrap())
        .expect("csbindgen failed");
}
