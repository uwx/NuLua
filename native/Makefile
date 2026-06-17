# Builds per-flavor Lua shared libraries that are shipped directly to
# src/NuLua.Runtime.{Display}/runtimes/{rid}/native/ and loaded by .NET P/Invoke.
#
# Required inputs from the caller:
#   RID           target RID, e.g. linux-arm64, osx-x64, win-x64
#   TARGET_OS     macos | linux | windows | posix
#   CC, AR, RANLIB
# Optional:
#   CFLAGS_EXTRA  appended to CFLAGS (e.g. "-arch arm64" on macOS multi-arch builds)
#   LDFLAGS_EXTRA appended to LDFLAGS
#
# Output: $(BUILD)/$(FLAVOR)/$(RID)/$(DYLIB_NAME)
#   linux  -> lib{flavor}.so
#   macos  -> lib{flavor}.dylib
#   windows-> {flavor}.dll

SUBMOD := ../submodules
BUILD  := build

FLAVORS := lua51 lua52 lua53 lua54 lua55

SKIP_lua51 := lua.c luac.c print.c
SKIP_lua52 := lua.c luac.c
SKIP_lua53 := lua.c luac.c
SKIP_lua54 := lua.c luac.c ltests.c onelua.c
SKIP_lua55 := lua.c luac.c ltests.c onelua.c

PLATFORM_DEFINE := $(if $(filter macos,$(TARGET_OS)),-DLUA_USE_MACOSX,\
                  $(if $(filter linux,$(TARGET_OS)),-DLUA_USE_LINUX,\
                  $(if $(filter windows,$(TARGET_OS)),-DLUA_BUILD_AS_DLL,-DLUA_USE_POSIX)))

CFLAGS  := -O2 -fPIC -w $(PLATFORM_DEFINE) $(CFLAGS_EXTRA)
LDFLAGS := $(LDFLAGS_EXTRA)

# Shared-library link flags + filename per OS.
ifeq ($(TARGET_OS),macos)
  SHARED_FLAGS  := -dynamiclib
  PLATFORM_LIBS := -lm
  dylib_name     = lib$1.dylib
else ifeq ($(TARGET_OS),linux)
  SHARED_FLAGS  := -shared
  PLATFORM_LIBS := -lm -ldl
  dylib_name     = lib$1.so
else ifeq ($(TARGET_OS),windows)
  SHARED_FLAGS  := -shared
  PLATFORM_LIBS :=
  dylib_name     = $1.dll
else
  SHARED_FLAGS  := -shared
  PLATFORM_LIBS := -lm
  dylib_name     = lib$1.so
endif

flavor_srcs = $(filter-out $(addprefix $(SUBMOD)/$1/,$(SKIP_$1)),$(wildcard $(SUBMOD)/$1/*.c))
flavor_objs = $(patsubst $(SUBMOD)/$1/%.c,$(BUILD)/$1/$(RID)/%.o,$(call flavor_srcs,$1))

define flavor_rules
$(BUILD)/$1/$(RID)/%.o: $(SUBMOD)/$1/%.c
	@mkdir -p $$(@D)
	$$(CC) $$(CFLAGS) -I$(SUBMOD)/$1 -c $$< -o $$@

$(BUILD)/$1/$(RID)/$(call dylib_name,$1): $(call flavor_objs,$1)
	$$(CC) $(SHARED_FLAGS) $$(LDFLAGS) -o $$@ $$^ $(PLATFORM_LIBS)

$1: $(BUILD)/$1/$(RID)/$(call dylib_name,$1)
.PHONY: $1
endef

$(foreach f,$(FLAVORS),$(eval $(call flavor_rules,$f)))

all: $(FLAVORS)
.PHONY: all

clean:
	rm -rf $(BUILD)
.PHONY: clean
