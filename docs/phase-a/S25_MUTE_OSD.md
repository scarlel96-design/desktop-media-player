# Phase A — S25 Soft Mute Opacity OSD

## Delivered (Shell-only)
- Mute toggle (button / `M` → `Mute_Click`) shows Opacity OSD `Mute On` / `Mute Off`
- Reuses existing Soft OSD path (`SubtitleOsdText` + fade timer) like `ShowVolumeOsd`
- Existing Facade `SetMute` / `GetMute` only · no new API

## Out of scope
- Blur/P1 · Facade enlarge · S1–S24 re-run · Shell→mpv · native · rate · KI PASS
