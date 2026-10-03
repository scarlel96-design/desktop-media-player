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
| `seek_latency` | S67 Soft (RV-01), log-only: ms from Seek request to PLAYBACK_RESTART (`info`; `warn` when `ms>=1000`). |
| `end_file` | S68 Soft (RV-02), log-only: END_FILE `reason`/`error` and engine state (`warn` when reason=error). |
| `mpv_event` | S69 Soft, log-only: mpv event ids not handled by `HandleEvent` (`id`/`name`/engine state). |
| `error` | Engine/UI error with code/message |

Timestamps are included on every line.

S67 Soft `seek_latency` Windows checks (PENDING, not Known Issues):
- One `seek_latency` line after a real Seek.
- PLAYBACK_RESTART arrives for a Seek near EOF with `keep-open=yes`.
- Rapid consecutive Seeks log one line, measured from the earliest request (design limit).
- Seek while paused.

S68 Soft `end_file` Windows checks (PENDING, not Known Issues):
- Normal EOF logs `reason=eof` once.
- END_FILE timing with `keep-open=yes`.
- Stop logs `reason=stop`.
- Corrupt file logs `reason=error` with an `error` code.
- Opening a new file: whether the previous file logs stop/redirect.

S69 Soft `mpv_event` Windows checks (PENDING, not Known Issues):
- e. Which ids actually arrive on Open.
- f. Whether id 17 (VIDEO_RECONFIG) arrives (id 13 is handled by the existing case and never appears in this log; infer 13 only from `first_frame` being logged before PLAYBACK_RESTART).
- g. Whether `seek(20)` arrives on Seek.
- h. No log flood.

S70 Soft SeekSlider bar-click Windows checks (PENDING, not Known Issues):
- a. Clicking the bar (not the thumb) seeks to the clicked point.
- b. Dragging the thumb still seeks once on release.
- c. The thumb does not jump back mid-drag (KI-024 behaviour kept).
- d. Releasing the mouse outside the slider still commits via LostMouseCapture.
- e. Hover time popup and keyboard seek are unaffected.

S71 Soft SeekSlider track-drag Windows checks (PENDING, not Known Issues):
- a. Press on the track (not the thumb) and keep the button down: the thumb jumps to the point, then follows the mouse.
- b. Releasing seeks once to the release position.
- c. No jump or flicker of the thumb on the first move after the press.
- d. A plain click (no move) still seeks to the clicked point; dragging the thumb itself is unchanged.
- e. Releasing outside the slider still commits via LostMouseCapture; hover popup and keyboard seek are unaffected.

S72 Soft `eof_reached` and extra `render_path` items, log-only (Windows checks PENDING, not Known Issues):
- a. Play a video to the end without pausing: `eof_reached pos=<sec> state=<state>` appears once (no line at open, where the initial value is no).
- b. State right after `eof_reached` (expected Paused before `end_file`/Ended), and whether the last frame stays on screen.
- c. Press Play after the end: record what the button does (replay, nothing, or error) and the log lines around it.
- d. During playback, `render_path` now also carries container-fps, estimated-vf-fps, display-fps, video-params/w, video-params/h, video-params/pixelformat, video-sync, avsync and mistimed-frame-count; record the values.
- e. Items mpv cannot read are omitted, never an error; vo-delayed-frame-count stays as `vo-drop`.

S73 Soft chrome auto-hide hover Windows checks (PENDING, not Known Issues):
- a. While playing, keep the pointer over the bottom bar: it does not auto-hide.
- b. Click the seek bar or a transport button, keep the pointer on the bar: it stays visible.
- c. Move the pointer off the bar: it hides after the usual delay.
- d. Pause/stop, flyouts, and mouse-move reveal behave as before.

S74 Soft play/pause toggle Windows checks (PENDING, not Known Issues):
- a. The bar shows one play/pause button: pause icon while playing, play icon when paused, stopped, or before opening.
- b. Clicking it while playing pauses; clicking while paused or stopped plays; Space behaves as before.
- c. The old separate Pause button is hidden; Stop, Prev, Next, and auto-hide are unaffected.
- d. The tooltip matches the icon (Pause while playing, Play otherwise).

S75 Soft Play after end-of-file Windows checks (PENDING, not Known Issues):
- a. After a video reaches its end, pressing Play restarts it from the beginning.
- b. Normal Play and Pause during playback are unchanged.
- c. Pressing Play at a position not near the end does not seek.

S76 Soft Space key with a focused button Windows checks (PENDING, not Known Issues):
- a. Click a transport button (or the play/pause button) so it has focus, then press Space: playback toggles exactly once (no double toggle).
- b. Enter and Tab on a focused button behave as before.
- c. Space with no button focused still toggles play/pause once.
- d. Other keys (arrows, F, M, and so on) are unchanged.

