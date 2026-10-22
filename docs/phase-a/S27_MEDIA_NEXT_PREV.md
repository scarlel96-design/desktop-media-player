# Phase A — S27 Soft MediaNextTrack / MediaPreviousTrack

## Delivered (Shell-only)
- `MediaNextTrack` → existing `Next_Click` → `IPlaylistService.PlayNext` Soft
- `MediaPreviousTrack` → existing `Prev_Click` → `IPlaylistService.PlayPrevious` Soft
- Ends stop Soft (no wrap) · `Window_KeyDown` Soft path
- No new Facade/Contracts/Engine API

## Out of scope
- wrap · Remove-by-index · Blur/P1 · S1–S26 re-run · Shell→mpv · native · KI PASS
- USER WINDOWS TEST REQUIRED (media keys) tracked separately from Soft Closure
