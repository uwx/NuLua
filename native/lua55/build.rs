use std::path::{Path, PathBuf};

const FLAVOR: &str = "lua55";
const CSHARP_NAMESPACE: &str = "NuLua.Interop.Lua55";
const CSHARP_CLASS: &str = "NativeMethods";
const CSHARP_PROJECT: &str = "NuLua.Interop.Lua55";

fn main() {
    let manifest_dir = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    let workspace_root = manifest_dir.parent().unwrap().parent().unwrap().to_path_buf();
    let lua_src = workspace_root.join("submodules").join(FLAVOR);
    let out_dir = PathBuf::from(std::env::var("OUT_DIR").unwrap());

    compile_lua(&lua_src);
    let bindings_rs = out_dir.join("bindings.rs");
    generate_rust_bindings(&lua_src, &bindings_rs);
    let cs_out = workspace_root
        .join("src")
        .join(CSHARP_PROJECT)
        .join("Generated")
        .join(format!("{CSHARP_CLASS}.g.cs"));
    generate_csharp_bindings(&bindings_rs, &cs_out);

    println!("cargo:rerun-if-changed=build.rs");
    println!("cargo:rerun-if-changed={}", lua_src.display());
}

fn compile_lua(lua_src: &Path) {
    let mut build = cc::Build::new();
    build.include(lua_src);
    apply_platform_defines(&mut build);

    for entry in std::fs::read_dir(lua_src).expect("read lua source dir") {
        let path = entry.unwrap().path();
        let ext = path.extension().and_then(|s| s.to_str());
        if ext != Some("c") {
            continue;
        }
        let name = path.file_name().unwrap().to_str().unwrap();
        if matches!(name, "lua.c" | "luac.c" | "ltests.c" | "onelua.c") {
            continue;
        }
        build.file(&path);
    }

    build.warnings(false);
    build.compile(FLAVOR);
}

fn apply_platform_defines(build: &mut cc::Build) {
    let target_os = std::env::var("CARGO_CFG_TARGET_OS").unwrap_or_default();
    match target_os.as_str() {
        "macos" => {
            build.define("LUA_USE_MACOSX", None);
        }
        "linux" | "android" => {
            build.define("LUA_USE_LINUX", None);
        }
        "windows" => {
            build.define("LUA_BUILD_AS_DLL", None);
        }
        _ => {
            build.define("LUA_USE_POSIX", None);
        }
    }
}

fn generate_rust_bindings(lua_src: &Path, out: &Path) {
    let bindings = bindgen::Builder::default()
        .header(lua_src.join("lua.h").to_string_lossy())
        .header(lua_src.join("lualib.h").to_string_lossy())
        .header(lua_src.join("lauxlib.h").to_string_lossy())
        .clang_arg(format!("-I{}", lua_src.display()))
        .allowlist_function("lua_.*")
        .allowlist_function("luaL_.*")
        .allowlist_function("luaopen_.*")
        .allowlist_type("lua_.*")
        .allowlist_type("luaL_.*")
        .allowlist_var("LUA_.*")
        .allowlist_var("LUAL_.*")
        .layout_tests(false)
        .generate()
        .expect("bindgen failed");
    bindings.write_to_file(out).expect("write bindings.rs");
}

fn generate_csharp_bindings(bindings_rs: &Path, out_cs: &Path) {
    std::fs::create_dir_all(out_cs.parent().unwrap()).expect("mkdir Generated/");
    csbindgen::Builder::default()
        .input_bindgen_file(bindings_rs)
        .csharp_class_accessibility("public")
        .csharp_file_header("using NuLua.Polyfills;")
        .csharp_dll_name(FLAVOR)
        .csharp_namespace(CSHARP_NAMESPACE)
        .csharp_class_name(CSHARP_CLASS)
        .generate_csharp_file(out_cs)
        .expect("csbindgen failed");
}
