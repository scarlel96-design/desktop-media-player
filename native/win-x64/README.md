# native/win-x64 — libmpv contract

**Build bot / release engineer must place LGPL `libmpv-2.dll` and its FFmpeg/runtime dependencies here.**

## Required

| File | Notes |
|------|--------|
| `libmpv-2.dll` | LGPL libmpv client API (x64). Primary load name used by `DllImportResolver`. |
| FFmpeg shared libs | Whatever your libmpv build links dynamically (e.g. `avcodec-*.dll`, `avformat-*.dll`, `avutil-*.dll`, `swresample-*.dll`, `swscale-*.dll`). |
| Other runtime deps | e.g. `libgcc`, `libstdc++`, `libwinpthread`, shaderc/SPIRV if required by that build. |

## Rules

1. **Do not download DLLs from the internet in CI or by agents.** Obtain from a controlled LGPL-compatible build pipeline or approved internal cache.
2. Dynamic-link only (LGPL). Do not statically link FFmpeg into the app binary.
3. Shell csproj copies `native/win-x64/*` to output **only if files exist** (`Condition="Exists(...)"`).
4. Override search path with env `DMP_LIBMPV_PATH` (directory containing `libmpv-2.dll`, or full path to the DLL).
5. This folder ships with `README.md` only in the empty repo — binaries are never committed.

## Load order (Engine.Mpv)

1. `DMP_LIBMPV_PATH` if set
2. `native/win-x64/libmpv-2.dll` relative to app base / repo layout
3. `AppContext.BaseDirectory/libmpv-2.dll` (after copy-to-output)
