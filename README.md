# desktop-media-player (Step 0 tech spike)

WPF + libmpv playback spike for Windows **win-x64** on **.NET 10** (`net10.0-windows`).

**Status: IMPLEMENTED source tree / WINDOWS VALIDATION PENDING — not PASS.**

## Layers

| Project | Role |
|---------|------|
| `DesktopMediaPlayer.Contracts` | Frozen `IPlaybackEngine`, `IPlaybackObserver`, `IRenderHost`, `PlaybackState` |
| `DesktopMediaPlayer.Platform` | `SpikeLogger`, `HardwareAccelPolicy` (D3D11VA → copy → software) |
| `DesktopMediaPlayer.Playback` | `PlaybackFacade` — volume clamp, observer fan-out, path validation |
| `DesktopMediaPlayer.Engine.Mpv` | libmpv P/Invoke, engine thread, `MpvPlaybackEngine`, `MpvRenderHost` |
| `DesktopMediaPlayer.Shell` | WPF UI, `VideoHwndHost`, PerMonitorV2 DPI; **no** mpv DllImport |
| `DesktopMediaPlayer.Playback.Tests` | Facade unit tests (no native DLL) |

UI talks **only** to `PlaybackFacade` (`IPlaybackEngine`). Shell never P/Invokes libmpv.

## Spike vs MVP

**Spike (this repo):** open local file, play/pause/stop/seek/volume, HWND embed, DPI physical-pixel resize, hwdec policy logging, missing-DLL graceful error.

**Not in spike:** playlist, resume, themes, subtitle UI, installer, auto-update, packaging of binaries in git.

## LGPL / dynamic-link note

libmpv and FFmpeg are expected as **dynamically linked** LGPL shared libraries (`libmpv-2.dll` + deps under `native/win-x64/`). Do not statically link FFmpeg into the application. Agents and CI must **not** download DLL binaries from the internet; a build bot places approved LGPL artifacts.

## Native DLL contract

See [`native/win-x64/README.md`](native/win-x64/README.md).

Load order:

1. Environment variable **`DMP_LIBMPV_PATH`** (directory or full path to `libmpv-2.dll`)
2. `native/win-x64/libmpv-2.dll` relative to layout
3. `AppContext.BaseDirectory`

Shell copies `native/win-x64/*` to output **only when files exist**.

## Build

```powershell
dotnet restore DesktopMediaPlayer.sln
dotnet build DesktopMediaPlayer.sln -c Release -p:Platform=x64
dotnet test -c Release -p:Platform=x64
```

`EnableWindowsTargeting=true` allows restore/compile on non-Windows SDKs; running Shell and loading native DLLs requires Windows.

## Repro / evidence

See [`docs/spike/REPRO.md`](docs/spike/REPRO.md). Do not claim **PASS** until Windows Release\|x64 evidence is attached.
