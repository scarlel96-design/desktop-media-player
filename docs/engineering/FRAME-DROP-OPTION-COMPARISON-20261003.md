# Frame-drop option comparison (document only, no code change)

Author: 수석 엔지니어 봇 / 2026-10-03 KST. Status: reference for a future high-risk Atomic. Windows NOT TESTED; every effect below is a hypothesis unless it cites the mpv 0.40.0 manual or source.

## Observed (S72 Windows log, spike-S72-user-20261003-0345.log)
- Counter: `frame-drop-count` (mpv command.c -> vo_get_drop_count). Rises about 10-20 per second at 60 fps; decoder-drop=0, vo-delayed=0, avsync about 0.
- video-sync=audio, hwdec d3d11va, vo gpu-next, gpu-context d3d11, source 2160x3840 60 fps, display 59.95 Hz.
- Mechanism (video/out/vo.c v0.40.0, render_frame): in non-display-sync mode a frame whose end time is already past at render time is dropped and counted (framedrop=vo is the default). So the VO is delivering frames late. Cause unconfirmed: render cost, present blocking, or core feeding late.

## Where options would be set
Only in `MpvPlaybackEngine.cs` near line 552-555 (`mpv_set_option_string` before init): currently hwdec, vo, gpu-context, keep-open. Any option below is +1 to +3 lines in Engine.Mpv, no Facade/Contracts change, but it changes video output behavior = HIGH RISK, needs Windows confirmation.

## Options (all from mpv 0.40.0 manual unless stated)
| # | Option | Intended effect | Risk | Notes |
|---|--------|-----------------|------|-------|
| 1 | `profile=fast` | Cheaper scalers (bilinear default for scale), less shader work | Lower scaling quality | Manual: bilinear is the default scale with the fast profile. Biggest expected win if render cost is the cause (hypothesis H1). |
| 2 | `scale=bilinear` / `dscale=bilinear` (targeted instead of whole profile) | Same as 1 but narrower | Softer image when downscaling 4K | Keeps other defaults; 2 lines. |
| 3 | `vo=gpu,gpu-next` (swap fallback order) | Use the older gpu renderer, usually lighter than libplacebo | Different look/features (HDR/tone mapping), changes KI-022 tested path | Current vo is gpu-next first. |
| 4 | `swapchain-depth=N` (default 3) | Allows more frames in flight, can prevent missed vsyncs | More latency, slightly more VRAM | Manual says higher depth can improve pipelining. Low risk, cheap to try. |
| 5 | `video-sync=display-resample` | Frames synced to display; exposes mistimed-frame-count and vo-delayed-frame-count; drops by ratio 60 vs 59.95 only | Audio is resampled (slight speed change); manual warns it is less robust and breaks on resize/fullscreen toggle (skips frames); A/V sync risk | HIGH RISK for A/V sync and seek behavior. Not recommended first. |
| 6 | `framedrop=no` | Counter stays 0; frames are shown late instead of dropped | Hides the symptom, may cause A/V desync instead of drops | Diagnostic only; not a fix. |
| 7 | `hwdec=d3d11va-copy` | Copy path, avoids some interop cost or a driver issue | Extra copy cost on 4K, usually slower | Only if native d3d11va interop is the bottleneck (H3); unlikely at decoder-drop=0. |
| 8 | `d3d11-flip=no` | Bitblt present instead of flip model | Tearing/latency, DWM copy cost | Manual notes flip-model may hurt on older drivers; helps only if the WPF child-HWND present is the issue (H2). |
| 9 | `interpolation` / `tscale` | Not applicable | - | Needs display-sync; skip. |

## Recommended order if a fix is ever LOCKed (one option per Atomic, single Windows check)
1. #4 swapchain-depth (lowest risk, cheap) or #2 dscale=bilinear.
2. #1 profile=fast.
3. #3 vo order.
Never combine with #5. Do not use #6 as a fix.

## Needed evidence before choosing
- Whether the user actually sees stutter (counter is not stutter).
- A standalone mpv.exe run of the same file with the same hwdec/vo (E-a) to split app embedding from file/GPU load.
- Window size dependency (E-b).
These are held because the user said routine Windows tests are over. Bundle them into one USER WINDOWS TEST REQUIRED when the frame-drop fix is prioritized.
