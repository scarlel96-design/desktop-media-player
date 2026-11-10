# desktop-media-player

WPF + libmpv desktop media player for Windows **win-x64** on **.NET 10** (`net10.0-windows`).

**상태: Phase A Soft Canonical `4ee4b6c` (BLD-20261105-PHASEA-GPUPOLICY03-002). Windows NOT TESTED. KI-031은 Medium, 원인 CONFIRMED(정밀 시크 디코드 비용), Open. 2026-11-10 10:18 KST USAGE-HALT-20261105-1743 RESOLVED, 작업 재개. 자세한 내용은 KNOWN_ISSUES와 docs/continuity/CONTINUITY-STATUS-NOW.md 참조.**

**상태 추가: KI-032 High · Open · Windows NOT TESTED (FFmpeg CVE-2026-125239). KNOWN_ISSUES 참조.**

이전 상태 표기(SUPERSEDED): Phase A Soft canonical e099c2e (BLD-20261021-PHASEA-S132-001). 2026-10-22 Windows 고위험 확인에서 KI-030·종료 PASS, 당시 KI-031 Low(원인 미검증).

## Layers

| Project | Role |
|---------|------|
| `DesktopMediaPlayer.Contracts` | Frozen `IPlaybackEngine`, `IPlaybackObserver`, `IRenderHost`, `PlaybackState` |
| `DesktopMediaPlayer.Platform` | `SpikeLogger`, `HardwareAccelPolicy` (D3D11VA → copy → software) |
| `DesktopMediaPlayer.Playback` | `PlaybackFacade` — volume clamp, observer fan-out, path validation; `MinimalPlaylistService` — playlist service |
| `DesktopMediaPlayer.Engine.Mpv` | libmpv P/Invoke, engine thread, `MpvPlaybackEngine`, `MpvRenderHost` |
| `DesktopMediaPlayer.Shell` | WPF UI, `VideoHwndHost`, PerMonitorV2 DPI; **no** mpv DllImport |
| `DesktopMediaPlayer.Playback.Tests` | Facade unit tests (no native DLL) |

UI talks **only** to `PlaybackFacade` (`IPlaybackEngine`). Shell never P/Invokes libmpv.

## Implemented scope

**Implemented (this repo):** open local file, play/pause/stop/seek/volume, HWND embed, DPI physical-pixel resize, hwdec policy logging, missing-DLL graceful error, playlist, resume (playback position), themes, subtitles (external subtitle load), screenshot, OSD, copy to clipboard (path/name/folder/title), always-on-top.

**Not implemented:** Blur/Acrylic, P1 features, installer, auto-update, packaging of binaries in git.

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

Requirements: .NET Desktop Runtime 10.0.13 이상 (x64) on Windows.
Build SDK: .NET SDK 10.0.402 (pinned by global.json, rollForward latestPatch).

```powershell
dotnet restore DesktopMediaPlayer.sln
dotnet build DesktopMediaPlayer.sln -c Release -p:Platform=x64
dotnet test -c Release -p:Platform=x64
```

`EnableWindowsTargeting=true` allows restore/compile on non-Windows SDKs; running Shell and loading native DLLs requires Windows.

## Repro / evidence

See [`docs/spike/REPRO.md`](docs/spike/REPRO.md). Do not claim **PASS** until Windows Release\|x64 evidence is attached.
