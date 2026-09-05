# Step 0 spike — reproduction & evidence

**Status: IMPLEMENTED / WINDOWS VALIDATION PENDING**

Gate note (PS-014): OpenGL-only native (`BLD-20260905-NATIVE-001`) can reach at most **조건부 PASS**. Formal PASS needs D3D11 vo (libplacebo d3d11) rebuild + re-run.

This document describes how to validate the WPF + libmpv tech spike on a Windows win-x64 machine. Linux agents can restore with `EnableWindowsTargeting=true` but cannot run the Shell UI or load native DLLs.

## Prerequisites

- .NET 10 SDK (Windows)
- Visual Studio 2022+ or `dotnet` CLI with Windows Desktop workload
- Local H.264 1080p sample file (not shipped)
- Place LGPL `libmpv-2.dll` + FFmpeg/runtime deps under `native/win-x64/` (see that README). No internet DLL download.

## Build (Release | x64)

```powershell
cd desktop-media-player
dotnet restore DesktopMediaPlayer.sln
dotnet build DesktopMediaPlayer.sln -c Release -p:Platform=x64
dotnet test tests/DesktopMediaPlayer.Playback.Tests -c Release -p:Platform=x64
```

Run Shell:

```powershell
dotnet run --project src/DesktopMediaPlayer.Shell -c Release -p:Platform=x64 --no-build
# or launch bin\x64\Release\net10.0-windows\DesktopMediaPlayer.Shell.exe
```

Optional: `$env:DMP_LIBMPV_PATH = "C:\path\to\folder-or-libmpv-2.dll"`

## Runtime validation matrix

| Check | Expectation |
|-------|-------------|
| DPI 150%+ | Video surface resizes using **physical pixels** (`ActualWidth * DpiScaleX`, etc.) |
| Open local H.264 1080p | Opens without crash; first frame paints into HWND host |
| Play / Pause / Stop / Seek / Volume | Via WPF only → `PlaybackFacade` → engine |
| Missing libmpv | Error text / `OnError` (`recoverable=false`); **no crash** |
| Focus | Clicking video does not trap keyboard focus (`WM_MOUSEACTIVATE` → `MA_NOACTIVATE`) |

## Log keys (spike.log)

UTF-8 log at `%LocalAppData%\desktop-media-player\spike.log` and Console.

| Event | Meaning |
|-------|---------|
| `open` | Path open requested |
| `first_frame` | First VIDEO_RECONFIG or PLAYBACK_RESTART after Open |
| `hwdec_active` | Hardware decode path active (`hwdec-current`) |
| `hwdec_fallback` | Stepped D3D11VA → D3D11VA-copy → software |
| `render_path` | `vo` / `gpu-context` / `hwdec-current` / `frame-drop` / `decoder-drop` / `vo-drop` + Soft `*-delta`/`*-rate` (after_init, first_frame, state transition, ~2s while Playing). **KI-014:** cumulative drop ≠ stutter; use Δrate. |
| `state` | PlaybackState transition |
| `error` | Engine/UI error with code/message |

Timestamps are included on every line.

## Evidence checklist (Windows operator)

- [ ] Release\|x64 build succeeded
- [ ] Unit tests (Playback.Tests) passed without native DLL
- [ ] Screenshot or note: video visible at 150%+ DPI
- [ ] Excerpt of `spike.log` showing `open`, `first_frame`, `state`, `render_path` (`vo`/`gpu-context`/`hwdec-current`), and hwdec line
- [ ] Confirm no PASS claim until Windows evidence attached
- [ ] Confirm DLLs were not fetched from the public internet by the agent

## Out of scope (spike vs MVP)

Playlist, resume, themes, subtitle UI, installer, auto-update — **not** in Step 0.