S77 Soft fullscreen bottom band Windows checks (PENDING, not Known Issues):
- a. Fullscreen playback, let the bar auto-hide: no dark band remains at the bottom of the screen.
- b. Move the pointer: the bar shows again (the video resizes once); keep the pointer still: no show/hide flicker loop.
- c. Windowed mode: auto-hide leaves the layout unchanged (no resize on hide/show).
- d. Paused, open flyouts/playlist, or hovering the bar: still never hides, as before.

S78 Soft Lumen F-1 token dictionaries (no Windows check needed; nothing consumes the tokens yet):
- a. Adds Themes/Lumen.Tokens.Shared.xaml, Lumen.Colors.Dark.xaml, Lumen.Colors.Light.xaml; App.xaml merges Shared and Dark only (Light is unmerged until F-2).
- b. No visual or behaviour change is expected; the app must still start as before.
- c. Only if the app fails to start: record the XamlParseException text from the first exception.

S79 Soft Lumen F-2 color theme binding (PENDING, not Known Issues):
- a. Dark theme looks identical to before (window, bottom bar and status text colors).
- b. Cycle the theme button Dark, Light, System: window and bottom bar both switch fully; no stale dark bar in Light.
- c. Only if the app fails to start or the theme does not apply: record the first exception text.

S80 Soft Lumen F-3 styles moved to Themes/Controls.xaml (PENDING, not Known Issues):
- a. Transport buttons, caption text and the side panel (playlist/info) look identical to before in the Dark theme.
- b. The app starts normally; all three styles still apply (button size 40, side panel border on the left).
- c. Only if the app fails to start: record the first exception text (a missing style key is a StaticResource error).

S81 Soft Lumen P2-1 chrome surface and video column background (PENDING, not Known Issues):
- a. The bottom control bar has an opaque dark surface with a thin top line; its height looks unchanged (100) and the flyouts still sit above it.
- b. The area around the video (letterbox) is pure black in Dark and Light themes; the video itself is not covered by the bar.
- c. Fullscreen auto-hide and the bar still behave as before; record any visible gap or overlap.

S82 Soft Lumen P2-2 control group dividers (PENDING, not Known Issues):
- a. Thin vertical dividers appear after Open, after Next (Prev/Play/Pause/Stop/Next group), after Frame step, and after Audio; they are not clickable.
- b. Button order, button size (40) and the bar height (100) look unchanged; the gap before the volume group looks like before.
- c. Dividers are visible but subtle in both Dark and Light themes.

S83 Soft Lumen P2-4 window minimum width (PENDING, not Known Issues):
- a. Drag the window narrower: it stops at about 912 wide (was 640), and the bottom control bar buttons are not cut off at that width.
- b. The start size (960x640), minimum height (400) and saved window size/position restore look unchanged.

S84 Soft Lumen P2-3 time labels (PENDING, not Known Issues):
- a. The elapsed and total time labels keep a steady width (about 56) and the elapsed time is right-aligned; the seek bar does not jitter as the digits change.
- b. Time text is monospace at size 12 with a softer secondary color, readable in Dark and Light themes.

S85 Soft Lumen T3-3 seek hover time tooltip (PENDING, not Known Issues):
- a. Hover over the seek bar: the time tooltip shows a solid raised surface with a thin subtle border, square corners, primary text at size 12, and no fade animation.
- b. The tooltip is readable in Dark and Light themes and still follows the pointer position as before.

S86 Soft Lumen T3-1 buffer bar colors (PENDING, not Known Issues):
- a. While a file is playing, the buffered-ahead bar uses the theme buffer color on the theme rail color instead of fixed blue/white.
- b. The buffer bar is readable in Dark and Light themes and still sits under the seek slider.

S87 Soft Lumen T3-2 seek slider template (PENDING, not Known Issues; high-risk seek):
- a. During playback, click the seek bar at two different positions: playback jumps to each clicked position and the blue fill follows up to the thumb.
- b. Hover the seek bar: a round thumb appears only while the pointer is over the bar or while dragging. Drag the thumb and release: playback resumes at the released position.
- c. Seek log lines (seek_latency) still appear for click and drag, and the hover time tooltip still follows the pointer.

## Evidence checklist (Windows operator)

- [ ] Release\|x64 build succeeded
- [ ] Unit tests (Playback.Tests) passed without native DLL
- [ ] Screenshot or note: video visible at 150%+ DPI
- [ ] Excerpt of `spike.log` showing `open`, `first_frame`, `state`, `render_path` (`vo`/`gpu-context`/`hwdec-current`), and hwdec line
- [ ] Confirm no PASS claim until Windows evidence attached
- [ ] Confirm DLLs were not fetched from the public internet by the agent

## Out of scope (spike vs MVP)

Playlist, resume, themes, subtitle UI, installer, auto-update — **not** in Step 0.
